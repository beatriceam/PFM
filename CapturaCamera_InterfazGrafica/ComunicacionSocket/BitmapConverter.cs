using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComunicacionSocket
{
    public class BitmapConverter
    {
        /// <summary>
        /// Convert a BitmapImage to a Bitmap.
        /// </summary>
        public static Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                using (var bitmap = new Bitmap(outStream))
                {
                    return new Bitmap(bitmap);
                }
            }
        }

        /// <summary>
        /// Convert a Bitmap to a BitmapImage.
        /// </summary>
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        /// <summary>
        /// Convert a Bitmap to a Mono8 Bitmap.
        /// </summary>
        public static Bitmap ConvertToMono8(Bitmap bitmap)
        {
            Bitmap mono8Bitmap = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            ColorPalette palette = mono8Bitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
            }
            mono8Bitmap.Palette = palette;

            BitmapData data = mono8Bitmap.LockBits(new Rectangle(0, 0, mono8Bitmap.Width, mono8Bitmap.Height),
                                                   ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    System.Drawing.Color originalColor = bitmap.GetPixel(x, y);
                    byte grayValue = (byte)((originalColor.R + originalColor.G + originalColor.B) / 3);
                    Marshal.WriteByte(data.Scan0, y * data.Stride + x, grayValue);
                }
            }
            mono8Bitmap.UnlockBits(data);
            return mono8Bitmap;
        }

        public static byte[] ConvertBitmapToByteArray(Bitmap bitmap)
        {
            // Bloquear os bits da imagem, permitindo acesso direto à memória da imagem
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            try
            {
                // Calcular o número total de bytes para os dados da imagem
                int byteCount = bmpData.Stride * bmpData.Height;
                byte[] pixels = new byte[byteCount];

                // Copiar os dados da imagem diretamente para o array de bytes
                IntPtr ptrFirstPixel = bmpData.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

                return pixels;
            }
            finally
            {
                // Desbloquear os bits da imagem
                bitmap.UnlockBits(bmpData);
            }
        }


        public static BitmapImage LoadBitmapImage(byte[] imageBytes, int width, int height)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("No valid image bytes!");

            // Cria um WriteableBitmap com base nos bytes e especificações dadas
            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), imageBytes, width, 0);

            // Encapsular o WriteableBitmap em um BitmapImage via MemoryStream (necessário para certas operações no WPF que requerem BitmapImage)
            using (MemoryStream memoryStream = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder(); // Pode usar outros encoders conforme necessário
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = memoryStream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                // A função abaixo converte um Bitmap Mono8 para BitmapSource mantendo o formato Gray8
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

                try
                {
                    BitmapSource bitmapSource = BitmapSource.Create(
                        bitmap.Width, bitmap.Height,
                        bitmap.HorizontalResolution, bitmap.VerticalResolution,
                        PixelFormats.Gray8,
                        BitmapPalettes.Gray256,
                        bitmapData.Scan0,
                        bitmapData.Stride * bitmap.Height,
                        bitmapData.Stride);

                    return bitmapSource;
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
            else
            {
                // Para formatos diferentes de Mono8, usa o caminho padrão
                IntPtr hBitmap = bitmap.GetHbitmap();
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(hBitmap);
                return bitmapSource;
            }
        }

        public static BitmapSource EnsureGray8AndResize(BitmapSource source, int targetWidth = 512, int targetHeight = 512)
        {
            BitmapSource gray8Image = source;

            // Se a imagem não for Gray8 ou não tiver as dimensões certas, converte e/ou redimensiona
            if (source.Format != PixelFormats.Gray8 || source.PixelWidth != targetWidth || source.PixelHeight != targetHeight)
            {
                Bitmap bitmap = ConvertBitmapSourceToBitmap(source);


                // Redimensiona para 512x512 se necessário
                if (bitmap.Width != targetWidth || bitmap.Height != targetHeight)
                {
                    bitmap = ResizeBitmap(bitmap, targetWidth, targetHeight);
                }

                // Converte para Mono8 (Gray8) se necessário
                if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                {
                    bitmap = ConvertToMono8(bitmap);
                }

                bitmap.Save("Debug Image Mono8.bmp", ImageFormat.Bmp);

                gray8Image = ConvertBitmapToBitmapSource(bitmap);
            }

            return gray8Image;
        }
        public static Bitmap ConvertByteArrayToBitmap(byte[] byteArray, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            Bitmap bitmap = new Bitmap(width, height, pixelFormat);

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);

            Marshal.Copy(byteArray, 0, bmpData.Scan0, byteArray.Length);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }


        public static byte[] ConvertToByteArrayAndEnsureGray8(BitmapSource source)
        {
            BitmapSource gray8Image = EnsureGray8AndResize(source);
            return ConvertBitmapSourceToByteArray(gray8Image);
        }
        public static Bitmap ResizeBitmap(Bitmap sourceBitmap, int targetWidth, int targetHeight)
        {
            // Calcula a proporção de redimensionamento para manter o aspecto
            float aspectRatio = Math.Min((float)targetWidth / sourceBitmap.Width, (float)targetHeight / sourceBitmap.Height);

            int newWidth = (int)(sourceBitmap.Width * aspectRatio);
            int newHeight = (int)(sourceBitmap.Height * aspectRatio);

            // Cria um bitmap redimensionado
            Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);

            // Usando Graphics para desenhar o bitmap redimensionado
            using (Graphics g = Graphics.FromImage(resizedBitmap))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.DrawImage(sourceBitmap, 0, 0, newWidth, newHeight);
            }

            // Agora, cria um bitmap 512x512 e centraliza o bitmap redimensionado nele
            Bitmap finalBitmap = new Bitmap(targetWidth, targetHeight);

            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.Clear(System.Drawing.Color.Black); // Preenche com preto ou qualquer cor de fundo desejada
                int offsetX = (targetWidth - newWidth) / 2;
                int offsetY = (targetHeight - newHeight) / 2;
                g.DrawImage(resizedBitmap, offsetX, offsetY);
            }

            // Verifica se o bitmap original é Mono8 e converte o bitmap final para Mono8 se necessário
            if (sourceBitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                finalBitmap = BitmapConverter.ConvertToMono8(finalBitmap);
            }

            return finalBitmap;
        }


        public static byte[] ConvertBitmapSourceToByteArray(BitmapSource bitmapSource)
        {
            if (bitmapSource.Format != PixelFormats.Gray8)
            {
                throw new InvalidOperationException("A imagem deve estar em formato Gray8 para essa conversão.");
            }

            int stride = bitmapSource.PixelWidth; // Cada linha de pixels em uma imagem Gray8 tem exatamente o número de pixels de largura.
            int size = stride * bitmapSource.PixelHeight;
            byte[] pixelData = new byte[size];

            bitmapSource.CopyPixels(pixelData, stride, 0);

            return pixelData;
        }



        public static Bitmap ConvertBitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            // Verifica o formato da imagem e converte diretamente se possível
            System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

            if (bitmapSource.Format == PixelFormats.Gray8)
            {
                pixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            }

            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[stride * height];

            bitmapSource.CopyPixels(pixelData, stride, 0);

            Bitmap bitmap = new Bitmap(width, height, pixelFormat);

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
            bitmap.UnlockBits(bitmapData);

            // Se for Gray8, precisamos configurar a paleta de cores para garantir a aparência correta
            if (bitmapSource.Format == PixelFormats.Gray8)
            {
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;
            }

            return bitmap;
        }


        public static BitmapSource LoadBitmapSource(byte[] imageData, int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), imageData, width, 0);
            return bitmap;
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

    }
}
