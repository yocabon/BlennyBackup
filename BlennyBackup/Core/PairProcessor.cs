using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlennyBackup.Diagnostics;
using BlennyBackup.Logging;

namespace BlennyBackup.Core
{
    /// <summary>
    /// All syncing logic goes here
    /// </summary>
    internal static class PairProcessor
    {
        /// <summary>
        /// Sync a source folder with a target folder (backup)
        /// </summary>
        /// <param name="SourcePath">Path to the source folder</param>
        /// <param name="TargetPath">Path to the target folder</param>
        /// <param name="FilterPattern">Filter pattern for GetFiles</param>
        /// <param name="IgnoreList">Files or folders to ignore</param>
        /// <param name="UseDate">Use last date of modification instead of hash</param>
        /// <param name="reportCount">Number of reports output to the console per section</param>
        public static void SyncPair(string SourcePath, string TargetPath, string FilterPattern, string[] IgnoreList, bool UseDate, int reportCount = 100)
        {
            if (SourcePath.Contains(TargetPath) || TargetPath.Contains(SourcePath))
                throw new System.Exception("Paths between source and target MUST BE COMPLETELY DIFFERENT");

            ProgressReporter.Logger.WriteLine("Starting syncing of pair " + SourcePath + " ---- " + TargetPath);

            ProgressReporter.Logger.WriteLine("Computing directory differences");

            // Remove folders first in order to try to gain some execution time
            string[] SourceDirectoryList = Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories).Select(s => s.Replace(SourcePath, "")).Where(s => IgnoreList.All(w => s.Contains(w))).ToArray();
            string[] TargetDirectoryList = Directory.GetDirectories(TargetPath, "*", SearchOption.AllDirectories).Select(s => s.Replace(TargetPath, "")).Where(s => IgnoreList.All(w => s.Contains(w))).ToArray();

            DeleteSourceOnlyDirectories(TargetPath, reportCount, SourceDirectoryList, TargetDirectoryList);

            ProgressReporter.Logger.WriteLine("Computing file differences");
            FolderDiff folderDiff = new FolderDiff(SourcePath, TargetPath, FilterPattern, IgnoreList, UseDate, reportCount);

            int errorCount = DeleteSourceOnlyFiles(folderDiff, reportCount);

            ProgressReporter progressReporter = new ProgressReporter(folderDiff);

            errorCount += AddSourceOnlyFiles(folderDiff, progressReporter, reportCount);
            errorCount += OverwriteModifiedFiles(folderDiff, progressReporter, reportCount);

            if (!UseDate)
            {
                string backupHashPath = Path.Combine(TargetPath, "blenny_backup_hash.txt");
                FileStream f = File.Create(backupHashPath);
                f.Close();

                File.WriteAllLines(backupHashPath, folderDiff.CommonFilesSourceHash);
            }

            ProgressReporter.Logger.WriteLine("Syncing of pair " + SourcePath + " ---- " + TargetPath + " ---- Ended with " + errorCount + " errors");
        }

        private static void DeleteSourceOnlyDirectories(string TargetPath, int reportCount, string[] SourceDirectoryList, string[] TargetDirectoryList)
        {
            string[] TargetOnlyDirectories = TargetDirectoryList.Except(SourceDirectoryList).ToArray();

            // Remove target only folders
            ProgressReporter.Logger.WriteLine(TargetOnlyDirectories.Length + " folders will be removed");

            int k = 0;
            int consoleTh = TargetOnlyDirectories.Length / reportCount;

            foreach (string folder in TargetOnlyDirectories)
            {
                string path = Path.Combine(TargetPath, folder);

                // Safeguard against directory recursive deleting
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                }
                catch (Exception) { }

                ProgressReporter.Logger.WriteLine("Deleted " + path + " --- " + k + " / " + TargetOnlyDirectories.Length + " folders", LogLevel.File);
                if (consoleTh == 0 || k % consoleTh == 0)
                {
                    ProgressReporter.Logger.WriteLine("Deleted " + k + " / " + TargetOnlyDirectories.Length + " folders", LogLevel.Console);
                }
            }
        }
        private static int DeleteSourceOnlyFiles(FolderDiff folderDiff, int reportCount)
        {
            // Remove target only files
            ProgressReporter.Logger.WriteLine(folderDiff.TargetOnlyFiles.Length + " files will be removed");

            int k = 0;
            int consoleTh = folderDiff.TargetOnlyFiles.Length / reportCount;
            int errorCount = 0;

            Parallel.For(0, folderDiff.TargetOnlyFiles.Length, new ParallelOptions() { MaxDegreeOfParallelism = Program.MaxDegreeOfParallelism }, i =>
            {
                string path = Path.Combine(folderDiff.TargetPath, folderDiff.TargetOnlyFiles[i]);

                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref errorCount);
                    ProgressReporter.Logger.WriteLine("\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\nERROR : " + e.ToString() + "\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\n");
                }

                int progress = Interlocked.Increment(ref k);
                ProgressReporter.Logger.WriteLine("Deleted " + path + " --- " + progress + " / " + folderDiff.TargetOnlyFiles.Length + " files", LogLevel.File);
                if (consoleTh == 0 || progress % consoleTh == 0)
                {
                    ProgressReporter.Logger.WriteLine("Deleted " + progress + " / " + folderDiff.TargetOnlyFiles.Length + " files", LogLevel.Console);
                }
            });
            return errorCount;
        }

        private static int AddSourceOnlyFiles(FolderDiff folderDiff, ProgressReporter progressReporter, int reportCount)
        {
            // Add source only files to target
            ProgressReporter.Logger.WriteLine("Copying " + folderDiff.SourceOnlyFiles.Length + " new files");

            int consoleTh = folderDiff.SourceOnlyFiles.Length / reportCount;
            int k = 0;
            int errorCount = 0;

            Parallel.For(0, folderDiff.SourceOnlyFiles.Length, new ParallelOptions() { MaxDegreeOfParallelism = Program.MaxDegreeOfParallelism }, i =>
            {
                string sourcePath = Path.Combine(folderDiff.SourcePath, folderDiff.SourceOnlyFiles[i]);
                string targetPath = Path.Combine(folderDiff.TargetPath, folderDiff.SourceOnlyFiles[i]);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.Copy(sourcePath, targetPath);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref errorCount);
                    ProgressReporter.Logger.WriteLine("\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\nERROR : " + e.ToString() + "\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\n");
                }
                progressReporter.AddNewFileProgress(i);

                int progress = Interlocked.Increment(ref k);
                progressReporter.WriteDetailedNewFileReport(sourcePath, targetPath, i, LogLevel.File);
                if (consoleTh == 0 || progress % consoleTh == 0)
                {
                    progressReporter.WriteReport(LogLevel.Console);
                }
            });
            progressReporter.WriteReport(LogLevel.Console | LogLevel.File);
            return errorCount;
        }

        private static int OverwriteModifiedFiles(FolderDiff folderDiff, ProgressReporter progressReporter, int reportCount)
        {
            // Replace target files that have been edited in source with their new version
            ProgressReporter.Logger.WriteLine("Overwritting " + folderDiff.ModifiedFiles.Length + " modified files");

            int consoleTh = folderDiff.ModifiedFiles.Length / reportCount;
            int k = 0;
            int errorCount = 0;

            Parallel.For(0, folderDiff.ModifiedFiles.Length, new ParallelOptions() { MaxDegreeOfParallelism = Program.MaxDegreeOfParallelism }, i =>
            {
                string sourcePath = Path.Combine(folderDiff.SourcePath, folderDiff.ModifiedFiles[i]);
                string targetPath = Path.Combine(folderDiff.TargetPath, folderDiff.ModifiedFiles[i]);

                try
                {
                    File.Copy(sourcePath, targetPath, true);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref errorCount);
                    ProgressReporter.Logger.WriteLine("\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\nERROR : " + e.ToString() + "\r\nERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR\r\n");
                }

                progressReporter.AddModifiedFileProgress(i);

                int progress = Interlocked.Increment(ref k);
                progressReporter.WriteDetailedModifiedFileReport(sourcePath, targetPath, i, LogLevel.File);
                if (consoleTh == 0 || progress % consoleTh == 0)
                {
                    progressReporter.WriteReport(LogLevel.Console);
                }
            });
            progressReporter.WriteReport(LogLevel.Console | LogLevel.File);
            return errorCount;
        }
    }
}
