using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TestYA1
{
    [Serializable]
    public class Job
    {
        public Job()
        {

        }

        public Job (Guid id, string instartpath, string inendpath)
        {
            ID = id;
            InStartPath = instartpath;
            InEndPath = inendpath;
        }

        public Guid ID { get; set; }
        public string InStartPath { get; set; }
        public string InEndPath { get; set; }
        public string Status { get; set; }
        public int Percent { get; set; }
    }
}
