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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common;

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
                ShowMessage("Заполните все поля!");
                return;
            }

            if (login.Contains(" ") || password.Contains(" "))
            {
                ShowMessage("Логин и пароль не должны содержать пробелы!");
                return;
            }

            var response = ServerClient.SendCommand(new ViewModelSend($"register {login} {password}", -1));

            if (response.Data == "Регистрация успешна")
            {
                ShowMessage("Успешно зарегистрированы! Теперь войдите.", true);
            }
            else
            {
                ShowMessage(response.Data);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Authtorization());
        }

        private void ShowMessage(string text, bool success = false)
        {
            txtMessage.Text = text;
            txtMessage.Foreground = success
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green)
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            txtMessage.Visibility = Visibility.Visible;
        }
    }
}
