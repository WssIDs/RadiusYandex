using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace UpdaterCreator
{
    [Serializable]
    public class UpdateAppData
    {
        public UpdateAppData()
        {
            AppID = "Default";
            Version = "";
            Description = "";
            LaunchArgs = "";
        }

        public string AppID { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string LaunchArgs { get; set; }
    }

    [Serializable]
    public class UpdateFileData
    {
        public UpdateFileData()
        {
        }

        [XmlIgnore]
        public string FullName { get; set; }
        public string Url { get; set; }
        public string FileName { get; set; }
        public string Md5 { get; set; }
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UpdateFileData Mainfile { get; set; }
        UpdateAppData AppData { get; set; }
        ObservableCollection<UpdateFileData> files = new ObservableCollection<UpdateFileData>();

        public MainWindow()
        {
            InitializeComponent();

            files_lb.DataContext = files;

            Mainfile = new UpdateFileData();
            AppData = new UpdateAppData();

            mainfile_stack.DataContext = AppData;
        }

        private void Open_bt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open_dlg = new OpenFileDialog();
            open_dlg.Multiselect = true;

            List<UpdateFileData> newfiles = files.ToList();

            if(open_dlg.ShowDialog() == true)
            {

                foreach (string file in open_dlg.FileNames)
                {
                    UpdateFileData filedata = new UpdateFileData();
                    filedata.FullName = file;
                    filedata.Url = urlserver_tb.Text + System.IO.Path.GetFileName(file);
                    filedata.FileName = System.IO.Path.GetFileName(file);
                    filedata.Md5 = Hasher.Hashfile(file,HashType.MD5);

                    newfiles.Add(filedata);
                }

                newfiles = newfiles.Distinct().ToList();

                files.Clear();
                foreach (UpdateFileData newfile in newfiles)
                {
                    files.Add(newfile);
                }
            }
        }

        private void Execadd_bt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open_dlg = new OpenFileDialog();
            open_dlg.Multiselect = false;

            //mainfile = new UpdateFileData();

            if (open_dlg.ShowDialog() == true)
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(open_dlg.FileName);
                string version = assemblyName.Version.ToString();
                AppData.Version = version;

                version_tb.Text = version;

                AppData.AppID = System.IO.Path.GetFileNameWithoutExtension(open_dlg.FileName);
                AppID_tb.Text = AppData.AppID;

                Mainfile.FullName = open_dlg.FileName;
                Mainfile.Url = urlserver_tb.Text + System.IO.Path.GetFileName(open_dlg.FileName);
                Mainfile.FileName = System.IO.Path.GetFileName(open_dlg.FileName);
                Mainfile.Md5 = Hasher.Hashfile(open_dlg.FileName, HashType.MD5);

                mainfilepath_tb.Text = Mainfile.FullName;
                mainfilenameurl_tb.Text = Mainfile.Url;
                mainfilename_tb.Text = Mainfile.FileName;
                mainfilehash_tb.Text = Mainfile.Md5;
            }
        }

        private void Savexml_bt_Click(object sender, RoutedEventArgs e)
        {

            files.Insert(0, Mainfile);

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "", ""));

            // RadiusUpdate
            XmlElement radiusUpdate = doc.CreateElement("RadiusUpdate");

            /// Update
            XmlElement update = doc.CreateElement("update");
            update.SetAttribute("appId", AppData.AppID);


            XmlElement version = doc.CreateElement("version");
            version.InnerText = AppData.Version;
            XmlElement description = doc.CreateElement("description");
            description.InnerText = AppData.Description;
            XmlElement launchArgs = doc.CreateElement("launchArgs");
            launchArgs.InnerText = AppData.LaunchArgs;

            update.AppendChild(version);
            update.AppendChild(description);
            update.AppendChild(launchArgs);
            /////////////////////////////

            /// Files
            XmlElement filesnode = doc.CreateElement("files");

            foreach (UpdateFileData item in files)
            {
                XmlElement file = doc.CreateElement("file");

                XmlElement url = doc.CreateElement("url");
                url.InnerText = item.Url;
                XmlElement filename = doc.CreateElement("filename");
                filename.InnerText = item.FileName;
                XmlElement md5 = doc.CreateElement("md5");
                md5.InnerText = item.Md5;

                file.AppendChild(url);
                file.AppendChild(filename);
                file.AppendChild(md5);

                filesnode.AppendChild(file);
            }

            radiusUpdate.AppendChild(update);
            radiusUpdate.AppendChild(filesnode);
            ///////////////

            doc.AppendChild(radiusUpdate);

            string path = AppData.AppID + "_" + AppData.Version;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);
            doc.Save(path+"\\" +"update.xml");

            files.RemoveAt(0);

            File.Copy(Mainfile.FullName, path + "\\"+Mainfile.FileName, true);

            foreach(UpdateFileData item in files)
            {
                File.Copy(item.FullName, path + "\\" + item.FileName, true);
            }

            MessageBox.Show("Успешная подготовка файлов для обновления","Завершение генерации",MessageBoxButton.OK,MessageBoxImage.Information);

            //// передаем в конструктор тип класса
            //XmlSerializer formatter = new XmlSerializer(typeof(UpdateAppData));

            //// получаем поток, куда будем записывать сериализованный объект
            //using (FileStream fs = new FileStream("update.xml", FileMode.OpenOrCreate))
            //{
            //    formatter.Serialize(fs, AppData);

            //    Console.WriteLine("Объект сериализован");
            //}
        }

        private void Clear_bt_Click(object sender, RoutedEventArgs e)
        {
            files.Clear();
        }
    }
}
