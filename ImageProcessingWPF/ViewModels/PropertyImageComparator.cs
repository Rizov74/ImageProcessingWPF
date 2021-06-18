using ImageProcessingWPF.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;

namespace ImageProcessingWPF.ViewModels
{
    public class PropertySet : ObservableObject
    {
        public ObservableCollection<string> ImagesInput { get; } = new ObservableCollection<string>();
        public Dictionary<string, ObservableCollection<string>> ImagesTest { get; } = new Dictionary<string, ObservableCollection<string>>();
        public string MatchKey { get => matchKey; set => SetValue(ref matchKey, value); }
        public bool IsNew { get => isNew; set => SetValue(ref isNew, value); }

        private bool isNew;
        private string matchKey;

        public PropertySet(string dir)
        {
            var nameSplit = dir.Split("\\");
            var name = nameSplit[nameSplit.Length - 1];
            var split = name.Split('_');
            if (split[0] == "NEW")
            {
                IsNew = true;
            }
            else
            {
                MatchKey = split[0];
            }


            var subDirs = Directory.EnumerateDirectories(dir);
            foreach (var d in subDirs)
            {
                var dNameSplit = d.Split("\\");
                var dName = dNameSplit[^1];
                if (dName == "input")
                {
                    foreach (var p in Directory.GetFiles(d, "*.bmp"))
                        ImagesInput.Add(p);

                    foreach (var p in Directory.GetFiles(d, "*.jpg"))
                        ImagesInput.Add(p);

                    foreach (var p in Directory.GetFiles(d, "*.jpeg"))
                        ImagesInput.Add(p);
                }
                else 
                {
                    // Enumerate listing
                    List<string> images = new List<string>();
                    foreach (var listing in Directory.EnumerateDirectories(d))
                    {
                        foreach (var p in Directory.GetFiles(listing, "*.bmp"))
                            images.Add(p);

                        foreach (var p in Directory.GetFiles(listing, "*.jpg"))
                            images.Add(p);

                        foreach (var p in Directory.GetFiles(listing, "*.jpeg"))
                            images.Add(p);

                    }
                    ImagesTest.Add(dName, new ObservableCollection<string>(images));

                }
            }

        }


    }


    public class PropertyImageComparator : ObservableObject
    {
        private string directory;
        public string CurrentDirectory { get => directory; set => SetValue(ref directory, value); }

        public ICommand LoadFolderCommand { get; }
        public PropertySet PropertySet { get => propertySet; set => SetValue(ref propertySet, value); }
        public string SelectedImageInput { get => selectedImageInput; set => SetValue(ref selectedImageInput, value); }
        public string SelectedImageTest { get => selectedImageTest; set => SetValue(ref selectedImageTest, value); }

        private PropertySet propertySet;

        private string selectedImageInput;
        private string selectedImageTest;


        public PropertyImageComparator()
        {
            LoadFolderCommand = new RelayCommand(p => LoadDirectory());
        }

        private void LoadDirectory()
        {
            var dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            dir = Path.Combine(dir, @"ImageToPlay\");
            var d = new FolderBrowserDialog()
            {
                ShowNewFolderButton = false,
                Description = "Select image directory",
                RootFolder = Environment.SpecialFolder.ApplicationData
            };
            if (d.ShowDialog() != DialogResult.OK)
                return;

            CurrentDirectory = d.SelectedPath;
            PropertySet = new PropertySet(CurrentDirectory);
        }
    }
}
