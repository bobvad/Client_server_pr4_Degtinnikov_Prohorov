using System;
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

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            await TryConnectAsync($"connect {login} {password}");
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            await TryConnectAsync($"register {login} {password}");
        }

        private async System.Threading.Tasks.Task TryConnectAsync(string command)
        {
            txtMessage.Visibility = Visibility.Collapsed;

            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(SERVER_IP, SERVER_PORT);

                var request = new ViewModelSend(command, -1);
                string json = JsonConvert.SerializeObject(request);
                byte[] data = Encoding.UTF8.GetBytes(json);
                socket.Send(data);

                byte[] buffer = new byte[1024];
                int bytesReceived = socket.Receive(buffer);
                string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                var response = JsonConvert.DeserializeObject<ViewModelMessage>(responseJson);

                if (response.Command == "autorization")
                {
                    int userId = int.Parse(response.Data);

                    // Переходим в файловый менеджер
                    var fileManagerPage = new FileManagerPage(SERVER_IP, SERVER_PORT, userId);
                    NavigationService.Navigate(fileManagerPage);
                }
                else
                {
                    ShowError(response.Data);
                }
            }
            catch (Exception ex)
            {
                ShowError("Не удалось подключиться к серверу:\n" + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            txtMessage.Visibility = Visibility.Visible;
            txtMessage.Text = message;
        }
    }
}