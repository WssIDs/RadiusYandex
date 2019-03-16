using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;

namespace Updater
{
    /// <summary>
    /// Interaction logic for UpdateDownload.xaml
    /// </summary>
    public partial class UpdateDownload : Window
    {
        private BackgroundWorker backgroundWorker;
        private List<string> tempFiles = new List<string>();
        private List<string> md5s = new List<string>();
        private int num;

        internal List<string> TempFilePaths
        {
            get { return this.tempFiles; }
        }

        public UpdateDownload(List<FileUpdateStruct> fileData, ImageSource programmIcon)
        {
            InitializeComponent();

            if (programmIcon != null)
            {
                this.Icon = programmIcon;
            }

            for (int i = 0; i < fileData.Count; i++)
            {
                tempFiles.Add(Path.GetTempFileName());
                this.md5s.Add(fileData[i].md5);
            }

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            StartDownload(fileData);
        }

        private async void StartDownload(List<FileUpdateStruct> fileData)
        {
            await DownloadManyFiles(fileData);
            Task.WaitAll();

            backgroundWorker.RunWorkerAsync(new List<List<string>> { tempFiles, md5s });
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DialogResult = (bool)e.Result;
            Close();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var files = ((List<List<string>>)e.Argument)[0];
            var md5s = ((List<List<string>>)e.Argument)[1];

            for (int i = 0; i < files.Count; i++)
            {
                if (Hasher.Hashfile(files[i], HashType.MD5) != md5s[i])
                {
                    e.Result = false;
                }
                else
                {
                    e.Result = true;
                }
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadstatus_pb.Value = e.ProgressPercentage;
            progress_tb.Text = String.Format("Загружено {0} из {1}", FormatBytes(e.BytesReceived, 1, true), FormatBytes(e.TotalBytesToReceive, 1, true));
        }

        private string FormatBytes(long bytes, int decimalPlaces, bool showByteType)
        {
            double newBytes = bytes;
            string formatString = "{0";
            string byteType = "Б";

            if (newBytes > 1024 && newBytes < 1048576)
            {
                newBytes /= 1024;
                byteType = "КБ";
            }
            else if (newBytes > 1048576 && newBytes < 1073741824)
            {
                newBytes /= 1048576;
                byteType = "МБ";
            }
            else
            {
                newBytes /= 1073741824;
                byteType = "ГБ";
            }

            if (decimalPlaces > 0)
            {
                formatString += ":0.";
            }

            for (int i = 0; i < decimalPlaces; i++)
            {
                formatString += "0";
            }

            formatString += "}";

            if (showByteType)
            {
                formatString += byteType;
            }

            return string.Format(formatString, newBytes);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
                DialogResult = false;
            }
        }

        public async Task DownloadManyFiles(List<FileUpdateStruct> fileData)
        {
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            wc.DownloadFileCompleted += (s, e) =>
            {
                if (num <= fileData.Count - 1)
                {
                    if (e.Error != null)
                    {
                        DialogResult = false;
                        Close();
                    }
                    else if (e.Cancelled)
                    {
                        DialogResult = false;
                        Close();
                    }
                    else
                    {
                        progress_tb.Text = "Проверка загрузки...";

                        num++;

                        downloadAllstatus_bar.Maximum = fileData.Count - 1;
                        downloadAllstatus_bar.Value = num;
                    }
                }
            };
            for (int i = 0; i < fileData.Count; i++)
            {
                await wc.DownloadFileTaskAsync(fileData[i].Uri, tempFiles[i]);
            }
            wc.Dispose();
        }
    }
}
