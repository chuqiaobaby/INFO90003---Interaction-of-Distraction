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
#define DEBUG_SOUND

#include <FastLED.h>

#define LED_PIN         13
#define NUM_LEDS        150
#define BRIGHTNESS      100

#define TOUCH_PIN       4
#define TOUCH_THRESHOLD 800

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
  int isTouching = (touchValue < TOUCH_THRESHOLD) ? 1 : 0;

    // ====== READ GROUNDING SENSORS (0 or 1) ======
  int leftValue  = analogRead(ldrLeft);
  int rightValue = analogRead(ldrRight);
  bool isGrounding = (leftValue < DARK_THRESHOLD) && (rightValue < DARK_THRESHOLD) ? 1 : 0;
  
// ====== SOUND SENSOR ======

unsigned long currentTime = millis();

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
  
  if (currentTime - lastLedUpdate >= LED_UPDATE_INTERVAL) {
  lastLedUpdate = currentTime;

  if(isTouching){
     // Create rainbow
        fill_rainbow(leds, NUM_LEDS, hue, 5);

        // Apply pulsing brightness
        FastLED.setBrightness(pulseBrightness);

        FastLED.show();

        // Cycle rainbow colours
        hue++;

        // Update pulse brightness
        pulseBrightness += pulseDirection * 2;

        // Reverse pulse direction at limits
        if (pulseBrightness >= BRIGHTNESS || pulseBrightness <= 10) {
            pulseDirection *= -1;
        }
  } else {
            // LEDs OFF
        FastLED.clear();
        FastLED.show();

        // Reset pulse when inactive
        pulseBrightness = 10;
        pulseDirection = 1;
    }
  }
  
  if (currentTime - lastSerialUpdate >= SERIAL_UPDATE_INTERVAL) {
    lastSerialUpdate = currentTime;

#ifdef DEBUG_SOUND
Serial.print("Sound: ");
Serial.print(digitalRead(soundPin));
Serial.print(" | Blowing: ");
Serial.println(isBlowing);
#endif

  /*int high = analogRead(sensorHigh);
  Serial.print("Water High:");
  Serial.print(high);
  int mid = analogRead(sensorMid);
  Serial.print(" Water Mid:");
  Serial.print(mid);
  int low = analogRead(sensorLow);
  Serial.print(" Water Low:");
  Serial.println(low);*/

  /*Serial.print("Left LDR:");
  Serial.print(leftValue);
  Serial.print(" Right LDR:");
  Serial.println(rightValue);*/

  
  // ====== SEND DATA TO UNITY ======
  // Format: [WaterLevel],[IsTouching],[IsGrounding],[IsBlowing]
  Serial.print(waterLevel);
  Serial.print(",");
  Serial.print(isTouching);
  Serial.print(",");
  Serial.print(isGrounding);
  Serial.print(",");
  Serial.println(isBlowing);
  
  delay(1);  // Sampling rate - adjust as needed
}
}
