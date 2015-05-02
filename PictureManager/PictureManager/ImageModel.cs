using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PictureManager
{
    public class ImageModel : INotifyPropertyChanged
    {
        private bool isProcessing;
        private BitmapImage image;

        public String ImagePath { get; set; }
        public BitmapImage Image
        { 
            get 
            {
                return image;
            }
            set
            {
                image = value;
                RaisePropertyChanged("Image");
            }
        }
        public bool IsProcessing
        {
            get
            {
                return isProcessing;
            }
            set 
            {
                isProcessing = value;
                RaisePropertyChanged("IsProcessing"); 
            }
        }

        public ImageModel(BitmapImage image, String imagePath, bool isProcessing)
        {
            this.image = image;
            this.isProcessing = isProcessing;
            this.ImagePath = imagePath;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
