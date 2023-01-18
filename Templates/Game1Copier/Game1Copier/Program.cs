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
        static List<TemplateInformation> templates = new List<TemplateInformation>
        {
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallAndroidTemplate/FlatRedBallAndroidTemplate/Game1.cs",
                Namespace = "FlatRedBallAndroidTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallDesktopGLTemplate/FlatRedBallDesktopGLTemplate/Game1.cs",
                Namespace = "FlatRedBallDesktopGlTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallDesktopGlNet6Template/FlatRedBallDesktopGlNet6Template/Game1.cs",
                Namespace = "FlatRedBallDesktopGlNet6Template"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBalliOSTemplate/FlatRedBalliOSTemplate/Game1.cs",
                Namespace = "FlatRedBalliOSTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallUwpTemplate/FlatRedBallUwpTemplate/Game1.cs",
                Namespace = "FlatRedBallUwpTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallXna4Template/FlatRedBallXna4Template/FlatRedBallXna4Template/Game1.cs",
                Namespace = "FlatRedBallXna4Template"
            },
            new TemplateInformation {
                DestinationGameFile = "Windows8Template/Windows8Template/Game1.cs",
                Namespace = "Windows8Template"
            },
        };
        static void Main(string[] args)
        {

            var assembly = Assembly.GetAssembly(typeof(Program));

            var game1String = System.IO.File.ReadAllText("MasterGame.cs");


            foreach (var template in templates)
            {
                var whatToReplace = "namespace FlatRedBallXna4Template";
                var whatToReplaceWith = $"namespace {template.Namespace}";
                string modifiedString = game1String.Replace(whatToReplace, whatToReplaceWith) ;


                System.IO.File.WriteAllText(template.DestinationGameFile, modifiedString);
            }
        }
    }
}
