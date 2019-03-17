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
    /// Interaction logic for UpdateInfo.xaml
    /// </summary>
    public partial class UpdateInfo : Window
    {
        public UpdateInfo()
        {
            InitializeComponent();
        }

        public UpdateInfo(IUpdatetable applicationInfo, UpdateXml updateInfo)
        {
            InitializeComponent();

            if(applicationInfo.ApplicationIcon != null)
            {
                this.Icon = applicationInfo.ApplicationIcon;
            }

            this.Title = applicationInfo.ApplicationName + " - Информация по обновлению";
            this.currentVersion_lb.Text = string.Format("Текущая версия: {0}", applicationInfo.ApplicationAssemby.GetName().Version.ToString());
            this.latestVersion_lb.Text = string.Format("Новая версия: {0}", updateInfo.Version.ToString());
            this.description_tb.Text = updateInfo.Description;
        }

        private void Description_tb_KeyDown(object sender, KeyEventArgs e)
        {
            if(!(e.Key == Key.LeftCtrl && e.Key == Key.C))
            {
                e.Handled = true;
            }
        }

        private void Back_bt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
