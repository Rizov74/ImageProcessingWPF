using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessingWPF.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ImageProcessingWPF.ViewModels
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
        SIFTGoodResult
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


    public class ImageComparatorViewModel : ObservableObject
    {
        // Commands
        public ICommand LoadImage1Command { get; }
        public ICommand LoadImage2Command { get; }
        public ICommand SwapImageCommand { get; }
        public ICommand LoadImageSet1Command { get; }
        public ICommand LoadImageSet2Command { get; }

        public ICommand SIFTCommand { get; }
        public ICommand HistoCompareCommand { get; }


        // Work data
        private string selectedImage1;
        private string selectedImage2;
        private readonly ImageWork Image1 = new ImageWork();
        private readonly ImageWork Image2 = new ImageWork();
        public string SelectedImage1
        {
            get => selectedImage1;
            set
            {
                if (SetValue(ref selectedImage1, value) && SelectedImage1 != null)
                    LoadImage1(SelectedImage1);
            }
        }
        public string SelectedImage2
        {
            get => selectedImage2;
            set
            {
                if (SetValue(ref selectedImage2, value) && SelectedImage2 != null)
                    LoadImage2(SelectedImage2);
            }
        }
        public ObservableCollection<string> ImageSet1 { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ImageSet2 { get; } = new ObservableCollection<string>();



        // GUI
        private bool isAlive;
        private readonly int minTimeBetweenGUIRefresh_ms = 30;
        private DateTime lastGUIRefresh = DateTime.MinValue;
        private ImageStep curImageStep;
        private WriteableBitmap imageSource1;
        private WriteableBitmap imageSource2;
        private WriteableBitmap imageResult1;
        private WriteableBitmap imageResult2;

        public bool IsAlive { get => isAlive; set => SetValue(ref isAlive, value); }
        public ImageStep CurImageStep
        {
            get => curImageStep;
            set
            {
                SetValue(ref curImageStep, value);
                RefreshImages();
            }
        }
        public WriteableBitmap ImageSource1 { get => imageSource1; protected set => SetValue(ref imageSource1, value); }
        public WriteableBitmap ImageSource2 { get => imageSource2; protected set => SetValue(ref imageSource2, value); }
        public WriteableBitmap ImageResult1 { get => imageResult1; protected set => SetValue(ref imageResult1, value); }
        public WriteableBitmap ImageResult2 { get => imageResult2; protected set => SetValue(ref imageResult2, value); }



        // SIFT Params
        private float siftMaskRatioBorder = 0.125f; // 1/8
        private float siftMaskRatioCenter = 0.75f; // 6/8
        private bool showSiftMask;
        private double uniquenessThreshold = 0.80;
        private double scaleIncrement = 1.5;
        private int rotationBins = 2;
        private double ransacReprojThreshold = 2;
        private int siftMatchCnt = 2;

        public float SiftMaskRatioBorder
        {
            get => siftMaskRatioBorder;
            set
            {
                SetValue(ref siftMaskRatioBorder, value);
                RefreshSiftMask();
            }
        }
        public float SiftMaskRatioCenter
        {
            get => siftMaskRatioCenter;
            set
            {
                SetValue(ref siftMaskRatioCenter, value);
                RefreshSiftMask();
            }
        }
        public bool ShowSiftMask
        {
            get => showSiftMask;
            set
            {
                SetValue(ref showSiftMask, value);
                RefreshSiftMask();
                OnPropertyChanged(nameof(ImageSource1));
            }
        }
        public double UniquenessThreshold { get => uniquenessThreshold; set => SetValue(ref uniquenessThreshold, value); }
        public double ScaleIncrement { get => scaleIncrement; set => SetValue(ref scaleIncrement, value); }
        public int RotationBins { get => rotationBins; set => SetValue(ref rotationBins, value); }
        public double RansacReprojThreshold { get => ransacReprojThreshold; set => SetValue(ref ransacReprojThreshold, value); }
        public int SiftMatchCnt { get => siftMatchCnt; set => SetValue(ref siftMatchCnt, value); }


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
        private float? siftMinDistance;
        private float? siftMaxDistance;
        private int? siftGoodResultCnt;
        private float? siftRMSE;
        private float? siftGoodResultRatio;

        public double? Hom22 { get => hom22; set => SetValue(ref hom22, value); }
        public double? Hom21 { get => hom21; set => SetValue(ref hom21, value); }
        public double? Hom20 { get => hom20; set => SetValue(ref hom20, value); }
        public double? Hom12 { get => hom12; set => SetValue(ref hom12, value); }
        public double? Hom11 { get => hom11; set => SetValue(ref hom11, value); }
        public double? Hom10 { get => hom10; set => SetValue(ref hom10, value); }
        public double? Hom02 { get => hom02; set => SetValue(ref hom02, value); }
        public double? Hom01 { get => hom01; set => SetValue(ref hom01, value); }
        public double? Hom00 { get => hom00; set => SetValue(ref hom00, value); }
        public float? SiftGoodResultRatio { get => siftGoodResultRatio; set => SetValue(ref siftGoodResultRatio, value); }
        public float? SiftMinDistance { get => siftMinDistance; set => SetValue(ref siftMinDistance, value); }
        public float? SiftMaxDistance { get => siftMaxDistance; set => SetValue(ref siftMaxDistance, value); }
        public float? SiftRMSE { get => siftRMSE; set => SetValue(ref siftRMSE, value); }
        public int? SiftGoodResultCnt { get => siftGoodResultCnt; set => SetValue(ref siftGoodResultCnt, value); }


        // Histo params
        private int histoWidth = 32;
        private int histoHeight = 32;
        private int stackHistoCount = 10;
        private int blurSize = 2;
        private int directionThreshold = 128;
        public int HistoWidth { get => histoWidth; set => SetValue(ref histoWidth, value); }
        public int HistoHeight { get => histoHeight; set => SetValue(ref histoHeight, value); }
        public int StackHistoCount { get => stackHistoCount; set => SetValue(ref stackHistoCount, value); }
        public int BlurSize { get => blurSize; set => SetValue(ref blurSize, value); }
        public int DirectionThreshold { get => directionThreshold; set => SetValue(ref directionThreshold, value); }

        // Histo Results
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

        public ImageComparatorViewModel()
        {
            LoadImage1Command = new RelayCommand(p => LoadImage1());
            LoadImage2Command = new RelayCommand(p => LoadImage2());
            SwapImageCommand = new RelayCommand(p => SwapImages());
            LoadImageSet1Command = new RelayCommand(p => LoadImageSet1());
            LoadImageSet2Command = new RelayCommand(p => LoadImageSet2());
            SIFTCommand = new RelayCommandAsync(async p => await CalcSIFT());
            HistoCompareCommand = new RelayCommandAsync(async p => await CalcHisto());

            // Default images loading
            LoadImage1(@"..\..\..\ImageToPlay\AngleDifferent2\0,7974.bmp");
            LoadImage2(@"..\..\..\ImageToPlay\AngleDifferent2\0,9962.bmp");

            RefreshImages();

            isAlive = true;
        }

        public void LoadImage1(string path)
        {
            if (File.Exists(path))
            {
                SetImageSource1(new Bitmap(path));
            }
        }
        public void LoadImage2(string path)
        {
            if (File.Exists(path))
            {
                SetImageSource2(new Bitmap(path));
            }
        }

        private void RefreshImages()
        {
            if ((DateTime.Now - lastGUIRefresh).TotalMilliseconds > minTimeBetweenGUIRefresh_ms)
            {
                lastGUIRefresh = DateTime.Now;
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
            }
        }

        private void LoadImage1()
        {
            var dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            dir = Path.Combine(dir, @"ImageToPlay\");
            var d = new OpenFileDialog()
            {
                Filter = "Bitmap (*.bmp)|*.bmp|All files (*.*)|*.*",
                InitialDirectory = dir
            };
            if (d.ShowDialog() != true)
                return;

            LoadImage1(d.FileName);
        }

        private void LoadImage2()
        {
            var dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            dir = Path.Combine(dir, @"ImageToPlay\");
            var d = new OpenFileDialog()
            {
                Filter = "Bitmap (*.bmp)|*.bmp|All files (*.*)|*.*",
                InitialDirectory = dir
            };
            if (d.ShowDialog() != true)
                return;

            LoadImage2(d.FileName);
        }

        private void LoadImageSet1()
        {
            var dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            dir = Path.Combine(dir, @"ImageToPlay\");
            var d = new FolderBrowserDialog()
            {
                ShowNewFolderButton = false,
                Description = "Select image directory for left",
                RootFolder = Environment.SpecialFolder.ApplicationData
            };
            if (d.ShowDialog() != DialogResult.OK)
                return;

            ImageSet1.Clear();
            SelectedImage1 = null;

            foreach (var p in Directory.GetFiles(d.SelectedPath, "*.bmp"))
                ImageSet1.Add(p);

            foreach (var p in Directory.GetFiles(d.SelectedPath, "*.jpg"))
                ImageSet1.Add(p);

            foreach (var p in Directory.GetFiles(d.SelectedPath, "*.jpeg"))
                ImageSet1.Add(p);

            if (ImageSet1.Count != 0)
                SelectedImage1 = ImageSet1[0];
        }

        private void LoadImageSet2()
        {
            var dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            dir = Path.Combine(dir, @"ImageToPlay\");
            var d = new FolderBrowserDialog()
            {
                ShowNewFolderButton = false,
                Description = "Select image directory for left",
                RootFolder = Environment.SpecialFolder.ApplicationData
            };
            if (d.ShowDialog() != DialogResult.OK)
                return;

            ImageSet2.Clear();
            SelectedImage2 = null;

            foreach (var p in Directory.GetFiles(d.SelectedPath, "*.bmp"))
                ImageSet2.Add(p);

            foreach (var p in Directory.GetFiles(d.SelectedPath, "*.jpg"))
                ImageSet2.Add(p);

            foreach (var p in Directory.GetFiles(d.SelectedPath, "*.jpeg"))
                ImageSet2.Add(p);

            if (ImageSet2.Count != 0)
                SelectedImage2 = ImageSet2[0];
        }

        private void SwapImages()
        {
            var img1 = Image1.Bmps[ImageStep.Input];
            var img2 = Image2.Bmps[ImageStep.Input];
            if (img2 != null)
            {
                SetImageSource1(img2);
            }

            if (img1 != null)
            {
                SetImageSource2(img1);
            }
        }


        private void SetImageSource1(Bitmap bmp)
        {
            Image1.Bmps[ImageStep.Input] = bmp;

            if (ShowSiftMask)
                RefreshSiftMask();
            else
            {
                BitmapTools.UpdateBuffer(ref imageSource1, Image1.Bmps[ImageStep.Input]);
                OnPropertyChanged(nameof(ImageSource1));
            }
        }

        private void SetImageSource2(Bitmap bmp)
        {
            Image2.Bmps[ImageStep.Input] = bmp;
            if (ShowSiftMask)
                RefreshSiftMask();
            else
            {
                BitmapTools.UpdateBuffer(ref imageSource2, Image2.Bmps[ImageStep.Input]);
                OnPropertyChanged(nameof(ImageSource2));
            }
        }

        private void RefreshSiftMask()
        {
            BitmapTools.UpdateBuffer(ref imageSource1, Image1.Bmps[ImageStep.Input]);
            if (ShowSiftMask)
                DisplaySiftMask(imageSource1);

            BitmapTools.UpdateBuffer(ref imageSource2, Image2.Bmps[ImageStep.Input]);
            if (ShowSiftMask)
                DisplaySiftMask(imageSource2);

            OnPropertyChanged(nameof(ImageSource1));
            OnPropertyChanged(nameof(ImageSource2));
        }

        private unsafe void DisplaySiftMask(WriteableBitmap imageSource)
        {
            imageSource.Lock();



            byte* pResStart = (byte*)imageSource.BackBuffer;

            int maskX = (int)(imageSource.PixelWidth * SiftMaskRatioBorder);
            int maskWidth = (int)(imageSource.PixelWidth * SiftMaskRatioCenter);
            int maskY = (int)(imageSource.PixelHeight * SiftMaskRatioBorder);
            int maskHeight = (int)(imageSource.PixelHeight * SiftMaskRatioCenter);
            int maskXStart = Math.Max(0, maskX);
            int maskYStart = Math.Max(0, maskY);
            int maskXEnd = Math.Min(maskX + maskWidth, imageSource.PixelWidth);
            int maskYEnd = Math.Min(maskY + maskHeight, imageSource.PixelHeight);
            int bpp = imageSource.Format.BitsPerPixel / 8;


            for (int j = 0; j < maskYStart; j++)
            {
                byte* pRow = pResStart + j * imageSource.PixelWidth * bpp;
                for (int i = 0; i < imageSource.PixelWidth; i++)
                {
                    byte* p = pRow + i * bpp;
                    for (int u = 0; u < bpp; u++)
                    {
                        *(p + u) = 0;
                    }
                }
            }

            for (int j = maskYStart; j < maskYEnd; j++)
            {
                byte* pRow = pResStart + j * imageSource.PixelWidth * bpp;
                for (int i = 0; i < maskXStart; i++)
                {
                    byte* p = pRow + i * bpp;
                    for (int u = 0; u < bpp; u++)
                    {
                        *(p + u) = 0;
                    }
                }

                //for (int i = maskXStart; i < maskXEnd; i++)
                //{
                //    byte* p = pRow + i * bpp;
                //    for (int u = 0; u < bpp; u++)
                //    {
                //        *(p + u) = 0;
                //    }
                //}
                for (int i = maskXEnd; i < imageSource.PixelWidth; i++)
                {
                    byte* p = pRow + i * bpp;
                    for (int u = 0; u < bpp; u++)
                    {
                        *(p + u) = 0;
                    }
                }
            }


            for (int j = maskYEnd; j < imageSource.PixelHeight; j++)
            {
                byte* pRow = pResStart + j * imageSource.PixelWidth * bpp;
                for (int i = 0; i < imageSource.PixelWidth; i++)
                {
                    byte* p = pRow + i * bpp;
                    for (int u = 0; u < bpp; u++)
                    {
                        *(p + u) = 0;
                    }
                }
            }
            imageSource.Unlock();
        }



        private async Task CalcSIFT()
        {
            try
            {
                IsAlive = false;
                SiftRMSE = SiftGoodResultRatio = SiftMinDistance = SiftMaxDistance = null;
                SiftGoodResultCnt = null;
                Hom00 = Hom01 = Hom02 = Hom10 = Hom11 = Hom12 = Hom20 = Hom21 = Hom22 = null;

                await Task.Run(() =>
                {
                    // Extract image 1
                    Image<Bgr, byte> image1CV = Image1.Bmps[ImageStep.Input].ToImage<Bgr, byte>();
                    Mat mat1 = image1CV.Mat;

                    // Calculate keypoints and descriptors
                    Mat imageDescriptors1 = new Mat();
                    VectorOfKeyPoint imageKeyPoints1 = new VectorOfKeyPoint();
                    Mat input1Mask = null;
                    if (ShowSiftMask)
                        input1Mask = CreateSiftInputMask(mat1);
                    Image1.Bmps[ImageStep.SIFTKeypoints] = CalculateDescriptors(mat1, imageDescriptors1, imageKeyPoints1, input1Mask);


                    // Extract image 2
                    // Set same image height to minimise offset
                    Image<Bgr, byte> image2CV = Image2.Bmps[ImageStep.Input].ToImage<Bgr, byte>();
                    if (image2CV.Height != image1CV.Height)
                        image2CV = image2CV.Resize((double)image1CV.Height / (double)image2CV.Height, Inter.Nearest);
                    Mat mat2 = image2CV.Mat;

                    // Calculate keypoints and descriptors
                    Mat imageDescriptors2 = new Mat();
                    VectorOfKeyPoint imageKeyPoints2 = new VectorOfKeyPoint();
                    Image2.Bmps[ImageStep.SIFTKeypoints] = CalculateDescriptors(mat2, imageDescriptors2, imageKeyPoints2);


                    // Calculate matches 
                    VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
                    using Emgu.CV.Flann.LinearIndexParams ip = new Emgu.CV.Flann.LinearIndexParams();
                    using Emgu.CV.Flann.SearchParams sp = new Emgu.CV.Flann.SearchParams();
                    using FlannBasedMatcher flann = new FlannBasedMatcher(ip, sp);
                    flann.Add(imageDescriptors1);
                    flann.KnnMatch(imageDescriptors2, matches, SiftMatchCnt);

                    // Draw matches
                    Image<Bgr, byte> result = new Image<Bgr, byte>(mat1.Width + mat2.Width, Math.Max(mat1.Height, mat2.Height));
                    Features2DToolbox.DrawMatches(mat1, imageKeyPoints1, mat2, imageKeyPoints2, matches, result, new MCvScalar(255, 255, 0), new MCvScalar(0, 255, 255), null);

                    // get good matches
                    Mat mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, UniquenessThreshold, mask);

                    SiftMinDistance = float.MaxValue;
                    SiftMaxDistance = float.MinValue;
                    for (int i = 0; i < matches.Size; i++)
                    {
                        if (matches[i].Size < 2)
                            continue;

                        MDMatch m1 = matches[i][0];

                        if (m1.Distance < SiftMinDistance) SiftMinDistance = m1.Distance;
                        if (m1.Distance > SiftMaxDistance) SiftMaxDistance = m1.Distance;
                    }

                    // draw good results
                    Image<Bgr, byte> goodresult = new Image<Bgr, byte>(mat1.Width + mat2.Width, Math.Max(mat1.Height, mat2.Height));
                    Features2DToolbox.DrawMatches(mat1, imageKeyPoints1, mat2, imageKeyPoints2, matches, goodresult, new MCvScalar(255, 0, 0), new MCvScalar(255, 255, 255), mask);


                    // Calculate homography
                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(imageKeyPoints1, imageKeyPoints2, matches, mask, ScaleIncrement, RotationBins);
                        SiftGoodResultCnt = nonZeroCount;
                        if (nonZeroCount >= 4)
                        {
                            Mat homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(imageKeyPoints1, imageKeyPoints2, matches, mask, RansacReprojThreshold);
                            if (homography != null)
                            {
                                SiftGoodResultRatio = (float)CvInvoke.CountNonZero(mask) / (float)matches.Size;

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


                                // ***** calcul RMSE
                                // store points
                                PointF[] ptsMatches = new PointF[matches.Size];
                                PointF[] ptsReals = new PointF[matches.Size];
                                for (int i = 0; i < matches.Size; i++)
                                {
                                    if (mask.GetValue(i, 0) == (byte)0) continue;

                                    ptsMatches[i] = imageKeyPoints1[matches[i][0].TrainIdx].Point;
                                    ptsReals[i] = imageKeyPoints2[matches[i][0].QueryIdx].Point;
                                }
                                // transform points image1
                                ptsMatches = CvInvoke.PerspectiveTransform(ptsMatches, homography);

                                // calculate distance
                                double sumDist = 0;
                                int cntDist = 0;
                                for (int i = 0; i < matches.Size; i++)
                                {
                                    if (mask.GetValue(i, 0) == (byte)0) continue;

                                    var pt1 = ptsMatches[i];
                                    var pt2 = ptsReals[i];
                                    sumDist += (pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y);
                                    cntDist++;
                                }
                                if (cntDist != 0)
                                    SiftRMSE = (float)(sumDist / (float)cntDist);


                                //draw a rectangle along the projected model
                                Rectangle rect = new Rectangle(Point.Empty, mat1.Size);
                                PointF[] ptsRect = new PointF[]
                                {
                                 new PointF(rect.Left, rect.Bottom),
                                 new PointF(rect.Right, rect.Bottom),
                                 new PointF(rect.Right, rect.Top),
                                 new PointF(rect.Left, rect.Top)
                                };
                                ptsRect = CvInvoke.PerspectiveTransform(ptsRect, homography);
                                Point[] points = Array.ConvertAll<PointF, Point>(ptsRect, Point.Round);
                                using (VectorOfPoint vp = new VectorOfPoint(points))
                                {
                                    CvInvoke.Polylines(goodresult, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                                }

                            }
                            else
                            {
                                Hom00 = Hom01 = Hom02 = Hom10 = Hom11 = Hom12 = Hom20 = Hom21 = Hom22 = null;
                            }
                        }
                    }

                    Image1.Bmps[ImageStep.SIFTResult] = result.ToBitmap();
                    Image1.Bmps[ImageStep.SIFTGoodResult] = goodresult.ToBitmap();

                    RefreshImages();
                    //CurImageStep = ImageStep.SIFTResult;
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

        private Mat CreateSiftInputMask(Mat mat)
        {
            // create mask for input 1
            Mat input1Mask = new Mat(mat.Size, DepthType.Cv8U, 1);
            unsafe
            {
                byte* pResStart = (byte*)input1Mask.DataPointer;
                int maskX = (int)(mat.Width * SiftMaskRatioBorder);
                int maskWidth = (int)(mat.Width * SiftMaskRatioCenter);
                int maskY = (int)(mat.Height * SiftMaskRatioBorder);
                int maskHeight = (int)(mat.Height * SiftMaskRatioCenter);

                int maskXStart = Math.Max(0, maskX);
                int maskYStart = Math.Max(0, maskY);
                int maskXEnd = Math.Min(maskX + maskWidth, input1Mask.Width);
                int maskYEnd = Math.Min(maskY + maskHeight, input1Mask.Height);
                int bpp = input1Mask.NumberOfChannels;
                for (int j = maskYStart; j < maskYEnd; j++)
                {
                    byte* pRow = pResStart + j * input1Mask.Width * bpp;
                    for (int i = 0; i < maskXStart; i++)
                    {
                        byte* p = pRow + i * bpp;
                        for (int u = 0; u < bpp; u++)
                        {
                            *(p + u) = 0;
                        }
                    }
                    for (int i = maskXStart; i < maskXEnd; i++)
                    {
                        byte* p = pRow + i * bpp;
                        for (int u = 0; u < bpp; u++)
                        {
                            *(p + u) = 255;
                        }
                    }
                    for (int i = maskXEnd; i < input1Mask.Width; i++)
                    {
                        byte* p = pRow + i * bpp;
                        for (int u = 0; u < bpp; u++)
                        {
                            *(p + u) = 0;
                        }
                    }
                }
            }

            return input1Mask;
        }

        private Bitmap CalculateDescriptors(Mat image, Mat imageDescriptors = null, VectorOfKeyPoint imageKeyPoints = null, Mat mask = null)
        {
            if (imageDescriptors == null)
                imageDescriptors = new Mat();
            if (imageKeyPoints == null)
                imageKeyPoints = new VectorOfKeyPoint();

            using SIFT sift = new SIFT();
            imageKeyPoints.Push(sift.Detect(image));
            sift.DetectAndCompute(image, mask, imageKeyPoints, imageDescriptors, false);

            Image<Bgr, Byte> result = new Image<Bgr, byte>(image.Width, image.Height);
            Features2DToolbox.DrawKeypoints(image, imageKeyPoints, result, new Bgr(Color.Red));
            return result.ToBitmap();
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

                    RefreshImages();
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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

                            *(pHisto + (int)(value + 0.5)) += 1;
                        }
                    }

                    bmp.UnlockBits(data);


                    if (ignoreBlack && cnt != 0)
                    {
                        for (float* ps = pHisto, pe = pHisto + 256; ps != pe; ++ps)
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
        private unsafe Bitmap GetChromacity(Bitmap src)
        {
            var res = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            var dataRes = res.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadWrite, res.PixelFormat);

            var data = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, src.PixelFormat);
            int bpp = Image.GetPixelFormatSize(src.PixelFormat) / 8;

            byte* pSrc = (byte*)data.Scan0;
            byte* pSrcEnd = pSrc + src.Height * src.Width * bpp;
            byte* pRes = (byte*)dataRes.Scan0;

            for (; pSrc != pSrcEnd; pSrc += bpp, pRes += 3)
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

                    // 0Â°
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

                    // 45Â°
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

                    // 90Â°
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

                    // 135Â°
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
    }
}
