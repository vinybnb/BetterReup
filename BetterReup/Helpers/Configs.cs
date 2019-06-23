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
        public int Inpage_Load { get; set; }
        public int Upload_Check_Interval { get; set; }
        public int Custom_Title { get; set; }
        public string Media_Id { get; set; }
        public string Media_Type { get; set; }
        public string Profile { get; set; }
        public string Video_Path { get; set; }

        public int Num_Videos_Inserted_End_Screen_Once { get; set; }
    }
}
