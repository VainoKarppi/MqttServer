#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <DHT.h>
#include <ArduinoSTL.h> // Allows: std::vector<float> -> we can use this to clear the list

#define DHTTYPE DHT11 // temp/moist sensor type
#define DHTPIN 27     // temp/moist sensor data pin(middle)

// wifi credentials
const char *ssid = "MB210-G";
const char *password = "studentMAMK";

const char *mqtt_server = "172.20.50.151";
const int mqtt_port = 1234;
const char* mqtt_clientID = "ESP32CLIENT";
const char* mqtt_username = "your-username";
const char* mqtt_password = "your-password";

// Variables for tracking the measurements for average calculation
int amountOfMeasurements = 5;

// Temperature Measurements array --> Allows clearing the list after sending the values
std::vector<float> measurements;



WiFiClient espClient;
PubSubClient client(espClient);
const int LED_OUTPUT_PIN = 19; // debug led
DHT dht(DHTPIN, DHTTYPE);


/* Function to calculate the average of measured temperatures */
float calculateAverage() {
    // Calculate the average of all measurements
    float average = 0.0;
    for (float value : measurements) {
        average += value;
    }

    // Calculate and return the average temp
    return(sum / amountOfMeasurements);
}

void setup() {
    Serial.begin(9600);

    pinMode(LED_OUTPUT_PIN, OUTPUT);
    digitalWrite(LED_OUTPUT_PIN, HIGH);

    dht.begin();

    bool connected = setup_wifi();
    if (connected) {
        client.setServer(mqtt_server, mqtt_port);
        client.setCallback(callback);
    }
}

void loop() {
    if (!client.connected()) { reconnect(); }
    client.loop();

    // Read temperature from sensor
    float measuredHumidity = dht.readHumidity();
    float measuredTemp = dht.readTemperature();


    // Add measurement to array every 1 second
    measurements.push_back(measuredTemp);
    

    // calculate average only AFTER 5 measurements and send it to server!
    if (sizeof(measurements) == amountOfMeasurements) {
        float averageTemp = calculateAverage();

        // Print measured temperature
        Serial.print("Average (array used): ");
        Serial.println(averageTemp);


        // Send temperature in this format: "temperature,humidity" -> "21.5,40.3"
        String data = String(averageTemp, 1) + "," + String(measuredHumidity, 1);

        // Send data to server!
        client.publish("esp32/weatherdata", data);

        // Clear all measurements from list since it has now been published!
        measurements.clear();
    }

    // Get Masurement every 1 second
    delay(1000);
}

bool setup_wifi() {
    // We start by connecting to a WiFi network
    Serial.print("\nConnecting to ");
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
        Serial.print(WiFi.localIP());
        return(true);
    } else {
        Serial.println("\nWiFi connection timed out!");
        return(false);
    }
}

// CALLBACKS
void callback(char *topic, byte *message, unsigned int length) {
    Serial.println("Topic: " + String(topic));
    String messageTemp;

    for (int i = 0; i < length; i++) {
        messageTemp += (char)message[i];
    }
    Serial.println("Message: " + messageTemp + "\n");



    // LED status Request from server. Read LED state end send result to server
    if (String(topic) == "getletstate") {
        int ledState = digitalRead(LED_OUTPUT_PIN);
        client.publish(String(topic) + "|response:" + String(key), String(ledState));
    }

    if (String(topic) == "setledstate") {
        if (messageTemp == "on") {
            digitalWrite(LED_OUTPUT_PIN, HIGH);
        } else if (messageTemp == "off") {
            digitalWrite(LED_OUTPUT_PIN, LOW);
        }
        Serial.println("LED TURNED " + messageTemp);
    }
}

// MQTT RECONNECT
void reconnect() {
    // Loop until we're reconnected
    while (!client.connected()) {
        delay(2500);

        Serial.print("Attempting MQTT connection...");
        // Attempt to connect
        if (client.connect("ESP32Client", mqtt_username, mqtt_password)) {
            Serial.println("connected");
            // Subscribe
            // TODO all callbacks
            client.subscribe("getledstate");
        } else {
            Serial.print("failed, rc=");
            Serial.print(client.state());
            Serial.println(" try again in 5 seconds");
            // Wait 5 seconds before retrying
            delay(2500);
        }
    }
}
