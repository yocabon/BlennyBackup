using System.Xml.Serialization;

namespace BlennyBackup.Configuration
{
    /// <summary>
    /// Container for the XmlConfig
    /// </summary>
    [XmlRoot(ElementName = "PairConfig")]
    public class PairConfig
    {
        /// <summary>
        /// Array of <see cref="Pair"/>
        /// </summary>
        [XmlElement(ElementName = "Pair")]
        public Pair[] PairArray { get; set; }

        /// <summary>
        /// Array of <see cref="DriveConfig"/>
        /// </summary>
        [XmlElement(ElementName = "DriveConfig")]
        public DriveConfig[] DriveConfigArray { get; set; }
    }
}
