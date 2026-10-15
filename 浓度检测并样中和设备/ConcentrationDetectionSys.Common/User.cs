using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Common
{
    public class User
    {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string Class { get; set; } = "操作员";
        public bool State { get; set; } = true;
        public DateTime DateCreated { get; set; }
    }
}
