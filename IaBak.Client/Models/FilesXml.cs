using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;



namespace IaBak.Models
{

    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot("files", IsNullable = false)]
    public partial class FilesXml
    {

        [XmlElement("file")]
        public FileXml[] files { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public partial class FileXml
    {
        [XmlElement] public long? mtime { get; set; }
        [XmlElement] public long? size { get; set; }
        [XmlElement] public string md5 { get; set; }
        [XmlElement] public string crc32 { get; set; }
        [XmlElement] public string sha1 { get; set; }
        [XmlElement] public string format { get; set; }
        [XmlElement] public string original { get; set; }
        [XmlElement] public bool? @private { get; set; }

        [XmlAttribute] public string name { get; set; }
        [XmlAttribute] public string source { get; set; }
    }


}
