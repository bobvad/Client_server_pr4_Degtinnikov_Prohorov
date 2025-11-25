using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class User
    {
        public int Id {  get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Src { get; set; }
        public string Temp_src { get; set; }
        public virtual ICollection<CommandUser> CommandUsers { get; set; }
        public User()
        {
            CommandUsers = new List<CommandUser>();
        }
        public User(string login,string password,string src) 
        {
            this.Login = login;
            this.Password = password;
            this.Src = src;
            Temp_src = src;
            CommandUsers = new List<CommandUser>();
        }
    }
}
