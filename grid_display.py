import os
import matplotlib.pyplot as plt

# Path to the image folder (assumes it's in the same directory level as this script)
img_folder = os.path.join(os.path.dirname(__file__), "imgs")

# Create a 4x4 grid plot
fig, axs = plt.subplots(nrows=4, ncols=4, figsize=(12, 12))

for sample in range(1, 5):         # Samples 1 to 4 (top to bottom)
    for cam in range(1, 5):        # Camera views 1 to 4 (left to right)
        # Construct filename: adjust file extension if necessary
        filename = os.path.join(img_folder, f"{sample+4}_cam_{cam}.jpg")
        print(f"Checking {filename}...")
        
        ax = axs[sample - 1, cam - 1]
        if os.path.exists(filename):
            img = plt.imread(filename)
            ax.imshow(img)
        else:
            ax.text(0.5, 0.5, "N/A", fontsize=12, ha="center", va="center")
        ax.axis('off')  # Disable individual axes labels and titles

# Add common x and y axis labels
fig.supxlabel("Camera number")
fig.supylabel("Sample number")

# Remove extra spacing between subplots and margins
plt.subplots_adjust(left=0.05, right=0.95, top=0.80, bottom=0.20, wspace=0, hspace=0)
plt.show()