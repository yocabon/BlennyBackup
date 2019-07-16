using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlennyBackup;
using System.IO;

namespace BlennyBackupTest
{
    [TestClass]
    public class TestDirectDate
    {
        [TestMethod]
        public void TestDate()
        {
            Tools.PrepareTest(out string sourcePath, out string targetPath, out string logsPath);

            BlennyBackup.Options.DirectPair directPair = new BlennyBackup.Options.DirectPair
            {
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FilterPattern = "*",
                LogFilePath = logsPath,
                ComparisonMode = BlennyBackup.Options.ComparisonMode.Date,
                ReportCount = 1,
                FlushDelay = 5
            };

            Program.SyncDirectPair(directPair);

            Tools.AssertTarget(targetPath, logsPath, " -- target = ");
            Tools.CleanTarget(targetPath, logsPath);
        }
    }
}
