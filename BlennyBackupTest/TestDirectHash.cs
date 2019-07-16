using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlennyBackup;
using System.IO;

namespace BlennyBackupTest
{
    [TestClass]
    public class TestDirectHash
    {
        [TestMethod]
        public void TestHash()
        {
            Tools.PrepareTest(out string sourcePath, out string targetPath, out string logsPath);

            BlennyBackup.Options.DirectPair directPair = new BlennyBackup.Options.DirectPair
            {
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FilterPattern = "*",
                LogFilePath = logsPath,
                ComparisonMode = BlennyBackup.Options.ComparisonMode.Hash,
                ReportCount = 1,
                FlushDelay = 5
            };

            Program.SyncDirectPair(directPair);

            Assert.IsTrue(File.Exists(Path.Combine(targetPath, "blenny_backup_hash.txt")));
            Tools.AssertTarget(targetPath, logsPath, "Processed hash for");
            Tools.CleanTarget(targetPath, logsPath);
        }
    }
}
