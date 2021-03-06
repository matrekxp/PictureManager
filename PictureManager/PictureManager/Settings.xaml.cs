﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PictureManager
{
    public partial class Settings : Window
    {
        public RotateFlipType SelectedRotateType { get; set; }
        public String Scale { get; set; }
        public String ThumbnailMaxWidth { get; set; }
        public String ThumbnailMaxHeight { get; set; }

        public Settings(String scale, String maxWidth, String maxHeight)
        {
            Scale = scale;
            ThumbnailMaxWidth = maxWidth;
            ThumbnailMaxHeight = maxHeight;
            InitializeComponent();
            DataContext = this;
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
