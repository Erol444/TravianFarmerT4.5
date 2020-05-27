using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravBot
{

    public class HelperClass
    {
        public string DB(string name)
        {
            return ("Data Source = " + name + ".sqlite; Version = 3");
        }

        public int ReturnRandom(int x)
        {
            Random rnd = new Random();
            return (x + rnd.Next(500, 1500));
        }

        public int ReturnRandom(int x, int y)
        {
            Random rnd = new Random();
            return (rnd.Next(x, y));
        }

        public int ReturnSec(int x) {
            Random rnd = new Random();
            return (x + rnd.Next(1, 3));
        }

        public class Farm
        {
            public string FLName { get; set; }
            public string Name { get; set; }
            public string FLId { get; set; }
            public string FarmName { get; set; }
            public int LastRaid { get; set; }
        }

        public class Villages
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class FarmLists
        {
            public int Period { get; set; }
            public bool Enabled { get; set; }
            public string FLId { get; set; }
            public string FLName { get; set; }
            public bool Send2 { get; set; }
            public bool Send3 { get; set; }
            public int Period_num { get; set; }
        }
    }
}
