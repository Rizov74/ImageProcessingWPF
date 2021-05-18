using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessingWPF
{
    public static class BitmapTools
    {
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);



        public static int GetBytePerPixel(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Indexed:
                case PixelFormat.Gdi:
                case PixelFormat.Alpha:
                case PixelFormat.PAlpha:
                case PixelFormat.Extended:
                case PixelFormat.Canonical:
                case PixelFormat.Undefined:
                    return 0;

                case PixelFormat.Format1bppIndexed:
                    return 1;

                case PixelFormat.Format4bppIndexed:
                    return 1;

                case PixelFormat.Format8bppIndexed:
                    return 1;

                case PixelFormat.Format16bppGrayScale:
                    return 1;

                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                    return 3;
                case PixelFormat.Format16bppArgb1555:
                    return 4;

                case PixelFormat.Format24bppRgb:
                    return 3;

                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return 3;

                case PixelFormat.Format48bppRgb:
                    return 3;

                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                case PixelFormat.Max:
                    return 4;

                default:
                    return 0;
            }
        }

        public static PixelFormat GetFormat(int bpp)
        {
            switch (bpp)
            {
                case 1:
                    return PixelFormat.Format8bppIndexed;
                case 3:
                    return PixelFormat.Format24bppRgb;
                default:
                    return PixelFormat.Undefined;
            }
        }



        public static bool CreateIfNecessary(ref Bitmap bmp, int newWidth, int newHeight, int bpp)
        {
            var desiredformat = GetFormat(bpp);
            if (desiredformat == PixelFormat.Undefined)
                return false;

            return CreateIfNecessary(ref bmp, newWidth, newHeight, desiredformat);
        }

        public static bool CreateIfNecessary(ref Bitmap bmp, int newWidth, int newHeight, PixelFormat format)
        {
            if (bmp == null || bmp.Width != newWidth || bmp.Height != newHeight || bmp.PixelFormat != format)
            {
                bmp = new Bitmap(newWidth, newHeight, format);
                return true;
            }
            return false;
        }

        public static bool CreateIfNecessary(ref WriteableBitmap bmp, int newWidth, int newHeight, System.Windows.Media.PixelFormat format)
        {
            if (bmp == null || bmp.Width != newWidth || bmp.Height != newHeight || bmp.Format != format)
            {
                bmp = new WriteableBitmap(newWidth, newHeight, 96, 96, format, null);
                return true;
            }
            return false;
        }

        public static void UpdateBuffer(ref Bitmap bmp, Bitmap source)
        {
            CreateIfNecessary(ref bmp, source.Width, source.Height, source.PixelFormat);

            // Lock buffers
            BitmapData dataSource = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            BitmapData dataBmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);


            // Copy the bitmap's data directly to the on-screen buffers
            CopyMemory(dataBmp.Scan0, dataSource.Scan0, source.Height * dataSource.Stride);

            source.UnlockBits(dataSource);
            bmp.UnlockBits(dataBmp);
        }

        public static void UpdateBuffer(ref Bitmap bmp, IntPtr ptrSrc, int width, int height, int bpp)
        {
            var desiredformat = GetFormat(bpp);
            if (desiredformat == PixelFormat.Undefined)
                return;

            CreateIfNecessary(ref bmp, width, height, desiredformat);

            BitmapData dataBmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);


            // Copy the bitmap's data directly to the on-screen buffers
            CopyMemory(dataBmp.Scan0, ptrSrc, width * height * bpp);

            bmp.UnlockBits(dataBmp);
        }

        public static void UpdateBuffer(ref Bitmap bmp, byte[] buffer, int width, int height, int bpp)
        {
            var desiredformat = GetFormat(bpp);
            if (desiredformat == PixelFormat.Undefined)
                return;

            CreateIfNecessary(ref bmp, width, height, desiredformat);

            BitmapData dataBmp = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);


            // Copy the bitmap's data directly to the on-screen buffers
            Marshal.Copy(buffer, 0, dataBmp.Scan0, width * height * bpp);

            bmp.UnlockBits(dataBmp);
        }

        /// <summary>
        /// Update an ImageSource buffer from a bitmap buffer
        /// </summary>
        /// <param name="img">WriteableBitmap to modify, allocated if null or different size</param>
        /// <param name="source">Reference Bitmap</param>
        public static void UpdateBuffer(ref WriteableBitmap image, Bitmap source)
        {
            System.Windows.Media.PixelFormat format;
            switch (source.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr24;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    format = System.Windows.Media.PixelFormats.Gray8;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                    format = System.Windows.Media.PixelFormats.Gray16;
                    break;
                default:
                    throw new InvalidOperationException("Pixel format is not acceptable for bitmap converting.");
            }


            // Allocate if necessary
            CreateIfNecessary(ref image, source.Width, source.Height, format);

            // Lock buffers
            BitmapData data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            image.Lock();

            // Copy the bitmap's data directly to the on-screen buffers
            CopyMemory(image.BackBuffer, data.Scan0, source.Height * data.Stride);

            // Moves the back buffer to the front.
            image.AddDirtyRect(new Int32Rect(0, 0, source.Width, source.Height));
            image.Unlock();

            source.UnlockBits(data);
        }

        public static void UpdateBuffer(ref WriteableBitmap image, IntPtr ptrSrc, int width, int height, int bpp)
        {
            var desiredformat = System.Windows.Media.PixelFormats.Gray8;
            switch (bpp)
            {
                case 1:
                    break;
                case 3:
                    desiredformat = System.Windows.Media.PixelFormats.Rgb24;
                    break;
                default:
                    throw new ArgumentException("Unsupported bpp");
            }

            // Allocate if necessary
            CreateIfNecessary(ref image, width, height, desiredformat);

            // Lock buffer
            image.Lock();

            // Copy
            CopyMemory(image.BackBuffer, ptrSrc, width * height * bpp);

            // Moves the back buffer to the front.
            image.AddDirtyRect(new Int32Rect(0, 0, width, height));
            image.Unlock();
        }

        public static void UpdateBuffer(ref WriteableBitmap image, byte[] buffer, int width, int height, int bpp)
        {
            var desiredformat = System.Windows.Media.PixelFormats.Gray8;
            switch (bpp)
            {
                case 1:
                    break;
                case 3:
                    desiredformat = System.Windows.Media.PixelFormats.Rgb24;
                    break;
                default:
                    throw new ArgumentException("Unsupported bpp");
            }

            // Allocate if necessary
            CreateIfNecessary(ref image, width, height, desiredformat);

            // Lock buffer
            image.Lock();

            // Copy
            Marshal.Copy(buffer, 0, image.BackBuffer, width * height * bpp);

            // Moves the back buffer to the front.
            image.AddDirtyRect(new Int32Rect(0, 0, width, height));
            image.Unlock();
        }

        public static unsafe void UpdateBuffer(ref WriteableBitmap image, IntPtr ptrSrc, int width, int height, out float min, out float max)
        {
            var desiredformat = System.Windows.Media.PixelFormats.Gray8;

            // Allocate if necessary
            CreateIfNecessary(ref image, width, height, desiredformat);

            // Lock buffer
            image.Lock();

            // Min - Max
            min = float.MaxValue;
            max = float.MinValue;
            float* ps = (float*)ptrSrc.ToPointer();
            float* endPtrSrc = ps + width * height;
            for (; ps != endPtrSrc; ++ps)
            {
                min = Math.Min(min, *ps);
                max = Math.Max(max, *ps);
            }
            float range = max - min;

            // Copy
            ps = (float*)ptrSrc.ToPointer();
            byte* p = (byte*)image.BackBuffer.ToPointer();
            for (; ps != endPtrSrc; ++p, ++ps)
            {
                *p = (byte)((*ps - min) / (range) * 255f + 0.5f);
            }

            // Moves the back buffer to the front.
            image.AddDirtyRect(new Int32Rect(0, 0, width, height));
            image.Unlock();
        }



        public static unsafe Bitmap ExctractChannel(Bitmap bmp, int channel)
        {
            var res = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);
            var dataRes = res.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, res.PixelFormat);
            byte* pResStart = (byte*)dataRes.Scan0;

            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            int heightBmp = data.Height;
            int widthBmp = data.Width * bpp;

            byte* pStart = (byte*)data.Scan0;

            for (int y = 0; y < heightBmp; y++)
            {
                byte* prow = pStart + (y * data.Stride);
                byte* prowRes = pResStart + (y * dataRes.Stride);
                int xRes = 0;
                for (int x = 0; x < widthBmp; x += bpp, xRes++)
                {
                    byte val = prow[x + channel];

                    *(prowRes + xRes) = val;
                }
            }

            bmp.UnlockBits(data);
            res.UnlockBits(dataRes);

            return res;
        }

        public static unsafe Bitmap ToGrayscale(Bitmap bmp)
        {
            var result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);
            var palette = result.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            result.Palette = palette;
            var dataRes = result.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, result.PixelFormat);
            byte* pResStart = (byte*)dataRes.Scan0;

            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            int heightBmp = data.Height;
            int widthBmp = data.Width * bpp;

            byte* pStart = (byte*)data.Scan0;

            for (int y = 0; y < heightBmp; y++)
            {
                byte* prow = pStart + (y * data.Stride);
                byte* prowRes = pResStart + (y * dataRes.Stride);
                int xRes = 0;
                for (int x = 0; x < widthBmp; x += bpp, xRes++)
                {
                    int b = *(prow +x);
                    int g = *(prow +x + 1);
                    int r = *(prow +x + 2);

                    int val = (int)(r * 0.3 + g * 0.59 + b * 0.1 + 0.5f);
                    val = Math.Max(0, val);
                    val = Math.Min(255, val);
                    *(prowRes + xRes) = (byte)val;
                }
            }

            bmp.UnlockBits(data);
            result.UnlockBits(dataRes);

            return result;
        }

        public static unsafe Bitmap ExtractHue(Bitmap bmp)
        {
            var result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);
            var palette = result.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            result.Palette = palette;
            var dataRes = result.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, result.PixelFormat);
            byte* pResStart = (byte*)dataRes.Scan0;

            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            int heightBmp = data.Height;
            int widthBmp = data.Width * bpp;

            byte* pStart = (byte*)data.Scan0;

            for (int y = 0; y < heightBmp; y++)
            {
                byte* prow = pStart + (y * data.Stride);
                byte* prowRes = pResStart + (y * dataRes.Stride);
                int xRes = 0;
                for (int x = 0; x < widthBmp; x += bpp, xRes++)
                {
                    int b = *(prow + x);
                    int g = *(prow + x + 1);
                    int r = *(prow + x + 2);

                    int val = (int)(Color.FromArgb(255, r, g, b).GetHue() / 360.0 * 255.0 + 0.5);
                    if (val > 255)
                        val = 255;
                    if (val < 0)
                        val = 0;
                    
                    *(prowRes + xRes) = (byte)val;
                }
            }

            bmp.UnlockBits(data);
            result.UnlockBits(dataRes);

            return result;
        }

        public static Bitmap ScaleTo(Bitmap bm, int wid, int hgt, InterpolationMode interpolation_mode)
        {
            Bitmap new_bm = new Bitmap(wid, hgt);
            using (Graphics gr = Graphics.FromImage(new_bm))
            {
                RectangleF source_rect = new RectangleF(-0.5f, -0.5f, bm.Width, bm.Height);
                Rectangle dest_rect = new Rectangle(0, 0, wid, hgt);
                gr.InterpolationMode = interpolation_mode;
                gr.DrawImage(bm, dest_rect, source_rect, GraphicsUnit.Pixel);
            }
            return new_bm;
        }


        public unsafe static Bitmap Blur(Bitmap image, int blurSize)
        {
            Bitmap blurred = null;
            UpdateBuffer(ref blurred, image);
            if (blurSize == 0)
                return blurred;

            // Lock the bitmap's bits
            BitmapData blurredData = blurred.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, blurred.PixelFormat);

            // Get bits per pixel for current PixelFormat
            int bitsPerPixel = Image.GetPixelFormatSize(blurred.PixelFormat);

            // Get pointer to first line
            byte* scan0 = (byte*)blurredData.Scan0.ToPointer();

            // look at every pixel in the blur rectangle
            for (int xx = 0; xx < image.Width; xx++)
            {
                for (int yy = 0; yy < image.Height; yy++)
                {
                    int avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (int x = xx; (x < xx + blurSize && x < image.Width); x++)
                    {
                        for (int y = yy; (y < yy + blurSize && y < image.Height); y++)
                        {
                            // Get pointer to RGB
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            avgB += data[0]; // Blue
                            avgG += data[1]; // Green
                            avgR += data[2]; // Red

                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (int x = xx; x < xx + blurSize && x < image.Width && x < image.Width; x++)
                    {
                        for (int y = yy; y < yy + blurSize && y < image.Height && y < image.Height; y++)
                        {
                            // Get pointer to RGB
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            // Change values
                            data[0] = (byte)avgB;
                            data[1] = (byte)avgG;
                            data[2] = (byte)avgR;
                        }
                    }
                }
            }

            // Unlock the bits
            blurred.UnlockBits(blurredData);

            return blurred;
        }

    }
}
