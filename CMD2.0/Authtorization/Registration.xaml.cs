using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common;
using Newtonsoft.Json;

namespace CMD2._0.Authtorization
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Page
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля");
                return;
            }
            try
            {
                var viewModelSend = new ViewModelSend($"register {login} {password}", -1);
                string jsonData = JsonConvert.SerializeObject(viewModelSend);
                string response = SendToServer(jsonData);
                var viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(response);

                if (viewModelMessage != null)
                {
                    if (viewModelMessage.Command == "message")
                    {
                        MessageBox.Show(viewModelMessage.Command);

                        if (viewModelMessage.Command == "Регистрация успешна")
                        {
                            NavigationService.GoBack();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
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
    }
}