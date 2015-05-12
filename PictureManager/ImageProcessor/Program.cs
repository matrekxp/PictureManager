using ImageResizer;
using ImageResizer.Configuration;
using ImageResizer.Plugins.SimpleFilters;
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
        private static Dictionary<String, Action<string, string>> actions = new Dictionary<String, Action<string, string>>();

        private static String _selectedDirectoryPath;
        private static String _processedDirectoryPath;
        private static String []_actionParameters = new String[2];
        private static String _resultFileName = "Results.txt";

        static void Main(string[] args)
        {
            new SimpleFilters().Install(Config.Current);
            actions.Add("rotate", RotateAction);
            actions.Add("scale", ScaleAction);
            actions.Add("grayscale", GrayscaleAction);
            actions.Add("thumbnails", ThumbnailAction);
            actions.Add("sepia", SepiaAction);
            actions.Add("invert", InvertAction);

            _selectedDirectoryPath = args[0];
            _processedDirectoryPath = args[1];
            string action = args[2];
            for (int i = 3; i < args.Length; i++)
                _actionParameters[i-3] = args[i];
            Action<string, string> actionToPerform = actions[action];

            using (new MPI.Environment(ref args))
            {
                WorkThreadFunction(actionToPerform);
            }
        }

        private static void InvertAction(string filePathToBeProcessed, string outFilePath)
        {
            ImageBuilder.Current.Build(filePathToBeProcessed, outFilePath, new ResizeSettings("s.invert=true"));
        }

        private static void SepiaAction(string filePathToBeProcessed, string outFilePath)
        {
            ImageBuilder.Current.Build(filePathToBeProcessed, outFilePath, new ResizeSettings("s.sepia=true"));
        }

        private static void ThumbnailAction(string filePathToBeProcessed, string outFilePath)
        {
            int width = int.Parse(_actionParameters[0]);
            int height = int.Parse(_actionParameters[1]);
            ImageBuilder.Current.Build(filePathToBeProcessed, outFilePath, new ResizeSettings("maxwidth=" + width + "&maxheight=" + height ));
        }

        private static void GrayscaleAction(string filePathToBeProcessed, string outFilePath)
        {
            ImageBuilder.Current.Build(filePathToBeProcessed, outFilePath, new ResizeSettings("s.grayscale=true"));
        }

        private static void ScaleAction(string filePathToBeProcessed, string outFilePath)
        {
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(filePathToBeProcessed))
            {
                float scale = float.Parse(_actionParameters[0]);
                scale /= 100;
                int width = ((int)(img.Width * scale));
                int height = ((int)(img.Height * scale));
           
                ImageBuilder.Current.Build(filePathToBeProcessed, outFilePath, new ResizeSettings("width=" + width + "&height=" + height +"&mode=stretch"));
            }
        }

        private static void RotateAction(string filePathToBeProcessed, string outFilePath)
        {
            RotateFlipType rotateType = (RotateFlipType)Enum.Parse(typeof(RotateFlipType), _actionParameters[0], true);
            ResizeSettings s = new ResizeSettings()
            {
                Flip = rotateType
            };
            ImageBuilder.Current.Build(filePathToBeProcessed, outFilePath, s);
        }

        static private void WorkThreadFunction(Action<string, string> actionToPerform)
        {
            Intracommunicator comm = Communicator.world;
            int processingThreadsCount = MPI.Communicator.world.Size;
            List<String>[] filesToThreads = new List<string>[processingThreadsCount];
            
            if (comm.Rank == 0)
            {
                PrepareImagesPerEachThread(processingThreadsCount, filesToThreads);
            }

            List<String> imagesToBeProcessed = null;
            if(comm.Rank == 0) 
                imagesToBeProcessed = comm.Scatter<List<String>>(filesToThreads);
            else
                imagesToBeProcessed = comm.Scatter<List<String>>(0);

            Stopwatch sw = WaitForAllThreadsAndInitStopwatch(comm);

            foreach(String imagePath in imagesToBeProcessed)
            {
                ProcessImage(actionToPerform, imagePath);
            }

            WaitForAllThreadsAndPrintOutExecutionTime(comm, sw);
        }

        private static void ProcessImage(Action<string, string> actionToPerform, String image)
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

            String processedFilePath = _processedDirectoryPath + "/" + imageFileName + nextFileIndex + ".jpg";

            actionToPerform(filePathToBeProcessed, processedFilePath);

            if (nextFileIndex > 1)
                File.Delete(_processedDirectoryPath + "/" + imageFileName + (--nextFileIndex) + ".jpg");

            Console.WriteLine(fileName + "$" + processedFilePath);
        }

        private static void WaitForAllThreadsAndPrintOutExecutionTime(Intracommunicator comm, Stopwatch sw)
        {
            comm.Barrier();
            if (comm.Rank == 0)
            {
                sw.Stop();

                Console.WriteLine("{0:mm\\:ss\\.fffff}", sw.Elapsed);

                using (StreamWriter writer = File.AppendText(_resultFileName))
                {
                    if (new FileInfo(_resultFileName).Length == 0)
                    {
                        writer.WriteLine("Time\t\tThreads");
                    }
                    writer.WriteLine("{0:mm\\:ss\\.fffff}\t{1}", sw.Elapsed, MPI.Communicator.world.Size);
                }
            }
        }

        private static Stopwatch WaitForAllThreadsAndInitStopwatch(Intracommunicator comm)
        {
            Stopwatch sw = null;
            comm.Barrier();
            if (comm.Rank == 0)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            return sw;
        }

        private static void PrepareImagesPerEachThread(int processingThreadsCount, List<String>[] filesToThreads)
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
            for (int i = 0; i < filesToThreads.Length; i++)
            {
                for (int j = 0; j < imagesPerTask; j++)
                {
                    filesToThreads[i].Add(lstImages[i * imagesPerTask + j]);
                }
            }

            int offset = imagesPerTask * processingThreadsCount;

            for (int i = 0; i < lstImages.Count % MPI.Communicator.world.Size; i++)
            {
                filesToThreads[i].Add(lstImages[offset + i]);
            }

            if (!Directory.Exists(_processedDirectoryPath))
                Directory.CreateDirectory(_processedDirectoryPath);
        }
    }
}
