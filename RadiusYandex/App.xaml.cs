using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RadiusYandex
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _m;
        Logger logger = LogManager.GetCurrentClassLogger();

        public void App_Startup(object sender, StartupEventArgs e)
        {
            bool isNew;

            string guid = Marshal.GetTypeLibGuidForAssembly(Assembly.GetExecutingAssembly()).ToString();

            _m = new Mutex(true, guid, out isNew);

            if (!isNew)
            {
                logger.Error("Попытка запуска второй копии приложения. Запуск более одной копии приложения невозможен.");
                logger.Info("Вторая копия приложения будет закрыта");
                Application.Current.Shutdown();
            }

            // Application is running
            // Process command line args
            bool startminimized = false;
            bool autostart = false;
            bool autoclose = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "-startminimized")
                {
                    startminimized = true;
                }
                if(e.Args[i] == "-autostart")
                {
                    autostart = true;
                }
                if (e.Args[i] == "-autoclose")
                {
                    autoclose = true;
                }
            }

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            if (startminimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
            }
            mainWindow.autostart = autostart;
            mainWindow.autoclose = autoclose;
            mainWindow.Show();
        }
    }
}
