from flask import Flask, request, jsonify
import torch
import numpy as np
from PIL import Image
from modelo import NeuroBotCNN

app = Flask(__name__)

# =============================
#  CARGAR MODELO
# =============================
try:
    model = NeuroBotCNN()  # num_classes=3 por defecto
    model.load_state_dict(torch.load("best_model.pt", map_location="cpu"))
    model.eval()
    print("✔ Modelo cargado correctamente")
except Exception as e:
    print("❌ Error cargando el modelo:", e)


# =============================
#  PREPROCESAMIENTO (ARREGLADO)
# =============================
def preprocess(img):
    try:
        # Redimensionar
        img = img.resize((224, 224))

        # Convertir imagen a float32
        img = np.array(img).astype(np.float32) / 255.0

        # Normalización en float32 (AQUÍ ESTABA EL ERROR)
        mean = np.array([0.485, 0.456, 0.406], dtype=np.float32)
        std  = np.array([0.229, 0.224, 0.225], dtype=np.float32)

        img = (img - mean) / std

        # HWC → CHW
        img = np.transpose(img, (2, 0, 1))

        # Convertir a tensor float32
        img = torch.tensor(img, dtype=torch.float32).unsqueeze(0)

        return img

    except Exception as e:
        print("❌ Error en preprocess:", e)
        return None


# =============================
#  ENDPOINT DE PREDICCIÓN
# =============================
@app.route("/predict", methods=["POST"])
def predict():
    try:
        if "image" not in request.files:
            return jsonify({"error": "no se envió imagen"}), 400

        file = request.files["image"]

        # Leer imagen
        try:
            img = Image.open(file.stream).convert("RGB")
        except Exception as e:
            print("❌ Error leyendo imagen:", e)
            return jsonify({"label": "error", "prob": 0.0, "raw_probs": []}), 500

        # Preprocesar
        tensor = preprocess(img)
        if tensor is None:
            return jsonify({"label": "error", "prob": 0.0, "raw_probs": []}), 500

        # Predicción
        with torch.no_grad():
            output = model(tensor)
            probs = torch.softmax(output, dim=1)[0].numpy()

        clases = ["asfalto", "cesped", "grava"]
        idx = int(np.argmax(probs))
        label = clases[idx]

        # Respuesta JSON
        return jsonify({
            "label": label,
            "prob": float(probs[idx]),
            "raw_probs": [float(p) for p in probs]
        })

    except Exception as e:
        print(" Error general en /predict:", e)
        return jsonify({"label": "error", "prob": 0.0, "raw_probs": []}), 500


# =============================
#  RUN SERVER
# =============================
if __name__ == "__main__":
    print(" Servidor Flask listo en http://127.0.0.1:5000/predict")
    app.run(host="0.0.0.0", port=5000)