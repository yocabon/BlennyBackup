using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlennyBackup;
using System.IO;

namespace BlennyBackupTest
{
    [TestClass]
    public class TestXMLBinary
    {
        [TestMethod]
        public void TestBinary()
        {
            Tools.PrepareTest(out string sourcePath, out string targetPath, out string logsPath);

            BlennyBackup.Configuration.Pair pair = new BlennyBackup.Configuration.Pair()
            {
                FilterPattern = "*",
                SourcePath = sourcePath,
                TargetPath = targetPath
            };

            BlennyBackup.Configuration.PairConfig pairConfig = new BlennyBackup.Configuration.PairConfig
            {
                ComparisonMode = BlennyBackup.Options.ComparisonMode.Binary,
                PairArray = new BlennyBackup.Configuration.Pair[] { pair },
            };

            Tools.GenerateXML(pairConfig, out string xmlPath);

            BlennyBackup.Options.XmlConfig xmlConfig = new BlennyBackup.Options.XmlConfig
            {
                ConfigFilePath = new string[] { xmlPath },
                LogFilePath = logsPath,
                ReportCount = 1,
                FlushDelay = 5
            };

            Program.SyncXmlPairs(xmlConfig);

            Tools.AssertTarget(targetPath, logsPath, "Compared binary for ");
            Tools.CleanTarget(targetPath, logsPath, xmlPath);
        }
    }
}
