using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;

namespace CMD2._0
{
    public static class ServerClient
    {
        public static string ServerIP = "127.0.0.1";  // потом можно сделать настройку
        public static int ServerPort = 8080;
        public static int UserId = -1;

        public static ViewModelMessage SendCommand(ViewModelSend command)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(ServerIP, ServerPort);
                    string json = JsonConvert.SerializeObject(command);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    socket.Send(data);

                    byte[] buffer = new byte[10 * 1024 * 1024]; // 10 МБ
                    int received = socket.Receive(buffer);
                    string response = Encoding.UTF8.GetString(buffer, 0, received);

                    return JsonConvert.DeserializeObject<ViewModelMessage>(response);
                }
            }
            catch (Exception ex)
            {
                return new ViewModelMessage("error", $"Ошибка подключения: {ex.Message}");
            }
        }
    }
}
