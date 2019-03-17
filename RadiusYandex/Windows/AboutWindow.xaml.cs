using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace RadiusYandex.Windows
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            developername_tb.Text = "Разработчик: "+GetAssemblyAttribute<AssemblyCompanyAttribute>(a => a.Company);
            version_tb.Text = "Версия: "+ GetAssemblyAttribute<AssemblyFileVersionAttribute>(a => a.Version);
            year_tb.Text = GetAssemblyAttribute<AssemblyCopyrightAttribute>(a => a.Copyright);
        }

        public string GetAssemblyAttribute<T>(Func<T, string> value)
             where T : Attribute
        {
            T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(T));
            return value.Invoke(attribute);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
