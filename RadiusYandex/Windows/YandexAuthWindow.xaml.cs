using Nemiro.OAuth;
using Nemiro.OAuth.Clients;
using System.Windows;
using RadiusYandex.Properties;
using System.Windows.Navigation;
using NLog;

namespace RadiusYandex.Windows
{
    /// <summary>
    /// Interaction logic for YandexAuthWindow.xaml
    /// </summary>
    public partial class YandexAuthWindow : Window
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public YandexAuthWindow()
        {
            InitializeComponent();

            logger.Info("Инициализация окна получения токена Яндекс.Диска");
        }

        private void Yandexweb_LoadCompleted(object sender, NavigationEventArgs e)
        {
            // ожидаем, когда появится адрес с результатами авторизации
            if (e.Uri.Query.IndexOf("code=") != -1 || e.Uri.Fragment.IndexOf("code=") != -1 || e.Uri.Query.IndexOf("oauth_verifier=") != -1)
            {
                // проверяем адрес
                var result = OAuthWeb.VerifyAuthorization(e.Uri.ToString());
                if (result.IsSuccessfully)
                {
                    logger.Info("Успешная проверка авторизации");
                    //показываем данные пользователя
                    Settings.Default.token = result.AccessToken;
                    Settings.Default.Save();
                    //DialogResult = true;
                    //this.Close();
                }
                else
                {
                    logger.Error(result.ErrorInfo.Message);
                }
            }
            else if (e.Uri.AbsoluteUri == "https://yandex.by/?nr=17961")
            {
                yandexweb.Navigate(source: OAuthWeb.GetAuthorizationUrl("Yandex"));
            }
        }

        private void Yandexauthwindow_Loaded(object sender, RoutedEventArgs e)
        {
            var client = new YandexClient
            (
                "ef69cea3cf4946d0b00dec064f1b26ee",
                "9446b005f22f4a5592b5a78d250551d2"
            );

            if (!OAuthManager.IsRegisteredClient("Yandex"))
            {
                OAuthManager.RegisterClient
                (
                    client
                );
            }

            yandexweb.Navigate(source: OAuthWeb.GetAuthorizationUrl("Yandex"));
        }

        private void Accept_bt_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Changeprofile_bt_Click(object sender, RoutedEventArgs e)
        {
            yandexweb.Navigate(source: OAuthWeb.GetAuthorizationUrl("Yandex"));
        }

        private void Yandexauthwindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(string.IsNullOrEmpty(Settings.Default.token))
            {
                DialogResult = false;
            }
            else
            {
                DialogResult = true;
            }
        }
    }
}
