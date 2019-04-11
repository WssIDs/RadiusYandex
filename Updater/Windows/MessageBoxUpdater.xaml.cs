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
    public enum MessageBoxInfoType
    {
        Default,
        Success,
        Info,
        Warning,
        Error,
        Fatal
    }

    public class MessageBoxInfo
    {
        public MessageBoxInfo()
        {

        }

        public string Title { get; set; }
        public string Message { get; set; }
        public string OptionalMessage { get; set; }
        public MessageBoxInfoType MessageType { get; set; }
    }


    public static class UpdateMessageBox
    {
        public static void Show(IUpdatetable applicationInfo, string title, string message, string optionalmessage, MessageBoxInfoType type)
        {
            MessageBoxUpdater messagebox = new MessageBoxUpdater(title, message, optionalmessage, type);
            messagebox.Owner = applicationInfo.Context;
            messagebox.ShowDialog();
        }

        public static void Show(IUpdatetable applicationInfo, string title, string message, MessageBoxInfoType type)
        {
            MessageBoxUpdater messagebox = new MessageBoxUpdater(title, message, type);
            messagebox.Owner = applicationInfo.Context;
            messagebox.ShowDialog();
        }
    }

    /// <summary>
    /// Interaction logic for MessageBoxUpdater.xaml
    /// </summary>
    public partial class MessageBoxUpdater : Window
    {
        MessageBoxInfo info = new MessageBoxInfo();

        public MessageBoxUpdater(string title, string message, string optionalmessage, MessageBoxInfoType type)
        {
            InitializeComponent();

            this.DataContext = info;

            info.Title = title;
            info.Message = message;
            info.OptionalMessage = optionalmessage;
            info.MessageType = type;
        }

        public MessageBoxUpdater(string title, string message, MessageBoxInfoType type)
        {
            InitializeComponent();

            this.DataContext = info;

            info.Title = title;
            info.Message = message;
            info.MessageType = type;
        }

        private void Mb_ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
