using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PictureManager
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        public DelegateCommand SelectDirectoryCommand { get; private set; }
        public DelegateCommand ScaleCommand { get; private set; }
        public DelegateCommand RotateCommand { get; private set; }
        public DelegateCommand ThumbnailsCommand { get; private set; }
        public DelegateCommand GrayscaleCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public String processingStatus;
        public String ProcessingStatus
        {
            get
            {
                return processingStatus;
            }
            set
            {
                processingStatus = value;
                RaisePropertyChanged("ProcessingStatus");
            }
        }
        private String _processedDirectoryPath;
        private String _selectedDirectoryPath;
        public String SelectedDirectoryPath
        {
            get
            {
                return _selectedDirectoryPath;
            }
            set
            {
                _selectedDirectoryPath = value;
                _processedDirectoryPath = _selectedDirectoryPath + "/processed";
                RaisePropertyChanged("SelectedDirectoryPath");
            }
        }
        private int _threadsCount = 1;
        public int ThreadsCount
        {
            get
            {
                return _threadsCount;
            }
            set
            {
                _threadsCount = value;
            }
        }


        private bool _isProcessing;

        public ObservableCollection<ImageModel> lstImages { get; set; }

        private readonly BackgroundWorker _loadImagesWorker = new BackgroundWorker();
        private readonly BackgroundWorker _processingImagesWorker = new BackgroundWorker();
        private readonly BackgroundWorker _mpiWorker = new BackgroundWorker();

        private Queue<ImageModel> processedImages = new Queue<ImageModel>();

        public MainWindow()
        {
            lstImages = new ObservableCollection<ImageModel>();
            this.DataContext = this;
            
            _loadImagesWorker.DoWork += loadImagesWorker_DoWork;
            _loadImagesWorker.RunWorkerCompleted += _runWorkerCompleted;

            _processingImagesWorker.DoWork += _processingImagesWorker_DoWork;
            _processingImagesWorker.RunWorkerCompleted += _runWorkerCompleted;

            _mpiWorker.DoWork += _mpiWorker_DoWork;

            SelectDirectoryCommand = new DelegateCommand(SelectDirectory);
            ScaleCommand = new DelegateCommand(PerformScale);
            RotateCommand = new DelegateCommand(PerformRotation);
            ThumbnailsCommand = new DelegateCommand(PerformThumbnails);
            GrayscaleCommand = new DelegateCommand(PerformGrayscale);

            InitializeComponent();
        }

        private void _processingImagesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while(_isProcessing || processedImages.Count != 0)
            {
                if(processedImages.Count != 0) {
                    ImageModel imageModel = processedImages.Dequeue();

                    imageModel.IsProcessing = false;
                    BitmapImage bitmapImage = LoadBitmapImage(imageModel.ImageProcessedPath);
                    Dispatcher.Invoke(((Action)(() => imageModel.Image = bitmapImage)));

                    int processedImagesCount = lstImages.Count(im => !im.IsProcessing);
                    ProcessingStatus = "Przetwarzanie... " + processedImagesCount + " z " + lstImages.Count + " wszystkich plików";
                }

            }
        }

        private static BitmapImage LoadBitmapImage(String imagePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.DecodePixelHeight = bitmapImage.DecodePixelWidth = 100;
            bitmapImage.UriSource = new Uri(imagePath);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private void _runWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ChangeButtonsState(true);
            ProcessingStatus = "Ostatnia operacja trwała: " + 2.466 + "s";
        }

        private void SelectDirectory()
        {
            //var dialog = new System.Windows.Forms.FolderBrowserDialog();
            //System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            SelectedDirectoryPath = "C:\\Users\\Mateusz\\Downloads\\HQ_Wallpapers_Pack\\walzzz56";
            //if (result == System.Windows.Forms.DialogResult.OK)
           // { 
                lstImages.Clear();
                List<string> lstFileNames = new List<string>(System.IO.Directory.EnumerateFiles(SelectedDirectoryPath, "*.jpg"));
                foreach (string fileName in lstFileNames)
                {
                    lstImages.Add(new ImageModel(null, fileName, true));
                }

                ChangeButtonsState(false);

                _loadImagesWorker.RunWorkerAsync();
           // }
        }

        private void ChangeButtonsState(bool isEnabled)
        {
            SelectDirectoryCommand.IsEnabled = isEnabled;
            ScaleCommand.IsEnabled = isEnabled;
            RotateCommand.IsEnabled = isEnabled;
            ThumbnailsCommand.IsEnabled = isEnabled;
            GrayscaleCommand.IsEnabled = isEnabled;
        }

        

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void loadImagesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            for(int i=0; i < lstImages.Count; i++)
            {
                lstImages.ElementAt(i).IsProcessing = false;

                BitmapImage bitmapImage = LoadBitmapImage(lstImages.ElementAt(i).ImageBasePath);
                Dispatcher.Invoke(((Action)(() => lstImages.ElementAt(i).Image = bitmapImage)));

                ProcessingStatus = "Przetwarzanie... " + (i + 1) + " z " + lstImages.Count + " wszystkich plików";

                Thread.Sleep(10);
            }
        }

        

        private void _mpiWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mpiexec",
                    Arguments = "-n " + _threadsCount + " ..\\..\\..\\ImageProcessor\\bin\\Debug\\ImageProcessor.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                String line = proc.StandardOutput.ReadLine();
                String[] processingInfo = line.Split('$');
                String baseFilePath = processingInfo[0];
                String processedFilePath = processingInfo[1];

                ImageModel imageModel = lstImages.Single(im => im.ImageBasePath == baseFilePath);
                imageModel.ImageProcessedPath = processedFilePath;

                processedImages.Enqueue(imageModel);

                Console.WriteLine(baseFilePath + " " + processedFilePath);
            }

            _isProcessing = false;
        }

        private void InitControlsBeforeProcessing()
        {
            ChangeButtonsState(false);
            for (int i = 0; i < lstImages.Count; i++)
            {
                lstImages.ElementAt(i).IsProcessing = true;
                lstImages.ElementAt(i).Image = null;
            }
        }


        private void PerformThumbnails()
        {
            throw new NotImplementedException();
        }

        private void PerformRotation()
        {
            InitControlsBeforeProcessing();

            _isProcessing = true;
            _mpiWorker.RunWorkerAsync();
            _processingImagesWorker.RunWorkerAsync();
        }

        private void PerformScale()
        {
            throw new NotImplementedException();
        }

        private void PerformGrayscale()
        {

        }
    }
}
