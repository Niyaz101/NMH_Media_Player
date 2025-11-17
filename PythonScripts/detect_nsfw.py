import sys
from PIL import Image
import torch
from torchvision import transforms, models

# Load model
model = models.resnet18(weights=models.ResNet18_Weights.DEFAULT)
model.eval()

# Image path from argument
img_path = sys.argv[1]

img = Image.open(img_path).convert("RGB")
preprocess = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor()
])
tensor = preprocess(img).unsqueeze(0)

with torch.no_grad():
    output = model(tensor)
    # Example: if class index 1 is NSFW
    prediction = torch.argmax(output, dim=1).item()

if prediction == 1:
    print("NSFW")
else:
    print("SAFE")
