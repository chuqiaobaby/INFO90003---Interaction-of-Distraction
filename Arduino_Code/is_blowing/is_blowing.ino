// GPIO pins for sound sensors
const int sound1 = 32;
const int sound2 = 33;

void setup() {
  Serial.begin(115200);

  pinMode(sound1, INPUT);
  pinMode(sound2, INPUT);
}

void loop() {

  bool s1 = digitalRead(sound1);
  bool s2 = digitalRead(sound2);

  // NOTE: many modules are ACTIVE LOW (sound = LOW)
  // So we invert them
  bool soundDetected = (!s1) || (!s2);

  if (soundDetected) {
    Serial.println(1);
  } else {
    Serial.println(0);
  }

  delay(200); //
}