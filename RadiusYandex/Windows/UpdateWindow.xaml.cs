using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Updater;

namespace RadiusYandex.Windows
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window, IUpdatetable
    {
        private MainUpdater updater;

        public UpdateWindow()
        {
            InitializeComponent();

            version_tb.Text = ApplicationAssemby.GetName().Version.ToString();
            updater = new MainUpdater(this);
        }

        public string ApplicationName
        {
            get { return "RadiusYandex"; }
        }

        public string ApplicationID
        {
            get { return "RadiusYandex"; }
        }

        public Assembly ApplicationAssemby
        {
            get { return Assembly.GetExecutingAssembly(); }
        }

        public ImageSource ApplicationIcon
        {
            get { return Icon; }
        }

        public Uri UpdateXmlLocation
        {
            get { return new Uri("http://kletsklib.by/assets/uploads/update.xml"); }
        }

        public Window Context
        {
            get { return this; }
        }

        private void Checkupdate_bt_Click(object sender, RoutedEventArgs e)
        {
            updater.DoUpdate();
        }
    }
}
