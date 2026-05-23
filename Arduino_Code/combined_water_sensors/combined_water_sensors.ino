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
// #define DEBUG_SOUND

// ====== WATER LEVEL SENSORS ======
const int sensorLow  = 14;
const int sensorMid  = 27;
const int sensorHigh = 26;

// ====== CAPACITIVE TOUCH SENSOR ======
const int touchPin = 4;
const int ledPin = 12;
const int TOUCH_THRESHOLD = 500;  // Calibrate as needed

// ====== GROUNDING SENSORS (LDRs) ======
const int ldrLeft  = 34;
const int ldrRight = 35;
const int DARK_THRESHOLD = 1500;  // Calibrate as needed

// ====== SOUND SENSORS (BLOWING) ======
// NOTE: Requires sound sensor modules with ANALOG output (AO pin)
// Common modules: KY-037, LM393-based sensors
// Connect AO pins to ESP32 analog-capable GPIOs (32, 33, 34, 35, etc.)
const int sound1 = 32;  // Analog pin for sound sensor 1
const int sound2 = 33;  // Analog pin for sound sensor 2
const int SOUND_THRESHOLD = 2500;  // Analog threshold (0-4095 on ESP32)
                                    // Higher = less sensitive, adjust based on room noise
const int BLOW_DURATION_MS = 150;   // Minimum duration to count as intentional blow
const bool REQUIRE_BOTH_SENSORS = false;  // Set true to require both sensors (more reliable)
const unsigned long BLOW_COOLDOWN_MS = 1500; // ms before another blow can be confirmed

// Blow detection timing
unsigned long blowStartTime = 0;
bool blowDetected = false;
unsigned long blowConfirmedTime = 0;

void setup() {
  Serial.begin(115200);
  
  // Water level sensors
  pinMode(sensorLow, INPUT);
  pinMode(sensorMid, INPUT);
  pinMode(sensorHigh, INPUT);
  
  // Touch LED indicator
  pinMode(ledPin, OUTPUT);
  
  // Sound sensors
  pinMode(sound1, INPUT);
  pinMode(sound2, INPUT);
  
  // LDR sensors are analog inputs (no pinMode needed)
}

void loop() {
  // ====== READ WATER LEVEL (0-3) ======
  // Sensors use inverted logic (NPN pulls LOW when active)
  bool low  = !digitalRead(sensorLow);
  bool mid  = !digitalRead(sensorMid);
  bool high = !digitalRead(sensorHigh);
  
  int waterLevel;
  if (high)      waterLevel = 3;
  else if (mid)  waterLevel = 2;
  else if (low)  waterLevel = 1;
  else           waterLevel = 0;
  
  // ====== READ TOUCH SENSOR (0 or 1) ======
  int touchValue = touchRead(touchPin);
  int isTouching = (touchValue < TOUCH_THRESHOLD) ? 1 : 0;
  
  // Update LED indicator
  digitalWrite(ledPin, isTouching ? HIGH : LOW);
  
  // ====== READ GROUNDING SENSORS (0 or 1) ======
  int leftValue  = analogRead(ldrLeft);
  int rightValue = analogRead(ldrRight);
  bool isGrounding = (leftValue < DARK_THRESHOLD) && (rightValue < DARK_THRESHOLD) ? 1 : 0;
  
  // ====== READ SOUND SENSORS (0 or 1) ======
  // Read analog values (0-4095 on ESP32)
  int sound1Value = analogRead(sound1);
  int sound2Value = analogRead(sound2);
  
  // Check if sound exceeds threshold
  bool s1Active = sound1Value > SOUND_THRESHOLD;
  bool s2Active = sound2Value > SOUND_THRESHOLD;
  
  // Determine if sound is currently detected
  bool soundNow;
  if (REQUIRE_BOTH_SENSORS) {
    soundNow = s1Active && s2Active;  // Both sensors must detect
  } else {
    soundNow = s1Active || s2Active;  // Either sensor can detect
  }
  
  // Duration filtering: require sustained sound
  unsigned long currentTime = millis();

  // One-shot blow: confirm once, then require cooldown + silence before re-arming
  if (blowDetected) {
    // Already fired — hold signal for 2 loop cycles (~40ms) then clear
    if (currentTime - blowConfirmedTime >= 40) {
      blowDetected = false;
      blowStartTime = 0;
    }
  } else if (currentTime - blowConfirmedTime < BLOW_COOLDOWN_MS) {
    // In cooldown — ignore sensor, drain the start timer
    blowStartTime = 0;
  } else if (soundNow) {
    if (blowStartTime == 0) {
      blowStartTime = currentTime;
    } else if ((currentTime - blowStartTime) >= BLOW_DURATION_MS) {
      blowDetected = true;
      blowConfirmedTime = currentTime;
    }
  } else {
    blowStartTime = 0;
  }

  int isBlowing = blowDetected ? 1 : 0;
  
  #ifdef DEBUG_SOUND
  // Debug output for calibration (comment out for production)
  Serial.print("Sound1: ");
  Serial.print(sound1Value);
  Serial.print(" | Sound2: ");
  Serial.print(sound2Value);
  Serial.print(" | Threshold: ");
  Serial.print(SOUND_THRESHOLD);
  Serial.print(" | Blowing: ");
  Serial.println(isBlowing);
  #endif
  
  // ====== SEND DATA TO UNITY ======
  // Format: [WaterLevel],[IsTouching],[IsGrounding],[IsBlowing]
  Serial.print(waterLevel);
  Serial.print(",");
  Serial.print(isTouching);
  Serial.print(",");
  Serial.print(isGrounding);
  Serial.print(",");
  Serial.println(isBlowing);
  
  delay(20);  // 50Hz sampling — fast enough for all sensors
}
