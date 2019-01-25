using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterReup.Helpers
{
    class Configs
    {
        public int Mode { get; set; }
        public int Cut_Second_Min { get; set; }
        public int Cut_Second_Max { get; set; }
        public int Start { get; set; }
        public int Num_Videos { get; set; }
        public int Concurrent { get; set; }
        public int Page_Load { get; set; }
        public int Dialog_Load { get; set; }
        public int Upload_Check_Interval { get; set; }
        public string Channel_Id { get; set; }
        public string Profile { get; set; }
        public string Video_Path { get; set; }
    }
}
