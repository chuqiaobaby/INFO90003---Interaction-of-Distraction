/*
 * Combined Water Interaction Sensors for Unity
 * Output Format: [WaterLevel],[IsTouching],[IsGrounding],[IsBlowing]
 * 
 * CALIBRATION FOR NOISY ENVIRONMENTS:
 * 1. Upload this sketch and open Serial Monitor (115200 baud)
 * 2. Uncomment the DEBUG_SOUND line below to see raw sensor values
 * 3. Note the ambient noise levels in your room
 * 4. Set SOUND_THRESHOLD to ~500-1000 above ambient noise
 * 5. Test blowing - adjust threshold and duration as needed
 * 6. Re-comment DEBUG_SOUND and re-upload for production
 */

// Uncomment to see sound sensor values for calibration
//#define DEBUG_SOUND

#include <FastLED.h>

#define LED_PIN         13
#define NUM_LEDS        150
#define BRIGHTNESS      100

#define TOUCH_PIN       4
#define TOUCH_THRESHOLD 350

CRGB leds[NUM_LEDS];

unsigned long lastLedUpdate = 0;
unsigned long lastSerialUpdate = 0;
const unsigned long LED_UPDATE_INTERVAL = 20;    // Update LEDs every 20ms
const unsigned long SERIAL_UPDATE_INTERVAL = 200; // Send data every 200ms

// ====== WATER LEVEL SENSORS ======
const int sensorLow  = 14;
const int sensorMid  = 32;
const int sensorHigh = 15;

const int threshold = 4050;

int isTriggered(int pin){
  int value = analogRead(pin);
  return (value < threshold);  // true if water detected
}

// ====== CAPACITIVE TOUCH SENSOR ======

// Rainbow cycling
uint8_t hue = 0;

// Pulse brightness
uint8_t pulseBrightness = 0;
int pulseDirection = 1;

// ====== GROUNDING SENSORS (LDRs) ======
const int ldrLeft  = 34;
const int ldrRight = 39;
const int DARK_THRESHOLD = 160;  // Calibrate as needed to esnure LDRs are covered

// ================= MOTOR =================
const int motorDIR = 12; 
const int motorPWM = 27;

// Motor State
const unsigned int MOTOR_RUNTIME = 10000;
unsigned long motorStartTime = 0;
bool motorActive = false;

// ====== SOUND SENSOR (BLOWING) ======
// Using DIGITAL output (DO pin)
// Sensor outputs:
// HIGH (1) = idle / no blow
// LOW  (0) = sound detected
const int soundPin = 33;

const int BLOW_DURATION_MS = 120;
const int SOUND_WINDOW_MS = 200;
unsigned long soundWindowStart = 0;
int soundHighCount = 0;

bool stableBlowing = false;
bool isBlowing = false;

bool previousTouchState = false;
bool previousTouchReading = false;
bool stableTouchState = false;

bool previousBlowState = false;

unsigned long groundingStartTime = 0;
bool groundingActive = false;

const unsigned long GROUNDING_ANIMATION_TIME = 5000; // 5 seconds

bool groundingLatched = false;

unsigned long touchChangeTime = 0;
const unsigned long TOUCH_DEBOUNCE_MS = 200;
// Blow detection timing
unsigned long blowStartTime = 0;
bool blowDetected = false;

void setup() {
  Serial.begin(115200);
  
  //Touch sensor 
  FastLED.addLeds<WS2812B, LED_PIN, GRB>(leds, NUM_LEDS);
    FastLED.setBrightness(BRIGHTNESS);

    FastLED.clear();
    FastLED.show();
  
  // Sound sensors
  pinMode(soundPin, INPUT);

  // Motor
  pinMode(motorDIR, OUTPUT);
  pinMode(motorPWM, OUTPUT);

  digitalWrite(motorDIR, LOW);
  analogWrite(motorPWM, 0);
}

void loop() {
  // ====== DECLARE CURRENT TIME FIRST ======
  unsigned long currentTime = millis();  // MOVED TO TOP!
  
  // ====== READ WATER LEVEL (0-3) ======
  // Sensors use inverted logic (NPN pulls LOW when active)
  bool low  = isTriggered(sensorLow);
  bool mid  = isTriggered(sensorMid);
  bool high = isTriggered(sensorHigh);
  
  int waterLevel;
  if (high)      waterLevel = 3;
  else if (mid)  waterLevel = 2;
  else if (low)  waterLevel = 1;
  else           waterLevel = 0;
  
  // ====== READ TOUCH SENSOR (0 or 1) ======
  int touchValue = touchRead(TOUCH_PIN);
  bool rawTouch = (touchValue < TOUCH_THRESHOLD);

  // Detect change
  if (rawTouch != previousTouchReading) {
      touchChangeTime = currentTime;
  }

  // If stable for long enough, accept new state
  if ((currentTime - touchChangeTime) > TOUCH_DEBOUNCE_MS) {
      stableTouchState = rawTouch;
  }

  previousTouchReading = rawTouch;

  int isTouching = stableTouchState ? 1 : 0;

  // ====== READ GROUNDING SENSORS (0 or 1) ======
  int leftValue  = analogRead(ldrLeft);
  int rightValue = analogRead(ldrRight);
  bool isGrounding = (leftValue < DARK_THRESHOLD) && (rightValue < DARK_THRESHOLD) ? 1 : 0;
  
  // ====== SOUND SENSOR ======
  int rawSound = digitalRead(soundPin);

  // start window
  if (soundWindowStart == 0) {
    soundWindowStart = currentTime;
  }

  // count highs in window
  if (rawSound == HIGH) {
    soundHighCount++;
  }

  // end window → evaluate
  if (currentTime - soundWindowStart >= SOUND_WINDOW_MS) {

    if (soundHighCount > (SOUND_WINDOW_MS / 20) * 0.6) {
      stableBlowing = true;
    } else {
      stableBlowing = false;
    }

    // reset window
    soundWindowStart = currentTime;
    soundHighCount = 0;
  }

  isBlowing = stableBlowing ? 1 : 0;

  //Motor Control
  if(isBlowing == 1 && !motorActive){
    digitalWrite(motorDIR, LOW);
    analogWrite(motorPWM, 200);

    motorStartTime = currentTime;

    motorActive = true;
  } else {
    if(currentTime - motorStartTime >= MOTOR_RUNTIME){
      digitalWrite(motorDIR, LOW);
      analogWrite(motorPWM, 0);

      motorActive = false;
      motorStartTime = 0;
    }
  }

bool blowStarted = (isBlowing == 1 && !previousBlowState);

if (blowStarted) {

    // ONLY triggers once per blowing event
    FastLED.clear(true);
    FastLED.show();

    // Optional: reset states so system restarts cleanly
    groundingLatched = false;
    groundingStartTime = 0;

} else {

if (isGrounding && !groundingLatched) {

    // Start timing (only once)
    if (groundingStartTime == 0) {
        groundingStartTime = currentTime;
    }

    float progress = (float)(currentTime - groundingStartTime) / 5000.0;

    // If completed → latch ON permanently
    if (progress >= 1.0) {
        groundingLatched = true;
        progress = 1.0;
    }

    int litLEDs = progress * NUM_LEDS;

    for (int i = 0; i < NUM_LEDS; i++) {
        if (i < litLEDs) {
            leds[i] = CHSV(0, 0, 255); // white
        } else {
            leds[i] = CRGB::Black;
        }
    }

    FastLED.show();

} else {

    // ❗ RESET if grounding stops BEFORE latch
    if (!groundingLatched) {
        groundingStartTime = 0;

        FastLED.clear(true);
    }

    // After latch → stay ON permanently
    if (groundingLatched) {
        for (int i = 0; i < NUM_LEDS; i++) {
            leds[i] = CHSV(0, 0, 255);
        }
        FastLED.show();
    }

    // Normal touch only if not latched
    if (!groundingLatched) {

        if (isTouching) {

            if (!previousTouchState) {
                hue += 32;
            }

            for (int i = 0; i < NUM_LEDS; i++) {
                leds[i] = CHSV(hue, 255, 255);
            }

            FastLED.show();

        }
    }

    previousTouchState = isTouching;
  }
}

previousBlowState = isBlowing;
  
  if (currentTime - lastSerialUpdate >= SERIAL_UPDATE_INTERVAL) {
    lastSerialUpdate = currentTime;

#ifdef DEBUG_SOUND
    Serial.print("Sound: ");
    Serial.print(digitalRead(soundPin));
    Serial.print(" | Blowing: ");
    Serial.println(isBlowing);
#endif

    /*Serial.print("Left LDR:");
    Serial.print(leftValue);
    Serial.print("Right LDR:");
    Serial.println(rightValue);*/

    /*Serial.print("Touch Value:");
    Serial.println(touchValue);*/

    // ====== SEND DATA TO UNITY ======
    // Format: [WaterLevel],[IsTouching],[IsGrounding],[IsBlowing]
    Serial.print(waterLevel);
    Serial.print(",");
    Serial.print(isTouching);
    Serial.print(",");
    Serial.print(isGrounding);
    Serial.print(",");
    Serial.println(isBlowing);
  }
  
  delay(25);  // Sampling rate - adjust as needed
}
