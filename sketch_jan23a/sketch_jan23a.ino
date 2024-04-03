#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <DHT.h>

#define DHTTYPE DHT11 // temp/moist sensor type
#define DHTPIN 14     // temp/moist sensor data pin(middle)
#define ENABLEMQQT true

const char *ssid = "MB210-G";
const char *password = "studentMAMK";

const char* mqtt_clientID = "ESP32CLIENT";
const char *mqtt_server = "172.20.50.151";
const uint16_t mqtt_port = 1234;

// time delay, of how often weather data should be sent to server (seconds)
const unsigned long weatherSendInterval = 60;

// Variables for tracking the measurements for average calculation
int amountOfMeasurements = 5;

WiFiClient espClient;
PubSubClient client(espClient);
const int LED_OUTPUT_PIN = 19;
DHT dht(DHTPIN, DHTTYPE); // DHT object
unsigned long previousMillis = 0;

void setup() {
    Serial.begin(9600);

    pinMode(LED_OUTPUT_PIN, OUTPUT);
    digitalWrite(LED_OUTPUT_PIN, HIGH);


    bool connected = setup_wifi();
    if (connected) {
        client.setServer(mqtt_server, mqtt_port);
        client.setCallback(callback);
        dht.begin();
    }
}

void loop() {
    if (!client.connected()) { reconnect(); }
    client.loop();
    
    unsigned long currentMillis = millis();

    // Send Weather data to MQTT server every X seconds (10 seconds)
    if (currentMillis - previousMillis >= weatherSendInterval*1000) {
        // Read temperature from sensor
        float measuredHumidity = dht.readHumidity();
        float measuredTemp = dht.readTemperature();

        // Send temperature in this format: "temperature,humidity" -> "21.5,40.3"
        String data = String(measuredTemp, 1) + "," + String(measuredHumidity, 1);
        Serial.println(data);

        // Send data to server!
        if (data != "nan,nan") {
            client.publish("esp32/weatherdata", data.c_str());
            Serial.println("Weather data sent!");
        }
        previousMillis = currentMillis;
    }
    

    // 3 seconds TEST
    
    delay(50);
}

bool setup_wifi() {
    delay(10);
    // We start by connecting to a WiFi network
    Serial.println();
    Serial.print("Connecting to ");
    Serial.println(ssid);

    WiFi.begin(ssid, password);

    unsigned long startTime = millis();
    unsigned long timeout = 10000;
    while (WiFi.status() != WL_CONNECTED && millis() - startTime < timeout) {
        delay(500);
        Serial.print(".");
    }

    if (WiFi.status() == WL_CONNECTED) {
        Serial.println("\nWiFi connected successfully!");
        Serial.print("IP address: ");
        Serial.println(WiFi.localIP());
        return(true);
    } else {
        Serial.println("\nWiFi connection timed out!");
        return(false);
    }
}

// CALLBACKS
void callback(char *topicChar, byte *messageBytes, unsigned int length) {
    Serial.println("Message arrived!");

    // Make sure this callback message was ment for this device!
    String topic = String(topicChar);
    String targetDevice;
    int topicIndex = topic.indexOf('/');
    if (topicIndex != -1) {
        targetDevice = topic.substring(topicIndex + 1);
        topic = topic.substring(0, topicIndex);
    }
    Serial.print("TargetDevice: ");
    Serial.println(targetDevice);
    Serial.print("Topic: ");
    Serial.println(topic);

    // Get message string from message bytes
    String message;
    for (int i = 0; i < length; i++) {
        message += (char)messageBytes[i];
    }
    Serial.print("Message: ");
    Serial.println(message);


    // Get request key if it exsists
    String key;
    int keyIndex = message.indexOf('|');
    if (keyIndex != -1) {
        key = message.substring(0, keyIndex);
    }

    // GET LED state. And return to server using key as response code
    if (topic == "getledstate") {
      int ledState = digitalRead(LED_OUTPUT_PIN);
      String data = "response:" + key + "|" + String(ledState);
      // response as: message = "response:123|message"
      client.publish("esp32/getledstate", data.c_str());
    }

    // Change LED state
    if (topic == "setledstate") {
        if (message == "true") {
            digitalWrite(LED_OUTPUT_PIN, HIGH);
        } else if (message == "false") {
            digitalWrite(LED_OUTPUT_PIN, LOW);
        }
    }
}




// MQTT RECONNECT
void reconnect() {
    // Loop until we're reconnected
    while (!client.connected()) {
        delay(2500);

        Serial.print("Attempting MQTT connection... ");
        // Attempt to connect
        if (client.connect(mqtt_clientID)) {
            Serial.println("connected");
            // Subscribe to callbacks for this specific device
            client.subscribe(("getledstate/" + String(mqtt_clientID)).c_str());
            client.subscribe(("setledstate/" + String(mqtt_clientID)).c_str());
        } else {
            Serial.print("failed, rc=");
            Serial.print(client.state());
            Serial.println(" try again in 5 seconds");
            // Wait 5 seconds before retrying
            delay(2500);
        }
    }
}
