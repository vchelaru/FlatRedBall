using Bridge.Html5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cube3D
{
    class Program
    {
        [Ready]
        public static void Main()
        {
            using (Game1 game = new Game1())
            {
                game.Run();
            }
        }
    }
}
