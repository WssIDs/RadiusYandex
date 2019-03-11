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

using RadiusYandex.Models;
using System.Xml;
using NLog;
using System.Windows.Input;
using System.Web.UI.WebControls;
using System.Windows.Controls;

namespace RadiusYandex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool autostart = false;
        public bool autoclose = false;

        public SavedJob MainJob = new SavedJob();
        DiskInfo diskInfo = new DiskInfo();
        Logger logger = LogManager.GetCurrentClassLogger();
        IDiskApi DiskApi;

        public MainWindow()
        {
            DataContext = Settings.Default;
            InitializeComponent();

            if(Settings.Default.maximized == false)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }

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
                            MainJob.Jobs = newjob.Jobs;
                            //MainJob.UploadJobs = newjob.UploadJobs;
                            //MainJob.DownloadJobs = newjob.DownloadJobs;
                        }
                    }
                }
            }

            db_ToYandex.DataContext = MainJob.Jobs;
            //db_ToYandex.DataContext = MainJob.UploadJobs;
            //db_ToYandex.DataContext = MainJob.DownloadJobs;
        }

        async Task UploadSample(string token = "")
        {
            logger.Info("");
            logger.Info("---------------- Инициализация задач по загрузке на Яндекс.Диск ----------------");

            if (MainJob.Jobs != null)
            {
                for (int n = 0; n < MainJob.Jobs.Count; n++)
                {
                    if (MainJob.Jobs[n].Type == TaskType.UPLOAD)
                    {
                        if (MainJob.Jobs[n].Active)
                        {
                            logger.Info("");
                            logger.Info("----- Запуск задачи " + MainJob.Jobs[n].ID + " -----");

                            DirectoryInfo dirInfo = new DirectoryInfo(MainJob.Jobs[n].LocalPath);

                            if (dirInfo != null)
                            {
                                List<FileInfo> files = MainJob.Jobs[n].Filter.Split('|').SelectMany(filter => dirInfo.GetFiles(filter, SearchOption.TopDirectoryOnly)).ToList();

                                //CreateDirectoryAsync(MainJob.UploadJobs[n].ExternalPath);

                                if (files != null)
                                {
                                    MainJob.Jobs[n].Status = "Загрузка...";
                                    db_ToYandex.Items.Refresh();
                                    //upload.IsEnabled = false;
                                    db_ToYandex.IsReadOnly = true;
                                    MainJob.Jobs[n].Percent = 0;

                                    for (int i = 0; i < files.Count; i++)
                                    {
                                        MainJob.Jobs[n].Percent = (i + 1) * 100 / files.Count;
                                        MainJob.Jobs[n].Status = i + 1 + "/" + files.Count + " Локальный каталог : " + files[i].FullName + " >>> Удаленный каталог : " + MainJob.Jobs[n].ExternalPath + "/" + files[i].Name + " Загрузка";
                                        db_ToYandex.Items.Refresh();

                                        await DiskApi.Files.UploadFileAsync(path: "/" + MainJob.Jobs[n].ExternalPath + "/" + files[i].Name,
                                                                            overwrite: true,
                                                                            localFile: files[i].FullName,
                                                                            cancellationToken: CancellationToken.None);

                                        MainJob.Jobs[n].Status = i + 1 + "/" + files.Count + " Локальный каталог : " + files[i].FullName + " >>> Удаленный каталог : " + MainJob.Jobs[n].ExternalPath + "/" + files[i].Name + " Загружен";
                                        logger.Info(i + 1 + "/" + files.Count + " Локальный каталог : " + files[i].FullName + " >>> Удаленный каталог : " + MainJob.Jobs[n].ExternalPath + "/" + files[i].Name + " Загружен");
                                        MainJob.Jobs[n].Percent = (i + 1) * 100 / files.Count;
                                        db_ToYandex.Items.Refresh();

                                        if (MainJob.Jobs[n].DeleteFiles)
                                        {
                                            logger.Info("Удаление файла из локального каталога: " + files[i].FullName);
                                            files[i].Delete();
                                        }
                                    }

                                    db_ToYandex.Items.Refresh();
                                    logger.Info("----- Успешное выполнение задачи " + MainJob.Jobs[n].ID + " -----");
                                    MainJob.Jobs[n].Status = "Выполнено";
                                    MainJob.Jobs[n].Percent = 100;
                                    //upload.IsEnabled = true;
                                    db_ToYandex.IsReadOnly = false;
                                }
                                else
                                {
                                    logger.Warn("Файлы в директории отсутствуют");
                                    logger.Info("----- Задача " + MainJob.Jobs[n].ID + " отменена -----");
                                }

                                logger.Info("");
                            }
                        }
                        else
                        {
                            MainJob.Jobs[n].Status = "Пропущено";
                            db_ToYandex.Items.Refresh();
                            logger.Info("----- Задача " + MainJob.Jobs[n].ID + " отключена. Выполнение отменено -----");
                        }
                    }
                }
            }
            else
            {
                logger.Info("Отсутствуют задачи по загрузке на Яндекс.Диск");
            }

            logger.Info("---------------- Завершение задач по загрузке на Яндекс.Диск ----------------");
            logger.Info("");
        }

        async Task DownloadAllFilesInFolder(string token = "")
        {
            logger.Info("");
            logger.Info("---------------- Инициализация задач по загрузке с Яндекс.Диска ----------------");

            if (MainJob.Jobs != null)
            {

                foreach (Job job in MainJob.Jobs)
                {
                    if (job.Type == TaskType.DOWNLOAD)
                    {
                        if (job.Active)
                        {
                            logger.Info("");
                            logger.Info("----- Запуск задачи " + job.ID + " -----");

                            db_ToYandex.Items.Refresh();
                            //upload.IsEnabled = false;
                            db_ToYandex.IsReadOnly = true;

                            //Getting information about folder /foo and all files in it
                            Resource fooResourceDescription = await DiskApi.MetaInfo.GetInfoAsync(
                                new ResourceRequest
                                {
                                    Path = "/" + job.ExternalPath, //Folder on Yandex Disk
                                Limit = 100
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

                                var SortedFiles = GetSortedfilesByFilter(allFilesInFolder, job.Filter);

                                foreach (var item in SortedFiles)
                                {
                                    job.Percent = (index + 1) * 100 / SortedFiles.Count();
                                    job.Status = index + 1 + "/" + SortedFiles.Count() + " Удаленный каталог : " + item.Path + " >>> Локальный каталог : " + job.LocalPath + "\\" + item.Name + " Загрузка";
                                    db_ToYandex.Items.Refresh();

                                    //Download file to local
                                    if (!File.Exists(job.LocalPath + "\\" + item.Name))
                                    {
                                        await DiskApi.Files.DownloadFileAsync(path: item.Path, localFile: Path.Combine(localFolder, item.Name));
                                        job.Status = index + 1 + "/" + SortedFiles.Count() + " Удаленный каталог : " + item.Path + " >>> Локальный каталог : " + job.LocalPath + "\\" + item.Name + " Загружен";
                                        logger.Info(index + 1 + "/" + SortedFiles.Count() + " Удаленный каталог : " + item.Path + " >>> Локальный каталог : " + job.LocalPath + "\\" + item.Name + " Загружен");
                                        job.Percent = (index + 1) * 100 / SortedFiles.Count();
                                        db_ToYandex.Items.Refresh();

                                        if (job.DeleteFiles)
                                        {
                                            await DiskApi.Commands.DeleteAsync(new DeleteFileRequest
                                            {
                                                Path = item.Path
                                            });

                                            logger.Info("Удаление файла c Яндекс.Диска: " + item.Path);
                                        }
                                    }
                                    else
                                    {
                                        logger.Error("Файл с именем: \"" + item.Name + "\" существует в директории: " + job.LocalPath);
                                    }

                                    index++;
                                }
                            }

                            db_ToYandex.Items.Refresh();
                            job.Status = "Выполнено";
                            job.Percent = 100;
                            db_ToYandex.IsReadOnly = false;

                            logger.Info("----- Успешное выполнение задачи " + job.ID + " -----");
                            logger.Info("");
                        }
                        else
                        {
                            job.Status = "Пропущено";
                            db_ToYandex.Items.Refresh();
                            logger.Info("----- Задача " + job.ID + " отключена. Выполнение отменено -----");
                        }
                    }
                }

                logger.Info("Очистка корзины на Яндекс.Диске");
                await DiskApi.Commands.EmptyTrashAsync(path: "/");
            }
            else
            {
                logger.Info("Отсутствуют задачи по загрузке с Яндекс.Диска");
            }

            logger.Info("---------------- Завершение задач по загрузке с Яндекс.Диска ----------------");
            logger.Info("");
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            try
            {
                await UploadSample(Settings.Default.token);
                Task.WaitAll();
                await DownloadAllFilesInFolder(Settings.Default.token);
                Task.WaitAll();
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message);

                YandexAuthWindow yaDlg = new YandexAuthWindow();
                yaDlg.Owner = this;

                if(yaDlg.ShowDialog() == true)
                {
                    await UploadSample(Settings.Default.token);
                    Task.WaitAll();
                    await DownloadAllFilesInFolder(Settings.Default.token);
                    Task.WaitAll();
                }
                else
                {
                    logger.Warn(ex.Message);
                }
            }

            if (autoclose)
            {
                Task.WaitAll();
                //upload.IsEnabled = true;
                logger.Info("Автозавершение работы приложения по окончанию всех задач");
                Close();
            }
            else
            {
                Task.WaitAll();
                //upload.IsEnabled = true;
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
            Settings.Default.width = MainYandexSync.Width;
            Settings.Default.height = MainYandexSync.Height;

            if (MainYandexSync.WindowState == WindowState.Maximized)
            {
                Settings.Default.maximized = true;
            }
            else
            {
                Settings.Default.maximized = false;
            }
            Settings.Default.Save();

            // передаем в конструктор тип класса
            XmlSerializer uploadformatter = new XmlSerializer(typeof(SavedJob));

            if (MainJob.Jobs != null)
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
            job.Filter = "*.*";

            MainJob.Jobs.Add(job);
        }

        private void RemToYa_bt_Click(object sender, RoutedEventArgs e)
        {
            if(db_ToYandex.SelectedItem != null)
            {
                Job selectedjob = (Job)db_ToYandex.SelectedItem;

                if(selectedjob != null)
                {
                    MainJob.Jobs.Remove(selectedjob);
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
            job.Filter = "*.*";

            MainJob.Jobs.Add(job);
        }

        private void RemFromYa_bt_Click(object sender, RoutedEventArgs e)
        {
            if (db_ToYandex.SelectedItem != null)
            {
                Job selectedjob = (Job)db_ToYandex.SelectedItem;

                if (selectedjob != null)
                {
                    MainJob.Jobs.Remove(selectedjob);
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

        public IDiskApi Authorize()
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
                    logger.Error(ex.Message);

                    await DiskApi.Commands.CreateDictionaryAsync
                    (
                        path: currentpath
                    );
                }
            }

            Task.WaitAll();
        }

        //  КНОПКИ МЕНЮ

           
        private void Exit_mi_Click(object sender, RoutedEventArgs e)
        {
            // Выход из приложения
            Close();
        }

        private void Settings_mi_Click(object sender, RoutedEventArgs e)
        {
            /// Показать окно настройки

            SettingsWindow settingswnd = new SettingsWindow();

            if (settingswnd != null)
            {
                settingswnd.Owner = this;
                settingswnd.ShowDialog();
            }
        }

        private void About_mi_Click(object sender, RoutedEventArgs e)
        {
            /// Показать окно "О нас"

            AboutWindow aboutwnd = new AboutWindow();

            if (aboutwnd != null)
            {
                aboutwnd.Owner = this;
                aboutwnd.ShowDialog();
            }
        }

        private void Updater_mi_Click(object sender, RoutedEventArgs e)
        {
            /// Вызвать Updater
        }

        private void Addtask_mi_Click(object sender, RoutedEventArgs e)
        {
            // Добавить задание
            Job job = new Job();
            job.ID = Guid.NewGuid();
            job.Filter = "*.*";
            TaskWindow addtask = new TaskWindow(job);

            if (addtask != null)
            {
                addtask.Owner = this;
                if (addtask.ShowDialog() == true)
                {
                    MainJob.Jobs.Add(job);
                }
            }
        }

        private void Edittask_mi_Click(object sender, RoutedEventArgs e)
        {
            int selectedindex = db_ToYandex.SelectedIndex;

            Job job = MainJob.Jobs[selectedindex];

            EditJob(job);
        }

        private void Db_ToYandex_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Вы действительно хотите удалить?", "Подтверждение удаления", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    int selectedindex = db_ToYandex.SelectedIndex;

                    Job job = MainJob.Jobs[selectedindex];
                    MainJob.Jobs.Remove(job);
                }
            }
            else if( e.Key == Key.Enter)
            {
                int selectedindex = db_ToYandex.SelectedIndex;

                Job job = MainJob.Jobs[selectedindex];

                EditJob(job);
            }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            // Some operations with this row

            int selectedindex = row.GetIndex();

            Job job = MainJob.Jobs[selectedindex];

            EditJob(job);

        }

        private void EditJob(Job job)
        {
            if (job != null)
            {
                Job tempjob = new Job();
                tempjob.Active = job.Active;
                tempjob.DeleteFiles = job.DeleteFiles;
                tempjob.ExternalPath = job.ExternalPath;
                tempjob.Filter = job.Filter;
                tempjob.ID = job.ID;
                tempjob.LocalPath = job.LocalPath;
                tempjob.Type = job.Type;

                TaskWindow addtask = new TaskWindow(job);   

                if (addtask != null)
                {
                    addtask.Owner = this;
                    if (addtask.ShowDialog() != true)
                    {
                        int selectedindex = db_ToYandex.SelectedIndex;
                        MainJob.Jobs[selectedindex] = tempjob;
                    }
                }
            }
        }
    }
}
