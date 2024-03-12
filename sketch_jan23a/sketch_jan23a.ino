#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <DHT.h>

#define DHTTYPE DHT11 // temp/moist sensor type
#define DHTPIN 27     // temp/moist sensor data pin(middle)

const char* ssid = "PIENKONE";
const char* password = "12345679";

const char *mqtt_server = "172.20.50.151";
const int mqtt_port = 1234;
const char* mqtt_clientID = "ESP32CLIENT";
const char* mqtt_username = "your-username";
const char* mqtt_password = "your-password";

WiFiClient espClient;
PubSubClient client(espClient);

const int LED_OUTPUT_PIN = 19; // debug led

DHT dht(DHTPIN, DHTTYPE);

/* Variables for tracking the measurements for average calculation */
int amountOfMeasurements = 5;
int measurementCounter = 0;

/* Measurements array */
float measurements[] = {0.0, 0.0, 0.0, 0.0, 0.0};

/* Function that is used to add or update new measurement in the list */
void addMeasurement(float temperature) {
	measurements[measurementCounter] = temperature;
	/* Update position */
	measurementCounter += 1;
  	/* Start again from the first item is the array is full */
	if (measurementCounter == amountOfMeasurements) {
		measurementCounter = 0;
	}
}

/* Function to calculate the average of measured temperatures */
float calculateAverage() {
	float avgTemp = 0.0;
	for (int counter = 0 ; counter < amountOfMeasurements ; counter++) {
		avgTemp += measurements[counter];
	}
	/* Calculate the average temp */
	avgTemp = avgTemp / amountOfMeasurements;
	return(avgTemp);
}

void setup() {
  Serial.begin(9600);
  //Serial.begin(115200);
  pinMode(LED_OUTPUT_PIN, OUTPUT);
  digitalWrite(LED_OUTPUT_PIN, HIGH);

  setup_wifi();
  client.setServer(mqtt_server, 1234);
  client.setCallback(callback);
  dht.begin();
}

void loop() {
    if (!client.connected()) {
        reconnect();
    }
    client.loop();

    // Variable for measured temperature
    float measuredTemp = 0.0;

    // Read temperature from sensor
    float h = dht.readHumidity();
    measuredTemp = dht.readTemperature();

    Serial.print(F("Humidity: "));
    Serial.print(h);
    Serial.print(F("%  Temperature: "));
    Serial.print(measuredTemp);
    Serial.println();

    // Variable for average temperature
    float averageTemp = 0.0;

    // Add measurement to list, calculate average temp and send it to mqtt
    addMeasurement(measuredTemp);
    averageTemp = calculateAverage();

    // Print measured temperature to serial console
    Serial.print("Average (array used): ");
    Serial.println(averageTemp);

     // Convert the average temperature value to a char array and publish it to MQTT
    char tempString[8];
    dtostrf(averageTemp, 1, 2, tempString);
    client.publish("esp32/temperature", tempString);


    delay(5000);
}

void setup_wifi() {
    delay(10);
    // We start by connecting to a WiFi network
    Serial.println();
    Serial.print("Connecting to ");
    Serial.println(ssid);

    WiFi.begin(ssid, password);

    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }

    Serial.println();
    Serial.print("WiFi connected! ");
    Serial.print("IP address: ");
    Serial.print(WiFi.localIP());
}

// CALLBACKS
void callback(char *topic, byte *message, unsigned int length) {
    Serial.print("Message arrived on topic: ");
    Serial.print(topic);
    Serial.print(". Message: ");
    String messageTemp;

    for (int i = 0; i < length; i++) {
        Serial.print((char)message[i]);
        messageTemp += (char)message[i];
    }
    Serial.println();

    // Feel free to add more if statements to control more GPIOs with MQTT

    // If a message is received on the topic esp32/output, you check if the message is either "on" or "off".
    // Changes the output state according to the message
    if (String(topic) == "hello") {
        Serial.print("Changing output to ");
        if (messageTemp == "on") {
            digitalWrite(LED_OUTPUT_PIN, HIGH);
            Serial.println("on");
        } else if (messageTemp == "off") {
            digitalWrite(LED_OUTPUT_PIN, LOW);
            Serial.println("off");
        }
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
            client.subscribe("hello");
        } else {
            Serial.print("failed, rc=");
            Serial.print(client.state());
            Serial.println(" try again in 5 seconds");
            // Wait 5 seconds before retrying
            delay(2500);
        }
    }
}
