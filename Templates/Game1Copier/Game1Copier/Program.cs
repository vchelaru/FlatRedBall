using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Game1Copier
{
    class Program
    {
        static void Main(string[] args)
        {

            var assembly = Assembly.GetAssembly(typeof(Program));

            var game1String = System.IO.File.ReadAllText("MasterGame.cs");

            List<string> targetFiles = new List<string>
            {
                "FlatRedBallAndroidTemplate/FlatRedBallAndroidTemplate/Game1.cs",
                "FlatRedBalliOSTemplate/FlatRedBalliOSTemplate/Game1.cs",
                "FlatRedBallXna4_360Template/FlatRedBallXna4Template/FlatRedBallXna4Template/Game1.cs",
                "FlatRedBallXna4Template/FlatRedBallXna4Template/FlatRedBallXna4Template/Game1.cs",
                "Windows8Template/Windows8Template/Game1.cs"
            };

            foreach (var file in targetFiles)
            {
                System.IO.File.WriteAllText(file, game1String);
            }
        }
    }
}
