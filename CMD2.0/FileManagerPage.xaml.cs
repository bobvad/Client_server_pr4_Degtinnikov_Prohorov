using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
using Server;

namespace CMD2._0
{
    /// <summary>
    /// Логика взаимодействия для FileManagerPage.xaml
    /// </summary>
    /// 
    public class FileItem
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public string Icon => IsDirectory ? "📁" : "📄";
    }

    public partial class FileManagerPage : Page
    {
        private DbContextUsers dbContextUsers = new DbContextUsers();
        private readonly string ServerIp;
        private readonly int ServerPort;
        private readonly int UserId;

        private string CurrentPath = "";  
        private Stack<string> BackHistory = new();
        private Stack<string> ForwardHistory = new();

        public FileManagerPage(string ip, int port, int userId)
        {
            InitializeComponent();
            ServerIp = ip;
            ServerPort = port;
            UserId = userId;
            
            LoadDirectory("");
        }
        private async void LoadDirectory(string relativePath)
        {
            try
            {
                StatusText.Text = "Загрузка...";

                string command = string.IsNullOrEmpty(relativePath.Trim()) ? "cd" : $"cd {relativePath}";
                var response = await SendCommandAsync(command);

                if (response?.Command == "cd")
                {
                    var items = JsonConvert.DeserializeObject<List<string>>(response.Data);

                    Dispatcher.Invoke(() =>
                    {
                        FileList.Items.Clear();

                        // Добавляем ".." для навигации вверх (кроме корневой директории)
                        if (!string.IsNullOrEmpty(relativePath) && relativePath != "" && relativePath != "cd")
                        {
                            FileList.Items.Add(new FileItem
                            {
                                Name = "..",
                                IsDirectory = true
                            });
                        }

                        foreach (var item in items)
                        {
                            bool isDir = item.EndsWith("/");
                            string name = isDir ? item[..^1] : item;

                            FileList.Items.Add(new FileItem
                            {
                                Name = name,
                                IsDirectory = isDir
                            });
                        }

                        StatusText.Text = $"Элементов: {FileList.Items.Count}";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = "Ошибка: " + (response?.Data ?? "Нет ответа");
                        MessageBox.Show(response?.Data ?? "Ошибка навигации", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Ошибка: " + ex.Message;
                    MessageBox.Show($"Произошла ошибка:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is FileItem item)
            {
                if (item.IsDirectory)
                {
                    LoadDirectory(item.Name);
                }
                else
                {
                    DownloadFile(item.Name);
                }
            }
        }
        private async Task<ViewModelMessage> SendCommandAsync(string message)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ServerIp, ServerPort); 

                var send = new ViewModelSend(message, UserId);
                string json = JsonConvert.SerializeObject(send);
                byte[] data = Encoding.UTF8.GetBytes(json);
                socket.Send(data);
                byte[] buffer = new byte[10 * 1024 * 1024];
                int bytes = socket.Receive(buffer);
                string resp = Encoding.UTF8.GetString(buffer, 0, bytes);
                return JsonConvert.DeserializeObject<ViewModelMessage>(resp);
            }
            catch (Exception ex)
            {
                return new ViewModelMessage("message", "Не удалось подключиться: " + ex.Message);
            }
        }
     
    


        private async void DownloadFile(string fileName)
        {
            StatusText.Text = $"Скачивание {fileName}...";

            try
            {
                var response = await SendCommandAsync($"get {fileName}");

                if (response?.Command == "file")
                {
                    byte[] fileBytes = JsonConvert.DeserializeObject<byte[]>(response.Data);

                    string savePath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        fileName 
                    );

                    File.WriteAllBytes(savePath, fileBytes);

                    StatusText.Text = $"Скачан: {fileName}";
                    MessageBox.Show($"Файл успешно скачан\n{savePath}",
                        "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = "Ошибка: " + (response?.Data ?? "Файл не найден");
                    MessageBox.Show("Не удалось скачать файл.\n" + (response?.Data ?? "Сервер не ответил"),
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка скачивания";
                MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnUploadFromPC_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private async void BtnMoveToDesktop_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                foreach (string filePath in dialog.FileNames)
                {
                    string fileName = System.IO.Path.GetFileName(filePath);
                    byte[] fileBytes = File.ReadAllBytes(filePath);

                    var fileInfo = new FileInfoFTP
                    {
                        Name = fileName,
                        Data = fileBytes
                    };

                    string json = JsonConvert.SerializeObject(fileInfo);
                    StatusText.Text = $"Загрузка: {fileName}...";

                    var response = await SendCommandAsync("set " + json);

                    if (response?.Data != null && response.Data.Contains("успешно"))
                    {
                        StatusText.Text = $"Загружен: {fileName}";
                        LoadDirectory(""); // обновить список
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки {fileName}\n{response?.Data}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
