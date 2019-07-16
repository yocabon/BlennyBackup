using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlennyBackup.Diagnostics;

namespace BlennyBackup.Core
{
    /// <summary>
    /// Base class for comparing file differences
    /// </summary>
    internal abstract class FolderDiffBase: IDisposable
    {
        /// <summary>
        /// Path to the source folder
        /// </summary>
        public string SourcePath { get; protected set; }

        /// <summary>
        /// Path to the target folder
        /// </summary>
        public string TargetPath { get; protected set; }

        /// <summary>
        /// List of files that are only in the source folder (new files)
        /// </summary>
        public string[] SourceOnlyFiles { get; protected set; }

        /// <summary>
        /// List of files that are only in the target folder (files that have been removed)
        /// </summary>
        public string[] TargetOnlyFiles { get; protected set; }

        /// <summary>
        /// List of files that are different in source and target folders (files that have been edited)
        /// </summary>
        public string[] ModifiedFiles { get; protected set; }

        /// <summary>
        /// Common files are source files that are also in target (so not in source only)
        /// </summary>
        public string[] CommonFiles { get; protected set; }

        /// <summary>
        /// Get all files and filter them
        /// </summary>
        /// <param name="sourcePath">Path to the source folder</param>
        /// <param name="targetPath">Path to the target folder</param>
        /// <param name="filterPattern">Filter pattern for GetFiles</param>
        /// <param name="ignoreList">Files or folders to ignore</param>
        public void InitFileArrays(string sourcePath, string targetPath, string filterPattern, string[] ignoreList)
        {
            this.SourcePath = sourcePath;
            this.TargetPath = targetPath;

            // Get the list of all files in both source and target
            string[] SourceFileList = Directory.GetFiles(sourcePath, filterPattern, SearchOption.AllDirectories).Select(s => s.Replace("\\", "/").Replace(sourcePath, "")).Where(s => !ignoreList.Any(w => s.Contains(w))).ToArray();
            string[] TargetFileList = Directory.GetFiles(targetPath, filterPattern, SearchOption.AllDirectories).Select(s => s.Replace("\\", "/").Replace(targetPath, "")).Where(s => !ignoreList.Any(w => s.Contains(w))).ToArray();

            // Remove files from the other list in order to get new or removed files
            this.SourceOnlyFiles = SourceFileList.Except(TargetFileList).ToArray();
            this.TargetOnlyFiles = TargetFileList.Except(SourceFileList).ToArray();

            // Common files are source files that are also in target (so not in source only)
            CommonFiles = SourceFileList.Except(SourceOnlyFiles).ToArray();
        }

        /// <summary>
        /// Fill <see cref="ModifiedFiles"/>
        /// </summary>
        /// <param name="reportCount">Number of reports output to the console per section</param>
        public abstract void ComputeModifiedFilesList(int reportCount);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion


    }
}
