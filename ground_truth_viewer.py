import os
import json
import math
import numpy as np
import matplotlib.pyplot as plt
import cv2
import re

# Parameters
img_folder = "imgs"  # folder with images and json files
image_width = 640
image_height = 480
vertical_fov_deg = 44.61998
cx = image_width / 2.0
cy = image_height / 2.0
f = (image_height / 2.0) / math.tan(math.radians(vertical_fov_deg / 2.0))

def project_point(point_3d):
    X, Y, Z = point_3d
    if Z == 0:
        Z = 1e-6
    u = f * (X / Z) + cx
    v = f * (Y / Z) + cy
    return (u, v)

def parse_vector(vec_str):
    vec_str = vec_str.strip()[1:-1]  # remove parentheses
    parts = vec_str.split(',')
    return tuple(float(p.strip()) for p in parts)

# Get list of JSON files in the folder (e.g. "1.json", "2.json", ...)
json_pattern = re.compile(r"^(\d+)\.json$")
json_files = sorted([f for f in os.listdir(img_folder) if json_pattern.match(f)])

for json_file in json_files:
    m = json_pattern.match(json_file)
    if not m:
        continue
    image_prefix = m.group(1)  # This number will be used as the leading number for the images.
    # Process only image 131
    if image_prefix != "136":
        continue
    json_path = os.path.join(img_folder, json_file)
    
    # Load JSON data
    with open(json_path, 'r') as fjson:
        data = json.load(fjson)
    cameras = data.get("cameras", {})
    
    for cam_name, cam_data in cameras.items():
        print(f"Processing JSON file {json_file} for camera {cam_name}...")
        iris_points = cam_data.get("iris_2d", [])
        iris_points_3d = [parse_vector(pt) for pt in iris_points]
        
        # Debug: print first iris point value
        if iris_points_3d:
            print(f"First iris point for {cam_name}:", iris_points_3d[0])
        
        gt = cam_data.get("ground_truth", {})
        iris_center_str = gt.get("iris_center", None)
        gaze_vector_str = gt.get("gaze_vector", None)
        eye_center_str = gt.get("eye_center", None)
        if iris_center_str is None or gaze_vector_str is None:
            print(f"Missing ground truth for {cam_name} in {json_file}, skipping.")
            continue

        iris_center_3d = np.array(parse_vector(iris_center_str))
        gaze_vector_3d = np.array(parse_vector(gaze_vector_str))
        # Instead of a vector arrow, we compute a gaze endpoint (iris_center + gaze_vector)
        gaze_endpoint_3d = iris_center_3d + gaze_vector_3d

        # For the iris points from JSON, invert only the y coordinate
        proj_iris = [(pt[0], image_height - pt[1]) for pt in iris_points_3d]

        # Project ground truth iris center, gaze endpoint and eye center using pinhole projection.
        proj_gt_center = project_point(iris_center_3d)
        proj_gaze_endpoint = project_point(gaze_endpoint_3d)
        
        # If available, parse and project the eye_center point.
        proj_eye_center = None
        if eye_center_str is not None:
            eye_center_3d = np.array(parse_vector(eye_center_str))
            proj_eye_center = project_point(eye_center_3d)

        # Construct corresponding image filename using the image prefix and camera name.
        img_filename = f"{image_prefix}_{cam_name}.jpg"
        img_path = os.path.join(img_folder, img_filename)
        if not os.path.exists(img_path):
            print(f"Image {img_path} not found, skipping {cam_name}.")
            continue

        img = cv2.imread(img_path)
        img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

        plt.figure(figsize=(8,6))
        plt.imshow(img_rgb)  # Image in its natural orientation (origin at top-left)
        plt.title(f"{cam_name} - Image {image_prefix}")
        proj_iris_np = np.array(proj_iris)
        plt.scatter(proj_iris_np[:, 0], proj_iris_np[:, 1],
                    c='blue', s=20, label='Iris Points')
        plt.plot(proj_gt_center[0], proj_gt_center[1],
                 marker='*', markersize=15, color='red', label='GT Iris Center')
        # Plot the gaze endpoint as a green circle.
        plt.scatter(proj_gaze_endpoint[0], proj_gaze_endpoint[1],
                    c='green', s=50, label='GT Gaze Endpoint')
        if proj_eye_center is not None:
            # Plot the eye center (world origin in camera coordinates) as a magenta circle.
            plt.scatter(proj_eye_center[0], proj_eye_center[1],
                        c='magenta', s=50, label='GT Eye Center')
        plt.legend()
        plt.xlim([0, image_width])
        plt.show()