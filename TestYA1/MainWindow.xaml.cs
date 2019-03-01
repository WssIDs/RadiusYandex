using Nemiro.OAuth;
using Nemiro.OAuth.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

using TestYA1.Windows;
using Microsoft.Win32;
using YandexDisk.Client.Protocol;
using System.Windows.Forms;
using TestYA1.Properties;
using System.IO;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace TestYA1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ObservableCollection<Job> jobs = new ObservableCollection<Job>();
        ObservableCollection<Job> downloadjobs = new ObservableCollection<Job>();

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("jobs.xml"))
            {
                // передаем в конструктор тип класса
                XmlSerializer formatter = new XmlSerializer(typeof(Job[]));

                //// получаем поток, куда будем записывать сериализованный объект
                //using (FileStream fs = new FileStream("persons.xml", FileMode.OpenOrCreate))
                //{
                //    formatter.Serialize(fs, person);

                //    Console.WriteLine("Объект сериализован");
                //}

                // десериализация
                using (FileStream fs = new FileStream("jobs.xml", FileMode.OpenOrCreate))
                {
                    Job[] newjobs = (Job[])formatter.Deserialize(fs);

                    foreach(var job in newjobs)
                    {
                        job.Status = "";
                        job.Percent = 0;
                        jobs.Add(job);
                    }
                }
            }

            db_ToYandex.DataContext = jobs;


            if (File.Exists("downloadjobs.xml"))
            {
                // передаем в конструктор тип класса
                XmlSerializer formatter = new XmlSerializer(typeof(Job[]));

                //// получаем поток, куда будем записывать сериализованный объект
                //using (FileStream fs = new FileStream("persons.xml", FileMode.OpenOrCreate))
                //{
                //    formatter.Serialize(fs, person);

                //    Console.WriteLine("Объект сериализован");
                //}

                // десериализация
                using (FileStream fs = new FileStream("downloadjobs.xml", FileMode.OpenOrCreate))
                {
                    Job[] newjobs = (Job[])formatter.Deserialize(fs);

                    foreach (var job in newjobs)
                    {
                        job.Status = "";
                        job.Percent = 0;
                        downloadjobs.Add(job);
                    }
                }
            }

            db_FromYandex.DataContext = downloadjobs;

            //INstartupPath_tb.Text = Settings.Default.INstartPath;
            //INendPath_tb.Text = Settings.Default.INendPath;

            if (Settings.Default.autostart)
            {
                RunAllTask();
            }
        }

        async Task UploadSample(string token = "")
        {
            if (jobs != null)
            {
                for (int n = 0; n < jobs.Count; n++)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(jobs[n].InStartPath);

                    if (dirInfo != null)
                    {
                        List<FileInfo> files = dirInfo.GetFiles().ToList();


                        if (files != null)
                        {
                            jobs[n].Status = "Загрузка...";
                            db_ToYandex.Items.Refresh();
                            upload.IsEnabled = false;
                            db_ToYandex.IsEnabled = false;
                            //You should have oauth token from Yandex Passport.
                            //See https://tech.yandex.ru/oauth/
                            //string oauthToken = "AQAAAAAYlmlrAADLWyLT7ciABU3uoL5tD-_sRcQ";

                            // Create a client instance
                            IDiskApi diskApi = new DiskHttpApi(token);

                            for (int i = 0; i < files.Count; i++)
                            {
                                jobs[n].Percent = (i + 1) * 100 / files.Count;
                                jobs[n].Status = i + 1 + " из " + files.Count + " - Файл : " + files[i].FullName + " - Загружается";
                                db_ToYandex.Items.Refresh();
                                //Upload file from local
                                await diskApi.Files.UploadFileAsync(path: "/" + jobs[n].InEndPath + "/" + files[i].Name,
                                                                    overwrite: true,
                                                                    localFile: files[i].FullName,
                                                                    cancellationToken: CancellationToken.None);

                                jobs[n].Status = i + 1 + " из " + files.Count + " - Файл : " + files[i].FullName + " - Загружен";
                                jobs[n].Percent = (i+1) * 100 / files.Count;
                                db_ToYandex.Items.Refresh();

                                files[i].Delete();
                            }

                            db_ToYandex.Items.Refresh();
                            jobs[n].Status = "Выполнено";
                            upload.IsEnabled = true;
                            db_ToYandex.IsEnabled = true;
                            //Task.WaitAll();
                            //Close();
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Файлы в директории отсутствуют");
                        }
                    }
                }
            }
        }

        async Task DownloadAllFilesInFolder(string token = "")
        {
            if (downloadjobs != null)
            {
                foreach (Job job in downloadjobs)
                {
                    db_FromYandex.Items.Refresh();
                    upload.IsEnabled = false;
                    db_ToYandex.IsEnabled = false;

                    // Create a client instance
                    IDiskApi diskApi = new DiskHttpApi(token);

                    //Getting information about folder /foo and all files in it
                    Resource fooResourceDescription = await diskApi.MetaInfo.GetInfoAsync(
                        new ResourceRequest
                        {
                            Path = "/" + job.InEndPath, //Folder on Yandex Disk
                    }, CancellationToken.None);

                    //Getting all files from response
                    IEnumerable<Resource> allFilesInFolder =
                        fooResourceDescription.Embedded.Items.Where(item => item.Type == ResourceType.File);

                    //Path to local folder for downloading files
                    string localFolder = job.InStartPath;

                    //Run all downloadings in parallel. DiskApi is thread safe.
                    IEnumerable<Task> downloadingTasks =
                        allFilesInFolder.Select(file =>
                          diskApi.Files.DownloadFileAsync(path: file.Path, localFile: System.IO.Path.Combine(localFolder, file.Name)));

                    db_FromYandex.Items.Refresh();
                    job.Status = "Выполнено";
                    upload.IsEnabled = true;
                    db_FromYandex.IsEnabled = true;

                    //Wait all done
                    await Task.WhenAll(downloadingTasks);

                    foreach (var item in allFilesInFolder)
                    {
                        await diskApi.Commands.DeleteAsync(new DeleteFileRequest
                        {
                            Path = item.Path
                        });
                    }
                }
            }
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(client.AccessToken);
            try
            {
                await UploadSample(Settings.Default.token);
                await DownloadAllFilesInFolder(Settings.Default.token);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);

                YandexAuthWindow yaDlg = new YandexAuthWindow();
                yaDlg.Owner = this;

                if(yaDlg.ShowDialog() == true)
                {
                    await UploadSample(Settings.Default.token);
                    await DownloadAllFilesInFolder(Settings.Default.token);
                }
                else
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }

            if (Settings.Default.autoclose)
            {
                Task.WaitAll();
                Close();
            }
        }

        private void Selectfolder_bt_Click(object sender, RoutedEventArgs e)
        {
            //using (var dialog = new FolderBrowserDialog())
            //{
            //    dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            //    dialog.SelectedPath = INstartupPath_tb.Text;
            //    if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //    {
            //        INstartupPath_tb.Text = dialog.SelectedPath;
            //        //Settings.Default.INstartPath = INstartupPath_tb.Text;
            //    }
            //}
        }

        private void MainYandexSync_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.autostart = false;
            //Settings.Default.INendPath = INendPath_tb.Text;
            Settings.Default.Save();

            // передаем в конструктор тип класса
            XmlSerializer uploadformatter = new XmlSerializer(typeof(Job[]));

            if (jobs != null)
            {
                //// получаем поток, куда будем записывать сериализованный объект
                using (FileStream fs = new FileStream("jobs.xml", FileMode.OpenOrCreate))
                {

                    uploadformatter.Serialize(fs, jobs.ToArray());

                    Console.WriteLine("Объект сериализован");
                }
            }

            // передаем в конструктор тип класса
            XmlSerializer downloadformatter = new XmlSerializer(typeof(Job[]));

            if (downloadjobs != null)
            {
                //// получаем поток, куда будем записывать сериализованный объект
                using (FileStream fs = new FileStream("downloadjobs.xml", FileMode.OpenOrCreate))
                {

                    downloadformatter.Serialize(fs, downloadjobs.ToArray());

                    Console.WriteLine("Объект сериализован");
                }
            }

        }

        public async void RunAllTask()
        {
            try
            {
                await UploadSample(Settings.Default.token);
                await DownloadAllFilesInFolder(Settings.Default.token);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);

                YandexAuthWindow yaDlg = new YandexAuthWindow();
                yaDlg.Owner = this;

                if (yaDlg.ShowDialog() == true)
                {
                    await UploadSample(Settings.Default.token);
                    await DownloadAllFilesInFolder(Settings.Default.token);
                }
                else
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }

            if (Settings.Default.autoclose)
            {
                Task.WaitAll();
                Close();
            }
        }

        private void AddToYa_bt_Click(object sender, RoutedEventArgs e)
        {
            Job job = new Job();
            job.ID = Guid.NewGuid();

            jobs.Add(job);
        }

        private void RemToYa_bt_Click(object sender, RoutedEventArgs e)
        {
            if(db_ToYandex.SelectedItem != null)
            {
                Job selectedjob = (Job)db_ToYandex.SelectedItem;

                if(selectedjob != null)
                {
                    jobs.Remove(selectedjob);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Не выбрано поле для удаления");
            }
        }

        private void AddFromYa_bt_Click(object sender, RoutedEventArgs e)
        {
            Job job = new Job();
            job.ID = Guid.NewGuid();

            downloadjobs.Add(job);
        }

        private void RemFromYa_bt_Click(object sender, RoutedEventArgs e)
        {
            if (db_FromYandex.SelectedItem != null)
            {
                Job selectedjob = (Job)db_FromYandex.SelectedItem;

                if (selectedjob != null)
                {
                    downloadjobs.Remove(selectedjob);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Не выбрано поле для удаления");
            }
        }
    }
}
