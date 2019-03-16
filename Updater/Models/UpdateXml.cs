using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Updater
{
    public struct FileUpdateStruct
    {
        public Uri Uri;
        public string filename;
        public string md5;
    };

    public class UpdateXml
    {

        private Version version;
        private List<FileUpdateStruct> fileData = new List<FileUpdateStruct>();
        //private List<Uri> uries = new List<Uri>();
        //private List<string> filenames = new List<string>();
        //private string md5;
        private string description;
        private string launchArgs;

        public Version Version
        {
            get => this.version;
        }
        
        public List<FileUpdateStruct> FileData
        {
            get => this.fileData;
        }

        //public List<Uri> Uries
        //{
        //    get => this.uries;
        //}
        //public List<string> Filenames
        //{
        //    get => this.filenames;
        //}
        //public string Md5
        //{
        //    get => this.md5;
        //}
        public string Description
        {
            get => this.description;
        }
        public string LaunchArgs
        {
            get => this.launchArgs;
        }

        internal UpdateXml(Version version, List<FileUpdateStruct> fileData, string description, string launchArgs)
        {
            this.version = version;
            this.fileData = fileData;
            //this.uries = uries;
            //this.filenames = filenames;
            //this.md5 = md5;
            this.description = description;
            this.launchArgs = launchArgs;
        }

        internal bool IsNewerThan(Version version)
        {
            return this.version > version;
        }

        internal static bool ExistsOnServer(Uri location)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(location.AbsoluteUri);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                resp.Close();

                return resp.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        internal static UpdateXml Parse(Uri location, string appID)
        {
            Version version = null;
            //string url = "";
            List <FileUpdateStruct> newfileData = new List<FileUpdateStruct>();
            //string md5 = "";
            string description = "";
            string launchArgs = "";

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(location.AbsoluteUri);

                XmlNode node = doc.DocumentElement.SelectSingleNode("//update[@appId='" + appID + "']");

                if (node == null)
                    return null;

                version = Version.Parse(node["version"].InnerText);
                //url = node["url"].InnerText;
                //md5 = node["md5"].InnerText;
                description = node["description"].InnerText;
                launchArgs = node["launchArgs"].InnerText;

                XmlNode nodefiles = doc.DocumentElement.SelectSingleNode("//files");
                XmlNodeList xnList = nodefiles.SelectNodes("//file");
                foreach (XmlNode xn in xnList)
                {
                    FileUpdateStruct data;

                    string url = xn["url"].InnerText;
                    data.Uri = new Uri(url);
                    data.filename = xn["filename"].InnerText;
                    data.md5 = xn["md5"].InnerText;

                    newfileData.Add(data);
                    //string firstName = xn["FirstName"].InnerText;
                    //string lastName = xn["LastName"].InnerText;
                    Console.WriteLine("file: {0}", xn.InnerText);
                }

                return new UpdateXml(version, newfileData, description, launchArgs);

            }
            catch
            {
                return null;
            }
        }

    }
}
