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
                DestinationGameFile = "FlatRedBallWebTemplate/FlatRedBallWebTemplate/Game1.cs",
                Namespace = "FlatRedBallWebTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallDesktopGlNet6Template/FlatRedBallDesktopGlNet6Template/Game1.cs",
                Namespace = "FlatRedBallDesktopGlNet6Template"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBalliOSMonoGameTemplate/FlatRedBalliOSMonoGameTemplate/Game1.cs",
                Namespace = "FlatRedBalliOSMonoGameTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallDesktopFnaTemplate/FlatRedBallDesktopFnaTemplate/Game1.cs",
                Namespace = "FlatRedBallDesktopFnaTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallAndroidMonoGameTemplate/FlatRedBallAndroidMonoGameTemplate/Game1.cs",
                Namespace = "FlatRedBallAndroidMonoGameTemplate"
            },
            new TemplateInformation {
                DestinationGameFile = "FlatRedBallDesktopGlMonoGameTemplate/FlatRedBallDesktopGlMonoGameTemplate/Game1.cs",
                Namespace = "FlatRedBallDesktopGlMonoGameTemplate"
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
