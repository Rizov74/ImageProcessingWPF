using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.XFeatures2D;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ImageProcessingWPF
{
    public enum ImageStep
    {
        Input,
        Gray,
        Red,
        Green,
        Blue,
        Hue,
        Blur,
        Directions,
        Scale,
        Chromacity,
        Chromacity_red,
        Chromacity_green,
        Histogram_gray,
        Histogram_hue,
        Histogram_red,
        Histogram_green,
        Histogram_blue,
        Histogram_dir,
        Histogram_scale,
        HistoChromacity_red,
        HistoChromacity_green,
        HarrisCorners,

        SIFTKeypoints,
        SIFTResult,
    }

    public class ImageWork
    {
        public Dictionary<ImageStep, Bitmap> Bmps { get; } = new Dictionary<ImageStep, Bitmap>();
        public float[] Histogramm_Gray;
        public float[] Histogramm_hue;
        public float[] Histogramm_red;
        public float[] Histogramm_green;
        public float[] Histogramm_blue;
        public float[] Histogramm_dir;
        public float[] Histogramm_scale;
        public float[] Histogramm_chromR;
        public float[] Histogramm_chromG;
    }

    public class ImageSet : ObservableObject
    {
        public string Path { get => path; set => SetValue(ref path, value); }
        public WriteableBitmap ImageSource1 { get => imageSource1; set => SetValue(ref imageSource1, value); }
        public WriteableBitmap ImageSource2 { get => imageSource2; set => SetValue(ref imageSource2, value); }

        private string path;
        private WriteableBitmap imageSource1;
        private WriteableBitmap imageSource2;


        public ImageSet(string path)
        {
            Path = path;
            

        
        }
    }

    public class MainViewModel : ObservableObject
    {
        public ICommand LoadImage1Command { get; }
        public ICommand LoadImage2Command { get; }

        public ICommand CalcHistoCommand { get; }
        public ICommand HarrisCommand { get; }
        public ICommand SIFTCommand { get; }

        public bool IsAlive { get => isAlive; set => SetValue(ref isAlive, value); }
        public int HistoWidth { get => histoWidth; set => SetValue(ref histoWidth, value); }
        public int HistoHeight { get => histoHeight; set => SetValue(ref histoHeight, value); }
        public int StackHistoCount { get => stackHistoCount; set => SetValue(ref stackHistoCount, value); }


        public WriteableBitmap ImageSource1 { get => imageSource1; protected set => SetValue(ref imageSource1, value); }
        public WriteableBitmap ImageSource2 { get => imageSource2; protected set => SetValue(ref imageSource2, value); }
        public WriteableBitmap ImageResult1 { get => imageResult1; protected set => SetValue(ref imageResult1, value); }
        public WriteableBitmap ImageResult2 { get => imageResult2; protected set => SetValue(ref imageResult2, value); }

        public ImageStep CurImageStep
        {
            get => curImageStep;
            set
            {
                SetValue(ref curImageStep, value);
                RefreshImages();
            }
        }

        public double CoeffHisto { get => coeffHisto; set => SetValue(ref coeffHisto, value); }
        public double CoeffHistoHue { get => coeffHistoHue; set => SetValue(ref coeffHistoHue, value); }
        public double CoeffHistoR { get => coeffHistoR; set => SetValue(ref coeffHistoR, value); }
        public double CoeffHistoG { get => coeffHistoG; set => SetValue(ref coeffHistoG, value); }
        public double CoeffHistoB { get => coeffHistoB; set => SetValue(ref coeffHistoB, value); }
        public double CoeffHistoDir { get => coeffHistoDir; set => SetValue(ref coeffHistoDir, value); }
        public double CoeffHistoScale { get => coeffHistoScale; set => SetValue(ref coeffHistoScale, value); }

        public double CoeffHistoChromR { get => coeffHistoChromR; set => SetValue(ref coeffHistoChromR, value); }
        public double CoeffHistoChromG { get => coeffHistoChromG; set => SetValue(ref coeffHistoChromG, value); }

        public double CoeffHistoMean { get => coeffHistoMean; set => SetValue(ref coeffHistoMean, value); }
        public int BlurSize { get => blurSize; set => SetValue(ref blurSize, value); }
        public int DirectionThreshold { get => directionThreshold; set => SetValue(ref directionThreshold, value); }


        public float HarrisThreshold { get => harrisThreshold; set => SetValue(ref harrisThreshold, value); }
        public int HarrisRadius { get => harrisRadius; set => SetValue(ref harrisRadius, value); }


        public double UniquenessThreshold { get => uniquenessThreshold; set => SetValue(ref uniquenessThreshold, value); }
        public double ScaleIncrement { get => scaleIncrement; set => SetValue(ref scaleIncrement, value); }
        public int RotationBins { get => rotationBins; set => SetValue(ref rotationBins, value); }
        public double RansacReprojThreshold { get => ransacReprojThreshold; set => SetValue(ref ransacReprojThreshold, value); }


        private bool isAlive;
        
        private ImageWork Image1 = new ImageWork();
        private ImageWork Image2 = new ImageWork();

        private WriteableBitmap imageSource1;
        private WriteableBitmap imageSource2;
        private WriteableBitmap imageResult1;
        private WriteableBitmap imageResult2;

        private double coeffHisto;
        private double coeffHistoHue;
        private double coeffHistoR;
        private double coeffHistoG;
        private double coeffHistoB;
        private double coeffHistoDir;
        private double coeffHistoScale;
        private double coeffHistoChromR;
        private double coeffHistoChromG;
        private double coeffHistoMean;

        private int histoWidth = 32;
        private int histoHeight = 32;
        private int stackHistoCount = 10;
        private int blurSize = 2;
        private int directionThreshold = 128;


        double uniquenessThreshold = 0.80;
        double scaleIncrement = 1.5;
        int rotationBins = 2;
        double ransacReprojThreshold = 2;

        private float harrisThreshold = 0.00001f;
        private int harrisRadius = 2;

        private readonly int minTimeBetweenGUIRefresh_ms = 30;
        private DateTime lastGUIRefresh = DateTime.Now;
        private ImageStep curImageStep;

        public MainViewModel()
        {
            LoadImage1Command = new RelayCommand(p => LoadImage1());
            LoadImage2Command = new RelayCommand(p => LoadImage2());
            CalcHistoCommand = new RelayCommandAsync(async p => await CalcHisto());
            HarrisCommand = new RelayCommandAsync(async p => await CalcHarris());
            SIFTCommand = new RelayCommandAsync(async p => await CalcSIFT());

            LoadImage1(@"D:\tmp\Images\RightMove_72519906_Redo\0\input.bmp");
            LoadImage2(@"D:\tmp\Images\RightMove_72519906_Redo\0\0,6572.bmp");

            RefreshImages();

            isAlive = true;
        }

        public void LoadImage1(string path)
        {
            Image1.Bmps[ImageStep.Input] = new Bitmap(path);
            BitmapTools.UpdateBuffer(ref imageSource1, Image1.Bmps[ImageStep.Input]);
            OnPropertyChanged(nameof(imageSource1));
        }

        public void LoadImage2(string path)
        {
            Image2.Bmps[ImageStep.Input] = new Bitmap(path);
            BitmapTools.UpdateBuffer(ref imageSource2, Image2.Bmps[ImageStep.Input]);
            OnPropertyChanged(nameof(imageSource2));
        }


        private void RefreshImages()
        {
            if ((DateTime.Now - lastGUIRefresh).TotalMilliseconds > minTimeBetweenGUIRefresh_ms)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Image1.Bmps.ContainsKey(CurImageStep))
                        BitmapTools.UpdateBuffer(ref imageResult1, Image1.Bmps[CurImageStep]);
                    else
                        ImageResult1 = null;
                    OnPropertyChanged(nameof(ImageResult1));

                    if (Image2.Bmps.ContainsKey(CurImageStep))
                        BitmapTools.UpdateBuffer(ref imageResult2, Image2.Bmps[CurImageStep]);
                    else
                        ImageResult2 = null;
                    OnPropertyChanged(nameof(ImageResult2));
                });
                lastGUIRefresh = DateTime.Now;
            }
        }

        private void LoadImage1()
        {
            var d = new OpenFileDialog()
            {
                Filter = "Bitmap (*.bmp)|*.bmp",
                InitialDirectory = "D:\\tmp"
            };
            if (d.ShowDialog() != true)
                return;

            LoadImage1(d.FileName);
        }
        private void LoadImage2()
        {
            var d = new OpenFileDialog()
            {
                Filter = "Bitmap (*.bmp)|*.bmp",
                InitialDirectory = "D:\\tmp"
            };
            if (d.ShowDialog() != true)
                return;

            LoadImage2(d.FileName);
        }

        private async Task CalcHisto()
        {
            try
            {
                IsAlive = false;

                await Task.Run(() =>
                {
                    CalcHisto(Image1);
                    CalcHisto(Image2);

                    CoeffHisto = ComputeCoeff(Image1.Histogramm_Gray, Image2.Histogramm_Gray);
                    CoeffHistoHue = ComputeCoeff(Image1.Histogramm_hue, Image2.Histogramm_hue);
                    CoeffHistoR = ComputeCoeff(Image1.Histogramm_red, Image2.Histogramm_red);
                    CoeffHistoG = ComputeCoeff(Image1.Histogramm_green, Image2.Histogramm_green);
                    CoeffHistoB = ComputeCoeff(Image1.Histogramm_blue, Image2.Histogramm_blue);
                    CoeffHistoDir = ComputeCoeff(Image1.Histogramm_dir, Image2.Histogramm_dir, true);

                    CoeffHistoChromR = ComputeCoeff(Image1.Histogramm_chromR, Image2.Histogramm_chromR);
                    CoeffHistoChromG = ComputeCoeff(Image1.Histogramm_chromG, Image2.Histogramm_chromG);

                    CoeffHistoMean = (CoeffHistoR + CoeffHistoG + CoeffHistoB + CoeffHistoDir + CoeffHistoChromR + CoeffHistoChromG) / 6;



                    //CurImageStep = ImageStep.Histogram;
                    RefreshImages();

                });
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsAlive = true;
            }
        }

        private async Task CalcHarris()
        {
            try
            {
                IsAlive = false;

                await Task.Run(() =>
                {
                    if (!Image1.Bmps.ContainsKey(ImageStep.Gray))
                        Image1.Bmps[ImageStep.Gray] = BitmapTools.ToGrayscale(Image1.Bmps[ImageStep.Input]);
                    var src = Image1.Bmps[ImageStep.Gray];
                    Image1.Bmps[ImageStep.Hue] = src;
                    var corners = FindCorners(src);
                    Image1.Bmps[ImageStep.HarrisCorners] = DrawCorners(corners, src.Width, src.Height);

                    var src2 = BitmapTools.ExtractHue(Image2.Bmps[ImageStep.Input]);
                    Image2.Bmps[ImageStep.Hue] = src2;
                    var corners2 = FindCorners(src2);
                    Image2.Bmps[ImageStep.HarrisCorners] = DrawCorners(corners2, src2.Width, src2.Height);

                    CurImageStep = ImageStep.HarrisCorners;
                });
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsAlive = true;
            }
        }

        private async Task CalcSIFT()
        {
            try
            {
                IsAlive = false;
                
                await Task.Run(() =>
                {
                    Image<Bgr, byte> image1CV = Image1.Bmps[ImageStep.Input].ToImage<Bgr, byte>();
                    Mat mat1 = image1CV.Mat;

                    Mat imageDescriptors1 = new Mat();
                    VectorOfKeyPoint imageKeyPoints1 = new VectorOfKeyPoint();
                    Image1.Bmps[ImageStep.SIFTKeypoints] = CalculateDescriptors(mat1, imageDescriptors1, imageKeyPoints1);

                    Image<Bgr, byte> image2CV = Image2.Bmps[ImageStep.Input].ToImage<Bgr, byte>();
                    Mat mat2 = image2CV.Mat;

                    Mat imageDescriptors2 = new Mat();
                    VectorOfKeyPoint imageKeyPoints2 = new VectorOfKeyPoint();
                    Image2.Bmps[ImageStep.SIFTKeypoints] = CalculateDescriptors(mat2, imageDescriptors2, imageKeyPoints2);

                    VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
                    using Emgu.CV.Flann.LinearIndexParams ip = new Emgu.CV.Flann.LinearIndexParams();
                    using Emgu.CV.Flann.SearchParams sp = new Emgu.CV.Flann.SearchParams();
                    using FlannBasedMatcher flann = new FlannBasedMatcher(ip, sp);
                    flann.Add(imageDescriptors1);
                    flann.KnnMatch(imageDescriptors2, matches, 2);

                    Image<Bgr, Byte> result = new Image<Bgr, byte>(mat1.Width + mat2.Width, Math.Max(mat1.Height, mat2.Height));


                    Mat mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, UniquenessThreshold, mask);

                    Features2DToolbox.DrawMatches(mat1, imageKeyPoints1, mat2, imageKeyPoints2, matches, result, new MCvScalar(255, 255, 0), new MCvScalar(0, 255, 255), mask);
                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(imageKeyPoints1, imageKeyPoints2, matches, mask, ScaleIncrement, RotationBins);
                        if (nonZeroCount >= 4)
                        {
                            Mat homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(imageKeyPoints1, imageKeyPoints2, matches, mask, RansacReprojThreshold);
                            Image2.Bmps[ImageStep.SIFTResult] = homography.ToBitmap();


                            //draw a rectangle along the projected model
                            Rectangle rect = new Rectangle(Point.Empty, mat1.Size);
                            PointF[] pts = new PointF[]
                            {
                                 new PointF(rect.Left, rect.Bottom),
                                 new PointF(rect.Right, rect.Bottom),
                                 new PointF(rect.Right, rect.Top),
                                 new PointF(rect.Left, rect.Top)
                            };
                            pts = CvInvoke.PerspectiveTransform(pts, homography);
                            Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                            
                            using VectorOfPoint vp = new VectorOfPoint(points);
                            CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 255, 0, 0), 5);
                        }
                    }
                    Image1.Bmps[ImageStep.SIFTResult] = result.ToBitmap();

                    CurImageStep = ImageStep.SIFTResult;
                });
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsAlive = true;
            }
        }

        private void CalcHisto(ImageWork img)
        {
            var src = img.Bmps[ImageStep.Input];

            var blured = BitmapTools.Blur(src, BlurSize);
            img.Bmps[ImageStep.Blur] = blured;

            var gray = BitmapTools.ToGrayscale(blured);
            img.Histogramm_Gray = GetHistogram(gray);
            img.Bmps[ImageStep.Gray] = gray;
            img.Bmps[ImageStep.Histogram_gray] = CreateHistoImage(img.Histogramm_Gray, HistoWidth, HistoHeight, StackHistoCount);

            var hue = BitmapTools.ExtractHue(blured);
            img.Histogramm_hue = GetHistogram(hue);
            img.Bmps[ImageStep.Hue] = hue;
            img.Bmps[ImageStep.Histogram_hue] = CreateHistoImage(img.Histogramm_hue, HistoWidth, HistoHeight, StackHistoCount);
            

            var red = BitmapTools.ExctractChannel(blured, 0);
            img.Histogramm_red = GetHistogram(red);
            img.Bmps[ImageStep.Red] = red;
            img.Bmps[ImageStep.Histogram_red] = CreateHistoImage(img.Histogramm_red, HistoWidth, HistoHeight, StackHistoCount);

            var green = BitmapTools.ExctractChannel(blured, 1);
            img.Histogramm_green = GetHistogram(green);
            img.Bmps[ImageStep.Green] = green;
            img.Bmps[ImageStep.Histogram_green] = CreateHistoImage(img.Histogramm_green, HistoWidth, HistoHeight, StackHistoCount);

            var blue = BitmapTools.ExctractChannel(blured, 2);
            img.Histogramm_blue = GetHistogram(blue);
            img.Bmps[ImageStep.Blue] = blue;
            img.Bmps[ImageStep.Histogram_blue] = CreateHistoImage(img.Histogramm_blue, HistoWidth, HistoHeight, StackHistoCount);



            var dir = GetDirectionImage(src);
            img.Bmps[ImageStep.Directions] = dir;
            img.Histogramm_dir = GetHistogram(BitmapTools.ToGrayscale(dir), true);
            img.Histogramm_dir[0] = 0;
            img.Bmps[ImageStep.Histogram_dir] = CreateHistoImage(img.Histogramm_dir, HistoWidth, HistoHeight, 4);


            var chrom = GetChromacity(src);
            img.Bmps[ImageStep.Chromacity] = chrom;

            var chromR = BitmapTools.ExctractChannel(chrom, 0);
            img.Histogramm_chromR = GetHistogram(chromR);
            img.Bmps[ImageStep.Chromacity_red] = chromR;
            img.Bmps[ImageStep.HistoChromacity_red] = CreateHistoImage(img.Histogramm_chromR, HistoWidth, HistoHeight, StackHistoCount);

            var chromG = BitmapTools.ExctractChannel(chrom, 1);
            img.Histogramm_chromG = GetHistogram(chromG);
            img.Bmps[ImageStep.Chromacity_green] = chromG;
            img.Bmps[ImageStep.HistoChromacity_green] = CreateHistoImage(img.Histogramm_chromG, HistoWidth, HistoHeight, StackHistoCount);
        }

        private double ComputeCoeff(float[] values1, float[] values2, bool ignore0 = false)
        {
            // Magic limit : 0.7
            if (values1.Length != values2.Length)
                throw new ArgumentException("values must be the same length");

            List<float> v1 = new List<float>();
            List<float> v2 = new List<float>();
            if (ignore0)
            {
                for (int i = 0; i < values1.Length; i++)
                {
                    if (values1[i] == 0 && values2[i] == 0)
                        continue;
                    v1.Add(values1[i]);
                    v2.Add(values2[i]);
                }
            }
            else
            {
                v1.AddRange(values1);
                v2.AddRange(values2);
            }

            if (!v1.Any() || !v2.Any())
                return -1;

            var avg1 = v1.Average();
            var avg2 = v2.Average();

            var sum1 = v1.Zip(v2, (x1, y1) => (x1 - avg1) * (y1 - avg2)).Sum();

            var sumSqr1 = v1.Sum(x => Math.Pow((x - avg1), 2.0));
            var sumSqr2 = v2.Sum(y => Math.Pow((y - avg2), 2.0));

            var result = sum1 / Math.Sqrt(sumSqr1 * sumSqr2);

            return result;
        }


        private float[] GetHistogram(Bitmap bmp, bool ignoreBlack = false)
        {
            if (bmp == null) return null;
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                return null;

            float[] hist = new float[256];

            int cnt = 0;

            unsafe
            {
                fixed (float* pHisto = hist)
                {
                    var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                    int bpp = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                    int heightBmp = data.Height;
                    int widthBmp = data.Width;
                    byte* pStart = (byte*)data.Scan0;

                    for (int y = 0; y < heightBmp; y++)
                    {
                        byte* prow = pStart + (y * data.Stride);
                        for (int x = 0; x < widthBmp; x++)
                        {
                            double value = 0;
                            value = prow[x];

                            if (value > 255) value = 255;
                            if (value < 0) value = 0;

                            if (value != 0)
                                cnt++;

                            * (pHisto + (int)(value + 0.5)) += 1;
                        }
                    }

                    bmp.UnlockBits(data);


                    if (ignoreBlack && cnt != 0)
                    {
                        for(float * ps = pHisto, pe = pHisto + 256; ps != pe; ++ps)
                            *ps /= (cnt);
                    }
                    else
                    {
                        for (float* ps = pHisto, pe = pHisto + 256; ps != pe; ++ps)
                            *ps /= (data.Height * data.Width);
                    }

                }
            }

            return hist;
        }

        private unsafe Bitmap CreateHistoImage(float[] histo, int width, int height, int stackCount)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            byte* pd = (byte*)data.Scan0;

            int stackRange = (int)((histo.Length / (float)stackCount));

            fixed (float* pHisto = histo)
            {
                int rowByStack = (int)(height / (float)stackCount);
                float* ps = pHisto;
                byte* pRow = pd;
                for (int s = 0; s < stackCount; ++s)
                {
                    // Get stack average
                    double sum = 0;
                    for (int i = 0; i < stackRange; i++, ps++)
                        sum += *ps;
                    //double val = (sum / stackRange);

                    int colCnt = (int)(width * sum);
                    // Fill row
                    for (int i = 0; i < rowByStack; i++, pRow += width)
                    {
                        // Fill col till val
                        for (byte* pc = pRow, pcEnd = pRow + colCnt; pc != pcEnd; ++pc)
                        {
                            *pc = 255;
                        }
                    }
                    
                }

            }
            bmp.UnlockBits(data);
            return bmp;
        }

        private void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        private unsafe Bitmap GetDirectionImage(Bitmap src)
        {
            var res = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            var dataRes = res.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadWrite, res.PixelFormat);

            var data = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, src.PixelFormat);
            int bpp = Image.GetPixelFormatSize(src.PixelFormat) / 8;


            byte* pSrc = (byte*)data.Scan0;
            byte* pRes = (byte*)dataRes.Scan0;

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++, pRes += 3)
                {
                    int max = DirectionThreshold;

                    // 0°
                    if (x != 0 && x != src.Width - 1)
                    {
                        var value = Math.Abs(*(pSrc + ((y * data.Width) + (x - 1)) * bpp) - *(pSrc + (y * data.Width + (x + 1)) * bpp));
                        if (value > max)
                        {
                            max = value;
                            //*pRes = 63;
                            *(pRes + 0) = 255;
                            *(pRes + 1) = 0;
                            *(pRes + 2) = 0;
                        }
                    }

                    // 45°
                    if (x != 0 && x != src.Width - 1 && y != 0 && y != src.Height - 1)
                    {
                        var value = Math.Abs(*(pSrc + ((y - 1) * data.Width + (x + 1)) * bpp) - *(pSrc + ((y + 1) * data.Width + (x - 1)) * bpp));
                        if (value > max)
                        {
                            max = value;
                            //*pRes = 127;
                            *(pRes + 0) = 255;
                            *(pRes + 1) = 0;
                            *(pRes + 2) = 255;
                        }
                    }

                    // 90°
                    if (y != 0 && y != src.Height - 1)
                    {
                        var value = Math.Abs(*(pSrc + ((y - 1) * data.Width + x) * bpp) - *(pSrc + ((y + 1) * data.Width + x) * bpp));
                        if (value > max)
                        {
                            max = value;
                            //*pRes = 191;
                            *(pRes + 0) = 0;
                            *(pRes + 1) = 255;
                            *(pRes + 2) = 0;
                        }
                    }

                    // 135°
                    if (x != 0 && x != src.Width - 1 && y != 0 && y != src.Height - 1)
                    {
                        var value = Math.Abs(*(pSrc + ((y - 1) * data.Width + (x - 1)) * bpp) - *(pSrc + ((y + 1) * data.Width + (x + 1)) * bpp));
                        if (value > max)
                        {
                            max = value;
                            //*pRes = 255;
                            *(pRes + 0) = 0;
                            *(pRes + 1) = 255;
                            *(pRes + 2) = 255;
                        }
                    }

                }
            }

            src.UnlockBits(data);
            res.UnlockBits(dataRes);


            return res;
        }

        private unsafe Bitmap GetChromacity(Bitmap src)
        {
            var res = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            var dataRes = res.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadWrite, res.PixelFormat);

            var data = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, src.PixelFormat);
            int bpp = Image.GetPixelFormatSize(src.PixelFormat) / 8;

            byte* pSrc = (byte*)data.Scan0;
            byte* pSrcEnd = pSrc + src.Height * src.Width * bpp;
            byte* pRes = (byte*)dataRes.Scan0;

            for ( ; pSrc != pSrcEnd; pSrc += bpp, pRes += 3)
            {
                byte R = *(pSrc + 0);
                byte G = *(pSrc + 1);
                byte B = *(pSrc + 2);

                *(pRes + 0) = (byte)(R / (float)(R + G + B) * 255);
                *(pRes + 1) = (byte)(G / (float)(R + G + B) * 255);
            }

            src.UnlockBits(data);
            res.UnlockBits(dataRes);


            return res;
        }

        private void Gradient5x5(Bitmap src)
        {

        }



        private unsafe IEnumerable<Point> FindCorners(Bitmap bmp)
        {
            List<Point> results = new List<Point>();

            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException("Grayscal only");

            // Sobel

            int width = bmp.Width;
            int height = bmp.Height;
            int size = width * height;

            float[] diffx = new float[size];
            float[] diffy = new float[size];
            float[] diffxy = new float[size];

            var srcData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            byte* pStart = (byte*)srcData.Scan0;

            fixed (float* pdx = diffx, pdy = diffy, pdxy = diffxy)
            {
                // Skip first row and first column
                float* dx = pdx + width + 1;
                float* dy = pdy + width + 1;
                float* dxy = pdxy + width + 1;
                byte* p = pStart + width + 1;

                // Ignore first and last rows / columns
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++, p++, dx++, dy++, dxy++)
                    {
                        // Convolution with horizontal differentiation kernel mask (right window column - left column) * 0.17
                        float h = (*(p - width + 1) + *(p + 1) + *(p + width + 1) - (*(p - width - 1) + *(p - 1) + *(p + width - 1))) * 0.166666667f;

                        // Convolution with vertical differentiation kernel mask (bot row - top row) * 0.17
                        float v = (*(p + width - 1) + *(p + width) + *(p + width + 1) - (*(p - width - 1) + *(p - width) + *(p - width + 1))) * 0.166666667f;

                        // Store squared differences directly
                        *dx = h * h;
                        *dy = v * v;
                        *dxy = h * v;
                    }

                    // Skip last column
                    dx++; dy++; dxy++;
                    p++;
                }
            }
            bmp.UnlockBits(srcData);

            // 3. Compute Harris Corner Response Map
            float[] map = new float[size];
            fixed (float* pdx = diffx, pdy = diffy, pdxy = diffxy, pmap = map)
            {
                float* dx = pdx;
                float* dy = pdy;
                float* dxy = pdxy;
                float* H = pmap;
                float M, A, B, C;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, dx++, dy++, dxy++, H++)
                    {
                        A = *dx;
                        B = *dy;
                        C = *dxy;

                        // Original Harris corner measure
                        //M = (A * B - C * C) - (0.5f * ((A + B) * (A + B)));
                        
                        // Harris-Noble corner measure
                        M = (A * B - C * C) / (A + B + float.Epsilon);

                        // insert value in the map
                        if (M > HarrisThreshold)
                            *H = M; 
                    }
                }
            }

            int r = HarrisRadius;
            // for each row
            for (int y = r, maxY = height - r; y < maxY; y++)
            {
                // for each pixel
                for (int x = r, maxX = width - r; x < maxX; x++)
                {
                    float currentValue = map[y * width + x];

                    // for each windows' row
                    for (int i = -r; (currentValue != 0) && (i <= r); i++)
                    {
                        // for each windows' pixel
                        for (int j = -r; j <= r; j++)
                        {
                            if (map[(y + i) * width + x + j] > currentValue)
                            {
                                currentValue = 0;
                                break;
                            }
                        }
                    }

                    // check if this point is really interesting
                    if (currentValue != 0)
                    {
                        results.Add(new Point(x, y));
                    }
                }
            }


            return results;
        }

        private unsafe Bitmap DrawCorners(IEnumerable<Point> corners, int width, int height)
        {
            var result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            var data = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, result.PixelFormat);
            byte* pStart = (byte*)data.Scan0;

            foreach (var p in corners)
            {
                *(pStart + p.Y * width + p.X) = 255;
            }
            result.UnlockBits(data);
            return result;
        }


        private Bitmap CalculateDescriptors(Mat image, Mat imageDescriptors = null, VectorOfKeyPoint imageKeyPoints = null)
        {
            if (imageDescriptors == null)
                imageDescriptors = new Mat();
            if (imageKeyPoints == null)
                imageKeyPoints = new VectorOfKeyPoint();

            using SIFT sift = new SIFT();
            imageKeyPoints.Push(sift.Detect(image));
            sift.DetectAndCompute(image, null, imageKeyPoints, imageDescriptors, false);

            Image<Bgr, Byte> result = new Image<Bgr, byte>(image.Width, image.Height);
            Features2DToolbox.DrawKeypoints(image, imageKeyPoints, result, new Bgr(Color.Red));
            return result.ToBitmap();
        }

    }
}
