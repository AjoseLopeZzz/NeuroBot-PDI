NeuroBot – Sistema de Visión y Control en Tiempo Real

NeuroBot es un proyecto que integra hardware, visión computacional y simulación en Unity para crear un robot capaz de recibir video en tiempo real desde una ESP32-CAM, 
procesarlo con una red neuronal y usar esa información para visualizar o controlar comportamientos dentro del entorno virtual. Todo el flujo se organiza en tres módulos 
bien separados: Arduino, Unity y un servidor Python con la CNN entrenada.

1. Estructura general del proyecto

El proyecto está dividido en carpetas específicas que separan cada parte del sistema:

Esta organización permite trabajar cada parte sin afectar las otras. Arduino se usa para todo el control y transmisión desde el ESP32. Unity se encarga de la 
visualización y el envío de frames al servidor. Python contiene el modelo entrenado y la API que responde a Unity con la predicción del terreno.

2. Funcionamiento general del sistema
El flujo del sistema es directo:

La ESP32-CAM transmite un stream MJPEG por la red WiFi.
Unity recibe ese stream, reconstruye cada frame y lo convierte en una textura.
De manera periódica, Unity toma uno de esos frames y lo envía al servidor Python.
El servidor recibe la imagen, la preprocesa y la pasa por la CNN.
La CNN devuelve un JSON con la clase más probable y las probabilidades de las demás.
Unity procesa esa respuesta y actualiza la interfaz, o toma decisiones según la clasificación.
Este proceso permite una integración tiempo real entre hardware, red neuronal y simulación.

3. Módulo Arduino (ESP32-CAM y control)

La carpeta Arduino contiene dos partes:

CameraWebServer_copy…
Versión usada para transmitir la cámara depende de como la conectes, por ejemplo que me sucedio a mi a la hora de hacer las pruebas es lo siguiente
http://10.175.187.79/stream

Se configura en el código cambiando el SSID y la clave. Basta subirlo y ver la IP en el monitor serial.

Movimiento-r2d2
Código donde se controla el movimiento del robot usando pines, PWM y un servidor UDP. Aquí Unity envía los comandos cuando está activo el modo manual. 
El ESP32 interpreta cada comando recibido como velocidades para cada motor derecho e izquierdo.

El Arduino no necesita modificaciones adicionales para la conexión con Unity, más que mantener la red activa y escuchar comandos.

4. Proyecto Unity (NeuroBot/)
Esta es la parte visual y de lógica central. Dentro de Unity se manejan tres responsabilidades:

Lectura del video
Un script (por ejemplo, ESP32Stream.cs) abre la URL del stream MJPEG y reconstruye los frames como Texture2D. Esto se asigna a un RawImage o cualquier superficie dentro del juego.

Envío del frame al servidor CNN
Otro script (como CNNClient.cs) convierte esa textura en un JPG en memoria, lo redimensiona y lo envía por POST a la ruta /predict.

Interfaz y lógica
Unity recibe una respuesta en JSON con el terreno detectado. El contenido típico es:

<img width="962" height="415" alt="image" src="https://github.com/user-attachments/assets/855de612-9c40-45d7-bd20-b1f97a3a9849" />

Con esa información se actualizan textos en la UI o se ejecutan comportamientos del robot virtual.
Unity también puede mandar comandos por UDP al ESP32 cuando el usuario controla el robot desde la interfaz o el joystick.

5. Servidor Python + CNN

Esta carpeta contiene:

El modelo entrenado best_model.pt
El archivo modelo.py que define la arquitectura y carga de clases
El servidor Flask server.py usado para recibir imágenes y responder con la predicción
El servidor hace tres pasos:

-Recibe el frame enviado desde Unity.
-Lo preprocesa a la resolución de entrenamiento.
-Lo pasa por el modelo PyTorch y devuelve la predicción como JSON.

El entrenamiento previo se hizo con un dataset que contiene tres clases:
asfalto
cesped
grava

El modelo final se carga en memoria apenas inicia el servidor, evitando retrasos en las predicciones.

6. Integración completa
Una vez que Arduino, Unity y Python están configurados, el flujo funciona sin intervención manual:

1. La cámara transmite.
2. Unity consume.
3. clasifica.
4. Unity responde.

El sistema entero depende de estar en la misma red WiFi y tener correctamente configuradas las rutas:

URL del stream en Unity
IP del servidor Python
IP del ESP32 para comandos UDP

La conexión se mantiene estable siempre que no se cambie de red.
