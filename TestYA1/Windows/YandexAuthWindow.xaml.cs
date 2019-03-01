using Nemiro.OAuth;
using Nemiro.OAuth.Clients;
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
using TestYA1.Properties;

namespace TestYA1.Windows
{
    /// <summary>
    /// Interaction logic for YandexAuthWindow.xaml
    /// </summary>
    public partial class YandexAuthWindow : Window
    {
        public YandexAuthWindow()
        {
            InitializeComponent();
        }

        private void Yandexweb_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // ожидаем, когда появится адрес с результатами авторизации
            if (e.Uri.Query.IndexOf("code=") != -1 || e.Uri.Fragment.IndexOf("code=") != -1 || e.Uri.Query.IndexOf("oauth_verifier=") != -1)
            {
                // проверяем адрес
                var result = OAuthWeb.VerifyAuthorization(e.Uri.ToString());
                if (result.IsSuccessfully)
                {
                    //показываем данные пользователя
                    MessageBox.Show
                    (
                      String.Format
                      (
                        "User ID: {0}\r\nUsername: {1}\r\nDisplay Name: {2}\r\nE-Mail: {3}\r\nToken: {4}",
                        result.UserInfo.UserId,
                        result.UserInfo.UserName,
                        result.UserInfo.DisplayName ?? result.UserInfo.FullName,
                        result.UserInfo.Email,
                        result.AccessToken
                      ),
                      "Successfully",
                      MessageBoxButton.OK,
                      MessageBoxImage.Information
                    );

                    Settings.Default.token = result.AccessToken;
                    Settings.Default.Save();
                    DialogResult = true;
                    this.Close();
                }
                else
                {
                    // ошибка
                    MessageBox.Show(result.ErrorInfo.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Yandexauthwindow_Loaded(object sender, RoutedEventArgs e)
        {
            var client = new YandexClient
            (
                "ef69cea3cf4946d0b00dec064f1b26ee",
                "9446b005f22f4a5592b5a78d250551d2"
            );

            OAuthManager.RegisterClient
            (
                client
            );

            yandexweb.Navigate(OAuthWeb.GetAuthorizationUrl("Yandex"));
        }
    }
}
