#define TOP_SENSOR 15
#define MID_SENSOR 32
#define BOT_SENSOR 14

const int THRESHOLD = 4050;  // adjust based on your readings

void setup() {
    Serial.begin(115200);
}

int isTriggered(int pin) {
    int value = analogRead(pin);
    return (value < THRESHOLD);  // true if water detected
}

void loop() {

    bool top = isTriggered(TOP_SENSOR);
    bool mid = isTriggered(MID_SENSOR);
    bool bot = isTriggered(BOT_SENSOR);

    int level = 0;

    if (top) {
        level = 3;
    }
    else if (mid) {
        level = 2;
    }
    else if (bot) {
        level = 1;
    }
    else {
        level = 0;
    }

    Serial.print("Top: ");
    Serial.print(top);
    Serial.print(" Mid: ");
    Serial.print(mid);
    Serial.print(" Bot: ");
    Serial.print(bot);
    Serial.print(" => Level: ");
    Serial.println(level);

    delay(200);
}