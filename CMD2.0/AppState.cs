using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD2._0
{
    public static class AppState
    {
        public static int UserId { get; set; } = -1;
        public static string ServerIP { get; set; } = "127.0.0.1";
        public static int ServerPort { get; set; } = 8080;
        public static string CurrentPath { get; set; } = "";
    }
}
