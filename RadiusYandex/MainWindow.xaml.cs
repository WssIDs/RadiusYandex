using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

using RadiusYandex.Windows;
using YandexDisk.Client.Protocol;
using RadiusYandex.Properties;
using System.IO;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

using RadiusYandex.Models;
using System.Xml;
using NLog;
using System.Runtime.InteropServices;
using System.Reflection;

namespace RadiusYandex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool autostart = false;
        public bool autoclose = false;
        //ObservableCollection<Job> jobs = new ObservableCollection<Job>();
        //ObservableCollection<Job> downloadjobs = new ObservableCollection<Job>();

        public SavedJob MainJob = new SavedJob();
        DiskInfo diskInfo = new DiskInfo();
        Mutex mutexObj;
        Logger logger = LogManager.GetCurrentClassLogger();

        IDiskApi DiskApi;

        public MainWindow()
        {
            bool existed;
            // получаем GIUD приложения
            string guid = Marshal.GetTypeLibGuidForAssembly(Assembly.GetExecutingAssembly()).ToString();

            mutexObj = new Mutex(true, guid, out existed);
            if (existed)
            {
                InitializeComponent();

                logger.Trace("Запуск приложения");

                if (File.Exists("jobs.xml"))
                {
                    // передаем в конструктор тип класса
                    XmlSerializer formatter = new XmlSerializer(typeof(SavedJob));

                    // десериализация
                    using (FileStream fs = new FileStream("jobs.xml", FileMode.OpenOrCreate))
                    {
                        XmlReader reader = new XmlTextReader(fs);
                        if (formatter.CanDeserialize(reader))
                        {
                            SavedJob newjob = (SavedJob)formatter.Deserialize(reader);

                            if (newjob != null)
                            {
                                MainJob.UploadJobs = newjob.UploadJobs;
                                MainJob.DownloadJobs = newjob.DownloadJobs;
                            }
                        }
                    }
                }

                db_ToYandex.DataContext = MainJob.UploadJobs;
                db_FromYandex.DataContext = MainJob.DownloadJobs;
            }
            else
            {
                MessageBox.Show("Приложение уже запущено! Запуск более одной копии приложения невозможен", "Ошибка",MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Error("Попытка запуска второй копии приложения");
                Close();
            }
        }

        async Task UploadSample(string token = "")
        {
            if (MainJob.UploadJobs != null)
            {
                for (int n = 0; n < MainJob.UploadJobs.Count; n++)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(MainJob.UploadJobs[n].LocalPath);

                    if (dirInfo != null)
                    {
                        List<FileInfo> files = MainJob.UploadJobs[n].Filter.Split('|').SelectMany(filter => dirInfo.GetFiles(filter,SearchOption.TopDirectoryOnly)).ToList();


                        //CreateDirectoryAsync(MainJob.UploadJobs[n].ExternalPath);

                        if (files != null)
                        {
                            MainJob.UploadJobs[n].Status = "Загрузка...";
                            db_ToYandex.Items.Refresh();
                            upload.IsEnabled = false;
                            db_ToYandex.IsEnabled = false;
                            MainJob.UploadJobs[n].Percent = 0;
                            //You should have oauth token from Yandex Passport.
                            //See https://tech.yandex.ru/oauth/
                            //string oauthToken = "AQAAAAAYlmlrAADLWyLT7ciABU3uoL5tD-_sRcQ";

                            // Create a client instance
                            //IDiskApi diskApi = new DiskHttpApi(token);

                            for (int i = 0; i < files.Count; i++)
                            {
                                MainJob.UploadJobs[n].Percent = (i + 1) * 100 / files.Count;
                                MainJob.UploadJobs[n].Status = i + 1 + " из " + files.Count + " - Файл : " + files[i].FullName + " - Загружается";
                                db_ToYandex.Items.Refresh();

                                //logger.Info(i + 1 + " из " + files.Count + " - Файл : " + files[i].FullName + " - Загружается");
                                //Upload file from local

                                await DiskApi.Files.UploadFileAsync(path: "/" + MainJob.UploadJobs[n].ExternalPath + "/" + files[i].Name,
                                                                    overwrite: true,
                                                                    localFile: files[i].FullName,
                                                                    cancellationToken: CancellationToken.None);

                                MainJob.UploadJobs[n].Status = i + 1 + " из " + files.Count + " - Файл : " + files[i].FullName + " - Загружен";
                                logger.Info(i + 1 + " из " + files.Count + " - Файл : " + files[i].FullName + " - Загружен");
                                MainJob.UploadJobs[n].Percent = (i+1) * 100 / files.Count;
                                db_ToYandex.Items.Refresh();

                                if (MainJob.UploadJobs[n].DeleteFiles)
                                {
                                    logger.Info("Удаление исходного файла: " + files[i]);
                                    files[i].Delete();
                                }
                            }

                            db_ToYandex.Items.Refresh();
                            logger.Info("Все файлы загружены на Яндекс.Диск");
                            MainJob.UploadJobs[n].Status = "Выполнено";
                            MainJob.UploadJobs[n].Percent = 100;
                            upload.IsEnabled = true;
                            db_ToYandex.IsEnabled = true;
                        }
                        else
                        {
                            Logger logger = LogManager.GetCurrentClassLogger();
                            //System.Windows.MessageBox.Show("Файлы в директории отсутствуют"); LOG
                            logger.Warn("Файлы в директории отсутствуют");
                        }
                    }
                }
            }
        }

        async Task DownloadAllFilesInFolder(string token = "")
        {
            if (MainJob.DownloadJobs != null)
            {
                foreach (Job job in MainJob.DownloadJobs)
                {
                    db_FromYandex.Items.Refresh();
                    upload.IsEnabled = false;
                    db_FromYandex.IsEnabled = false;

                    // Create a client instance
                    //IDiskApi diskApi = new DiskHttpApi(token);

                    //CreateDirectoryAsync(job.ExternalPath);

                    //Getting information about folder /foo and all files in it
                    Resource fooResourceDescription = await DiskApi.MetaInfo.GetInfoAsync(
                        new ResourceRequest
                        {
                            Path = "/" + job.ExternalPath, //Folder on Yandex Disk
                    }, CancellationToken.None);

                    //Getting all files from response
                    IEnumerable<Resource> allFilesInFolder =
                        fooResourceDescription.Embedded.Items.Where(item => item.Type == ResourceType.File);

                    //Path to local folder for downloading files
                    string localFolder = job.LocalPath;

                    if (allFilesInFolder != null)
                    {
                        int index = 0;
                        job.Percent = 0;

                        var SortedFiles = GetSortedfilesByFilter(allFilesInFolder,job.Filter);

                        foreach (var item in SortedFiles)
                        {
                            job.Percent = (index + 1) * 100 / allFilesInFolder.Count();
                            //logger.Info(index + 1 + " из " + allFilesInFolder.Count() + " - Файл : " + item.Name + " - Загружается");
                            job.Status = index + 1 + " из " + allFilesInFolder.Count() + " - Файл : " + item.Name + " - Загружается";
                            db_FromYandex.Items.Refresh();
                            //Upload file to local
                            await DiskApi.Files.DownloadFileAsync(path: item.Path, localFile: Path.Combine(localFolder, item.Name));
                            job.Status = index + 1 + " из " + allFilesInFolder.Count() + " - Файл : " + item.Name + " - Загружен";
                            logger.Info(index + 1 + " из " + allFilesInFolder.Count() + " - Файл : " + item.Name + " - Загружен");
                            job.Percent = (index + 1) * 100 / allFilesInFolder.Count();
                            db_FromYandex.Items.Refresh();

                            if (job.DeleteFiles)
                            {
                                await DiskApi.Commands.DeleteAsync(new DeleteFileRequest
                                {
                                    Path = item.Path
                                });

                                logger.Info("Удаление файла c Яндекс.Диска: " + item.Path);
                            }

                            index++;
                        }
                    }

                    db_FromYandex.Items.Refresh();
                    job.Status = "Выполнено";
                    logger.Info("Все файлы загружены на локальный компьютер");
                    job.Percent = 100;
                    db_FromYandex.IsEnabled = true;

                    await DiskApi.Commands.EmptyTrashAsync(path: "/");
                }
            }
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            //MessageBox.Show(client.AccessToken);
            try
            {
                await UploadSample(Settings.Default.token);
                await DownloadAllFilesInFolder(Settings.Default.token);
            }
            catch (Exception ex)
            {
                //System.Windows.MessageBox.Show(ex.Message); LOG

                logger.Warn(ex.Message);

                YandexAuthWindow yaDlg = new YandexAuthWindow();
                yaDlg.Owner = this;

                if(yaDlg.ShowDialog() == true)
                {
                    await UploadSample(Settings.Default.token);
                    await DownloadAllFilesInFolder(Settings.Default.token);
                }
                else
                {
                    //System.Windows.MessageBox.Show(ex.Message); LOG
                    logger.Warn(ex.Message);
                }
            }

            if (autoclose)
            {
                Task.WaitAll();
                upload.IsEnabled = true;
                logger.Info("Автозавершение работы приложения по окончанию всех задач");
                Close();
            }
            else
            {
                Task.WaitAll();
                upload.IsEnabled = true;
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
            Logger logger = LogManager.GetCurrentClassLogger();
            Settings.Default.Save();

            // передаем в конструктор тип класса
            XmlSerializer uploadformatter = new XmlSerializer(typeof(SavedJob));

            if (MainJob.UploadJobs != null || MainJob.DownloadJobs != null)
            {
                //// получаем поток, куда будем записывать сериализованный объект
                using (FileStream fs = new FileStream("jobs.xml", FileMode.Create))
                {

                    uploadformatter.Serialize(fs,MainJob);
                    logger.Info("Успешное сохранение задач в файле jobs.xml");

                }
            }

            logger.Debug("Завершение приложения");
            LogManager.Shutdown();
        }

        public async void RunAllTask()
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            try
            {
                await UploadSample(Settings.Default.token);
                await DownloadAllFilesInFolder(Settings.Default.token);
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message);

                YandexAuthWindow yaDlg = new YandexAuthWindow();
                yaDlg.Owner = this;

                if (yaDlg.ShowDialog() == true)
                {
                    await UploadSample(Settings.Default.token);
                    await DownloadAllFilesInFolder(Settings.Default.token);
                }
                else
                {
                    logger.Warn(ex.Message);
                }
            }

            if (autoclose)
            {
                Task.WaitAll();
                logger.Info("Автозавершение работы приложения по окончанию всех задач");
                Close();
            }
        }

        private void AddToYa_bt_Click(object sender, RoutedEventArgs e)
        {
            Job job = new Job();
            job.ID = Guid.NewGuid();

            MainJob.UploadJobs.Add(job);
        }

        private void RemToYa_bt_Click(object sender, RoutedEventArgs e)
        {
            if(db_ToYandex.SelectedItem != null)
            {
                Job selectedjob = (Job)db_ToYandex.SelectedItem;

                if(selectedjob != null)
                {
                    MainJob.UploadJobs.Remove(selectedjob);
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

            MainJob.DownloadJobs.Add(job);
        }

        private void RemFromYa_bt_Click(object sender, RoutedEventArgs e)
        {
            if (db_FromYandex.SelectedItem != null)
            {
                Job selectedjob = (Job)db_FromYandex.SelectedItem;

                if (selectedjob != null)
                {
                    MainJob.DownloadJobs.Remove(selectedjob);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Не выбрано поле для удаления");
            }
        }

        async void GetDiskSpace()
        {
            DiskApi = Authorize();

            if (DiskApi != null)
            {
                Disk result = await DiskApi.MetaInfo.GetDiskInfoAsync();

                if (result != null)
                {
                    logger.Trace("Получение информации о диске");
                    diskInfo.TotalSpace = result.TotalSpace / Math.Pow(2, 30);
                    diskInfo.UsedSpace = result.UsedSpace / Math.Pow(2, 30);
                    diskInfo.TrashSpace = result.TrashSize / Math.Pow(2, 30);

                    usedspace_tb.Text = "Использовано: " + Math.Round(diskInfo.UsedSpace, 2).ToString() + " Гб";
                    totalspace_tb.Text = "Всего: " + diskInfo.TotalSpace.ToString() + " Гб";
                }
                else
                {
                    logger.Warn("Ошибка получения информации о диске");
                }
            }
        }

        private IDiskApi Authorize()
        {
            try
            {
                IDiskApi TestdiskApi = new DiskHttpApi(Settings.Default.token);
                logger.Trace("Получение токена");

                if (TestdiskApi != null)
                {
                    Disk result = TestdiskApi.MetaInfo.GetDiskInfoAsync().Result;
                    logger.Trace("Успешное получение токена");
                    return TestdiskApi;
                }
                else
                {
                    logger.Error("Ошибка создания экземпляра класса DiskApi");
                }
            }
            catch (Exception ex)
            {
                logger.Warn("Ошибка получения токена: " + ex.Message);

                logger.Trace("Попытка получения токена: Инциализация диалогового окна регистраци приложения Яндекс.Диска");

                YandexAuthWindow yaDlg = new YandexAuthWindow();
                yaDlg.Owner = this;

                if (yaDlg.ShowDialog() == true)
                {
                    IDiskApi TestdiskApi = new DiskHttpApi(Settings.Default.token);
                    logger.Trace("Получение токена");

                    if (TestdiskApi != null)
                    {
                        Disk result = TestdiskApi.MetaInfo.GetDiskInfoAsync().Result;
                        logger.Trace("Успешное получение токена");
                        return TestdiskApi;
                    }
                    else
                    {
                        logger.Error("Ошибка создания экземпляра класса DiskApi");
                    }
                }
                else
                {
                    logger.Error("Ошибка инициализации диалогового окна регистраци приложения Яндекс.Диска: " + ex.Message);
                }
            }

            return null;
        }

        private void MainYandexSync_Loaded(object sender, RoutedEventArgs e)
        {
            GetDiskSpace();

            Task.WaitAll();

            if (autostart)
            {
                RunAllTask();
            }
        }

        /// <summary>
        /// Фильтр файлов на гугл диске
        /// </summary>
        /// <param name="files"></param>
        HashSet<Resource> GetSortedfilesByFilter(IEnumerable<Resource> files,string filter = "*.*")
        {
            //string search = "*.docx|*.doc|Для проекта.*";

            List<string> filenames = new List<string>();
            List<string> extensions = new List<string>();

            var searcharray = filter.Split('|');

            foreach(var item in searcharray)
            {
                var newitems = item.Split('.');

                filenames.Add(newitems[0]);
                extensions.Add("."+newitems[1]);
            }


            HashSet<Resource> sortedfiles = new HashSet<Resource>();

            for (int i = 0; i < filenames.Count; i++)
            {
                foreach (var file in files)
                {
                    string filename = Path.GetFileNameWithoutExtension(file.Name);
                    string extension = Path.GetExtension(file.Name);

                    if (filenames[i] == "*" && extensions[i] == "*")
                    {
                        sortedfiles.Add(file);
                    }
                    if (filenames[i] == filename && extensions[i] == extension)
                    {
                        sortedfiles.Add(file);
                    }
                    if(filenames[i] == "*" && extensions[i] == extension)
                    {
                        sortedfiles.Add(file);
                    }
                    if (filenames[i] == "*" && (extensions[i].Length >= 2 && extensions[i].Contains("*")))
                    {
                        if (extension.Contains(extensions[i].Remove(extensions[i].Length - 1)))
                        {
                            sortedfiles.Add(file);
                        }
                    }
                    if (filenames[i] == filename && extensions[i] == "*")
                    {
                        sortedfiles.Add(file);
                    }
                    if ((filenames[i].Length >= 2 && filenames[i].Contains("*")) && extensions[i] == "*")
                    {
                        if (filename.Contains(filenames[i].Remove(filenames[i].Length - 1)))
                        {
                            sortedfiles.Add(file);
                        }
                    }
                    if ((filenames[i].Length >= 2 && filenames[i].Contains("*")) && (extensions[i].Length >= 2 && extensions[i].Contains("*")))
                    {
                        if (filename.Contains(filenames[i].Remove(filenames[i].Length - 1)) && extension.Contains(extensions[i].Remove(extensions[i].Length - 1)))
                        {
                            sortedfiles.Add(file);
                        }
                    }

                    if ((filenames[i].Length >= 2 && filenames[i].Contains("*")) && extensions[i] == extension)
                    {
                        if (filename.Contains(filenames[i].Remove(filenames[i].Length - 1)))
                        {
                            sortedfiles.Add(file);
                        }
                    }

                    if (filenames[i] == filename && (extensions[i].Length >= 2 && extensions[i].Contains("*")))
                    {
                        if (extension.Contains(extensions[i].Remove(extensions[i].Length - 1)))
                        {
                            sortedfiles.Add(file);
                        }
                    }
                }
            }

            return sortedfiles;
        }


        async void CreateDirectoryAsync(string Path)
        {
            string[] paths = Path.Split('/');

            string currentpath = "";
            foreach (string path in paths)
            {
                currentpath += "/"+path;

                try
                {
                    Resource fooResourceDescription = await DiskApi.MetaInfo.GetInfoAsync(
                        new ResourceRequest
                        {
                            Path = currentpath, //Folder on Yandex Disk
                        });

                }
                catch (Exception ex)
                {
                    await DiskApi.Commands.CreateDictionaryAsync
                    (
                        path: currentpath
                    );
                }
            }

            Task.WaitAll();
        }
    }
}
