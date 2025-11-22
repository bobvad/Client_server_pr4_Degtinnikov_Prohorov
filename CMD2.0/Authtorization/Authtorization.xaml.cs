using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CMD2._0.Authtorization
{
    public partial class Authtorization : Page
    {
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
                MessageBox.Show("Введите логин и пароль");
                return;
            }
            try
            {

                MessageBox.Show("Авторизация успешна!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}");
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            Registration registrationPage = new Registration();
            NavigationService.Navigate(registrationPage);
        }
    }
}