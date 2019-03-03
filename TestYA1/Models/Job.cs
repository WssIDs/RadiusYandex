using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace TestYA1.Models
{
    [Serializable]
    public class Job
    {
        public Job()
        {

        }

        public Job (Guid id, string localpath, string externalpath)
        {
            ID = id;
            LocalPath = localpath;
            ExternalPath = externalpath;
        }

        public Guid ID { get; set; }
        public string LocalPath { get; set; }
        public string ExternalPath { get; set; }
        [XmlIgnore]
        public string Status { get; set; }
        [XmlIgnore]
        public int Percent { get; set; }
    }
}
