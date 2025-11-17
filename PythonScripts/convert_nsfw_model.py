# convert_nsfw_model.py
import torch
from ultralytics import YOLO
import os

# ✅ Paths
MODEL_PATH = os.path.join("..", "Models", "320n.pt")   # your checkpoint
OUTPUT_PATH = os.path.join("..", "Models", "320n_ts.pt")  # output TorchScript

print(f"🔹 Loading NSFW model from: {MODEL_PATH}")

# 1️⃣ Load the model directly from checkpoint
# This automatically matches architecture to checkpoint classes
model = YOLO(MODEL_PATH)

# 2️⃣ Set model to eval mode
model.eval()
print("✅ Model loaded successfully")

# 3️⃣ Convert to TorchScript
print("🔹 Converting model to TorchScript...")
# export as TorchScript with verbose=True to see the layers
ts_model = model.export(format="torchscript", verbose=True)
print(f"✅ TorchScript model saved at: {OUTPUT_PATH}")
