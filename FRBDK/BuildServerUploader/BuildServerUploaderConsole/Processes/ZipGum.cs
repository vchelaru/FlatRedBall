using FlatRedBall.IO;
namespace BuildServerUploaderConsole.Processes
{
    public class ZipGum : ProcessStep
    {
        public ZipGum(IResults results)
            : base(
                @"Zipping Gum", results)
        { }

        public override void ExecuteStep()
        {
            var destination = FileManager.GetDirectory(DirectoryHelper.GumBuildDirectory);

            ZipHelper.CreateZip(Results, destination, DirectoryHelper.GumBuildDirectory, "Gum");
        }
    }
}
