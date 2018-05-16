using System.Xml.Serialization;

namespace BlennyBackup.Configuration
{
    /// <summary>
    /// A pair source directory / target directory
    /// </summary>
    [XmlRoot(ElementName = "Pair")]
    public class Pair
    {
        /// <summary>
        /// Path to the source folder
        /// </summary>
        [XmlElement(ElementName = "Source")]
        public string SourcePath { get; set; }

        /// <summary>
        /// Path to the target folder
        /// </summary>
        [XmlElement(ElementName = "Target")]
        public string TargetPath { get; set; }

        /// <summary>
        /// Filter pattern for GetFiles
        /// </summary>
        [XmlElement(ElementName = "Filter")]
        public string FilterPattern { get; set; }

        /// <summary>
        /// Filter pattern for GetFiles
        /// </summary>
        [XmlElement(ElementName = "Ignore")]
        public string[] IgnoreList { get; set; }
    }
}
