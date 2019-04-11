using RadiusYandex.Models;
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

namespace RadiusYandex.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            
            this.DataContext = Properties.Settings.Default;

            if(string.IsNullOrEmpty(Properties.Settings.Default.token))
            {
                deletetoken_bt.IsEnabled = false;
                autorize_bt.IsEnabled = true;
            }
            else
            {
                deletetoken_bt.IsEnabled = true;
                autorize_bt.IsEnabled = false;
            }
        }

        private void Deletetoken_bt_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.token = "";

            deletetoken_bt.IsEnabled = false;
            autorize_bt.IsEnabled = true;
        }

        private void Autorize_bt_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Owner).Authorize();

            deletetoken_bt.IsEnabled = true;
            autorize_bt.IsEnabled = false;
        }

        private void Accept_bt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_bt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
