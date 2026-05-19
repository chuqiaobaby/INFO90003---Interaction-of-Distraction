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

#define LED_PIN         12
#define NUM_LEDS        150
#define BRIGHTNESS      100

#define TOUCH_PIN       4
#define TOUCH_THRESHOLD 400

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
const int DARK_THRESHOLD = 160;  // Calibrate as needed

// ====== SOUND SENSOR (BLOWING) ======
// Using DIGITAL output (DO pin)
// Sensor outputs:
// HIGH (1) = idle / no blow
// LOW  (0) = sound detected
const int soundPin = 33;

const int BLOW_DURATION_MS = 150;

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
  
  // LDR sensors are analog inputs (no pinMode needed)
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
  
// ====== READ SOUND SENSOR (0 or 1) ======
// Sensor logic:
// LOW  (0) = idle / no blow
// HIGH (1) = blowing detected
bool soundDetected = digitalRead(soundPin) == HIGH;

// Duration filtering: require sustained sound
unsigned long currentTime = millis();

if (soundDetected) {
  if (blowStartTime == 0) {
    blowStartTime = currentTime;
  } 
  else if ((currentTime - blowStartTime) >= BLOW_DURATION_MS) {
    blowDetected = true;
  }
} else {
  blowStartTime = 0;
  blowDetected = false;
}

// Final Unity output
int isBlowing = soundDetected ? 1 : 0;
  
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

/*#ifdef DEBUG_SOUND
Serial.print("Sound: ");
Serial.print(digitalRead(soundPin));
Serial.print(" | Blowing: ");
Serial.println(isBlowing);
#endif*/

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
