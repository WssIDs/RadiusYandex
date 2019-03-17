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
            List <FileUpdateStruct> newfileData = new List<FileUpdateStruct>();
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
