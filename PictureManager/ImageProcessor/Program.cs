using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageProcessor
{
    class Program
    {
        private static String _selectedDirectoryPath = "C:\\Users\\Mateusz\\Downloads\\HQ_Wallpapers_Pack\\walzzz56";
        private static String _processedDirectoryPath = "C:\\Users\\Mateusz\\Downloads\\HQ_Wallpapers_Pack\\walzzz56\\processed";

        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                WorkThreadFunction();
            }
        }

        static private void WorkThreadFunction()
        {
            List<String> lstImages = new List<String>();
            List<string> lstFileNames = new List<string>(System.IO.Directory.EnumerateFiles(_selectedDirectoryPath, "*.jpg"));
            foreach (string fileName in lstFileNames)
            {
                lstImages.Add(fileName);
            }

            int imagesPerTask = lstImages.Count / MPI.Communicator.world.Size;
            int startIndex = MPI.Communicator.world.Rank * imagesPerTask;
            

            if (!Directory.Exists(_processedDirectoryPath))
                Directory.CreateDirectory(_processedDirectoryPath);

            for (int i = startIndex; i < startIndex + imagesPerTask; i++)
            {
                String fileName = lstImages.ElementAt(i);
                String imageFileName = fileName.Split('\\').Last();
                imageFileName = imageFileName.Split('.').First();
                String[] currentlyProcessedFiles = Directory.GetFiles(_processedDirectoryPath, imageFileName + "*");

                int nextFileIndex = 1;
                String filePathToBeProcessed = fileName;

                if (currentlyProcessedFiles.Length != 0)
                {
                    int startFileIndexPos = currentlyProcessedFiles[0].LastIndexOf(imageFileName) + imageFileName.Length;
                    String fileNameIndex = currentlyProcessedFiles[0].Split('.').First().Substring(startFileIndexPos);
                    nextFileIndex = int.Parse(fileNameIndex);
                    nextFileIndex++;
                    filePathToBeProcessed = _processedDirectoryPath + "/" + imageFileName + (fileNameIndex) + ".jpg";
                }

                using (System.Drawing.Image img = System.Drawing.Image.FromFile(filePathToBeProcessed))
                {
                    String processedFilePath = _processedDirectoryPath + "/" + imageFileName + nextFileIndex + ".jpg";
                    img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    img.Save(processedFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                    if (nextFileIndex > 1)
                        File.Delete(_processedDirectoryPath + "/" + imageFileName + (--nextFileIndex) + ".jpg");

                    Console.WriteLine(fileName + "$" + processedFilePath);
                }
            }
        }
    }
}
