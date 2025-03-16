import os
import json
import math
import numpy as np
import matplotlib.pyplot as plt
import cv2
import re

# Parameters
img_folder = "unityeyes1"  # folder with images and JSON files
image_width = 640
image_height = 480

def parse_vector(vec_str):
    vec_str = vec_str.strip()[1:-1]  # remove surrounding parentheses
    parts = vec_str.split(',')
    return tuple(float(p.strip()) for p in parts)

# Get list of JSON files matching pattern "(\d+).json"
json_pattern = re.compile(r"^(\d+)\.json$")
json_files = sorted([f for f in os.listdir(img_folder) if json_pattern.match(f)])

for json_file in json_files:
    m = json_pattern.match(json_file)
    if not m:
        continue
    image_prefix = m.group(1)  # this number will be used as the image filename prefix
    json_path = os.path.join(img_folder, json_file)
    
    # Load JSON data
    with open(json_path, 'r') as f:
        data = json.load(f)
    
    # Assume the JSON file directly contains an "iris_2d" array
    iris_points = data.get("iris_2d", [])
    # Parse each iris point
    iris_points_3d = [parse_vector(pt) for pt in iris_points]
    # For 2D visualization, invert the y coordinate (from bottom-left origin to top-left)
    proj_iris = [(pt[0], image_height - pt[1]) for pt in iris_points_3d]
    
    # Construct corresponding image filename (e.g. "1.jpg")
    img_filename = f"{image_prefix}.jpg"
    img_path = os.path.join(img_folder, img_filename)
    if not os.path.exists(img_path):
        print(f"Image {img_path} not found, skipping {image_prefix}.")
        continue

    # Read and display the image
    img = cv2.imread(img_path)
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    
    plt.figure(figsize=(8, 6))
    plt.imshow(img_rgb)  # Image is shown in natural orientation (origin at top left)
    plt.title(f"Image {image_prefix}")
    
    if proj_iris:
        proj_iris_np = np.array(proj_iris)
        plt.scatter(proj_iris_np[:, 0], proj_iris_np[:, 1],
                    c='blue', s=20, label='2D Iris Points')
    else:
        print(f"No iris points found in {json_file}.")
        
    plt.legend()
    plt.xlim([0, image_width])
    plt.ylim([image_height, 0])  # y-axis: top (0) to bottom (image_height)
    plt.show()