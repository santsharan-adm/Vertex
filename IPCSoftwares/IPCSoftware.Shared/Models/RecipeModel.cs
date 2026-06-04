using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace IPCSoftware.Shared.Models
{

        public class ServoRecipeModel : ObservableObjectVM
        {
            public int ProgramNo { get; set; }

            public bool IsChecked { get; set; }
            public int S1 { get; set; }
            public int S2 { get; set; }
            public int S3 { get; set; }
            public int S4 { get; set; }
            public int S5 { get; set; }
            public int S6 { get; set; }
            public int S7 { get; set; }
            public int S8 { get; set; }
            public int S9 { get; set; }
            public int S10 { get; set; }
            public int S11 { get; set; }
            public int S12 { get; set; }

            public double X0 { get; set; }

            public double X1 { get; set; }
            public double X2 { get; set; }
            public double X3 { get; set; }
            public double X4 { get; set; }
            public double X5 { get; set; }

            public double X6 { get; set; }
            public double X7 { get; set; }
            public double X8 { get; set; }
            public double X9 { get; set; }
            public double X10 { get; set; }
            public double X11 { get; set; }
            public double X12 { get; set; }
            public double Y0 { get; set; }
            public double Y1 { get; set; }
            public double Y2 { get; set; }
            public double Y3 { get; set; }
            public double Y4 { get; set; }
            public double Y5 { get; set; }
            public double Y6 { get; set; }
            public double Y7 { get; set; }
            public double Y8 { get; set; }
            public double Y9 { get; set; }
            public double Y10 { get; set; }
            public double Y11 { get; set; }
            public double Y12 { get; set; }

            public double Xmin { get; set; }
            public double Xmax { get; set; }
            public double Ymin { get; set; }
            public double Ymax { get; set; }

            public double AngleMin { get; set; }
            public double AngleMax { get; set; }

            public string ProductName { get; set; }

            public string ProductCode { get; set; }

            public int TotalItems { get; set; }

            public int GridRows { get; set; }

            public int GridColumns { get; set; }

            public int Position_0 { get; set; } = 0;

            public int Position_1 { get; set; } = 1;
            public int Position_2 { get; set; } = 2;
            public int Position_3 { get; set; } = 3;
            public int Position_4 { get; set; } = 4;
            public int Position_5 { get; set; } = 5;
            public int Position_6 { get; set; } = 6;
            public int Position_7 { get; set; } = 7;
            public int Position_8 { get; set; } = 8;
            public int Position_9 { get; set; } = 9;
            public int Position_10 { get; set; } = 10;
            public int Position_11 { get; set; } = 11;
            public int Position_12 { get; set; } = 12;

            public string Name_0 { get; set; } = "Position Home(0)";
            public string Name_1 { get; set; } = "Position 1";
            public string Name_2 { get; set; } = "Position 2";
            public string Name_3 { get; set; } = "Position 3";
            public string Name_4 { get; set; } = "Position 4";
            public string Name_5 { get; set; } = "Position 5";
            public string Name_6 { get; set; } = "Position 6";
            public string Name_7 { get; set; } = "Position 7";
            public string Name_8 { get; set; } = "Position 8";
            public string Name_9 { get; set; } = "Position 9";
            public string Name_10 { get; set; } = "Position 10";
            public string Name_11 { get; set; } = "Position 11";
            public string Name_12 { get; set; } = "Position 12";

            public string Discription_0 { get; set; } = "null";

            public string Discription_1 { get; set; } = "null";
            public string Discription_2 { get; set; } = "null";
            public string Discription_3 { get; set; } = "null";
            public string Discription_4 { get; set; } = "null";
            public string Discription_5 { get; set; } = "null";
            public string Discription_6 { get; set; } = "null";
            public string Discription_7 { get; set; } = "null";
            public string Discription_8 { get; set; } = "null";
            public string Discription_9 { get; set; } = "null";
            public string Discription_10 { get; set; } = "null";
            public string Discription_11 { get; set; } = "null";
            public string Discription_12 { get; set; } = "null";

            public bool Is_Enabled_0 { get; set; } = true;
            public bool Is_Enabled_1 { get; set;} = true;
            public bool Is_Enabled_2 { get; set; } = true;

            public bool Is_Enabled_3 { get; set; } = true;
            public bool Is_Enabled_4 { get; set; } = true;
            public bool Is_Enabled_5 { get; set; } = true;
            public bool Is_Enabled_6 { get; set; } = true;
            public bool Is_Enabled_7 { get; set; } = true;
            public bool Is_Enabled_8 { get; set; } = true;
            public bool Is_Enabled_9 { get; set; } = true;
            public bool Is_Enabled_10 { get; set; } = true;
            public bool Is_Enabled_11 { get; set; } = true;
            public bool Is_Enabled_12 { get; set; } = true;
        


        };

}

