using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleGame
{
    public static class Extensions
    {
        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
