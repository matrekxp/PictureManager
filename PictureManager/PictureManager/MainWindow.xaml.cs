using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public ObservableCollection<ImageModel> lstImages { get; set; }

        private readonly BackgroundWorker worker = new BackgroundWorker();

        private void btnLoadFolderPath_Click(object sender, RoutedEventArgs e)
        {
            lstImages.Clear();
            List<string> lstFileNames = new List<string>(System.IO.Directory.EnumerateFiles(txtFolderPath.Text, "*.jpg"));
            foreach (string fileName in lstFileNames)
            {
                lstImages.Add(new ImageModel(null, fileName, true));
            }

            worker.RunWorkerAsync();
        }



        public MainWindow()
        {
            lstImages = new ObservableCollection<ImageModel>();
            this.DataContext = this;
            worker.DoWork += worker_DoWork;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            InitializeComponent();
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Image imgTemp = new Image();
            imgTemp.Source = new BitmapImage(new Uri(lstImages.ElementAt(e.ProgressPercentage).ImagePath));
            imgTemp.Height = imgTemp.Width = 100;
            lstImages.ElementAt(e.ProgressPercentage).Image = imgTemp;
            ProcessingStatus = e.ProgressPercentage + " z " + lstImages.Count + " wszystkich plików";
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            
            for(int i=0; i < lstImages.Count; i++)
            {
                lstImages.ElementAt(i).IsProcessing = false;
                worker.ReportProgress(i);
                Thread.Sleep(500);
            }
        }
    }
}
