#include <Wire.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <Adafruit_VL53L0X.h>

// ======================
// CONFIG WIFI UDP
// ======================
const char* ssid = "Redmi Note 13";
const char* pass = "1108558378";

WiFiUDP udp;
int udpPort = 4210;

// ======================
// PINES MOTORES
// ======================
#define MOTOR_A_IN1 12
#define MOTOR_A_IN2 13
#define MOTOR_A_ENA 14
#define MOTOR_B_IN3 27
#define MOTOR_B_IN4 26
#define MOTOR_B_ENB 25

// ======================
// VL53L0X + PCA9548A
// ======================
#define TCA9548A_ADDRESS 0x70
Adafruit_VL53L0X sensors[5];
int dist[5];

void selectChannel(uint8_t ch){
  Wire.beginTransmission(TCA9548A_ADDRESS);
  Wire.write(1<<ch);
  Wire.endTransmission();
}

void setupSensors(){
  Wire.begin();
  delay(300);

  Serial.println("Inicializando sensores VL53L0X...");
  for(int i=0;i<5;i++){
    selectChannel(i);
    if (sensors[i].begin(0x29)) {
      Serial.print("Sensor ");
      Serial.print(i);
      Serial.println(" ✔ OK");
    } else {
      Serial.print("Sensor ");
      Serial.print(i);
      Serial.println(" ✖ ERROR");
    }
    delay(40);
  }
}

void readSensors(){
  VL53L0X_RangingMeasurementData_t m;
  for(int i=0;i<5;i++){
    selectChannel(i);
    sensors[i].rangingTest(&m,false);
    dist[i] = m.RangeMilliMeter;
  }
}

// ======================
// MOTORES
// ======================
void setupMotors(){
  pinMode(MOTOR_A_IN1,OUTPUT);
  pinMode(MOTOR_A_IN2,OUTPUT);
  pinMode(MOTOR_A_ENA,OUTPUT);
  pinMode(MOTOR_B_IN3,OUTPUT);
  pinMode(MOTOR_B_IN4,OUTPUT);
  pinMode(MOTOR_B_ENB,OUTPUT);

  Serial.println("Motores listos.");
}

void motorA(int pwm){
  pwm = constrain(pwm,-250,250);
  if(pwm > 0){
    digitalWrite(MOTOR_A_IN1,HIGH);
    digitalWrite(MOTOR_A_IN2,LOW);
    analogWrite(MOTOR_A_ENA,pwm);
  }
  else if(pwm < 0){
    digitalWrite(MOTOR_A_IN1,LOW);
    digitalWrite(MOTOR_A_IN2,HIGH);
    analogWrite(MOTOR_A_ENA,-pwm);
  }
  else{
    digitalWrite(MOTOR_A_IN1,LOW);
    digitalWrite(MOTOR_A_IN2,LOW);
    analogWrite(MOTOR_A_ENA,0);
  }
}

void motorB(int pwm){
  pwm = constrain(pwm,-200,200);
  if(pwm > 0){
    digitalWrite(MOTOR_B_IN3,HIGH);
    digitalWrite(MOTOR_B_IN4,LOW);
    analogWrite(MOTOR_B_ENB,pwm);
  }
  else if(pwm < 0){
    digitalWrite(MOTOR_B_IN3,LOW);
    digitalWrite(MOTOR_B_IN4,HIGH);
    analogWrite(MOTOR_B_ENB,-pwm);
  }
  else{
    digitalWrite(MOTOR_B_IN3,LOW);
    digitalWrite(MOTOR_B_IN4,LOW);
    analogWrite(MOTOR_B_ENB,0);
  }
}

// ======================
// SETUP
// ======================
void setup(){
  Serial.begin(115200);
  Serial.println("\n====== INICIANDO ROBOT MODO MANUAL ======\n");

  setupMotors();
  setupSensors();

  Serial.println("Conectando al WiFi...");
  WiFi.begin(ssid,pass);

  while(WiFi.status() != WL_CONNECTED){
    Serial.print(".");
    delay(300);
  }

  Serial.println("\nWiFi conectado!");
  Serial.print("IP del robot: ");
  Serial.println(WiFi.localIP());

  udp.begin(udpPort);
  Serial.println("UDP esperando comandos de Unity...");
  Serial.println("==========================================\n");
}

// ======================
// LOOP SOLO MANUAL
// ======================
void loop(){

  // ===========================
  // 1) RECIBIR COMANDO DESDE UNITY
  // ===========================
  char buffer[40];
  int len = udp.parsePacket();

  if(len > 0){
    udp.read(buffer,40);
    String msg = String(buffer);

    int coma = msg.indexOf(',');
    if(coma > 0){
      int mA = msg.substring(0,coma).toInt();
      int mB = msg.substring(coma+1).toInt();

      motorA(mA);
      motorB(mB);

      Serial.printf("[MANUAL] mA=%d mB=%d\n", mA, mB);
    }
  }

  // ===========================
  // 2) SENSORES (solo lectura)
  // ===========================
  readSensors();

  Serial.printf("Sensores: Frente=%d  IzqF=%d  DerF=%d  IzqA=%d  DerA=%d\n",
    dist[4], dist[0], dist[2], dist[1], dist[3]);

  delay(40);
}