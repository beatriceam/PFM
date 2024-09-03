#include <iostream>
#include <vector>
#include <chrono>
#include <fstream>

using namespace std;
using namespace std::chrono;

// Optimized 2D convolution function using pointers
void apply2DConvolution(const uint8_t* image, uint8_t* output, int img_width, int img_height, const int kernel[5][5], int kernel_sum) {
    int pad_h = 2; // Half the kernel size (5x5)
    int pad_w = 2;

    // Perform convolution
    for (int i = pad_h; i < img_height - pad_h; i++) {
        for (int j = pad_w; j < img_width - pad_w; j++) {
            int sum = 0;

            const uint8_t* image_ptr = image + (i - pad_h) * img_width + (j - pad_w);

            for (int ki = 0; ki < 5; ki++) {
                for (int kj = 0; kj < 5; kj++) {
                    sum += *(image_ptr + ki * img_width + kj) * kernel[ki][kj];
                }
            }

            *(output + i * img_width + j) = static_cast<uint8_t>(sum / kernel_sum);
        }
    }
}

// Function to save the image as a BMP file
void saveAsBMP(const string& filename, const uint8_t* image, int img_width, int img_height) {
    ofstream file(filename, ios::binary);

    // BMP File Header
    uint32_t file_size = 54 + img_width * img_height * 3; // header size + image data size
    uint16_t reserved = 0;
    uint32_t offset = 54; // 14 (file header) + 40 (DIB header)

    // DIB Header (BITMAPINFOHEADER)
    uint32_t dib_header_size = 40;
    uint16_t planes = 1;
    uint16_t bits_per_pixel = 24; // 24-bit color depth
    uint32_t compression = 0;
    uint32_t image_size = img_width * img_height * 3;
    uint32_t x_pixels_per_meter = 2835;
    uint32_t y_pixels_per_meter = 2835;
    uint32_t colors_used = 0;
    uint32_t important_colors = 0;

    // File header
    file.put('B').put('M');  // Signature
    file.write(reinterpret_cast<const char*>(&file_size), 4);
    file.write(reinterpret_cast<const char*>(&reserved), 2);
    file.write(reinterpret_cast<const char*>(&reserved), 2);
    file.write(reinterpret_cast<const char*>(&offset), 4);

    // DIB header (BITMAPINFOHEADER)
    file.write(reinterpret_cast<const char*>(&dib_header_size), 4);
    file.write(reinterpret_cast<const char*>(&img_width), 4);
    file.write(reinterpret_cast<const char*>(&img_height), 4);
    file.write(reinterpret_cast<const char*>(&planes), 2);
    file.write(reinterpret_cast<const char*>(&bits_per_pixel), 2);
    file.write(reinterpret_cast<const char*>(&compression), 4);
    file.write(reinterpret_cast<const char*>(&image_size), 4);
    file.write(reinterpret_cast<const char*>(&x_pixels_per_meter), 4);
    file.write(reinterpret_cast<const char*>(&y_pixels_per_meter), 4);
    file.write(reinterpret_cast<const char*>(&colors_used), 4);
    file.write(reinterpret_cast<const char*>(&important_colors), 4);

    // Image data (BGR format)
    for (int i = img_height - 1; i >= 0; i--) {  // BMP files are stored bottom to top
        for (int j = 0; j < img_width; j++) {
            uint8_t pixel = *(image + i * img_width + j);
            file.put(pixel).put(pixel).put(pixel); // BGR, grayscale so all are the same
        }
    }

    file.close();
}

int main() {
    // Define the width and height of the image
    int img_width = 512;
    int img_height = 512;

    // Create a dummy image array
    vector<uint8_t> image_data(img_width * img_height);
    for (int i = 0; i < img_height; i++) {
        for (int j = 0; j < img_width; j++) {
            image_data[i * img_width + j] = static_cast<uint8_t>((i + j) % 256);
        }
    }

    // Create an output image array
    vector<uint8_t> output_image(img_width * img_height, 0);

    // Define the 5x5 Gaussian kernel
    int gaussian_kernel_2D[5][5] = {
        {1, 4, 6, 4, 1},
        {4, 16, 24, 16, 4},
        {6, 24, 36, 24, 6},
        {4, 16, 24, 16, 4},
        {1, 4, 6, 4, 1}
    };

    int kernel_sum = 256; // Normalized sum of the Gaussian kernel (256)

    // Measure the time for 10 runs of standard 2D convolution and calculate the average
    long long total_time_2D = 0;

    for (int i = 0; i < 10; i++) {
        auto start_time = high_resolution_clock::now();

        // Apply the standard 2D Gaussian filter
        apply2DConvolution(image_data.data(), output_image.data(), img_width, img_height, gaussian_kernel_2D, kernel_sum);

        auto end_time = high_resolution_clock::now();
        auto processing_time = duration_cast<microseconds>(end_time - start_time).count();
        cout << "Standard 2D convolution time for run " << (i + 1) << ": " << processing_time << " microseconds" << endl;
        total_time_2D += processing_time;
    }
    cout << "Average standard 2D convolution time: " << (total_time_2D / 10) << " microseconds" << endl;

    // Save the final output as a BMP file
    saveAsBMP("output_image_2D.bmp", output_image.data(), img_width, img_height);

    return 0;
}
