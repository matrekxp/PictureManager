using MPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static String _selectedDirectoryPath;
        private static String _processedDirectoryPath;

        private static String _resultFileName = "Results.txt";

        static void Main(string[] args)
        {
            _selectedDirectoryPath = args[0];
            _processedDirectoryPath = args[1];
            using (new MPI.Environment(ref args))
            {
                WorkThreadFunction();
            }
        }

        static private void WorkThreadFunction()
        {
            Intracommunicator comm = Communicator.world;
            int processingThreadsCount = MPI.Communicator.world.Size;
            List<String>[] filesToThreads = new List<string>[processingThreadsCount];
            List<String> imagesToBeProcessed = null;
            if (comm.Rank == 0)
            {
                List<String> lstImages = new List<String>();
                List<string> lstFileNames = new List<string>(System.IO.Directory.EnumerateFiles(_selectedDirectoryPath, "*.jpg"));
                foreach (string fileName in lstFileNames)
                {
                    lstImages.Add(fileName);
                }

                for (int i = 0; i < filesToThreads.Length; i++)
                {
                    filesToThreads[i] = new List<string>();
                }

                int imagesPerTask = lstImages.Count / processingThreadsCount;
                for(int i = 0; i < filesToThreads.Length; i++)
                {
                    for(int j=0; j < imagesPerTask; j++) 
                    {
                        filesToThreads[i].Add(lstImages[i * imagesPerTask + j]);
                    }
                }

                int offset = imagesPerTask * processingThreadsCount;

                for(int i=0; i < lstImages.Count % MPI.Communicator.world.Size; i++)
                {
                    filesToThreads[i].Add(lstImages[offset + i]);
                }

                if (!Directory.Exists(_processedDirectoryPath))
                    Directory.CreateDirectory(_processedDirectoryPath);
            }

            if(comm.Rank == 0) 
                imagesToBeProcessed = comm.Scatter<List<String>>(filesToThreads);
            else
                imagesToBeProcessed = comm.Scatter<List<String>>(0);

            Stopwatch sw = null;
            comm.Barrier();
            if(comm.Rank == 0)
            { 
                sw = new Stopwatch();
                sw.Start();
            }

            foreach(String image in imagesToBeProcessed)
            {
                String fileName = image;
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

            comm.Barrier();
            if (comm.Rank == 0)
            {
                sw.Stop();
            
                Console.WriteLine("{0:s\\.fffff}", sw.Elapsed);

                using (StreamWriter writer = File.AppendText(_resultFileName))
                {
                    if (new FileInfo(_resultFileName).Length == 0)
                    {
                        writer.WriteLine("Time\t\tThreads");
                    }
                    writer.WriteLine("{0:ss\\.fffff}\t{1}", sw.Elapsed, MPI.Communicator.world.Size);
                }
            }
        }
    }
}
