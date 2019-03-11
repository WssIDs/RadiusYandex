using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RadiusYandex.Models
{
    [Serializable]
    public class SavedJob
    {
        public SavedJob()
        {
            //UploadJobs = new ObservableCollection<Job>();
           // DownloadJobs = new ObservableCollection<Job>();
            Jobs = new ObservableCollection<Job>();
        }

        [XmlArray("Jobs")]
        [XmlArrayItem("Job")]
        public ObservableCollection<Job> Jobs { get; set; }

        //[XmlArray("UploadJobs")]
        //[XmlArrayItem("Job")]
        //public ObservableCollection<Job> UploadJobs { get; set;}

        //[XmlArray("DownloadJobs")]
        //[XmlArrayItem("Job")]
        //public ObservableCollection<Job> DownloadJobs { get; set; }
    }
}
