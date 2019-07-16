using BlennyBackup.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlennyBackup.Logging;

namespace BlennyBackup.Diagnostics
{
    /// <summary>
    /// Writes reports to <see cref="Logger"/> when syncing folders
    /// </summary>
    internal class ProgressReporter
    {
        /// <summary>
        /// An asynchronous file/console logging system
        /// </summary>
        public static Logger Logger;

        /// <summary>
        /// Size of all source only files in byte
        /// </summary>
        public long NewFilesSize { get; private set; }

        /// <summary>
        /// Size of all edited files in byte
        /// </summary>
        public long ModifiedFilesSize { get; private set; }

        /// <summary>
        /// <see cref="NewFilesSize"/> + <see cref="ModifiedFilesSize"/>
        /// </summary>
        public long ToBeCopiedSize
        {
            get
            {
                return NewFilesSize + ModifiedFilesSize;
            }
        }

        /// <summary>
        /// Size of all source only files that have already been copied in byte
        /// </summary>
        private long NewFilesProgress;

        /// <summary>
        ///  Size of all edited files that have already been copied in byte
        /// </summary>
        private long ModifiedFilesProgress;

        /// <summary>
        /// <see cref=" NewFilesProgress"/> + <see cref="ModifiedFilesProgress"/>
        /// </summary>
        private long ToBeCopiedProgress
        {
            get
            {
                return NewFilesProgress + ModifiedFilesProgress;
            }
        }

        /// <summary>
        /// Per file size of <see cref="FolderDiffBase.SourceOnlyFiles"/>
        /// </summary>
        private long[] NewFilesSizeArray;

        /// <summary>
        /// Per file size of <see cref="FolderDiffBase.ModifiedFiles"/>
        /// </summary>
        private long[] ModifiedFilesSizeArray;

        /// <summary>
        /// Converts a number in byte to a human readable file size
        /// </summary>
        /// <param name="byteCount">number in byte</param>
        /// <returns>human readable file size</returns>
        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        /// <summary>
        /// Compute file sizes from a <see cref="FolderDiffBase"/> elements in order to have relevant reports
        /// </summary>
        /// <param name="folderDiff">An object containing source/target differences</param>
        public ProgressReporter(FolderDiffBase folderDiff)
        {
            NewFilesProgress = 0;
            ModifiedFilesProgress = 0;

            long sizeIncrement = 0;

            Logger.WriteLine("Estimating size of new files");
            NewFilesSizeArray = new long[folderDiff.SourceOnlyFiles.Length];
            Parallel.For(0, folderDiff.SourceOnlyFiles.Length, (i) =>
            {
                string path = Path.Combine(folderDiff.SourcePath, folderDiff.SourceOnlyFiles[i]);
                long length = new System.IO.FileInfo(path).Length;
                NewFilesSizeArray[i] = length;
                Interlocked.Add(ref sizeIncrement, length);
            });

            NewFilesSize = sizeIncrement;
            Logger.WriteLine("New files : " + BytesToString(sizeIncrement));

            sizeIncrement = 0;
            Logger.WriteLine("Estimating size of modified files");
            ModifiedFilesSizeArray = new long[folderDiff.ModifiedFiles.Length];

            Parallel.For(0, folderDiff.ModifiedFiles.Length, i =>
            {
                string path = Path.Combine(folderDiff.SourcePath, folderDiff.ModifiedFiles[i]);
                long length = new System.IO.FileInfo(path).Length;
                ModifiedFilesSizeArray[i] = length;
                Interlocked.Add(ref sizeIncrement, length);
            });

            ModifiedFilesSize = sizeIncrement;
            Logger.WriteLine("Modified files : " + BytesToString(sizeIncrement));
            Logger.WriteLine("Total to copy : " + BytesToString(ToBeCopiedSize));
        }

        /// <summary>
        /// Update <see cref="NewFilesProgress"/> with the size of NewFilesSizeArray[index]
        /// </summary>
        /// <param name="index">index in NewFilesSizeArray</param>
        public void AddNewFileProgress(int index)
        {
            long progress = NewFilesSizeArray[index];
            Interlocked.Add(ref NewFilesProgress, progress);
        }

        /// <summary>
        /// Update <see cref="ModifiedFilesProgress"/> with the size of ModifiedFilesSizeArray[index]
        /// </summary>
        /// <param name="index">index in ModifiedFilesSizeArray</param>
        public void AddModifiedFileProgress(int index)
        {
            long progress = ModifiedFilesSizeArray[index];
            Interlocked.Add(ref ModifiedFilesProgress, progress);
        }

        /// <summary>
        /// Write <see cref="NewFilesProgress"/> to <see cref="Logger"/>
        /// </summary>
        /// <param name="logLevel">Console, log file or both</param>
        public void WriteDetailedNewFileReport(string sourcepath, string targetpath, int k, LogLevel logLevel)
        {
            Logger.WriteLine("Finished copy of " + sourcepath + " to " + targetpath + " of size " + BytesToString(NewFilesSizeArray[k]) + " --- Copy of new files : " + BytesToString(NewFilesProgress) + " / " + BytesToString(NewFilesSize), logLevel);
        }

        /// <summary>
        /// Write <see cref="ModifiedFilesProgress"/> to <see cref="Logger"/>
        /// </summary>
        /// <param name="logLevel">Console, log file or both</param>
        public void WriteDetailedModifiedFileReport(string sourcepath, string targetpath, int k, LogLevel logLevel)
        {
            Logger.WriteLine("Finished copy of " + sourcepath + " to " + targetpath + " of size " + BytesToString(ModifiedFilesSizeArray[k]) + " --- Copy of modified files : " + BytesToString(ModifiedFilesProgress) + " / " + BytesToString(ModifiedFilesSize), logLevel);
        }

        /// <summary>
        /// Write <see cref="NewFilesProgress"/>, <see cref="ModifiedFilesProgress"/>, <see cref="ToBeCopiedProgress"/> to <see cref="Logger"/>
        /// </summary>
        /// <param name="logLevel">Console, log file or both</param>
        public void WriteReport(LogLevel logLevel)
        {
            Logger.WriteLine("New files : " + BytesToString(NewFilesProgress) + " / " + BytesToString(NewFilesSize) + ", Modified files : " + BytesToString(ModifiedFilesProgress) + " / " + BytesToString(ModifiedFilesSize) + ", Overall : " + BytesToString(ToBeCopiedProgress) + " / " + BytesToString(ToBeCopiedSize), logLevel);
        }
    }
}
