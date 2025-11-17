import torch
import os

# === CONFIG ===
input_model_path = r"C:\Users\nmhxy\source\repos\NMH Media Player\Models\320n.pt"
output_model_path = r"C:\Users\nmhxy\source\repos\NMH Media Player\Models\320n_torchscript.pt"

# === STEP 1: Load your existing PyTorch model ===
# If your model is a state_dict, you need the class definition.
# For example, if you trained NSFW model with FastAI/ResNet:
# from your_model_file import NSFWModel
# model = NSFWModel()
# model.load_state_dict(torch.load(input_model_path, map_location='cpu'))

# If your checkpoint is full model (not just state_dict), use:
model = torch.load(input_model_path, map_location='cpu')

# Set to evaluation mode
model.eval()

# === STEP 2: Create example input tensor ===
# This should match the input your model expects: [batch, channels, height, width]
example_input = torch.randn(1, 3, 224, 224)  # typical size for CNNs

# === STEP 3: Convert to TorchScript ===
traced_model = torch.jit.trace(model, example_input)

# === STEP 4: Save TorchScript model ===
torch.jit.save(traced_model, output_model_path)

print(f"TorchScript model saved at: {output_model_path}")
