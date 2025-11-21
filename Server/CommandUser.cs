using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class CommandUser
    {
        public int Id { get; set; }
        public string Command { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; }
    }
}
