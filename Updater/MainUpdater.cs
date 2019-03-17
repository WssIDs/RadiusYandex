using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    public class MainUpdater
    {
        private IUpdatetable applicationInfo;
        private BackgroundWorker backgrounWorker;

        public MainUpdater(IUpdatetable applicationInfo)
        {
            this.applicationInfo = applicationInfo;

            backgrounWorker = new BackgroundWorker();
            backgrounWorker.DoWork += BackgrounWorker_DoWork;
            backgrounWorker.RunWorkerCompleted += BackgrounWorker_RunWorkerCompleted;
        }

        public void DoUpdate()
        {
            if(!backgrounWorker.IsBusy)
            {
                backgrounWorker.RunWorkerAsync(applicationInfo);
            }
        }

        private void BackgrounWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(!e.Cancelled)
            {
                UpdateXml update = (UpdateXml)e.Result;

                if(update != null && update.IsNewerThan(this.applicationInfo.ApplicationAssemby.GetName().Version))
                {
                    UpdateAccept updateAcceptWnd = new UpdateAccept(applicationInfo, update);
                    updateAcceptWnd.Owner = applicationInfo.Context;

                    if (updateAcceptWnd.ShowDialog() == true)
                    {
                        DownloadUpdate(update);
                    }
                }
                else
                {
                    MessageBox.Show("Обновление не требуется.\n У вас актуальная версия приложения.", "Обновление приложения", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DownloadUpdate(UpdateXml update)
        {
            UpdateDownload updateDownloadWnd = new UpdateDownload(update.FileData, applicationInfo.ApplicationIcon);
            updateDownloadWnd.Owner = applicationInfo.Context;
            var result = updateDownloadWnd.ShowDialog();

            if (result == true)
            {
                string currentPath = applicationInfo.ApplicationAssemby.Location;
                List<string> newPaths = new List<string>();

                foreach (FileUpdateStruct item in update.FileData)
                {
                    newPaths.Add(Path.GetDirectoryName(currentPath) + "\\" + item.filename);
                }

                UpdateApplication(updateDownloadWnd.TempFilePaths, currentPath, newPaths, update.LaunchArgs);

                Environment.Exit(exitCode: 0);
            }
            else if(result == false)
            {
                MessageBox.Show("Загрузка обновления отменена.\nПрограмма осталась без изменений.", "Загрузка обновления отменена", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Обнаружена проблема с загрузкой обновления.\nПожалуйста, попробуйте позже.", "Ошибка загрузки обновления", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateApplication(List<string> tempFilePaths, string currentPath, List<string> newPaths, string launchArgs)
        {
            for (int i = 1; i < newPaths.Count; i++)
            {
                string argumentfiles = "/C Choice /C Y /N /D Y /T 4 & Del /F /Q \"{0}\" &  Chioce /C Y /N /D Y /T 2 & Move /Y \"{1}\" \"{2}\"";
                ProcessStartInfo info = new ProcessStartInfo();

                info.Arguments = string.Format(argumentfiles, newPaths[i], tempFilePaths[i], newPaths[i], Path.GetDirectoryName(newPaths[i]), Path.GetFileName(newPaths[i]));
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.CreateNoWindow = true;
                info.FileName = "cmd.exe";
                Process.Start(info);
            }

            string argument = "/C Choice /C Y /N /D Y /T 4 & Del /F /Q \"{0}\" &  Chioce /C Y /N /D Y /T 2 & Move /Y \"{1}\" \"{2}\" & Start \"\" /D \"{3}\" \"{4}\" {5}";

            ProcessStartInfo infoMainFile = new ProcessStartInfo();

            infoMainFile.Arguments = string.Format(argument, currentPath, tempFilePaths[0], newPaths[0], Path.GetDirectoryName(newPaths[0]), Path.GetFileName(newPaths[0]), launchArgs);
            infoMainFile.WindowStyle = ProcessWindowStyle.Hidden;
            infoMainFile.CreateNoWindow = true;
            infoMainFile.FileName = "cmd.exe";
            Process.Start(infoMainFile);
        }

        private void BackgrounWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            IUpdatetable applicationInfo = (IUpdatetable)e.Argument;

            if(!UpdateXml.ExistsOnServer(applicationInfo.UpdateXmlLocation))
            {
                e.Cancel = true;
                MessageBox.Show("Обновления отсутствуют","Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                e.Result = UpdateXml.Parse(applicationInfo.UpdateXmlLocation, applicationInfo.ApplicationID);
            }
        }
    }
}
