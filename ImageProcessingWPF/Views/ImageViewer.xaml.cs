using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageProcessingWPF.Views
{
    /// <summary>
    /// Logique d'interaction pour ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : System.Windows.Controls.Image
    {
        public ImageViewer()
        {
            InitializeComponent();
        }




        //private void Load_Click(object sender, RoutedEventArgs e)
        //{
        //    var d = new OpenFileDialog
        //    {
        //        Filter = "Bitmap (*.bmp)|*.bmp",
        //        InitialDirectory = "D:\\tmp"
        //    };

        //    if (d.ShowDialog() != true)
        //        return;

        //    var bmp = new Bitmap(d.FileName);
        //    BitmapTools.
        //    Source = 
        //}

        private void Save_Click(object sender, RoutedEventArgs e)
        {

            var d = new SaveFileDialog
            {
                Filter = "Bitmap (*.bmp)|*.bmp",
                InitialDirectory = "D:\\tmp"
            };

            if (d.ShowDialog() != true)
                return;

            if (Source is BitmapSource bmp)
            {
                Save(bmp, d.FileName);
            }
        }


        // Save the WriteableBitmap into a PNG file.
        public void Save(BitmapSource wbitmap, string filename)
        {
            // Save the bitmap into a file.
            using FileStream stream = new FileStream(filename, FileMode.Create);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(wbitmap));
            encoder.Save(stream);
        }
    }
}
