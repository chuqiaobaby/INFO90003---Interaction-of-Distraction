const int ldrLeft  = 34;
const int ldrRight = 35;

// Darkness threshold - can be changed
const int DARK_THRESHOLD = 1500;

void setup() {
  Serial.begin(115200) //Baud rate;
}

void loop() {

  int leftValue  = analogRead(ldrLeft);
  int rightValue = analogRead(ldrRight);

  // Check if both sensors are covered
  bool bothCovered = (leftValue < DARK_THRESHOLD) && (rightValue < DARK_THRESHOLD);

  // Send continuously
  if (bothCovered) {
    Serial.println(1);
  } else {
    Serial.println(0);
  }

  delay(200);