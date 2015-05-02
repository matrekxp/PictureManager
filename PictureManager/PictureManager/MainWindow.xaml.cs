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
                RaisePropertyChanged("SelectedDirectoryPath");
            }
        }

        private bool _isProcessing;

        public ObservableCollection<ImageModel> lstImages { get; set; }

        private readonly BackgroundWorker _loadImagesWorker = new BackgroundWorker();
        private readonly BackgroundWorker _processingImagesWorker = new BackgroundWorker();
        private readonly BackgroundWorker _mpiWorker = new BackgroundWorker();

        private Queue<String> processedImagePaths = new Queue<string>();

        public MainWindow()
        {
            lstImages = new ObservableCollection<ImageModel>();
            this.DataContext = this;
            
            _loadImagesWorker.DoWork += worker_DoWork;
            _loadImagesWorker.RunWorkerCompleted += _runWorkerCompleted;

            _processingImagesWorker.DoWork += _processingImagesWorker_DoWork;
            _processingImagesWorker.RunWorkerCompleted += _runWorkerCompleted;

            SelectDirectoryCommand = new DelegateCommand(SelectDirectory);
            ScaleCommand = new DelegateCommand(PerformScale);
            RotateCommand = new DelegateCommand(PerformRotation);
            ThumbnailsCommand = new DelegateCommand(PerformThumbnails);
            GrayscaleCommand = new DelegateCommand(PerformGrayscale);

            InitializeComponent();
        }

        private void _processingImagesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while(_isProcessing || processedImagePaths.Count != 0)
            {
                if(processedImagePaths.Count != 0) {
                    String imagePath = processedImagePaths.Dequeue();
                    ImageModel imageModel = lstImages.Single(im => im.ImagePath == imagePath);

                    imageModel.IsProcessing = false;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(imagePath + "processed");
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    Dispatcher.Invoke(((Action)(() => imageModel.Image = bitmapImage)));
                }
            }
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

        //private void InitMPI()
        //{
        //    string[] args = Environment.GetCommandLineArgs();
        //    using (new MPI.Environment(ref args))
        //    {

        //        Console.WriteLine("Hello, World! from rank " + MPI.Communicator.world.Rank
        //          + " (running on " + MPI.Environment.ProcessorName + ")");
        //        if (MPI.Communicator.world.Rank != 0)
        //        {
        //            this.Visibility = System.Windows.Visibility.Hidden;
        //        }
        //    }
            
        //}

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            

            if (lstImages.Count - 1 == e.ProgressPercentage)
            {
                ProcessingStatus = "Ostatnia operacja trwała: " + 2.466 + "s";
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            

            for(int i=0; i < lstImages.Count; i++)
            {
                lstImages.ElementAt(i).IsProcessing = false;

                BitmapImage bitmapImage = new BitmapImage(new Uri(lstImages.ElementAt(i).ImagePath));
                bitmapImage.Freeze();
                Dispatcher.Invoke(((Action)(() => lstImages.ElementAt(i).Image = bitmapImage)));

                ProcessingStatus = "Przetwarzanie... " + (i + 1) + " z " + lstImages.Count + " wszystkich plików";

                Thread.Sleep(75);
            }


            //var proc = new Process
            //{
            //    StartInfo = new ProcessStartInfo
            //    {
            //        FileName = "mpiexec",
            //        Arguments = "-n 8 PingPong.exe",
            //        UseShellExecute = false,
            //        RedirectStandardOutput = true,
            //        CreateNoWindow = true
            //    }
            //};

            //proc.Start();

            //while (!proc.StandardOutput.EndOfStream)
            //{
            //    string line = proc.StandardOutput.ReadLine();
            //    Console.WriteLine(line);
            //}
        }

        private void PerformGrayscale()
        {
            for (int i = 0; i < lstImages.Count; i++)
            {
                lstImages.ElementAt(i).IsProcessing = true;
                lstImages.ElementAt(i).Image = null;
            }

            _isProcessing = true;
            _processingImagesWorker.RunWorkerAsync();


            Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
            thread.Start();
        }
        //int proc = 0;
        private void WorkThreadFunction()
        {
            for (int i = 0; i < lstImages.Count; i++)
            {
                using (System.Drawing.Image img = System.Drawing.Image.FromFile(lstImages.ElementAt(i).ImagePath))
                {
                    img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    if (File.Exists(lstImages.ElementAt(i).ImagePath + "processed"))
                        File.Delete(lstImages.ElementAt(i).ImagePath + "processed");
                    img.Save(lstImages.ElementAt(i).ImagePath + "processed", System.Drawing.Imaging.ImageFormat.Jpeg);
                    lstImages.ElementAt(i).ImagePath = lstImages.ElementAt(i).ImagePath + "processed";
                }
                processedImagePaths.Enqueue(lstImages.ElementAt(i).ImagePath);
                Thread.Sleep(75);
            }
 
            _isProcessing = false;
        }

        private void PerformThumbnails()
        {
            throw new NotImplementedException();
        }

        private void PerformRotation()
        {
            throw new NotImplementedException();
        }

        private void PerformScale()
        {
            throw new NotImplementedException();
        }
    }
}
