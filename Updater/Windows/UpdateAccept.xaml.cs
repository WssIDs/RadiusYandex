using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Updater
{
    /// <summary>
    /// Interaction logic for UpdateAccept.xaml
    /// </summary>
    public partial class UpdateAccept : Window
    {
        private IUpdatetable applicationInfo;
        private UpdateXml updateInfo;

        private UpdateInfo updateInfoWnd;

        public UpdateAccept(IUpdatetable applicationInfo, UpdateXml updateInfo)
        {
            InitializeComponent();

            this.applicationInfo = applicationInfo;
            this.updateInfo = updateInfo;

            this.Title = this.applicationInfo.ApplicationName + " - Обновление доступно";

            if(this.applicationInfo.ApplicationIcon != null)
            {
                this.Icon = this.applicationInfo.ApplicationIcon;
            }

            newversion_tb.Text = string.Format("Новая версия: {0}", this.updateInfo.Version.ToString());
        }

        private void Yes_bt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void No_bt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Details_bt_Click(object sender, RoutedEventArgs e)
        {
            if(updateInfoWnd == null)
            {
                updateInfoWnd = new UpdateInfo(applicationInfo, updateInfo);
                updateInfoWnd.Owner = this;

                updateInfoWnd.ShowDialog();
            }
        }
    }
}
