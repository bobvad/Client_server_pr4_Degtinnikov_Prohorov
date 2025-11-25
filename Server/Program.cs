using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Server
{
    public class Program
    {
        public static List<User> Users = new List<User>();
        public static IPAddress IpAdress;
        public static int Port;
        static void Main(string[] args)
        {
            using (var db = new DbContextUsers())
            {
                db.Database.EnsureCreated();
            }
            Console.Write("Введите IP адрес сервер: ");
            string sIpAdress = "127.0.0.4";
            Console.Write("\nВведите порт: ");
            string sPort = "5004";
            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nДанные успешно введены. Запускаю сервер");
                StartServer();
            }
        }

        public static bool AutorizationUser(string login, string password)
        {
            try
            {
                using (DbContextUsers db = new DbContextUsers())
                {
                    var user = db.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
                    return user != null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации в БД: {ex.Message}");
                return false;
            }
        }


        public static bool RegistrationUser(string login, string password)
        {
            try
            {
                using (DbContextUsers db = new DbContextUsers())
                {
                    if (db.Users.Any(u => u.Login == login))
                    {
                        return false;
                    }

                    string userPath = @"C:\FTP\" + login + @"\";
                    if (!Directory.Exists(userPath))
                    {
                        Directory.CreateDirectory(userPath);
                    }

                    db.Users.Add(new User(login, password, userPath));
                    db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка регистрации в БД: {ex.Message}");
                return false;
            }
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();
            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = dir.Replace(src, "");
                    FoldersFiles.Add(NameDirectory + "/");
                }
                string[] files = Directory.GetFiles(src);
                foreach (string file in files)
                {
                    string NameFile = file.Replace(src, "");
                    FoldersFiles.Add(NameFile);
                }
            }
            return FoldersFiles;
        }

        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IpAdress, Port);
            Socket sListener = new Socket(
                AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sListener.Bind(endPoint);
            sListener.Listen(10);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен.");
            while (true)
            {
                try
                {
                    Socket Handler = sListener.Accept();
                    string Data = null;
                    byte[] Bytes = new byte[10485760];
                    int BytesRec = Handler.Receive(Bytes);
                    Data += Encoding.UTF8.GetString(Bytes, 0, BytesRec);
                    Console.WriteLine("Сообщение от пользователя: " + Data + "\n");
                    string Reply = "";
                    ViewModelSend ViewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);
                    if (ViewModelSend != null)
                    {
                        ViewModelMessage viewModelMessage;
                        string[] DataCommand = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);

                        if (DataCommand[0] == "connect")
                        {
                            string[] parts = ViewModelSend.Message.Split(' ', 3);
                            if (parts.Length < 3)
                            {
                                viewModelMessage = new ViewModelMessage("message", "Неверный формат команды");
                            }
                            else
                            {
                                string login = parts[1];
                                string password = parts[2];

                                using (var db = new DbContextUsers())
                                {
                                    var user = db.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
                                    if (user != null)
                                    {
                                        int index = Users.FindIndex(u => u.Id == user.Id);
                                        if (index == -1)
                                        {
                                            Users.Add(user); 
                                            index = Users.Count - 1;
                                        }

                                        viewModelMessage = new ViewModelMessage("autorization", index.ToString());
                                    }
                                    else
                                    {
                                        viewModelMessage = new ViewModelMessage("message", "Неверный логин или пароль");
                                    }
                                }
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);

                        }
                        else if (DataCommand[0] == "register")
                        {
                            string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                            if (DataMessage.Length >= 3)
                            {
                                string login = DataMessage[1];
                                string password = DataMessage[2];

                                if (RegistrationUser(login, password))
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Регистрация успешна");
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Зарегистрирован новый пользователь: {login}");
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                                else
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Пользователь с таким логином уже существует");
                                }
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Неверный формат команды регистрации");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "cd")
                        {
                            if (ViewModelSend.Id == -1)
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }
                            else
                            {
                                try
                                {
                                    // Получаем путь после команды cd (может содержать пробелы)
                                    string rawPath = ViewModelSend.Message.Length > 2 ?
                                        ViewModelSend.Message.Substring(2).Trim() : "";

                                    using (var db = new DbContextUsers())
                                    {
                                        var user = db.Users.FirstOrDefault(u => u.Id == Users[ViewModelSend.Id].Id);
                                        if (user == null)
                                        {
                                            viewModelMessage = new ViewModelMessage("message", "Пользователь не найден");
                                        }
                                        else
                                        {
                                            string targetPath;
                                            string currentTempSrc = user.Temp_src ?? user.Src;

                                            if (string.IsNullOrEmpty(rawPath))
                                            {
                                                // Возврат в корневую директорию
                                                targetPath = user.Src;
                                            }
                                            else if (rawPath == "..")
                                            {
                                                // Переход на уровень выше
                                                string currentPath = currentTempSrc.TrimEnd('\\', '/');
                                                DirectoryInfo parentDir = Directory.GetParent(currentPath);

                                                if (parentDir != null && parentDir.FullName.StartsWith(user.Src.TrimEnd('\\', '/')))
                                                {
                                                    targetPath = parentDir.FullName + "\\";
                                                }
                                                else
                                                {
                                                    targetPath = user.Src; // Остаемся в корне
                                                }
                                            }
                                            else
                                            {
                                                // Переход в указанную папку
                                                targetPath = Path.Combine(currentTempSrc, rawPath);

                                                // Нормализуем путь
                                                targetPath = Path.GetFullPath(targetPath);
                                                if (!targetPath.EndsWith("\\"))
                                                    targetPath += "\\";

                                                // Проверяем, что путь в пределах разрешенной директории
                                                string userRoot = Path.GetFullPath(user.Src);
                                                if (!targetPath.StartsWith(userRoot))
                                                {
                                                    targetPath = user.Src;
                                                }
                                            }

                                            // Проверяем существование директории
                                            if (!Directory.Exists(targetPath))
                                            {
                                                viewModelMessage = new ViewModelMessage("message", $"Директория не существует: {targetPath}");
                                            }
                                            else
                                            {
                                                // Обновляем временный путь в БД
                                                user.Temp_src = targetPath;
                                                db.SaveChanges();

                                                // Обновляем пользователя в памяти
                                                Users[ViewModelSend.Id] = user;

                                                // Получаем содержимое директории
                                                var items = GetDirectory(targetPath);
                                                viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(items));

                                                // Логируем команду
                                                var commandUser = new CommandUser
                                                {
                                                    Command = $"cd {rawPath}",
                                                    UserId = user.Id
                                                };
                                                db.CommandUsers.Add(commandUser);
                                                db.SaveChanges();

                                                Console.WriteLine($"[CD] User {user.Login} navigated to: {targetPath}");
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[CD ERROR] {ex.Message}");
                                    viewModelMessage = new ViewModelMessage("message", $"Ошибка навигации: {ex.Message}");
                                }
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                            Handler.Shutdown(SocketShutdown.Both);
                            Handler.Close();
                        }
                        else if (DataCommand[0] == "get")
                        {
                            if (ViewModelSend.Id == -1)
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            }
                            else
                            {
                                ///
                                try
                                {
                                    string relativePath = ViewModelSend.Message["get".Length..].Trim();
                                    string fullPath = Path.Combine(Users[ViewModelSend.Id].Temp_src, relativePath);
                                    fullPath = Path.GetFullPath(fullPath);

                                    byte[] byteFile = File.ReadAllBytes(fullPath);
                                    viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));
                                    using (DbContextUsers db = new DbContextUsers())
                                    {
                                        var commandUser = new CommandUser
                                        {
                                            Command = "get " + relativePath,
                                            UserId = Users[ViewModelSend.Id].Id
                                        };
                                        db.CommandUsers.Add(commandUser);
                                        db.SaveChanges();
                                    }
                                }
                                catch
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Файл не найден");
                                }
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                            Handler.Shutdown(SocketShutdown.Both);
                            Handler.Close();
                          
                        }
                        else if (DataCommand[0] == "set")  
                        {
                            if (ViewModelSend.Id == -1)
                            {
                                viewModelMessage = new ViewModelMessage("message", "Не авторизован");
                            }
                            else
                            {
                                try
                                {
                                    string jsonPart = ViewModelSend.Message.Length > 4
                                        ? ViewModelSend.Message.Substring(4).Trim()
                                        : ViewModelSend.Message.Trim();

                                    FileInfoFTP fileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(jsonPart);

                                    string fullPath = Path.Combine(Users[ViewModelSend.Id].Temp_src, fileInfo.Name);
                                    fullPath = Path.GetFullPath(fullPath);

                                    File.WriteAllBytes(fullPath, fileInfo.Data);

                                    viewModelMessage = new ViewModelMessage("message", "Файл успешно загружен");
                                    Console.WriteLine($"[SET] Загружен файл: {fileInfo.Name} ({fileInfo.Data.Length} байт) → {fullPath}");
                                    using (DbContextUsers db = new DbContextUsers())
                                    {
                                        var commandUser = new CommandUser
                                        {
                                            Command = "set " + fullPath,
                                            UserId = Users[ViewModelSend.Id].Id
                                        };
                                        db.CommandUsers.Add(commandUser);
                                        db.SaveChanges();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Ошибка загрузки: " + ex.Message);
                                    Console.WriteLine($"[SET] Ошибка: {ex.Message}");
                                }
                            }

                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                            Handler.Shutdown(SocketShutdown.Both);
                            Handler.Close();
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "Неизвестная команда");
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                            Handler.Shutdown(SocketShutdown.Both);
                            Handler.Close();
                        }
                    }
                }
                catch (Exception exp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Ошибка: " + exp.Message);
                }
            }
        }
    }
}