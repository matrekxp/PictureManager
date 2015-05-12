using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public DelegateCommand SepiaCommand { get; private set; }
        public DelegateCommand InvertCommand { get; private set; }
        public DelegateCommand OpenSettingsCommand { get; private set; }
        public DelegateCommand OpenSelectedDirectoryCommand { get; private set; }
        public DelegateCommand AboutCommand { get; private set; }

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

        public Boolean GatherResults { get; set; }

        private RotateFlipType _rotationFlipType = RotateFlipType.Rotate180FlipNone;
        private String _scale = "100";
        private String _maxWidth = "100";
        private String _maxHeight = "100";

        private string _lastTimeExecution = "";
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
            SepiaCommand = new DelegateCommand(PerformSepia);
            InvertCommand = new DelegateCommand(PerformInvert);
            OpenSettingsCommand = new DelegateCommand(OpenSettings);
            OpenSelectedDirectoryCommand = new DelegateCommand(OpenSelectedDirectory);
            AboutCommand = new DelegateCommand(About);
   
            OpenSelectedDirectoryCommand.IsEnabled = false;
            ChangeActionButtonsState(false);

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
                    ProcessingStatus = "Processing... " + processedImagesCount + " of " + lstImages.Count + " all files";
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
            if (_lastTimeExecution == "")
                ProcessingStatus = "Ready to perform actions";
            else
                ProcessingStatus = "Last operation lasted: " + _lastTimeExecution + "s";
        }

        private void SelectDirectory()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            SelectedDirectoryPath = dialog.SelectedPath;
            if (result == System.Windows.Forms.DialogResult.OK)
            { 
                if (Directory.Exists(_processedDirectoryPath))
                    Directory.Delete(_processedDirectoryPath, true);

                OpenSelectedDirectoryCommand.IsEnabled = true;

                lstImages.Clear();
                List<string> lstFileNames = new List<string>(System.IO.Directory.EnumerateFiles(SelectedDirectoryPath, "*.jpg"));
                foreach (string fileName in lstFileNames)
                {
                    lstImages.Add(new ImageModel(null, fileName, true));
                }

                ChangeButtonsState(false);

                _loadImagesWorker.RunWorkerAsync();
            }
        }

        private void ChangeButtonsState(bool isEnabled)
        {
            SelectDirectoryCommand.IsEnabled = isEnabled;
            ChangeActionButtonsState(isEnabled);
        }

        private void ChangeActionButtonsState(bool isEnabled)
        {
            ScaleCommand.IsEnabled = isEnabled;
            RotateCommand.IsEnabled = isEnabled;
            ThumbnailsCommand.IsEnabled = isEnabled;
            GrayscaleCommand.IsEnabled = isEnabled;
            SepiaCommand.IsEnabled = isEnabled;
            InvertCommand.IsEnabled = isEnabled;
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

                ProcessingStatus = "Processing... " + (i + 1) + " of " + lstImages.Count + " all files";

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
                    Arguments = "-n " + _threadsCount + " ..\\..\\..\\ImageProcessor\\bin\\Debug\\ImageProcessor.exe " +
                    _selectedDirectoryPath + " " + _processedDirectoryPath + " " + GatherResults + " " + e.Argument,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                String line = proc.StandardOutput.ReadLine();
                if (!line.Contains("$"))
                {
                    _lastTimeExecution = line;
                    continue;
                }

                String[] processingInfo = line.Split('$');
                String baseFilePath = processingInfo[0];
                String processedFilePath = processingInfo[1];

                ImageModel imageModel = lstImages.Single(im => im.ImageBasePath == baseFilePath);
                imageModel.ImageProcessedPath = processedFilePath;

                processedImages.Enqueue(imageModel);
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
            InitControlsBeforeProcessing();
            RunImageAction("thumbnails " + _maxWidth + " " + _maxHeight);
        }

        private void PerformRotation()
        {
            InitControlsBeforeProcessing();
            RunImageAction("rotate " + _rotationFlipType);
        }

        private void RunImageAction(string action)
        {
            ProcessingStatus = "Processing... ";
            _isProcessing = true;
            _mpiWorker.RunWorkerAsync(action);
            _processingImagesWorker.RunWorkerAsync();
        }

        private void PerformScale()
        {
            InitControlsBeforeProcessing();
            RunImageAction("scale " + _scale);
        }

        private void PerformGrayscale()
        {
            InitControlsBeforeProcessing();
            RunImageAction("grayscale");
        }

        private void PerformSepia()
        {
            InitControlsBeforeProcessing();
            RunImageAction("sepia");
        }

        private void PerformInvert()
        {
            InitControlsBeforeProcessing();
            RunImageAction("invert");
        }

        private void OpenSettings()
        {
            Settings settings = new Settings(_scale, _maxWidth, _maxHeight);
            settings.ShowDialog();

            _rotationFlipType = settings.SelectedRotateType;
            _scale = settings.Scale;
            _maxWidth = settings.ThumbnailMaxWidth;
            _maxHeight = settings.ThumbnailMaxHeight;
        }

        private void About()
        {
            new About().ShowDialog();
        }

        private void OpenSelectedDirectory()
        {
            Process.Start(@_selectedDirectoryPath);
        }
    }
}
