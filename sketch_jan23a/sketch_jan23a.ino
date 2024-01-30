#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <dht.h>

#define DHTTYPE DHT11 // sensor type
#define DHTPIN 34 // data pin(middle)

const char* ssid = "PIENKONE";
const char* password = "12345679";

const char* mqtt_server = "192.168.137.1";

WiFiClient espClient;
PubSubClient client(espClient);


const int LED_OUTPUT_PIN = 19;

DHT dht(DHTPIN, DHTTYPE); // DHT object

void setup() {
  Serial.begin(9600);
  //Serial.begin(115200);
  pinMode(LED_OUTPUT_PIN, OUTPUT);
  digitalWrite(LED_OUTPUT_PIN, HIGH);

  /*setup_wifi();
  client.setServer(mqtt_server, 1234);
  client.setCallback(callback);*/
  dht.begin();
}

void loop() {
  /*if (!client.connected()) {
    reconnect();
  }
  client.loop();*/

  float h = dht.readHumidity();
  float t = dht.readTemperature();

  Serial.print(F("Humidity: "));
  Serial.print(h);
  Serial.print(F("%  Temperature: "));
  Serial.print(t);
  Serial.println();

  /*char message[50];
  snprintf(message, 50, "Hello from ESP32 at %ld", millis());
  client.publish("esp32/test", message);
  Serial.print("Message sent: ");
  Serial.println(message);*/
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
void callback(char* topic, byte* message, unsigned int length) {
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
    if(messageTemp == "on"){
      digitalWrite(LED_OUTPUT_PIN, HIGH);
      Serial.println("on");
    }
    else if(messageTemp == "off"){
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
    if (client.connect("ESP8266Client")) {
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