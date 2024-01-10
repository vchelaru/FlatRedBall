using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildServerUploaderConsole.Processes
{
    public class AddRunFlatRedBallBatch : ProcessStep
    {
        public AddRunFlatRedBallBatch(IResults results) : 
            base("Adding Run FlatRedBall.bat file", results)
        {

        }

        public override void ExecuteStep()
        {
            var directory = DirectoryHelper.FrbdkForZipReleaseDirectory;

            // create a .bat file that runs FlatRedBall.exe and place it in directory
            var batchFile = directory + "Run FlatRedBall.bat";
            // save the file:
            var contents = "start \"\" \"Xna 4 Tools\\GlueFormsCore.exe\"";
            System.IO.File.WriteAllText(batchFile, contents);

        }
    }
}
