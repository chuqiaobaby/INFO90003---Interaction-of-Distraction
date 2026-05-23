// Pin definitions (choose any safe GPIOs)
const int sensorLow  = 14;
const int sensorMid  = 27;
const int sensorHigh = 26;

int level = -1;        // track last sent value

void setup() {
  Serial.begin(115200);

  pinMode(sensorLow, INPUT);
  pinMode(sensorMid, INPUT);
  pinMode(sensorHigh, INPUT);
}

void loop() {

  // Read sensors (invert because NPN pulls LOW when active)
  bool low  = !digitalRead(sensorLow);
  bool mid  = !digitalRead(sensorMid);
  bool high = !digitalRead(sensorHigh);

  int newLevel;

  if (high)      newLevel = 3;
  else if (mid)  newLevel = 2;
  else if (low)  newLevel = 1;
  else           newLevel = 0;

  // Only send when it changes (prevents spam in Unity)
  if (newLevel != level) {
    level = newLevel;
    Serial.println(level);
  }

  delay(200); // small stability delay
}