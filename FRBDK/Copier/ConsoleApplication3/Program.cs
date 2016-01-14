using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{
    class Program
    {
        static void Main(string[] args)
        {
            string pluginsFolder = @"C:\Users\Victor\AppData\Roaming\FRBDK\Plugins\";
            string targetFolder = pluginsFolder + "AnimationEditor\\";
            string[] filesToCopy = new string[]
            {
                "AnimationEditorPlugin.dll",
                "AnimationEditorPlugin.pdb",
                "FlatRedBall.AnimationEditorForms.dll",
                "FlatRedBall.AnimationEditorForms.pdb",
                "FlatRedBall.SpecializedXnaControls.dll",
                "FlatRedBall.SpecializedXnaControls.pdb",
                "InputLibrary.dll",
                "InputLibrary.pdb",
                "RenderingLibrary.dll",
                "RenderingLibrary.pdb",
                "SelectionInterface.dll",
                "SelectionInterface.pdb",
                "TargaImage.dll",
                "TargaImage.pdb",
                "ToolsUtilities.dll",
                "ToolsUtilities.pdb",
                "XnaAndWinforms.dll",
                "XnaAndWinforms.pdb"
            };

            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
            var directoryName = System.IO.Path.GetDirectoryName(codeBase) + "\\";
            if (directoryName.StartsWith("file:\\"))
            {
                directoryName = directoryName.Substring("file:\\".Length);
            }
            System.Console.WriteLine("Starting copying");

            foreach (var file in filesToCopy)
            {
                try
                {
                    
                    string sourceFile = directoryName + file;
                    string targetFile = targetFolder + file;
                    System.Console.WriteLine("Trying " + sourceFile);

                    if (System.IO.File.Exists(sourceFile))
                    {
                        System.IO.File.Copy(sourceFile, targetFile, true);
                        System.Console.WriteLine("Copied to " + targetFile);
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.ToString());
                }
            }

        }
    }
}
