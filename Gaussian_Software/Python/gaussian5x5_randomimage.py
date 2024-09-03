import numpy as np
import cv2
import time
import matplotlib.pyplot as plt

# Define the width and height of the image
img_width = 512
img_height = 512

# Generate a random image
imageLena = np.random.randint(0, 256, (img_height, img_width), dtype=np.uint8)

# Initialize an array to store the processing times
processing_times = []

# Execute the Gaussian blur 10 times
for i in range(10):
    # Measure the time before applying the Gaussian with microsecond precision
    start_time = time.perf_counter()

    # Apply the Gaussian filter 5x5
    gaussian_blur = cv2.GaussianBlur(imageLena, (5, 5), 0)

    # Measure the time after applying the Gaussian with microsecond precision
    end_time = time.perf_counter()

    # Calculate the processing time in microseconds
    processing_time = (end_time - start_time) * 1_000_000  # Converting to microseconds
    processing_times.append(processing_time)
    print(f"Processing time for run {i+1}: {processing_time:.3f} microseconds")

# Calculate the average processing time
average_time = sum(processing_times) / len(processing_times)
print(f"\nAverage processing time: {average_time:.3f} microseconds")

# Plot the original image and the filtered image from the last run
plt.figure(figsize=(10, 5))

# Input image
plt.subplot(1, 2, 1)
plt.title("Original Image")
plt.imshow(imageLena, cmap='gray')
plt.axis('off')

# Output image (Gaussian applied)
plt.subplot(1, 2, 2)
plt.title("Image with Gaussian 5x5")
plt.imshow(gaussian_blur, cmap='gray')
plt.axis('off')

# Display the images
plt.show()
