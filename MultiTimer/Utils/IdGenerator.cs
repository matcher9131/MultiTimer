using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiTimer.Utils
{
    public static class IdGenerator
    {
        private static int id = 0;

        public static int Generate() => id++;
    }
}
