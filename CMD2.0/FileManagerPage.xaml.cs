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
        private readonly string ServerIp;
        private readonly int ServerPort;
        private readonly int UserId;

        private string CurrentPath = "";  
        private Stack<string> BackHistory = new();
        private Stack<string> ForwardHistory = new();

        private Socket socket;
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
                string fullPath = relativePath;
                if (!string.IsNullOrEmpty(CurrentPath) && !string.IsNullOrEmpty(relativePath) && relativePath != "..")
                {
                    fullPath = System.IO.Path.Combine(CurrentPath, relativePath);
                }

                string command = string.IsNullOrEmpty(fullPath.Trim()) ? "cd" : $"cd {fullPath}";

                var response = await SendCommandAsync(command);

                if (response?.Command == "cd")
                {
                    var items = JsonConvert.DeserializeObject<List<string>>(response.Data);

                    Dispatcher.Invoke(() =>
                    {
                        FileList.Items.Clear();
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

                        if (!string.IsNullOrEmpty(relativePath) && relativePath != ".." && relativePath != ".")
                        {
                            BackHistory.Push(CurrentPath);
                            ForwardHistory.Clear();
                        }

                        CurrentPath = fullPath ?? "";
                        PathBox.Text = "~/" + CurrentPath.Replace("\\", "/");
                        StatusText.Text = $"Элементов: {items.Count}";
                        BtnBack.IsEnabled = BackHistory.Count > 0;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Ошибка: " + (response?.Data ?? "Нет ответа"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Ошибка: " + ex.Message;
                    MessageBox.Show($"Произошла ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
        private async void PathBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string userPath = PathBox.Text.Trim();
            if (string.IsNullOrEmpty(userPath)) return;

            StatusText.Text = "Переход...";

            try
            {
                string command = $"cd {userPath}";

                var response = await SendCommandAsync(command);

                if (response?.Command == "cd")
                {
                    var items = JsonConvert.DeserializeObject<List<string>>(response.Data);

                    Dispatcher.Invoke(() =>
                    {
                        FileList.Items.Clear();
                        foreach (var item in items)
                        {
                            bool isDir = item.EndsWith("/");
                            string name = isDir ? item.Substring(0, item.Length - 1) : item;

                            FileList.Items.Add(new FileItem
                            {
                                Name = name,
                                IsDirectory = isDir
                            });
                        }
                        PathBox.Text = userPath;
                        StatusText.Text = $"Элементов: {items.Count}";
                        BtnBack.IsEnabled = true; 
                    });
                }
                else
                {
                    StatusText.Text = "Ошибка: " + (response?.Data ?? "Путь не найден");
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
            }
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (BackHistory.Count > 0)
            {
                string prev = BackHistory.Pop();
                ForwardHistory.Push(CurrentPath);
                CurrentPath = prev;
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

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
