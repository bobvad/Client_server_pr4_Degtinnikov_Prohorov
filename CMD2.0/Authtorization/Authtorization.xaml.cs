using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Common;
using Newtonsoft.Json;

namespace CMD2._0.Authtorization
{
    public partial class Authtorization : Page
    {
        private const string SERVER_IP = "127.0.0.4";
        private const int SERVER_PORT = 5004;
        public Authtorization()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var request = new ViewModelSend($"connect {login} {password}", -1);
                string jsonRequest = JsonConvert.SerializeObject(request);

                string jsonResponse = SendToServer(jsonRequest);

                var response = JsonConvert.DeserializeObject<ViewModelMessage>(jsonResponse);

                if (response == null)
                {
                    MessageBox.Show("Сервер вернул пустой ответ.");
                    return;
                }

                if (response.Command == "autorization")
                {
                    int userId = JsonConvert.DeserializeObject<int>(response.Data);

                    MessageBox.Show("Авторизация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var fileManagerPage = new FileManagerPage(SERVER_IP, SERVER_PORT, userId);
                    NavigationService.Navigate(fileManagerPage);
                }
                else if (response.Command == "message" || response.Command == "error")
                {
                    MessageBox.Show(response.Data ?? "Неизвестная ошибка авторизации", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Неожиданный ответ от сервера.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string SendToServer(string jsonData)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.4");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 5004);

                using (Socket sender = new Socket(AddressFamily.InterNetwork,
                       SocketType.Stream, ProtocolType.Tcp))
                {
                    sender.Connect(remoteEP);

                    byte[] msg = Encoding.UTF8.GetBytes(jsonData);
                    sender.Send(msg);

                    byte[] bytes = new byte[1024];
                    int bytesRec = sender.Receive(bytes);
                    string response = Encoding.UTF8.GetString(bytes, 0, bytesRec);

                    sender.Shutdown(SocketShutdown.Both);

                    return response;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подключения к серверу: {ex.Message}");
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            Registration registrationPage = new Registration();
            NavigationService.Navigate(registrationPage);
        }
    }
}