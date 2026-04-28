int touchPin = 4; //A5 pin on ESP32
int ledPin = 12; //12 pin on ESP32

void setup() {
  pinMode(ledPin, OUTPUT);
  Serial.begin(115200) //Baud rate - make sure to set the same baud rate in Arduino or else values will be undefined.;
}

void loop() {
  int touchValue = touchRead(touchPin);

  if (touchValue < 500) { //capacitance threshold (can be calibrated)
    digitalWrite(ledPin, HIGH);
    Serial.println(touchValue);
    Serial.println("1");
  } else {
    digitalWrite(ledPin, LOW);
    Serial.println(touchValue);
    Serial.println("0");
  }

  delay(200);
}