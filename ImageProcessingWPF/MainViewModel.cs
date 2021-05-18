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
        public ICommand SIFTCommand { get; }

        public bool IsAlive { get => isAlive; set => SetValue(ref isAlive, value); }

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

        public double UniquenessThreshold { get => uniquenessThreshold; set => SetValue(ref uniquenessThreshold, value); }
        public double ScaleIncrement { get => scaleIncrement; set => SetValue(ref scaleIncrement, value); }
        public int RotationBins { get => rotationBins; set => SetValue(ref rotationBins, value); }
        public double RansacReprojThreshold { get => ransacReprojThreshold; set => SetValue(ref ransacReprojThreshold, value); }

        public double? Hom22 { get => hom22; set => SetValue(ref hom22, value); }
        public double? Hom21 { get => hom21; set => SetValue(ref hom21, value); }
        public double? Hom20 { get => hom20; set => SetValue(ref hom20, value); }
        public double? Hom12 { get => hom12; set => SetValue(ref hom12, value); }
        public double? Hom11 { get => hom11; set => SetValue(ref hom11, value); }
        public double? Hom10 { get => hom10; set => SetValue(ref hom10, value); }
        public double? Hom02 { get => hom02; set => SetValue(ref hom02, value); }
        public double? Hom01 { get => hom01; set => SetValue(ref hom01, value); }
        public double? Hom00 { get => hom00; set => SetValue(ref hom00, value); }


        // Work data
        private readonly ImageWork Image1 = new ImageWork();
        private readonly ImageWork Image2 = new ImageWork();

        // GUI
        private bool isAlive;
        private readonly int minTimeBetweenGUIRefresh_ms = 30;
        private DateTime lastGUIRefresh = DateTime.Now;
        private ImageStep curImageStep;
        private WriteableBitmap imageSource1;
        private WriteableBitmap imageSource2;
        private WriteableBitmap imageResult1;
        private WriteableBitmap imageResult2;

        // SIFT Params
        private double uniquenessThreshold = 0.80;
        private double scaleIncrement = 1.5;
        private int rotationBins = 2;
        private double ransacReprojThreshold = 2;

        // SIFT Results
        private double? hom00;
        private double? hom01;
        private double? hom02;
        private double? hom10;
        private double? hom11;
        private double? hom12;
        private double? hom20;
        private double? hom21;
        private double? hom22;
                      
        public MainViewModel()
        {
            LoadImage1Command = new RelayCommand(p => LoadImage1());
            LoadImage2Command = new RelayCommand(p => LoadImage2());
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

                    // Set same image height to minimise offset
                    Image<Bgr, byte> image2CV = Image2.Bmps[ImageStep.Input].ToImage<Bgr, byte>();
                    if (image2CV.Height != image1CV.Height)
                    {
                        image2CV = image2CV.Resize((double)image1CV.Height / (double)image2CV.Height, Inter.Nearest);
                    }
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
                            if (homography != null)
                            {
                                var array = homography.GetData();
                                Hom00 = homography.GetValue(0, 0);
                                Hom01 = homography.GetValue(0, 1);
                                Hom02 = homography.GetValue(0, 2);
                                Hom10 = homography.GetValue(1, 0);
                                Hom11 = homography.GetValue(1, 1);
                                Hom12 = homography.GetValue(1, 2);
                                Hom20 = homography.GetValue(2, 0);
                                Hom21 = homography.GetValue(2, 1);
                                Hom22 = homography.GetValue(2, 2);
                            }
                            else
                            {
                                Hom00 = Hom01 = Hom02 = Hom10 = Hom11 = Hom12 = Hom20 = Hom21 = Hom22 = null;
                            }

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
                            CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
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
