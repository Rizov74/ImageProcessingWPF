using ImageProcessingWPF.ViewModels;
using System;
using System.Collections.Generic;
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
    /// Logique d'interaction pour ImageComparator.xaml
    /// </summary>
    public partial class ImageComparator : UserControl
    {
        ImageComparatorViewModel viewModel = new ImageComparatorViewModel();
        public ImageComparator()
        {
            InitializeComponent();

            cbImageStep.ItemsSource = Enum.GetValues(typeof(ImageStep));

            this.DataContext = viewModel;
        }

        private void Image1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                viewModel.LoadImage1(files[0]);
            }
        }
        private void Image2_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                viewModel.LoadImage2(files[0]);
            }
        }
    }
}
