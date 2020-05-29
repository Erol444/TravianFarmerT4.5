using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravBot
{

    public static class Helper
    {
        public static string DB(string name)
        {
            return ("Data Source = " + name + ".sqlite; Version = 3");
        }

        public static int ReturnRandom(int x)
        {
            Random rnd = new Random();
            return (x + rnd.Next(500, 1500));
        }

        public static int ReturnRandom(int x, int y)
        {
            Random rnd = new Random();
            return (rnd.Next(x, y));
        }

        public static int ReturnSec(int x) {
            Random rnd = new Random();
            return (x + rnd.Next(1, 3));
        }
    }
}
