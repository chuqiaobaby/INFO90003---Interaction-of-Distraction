#include <FastLED.h>

#define LED_PIN         12
#define NUM_LEDS        150
#define BRIGHTNESS      80

#define TOUCH_PIN       4
#define TOUCH_THRESHOLD 800

CRGB leds[NUM_LEDS];

// Rainbow cycling
uint8_t hue = 0;

// Pulse brightness
uint8_t pulseBrightness = 0;
int pulseDirection = 1;

void setup() {

    Serial.begin(115200);

    FastLED.addLeds<WS2812B, LED_PIN, GRB>(leds, NUM_LEDS);
    FastLED.setBrightness(BRIGHTNESS);

    FastLED.clear();
    FastLED.show();
}

void loop() {

    int touchValue = touchRead(TOUCH_PIN);
    Serial.println(touchValue);

    // Hand detected in water
    if (touchValue < TOUCH_THRESHOLD) {

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

        Serial.println(1);

        delay(20);

    } else {

        // LEDs OFF
        FastLED.clear();
        FastLED.show();

        // Reset pulse when inactive
        pulseBrightness = 10;
        pulseDirection = 1;

        Serial.println(0);

        delay(50);
    }
}