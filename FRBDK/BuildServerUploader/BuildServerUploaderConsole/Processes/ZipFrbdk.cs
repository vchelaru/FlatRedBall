using FlatRedBall.IO;
namespace BuildServerUploaderConsole.Processes
{
    public class ZipFrbdk : ProcessStep
    {
        static string mDestinationDirectory = DirectoryHelper.ReleaseDirectory;
        static string mFrbdkZipFile = "FRBDK";

        public static string DestinationFile
        {
            get
            {
                
                return FileManager.Standardize(
                    mDestinationDirectory + mFrbdkZipFile + ".zip");

            }
        }

        public ZipFrbdk(IResults results)
            : base(
                @"Zip copied FRBDK files", results)
        { }

        public override void ExecuteStep()
        {
            string sourceFrbdkDirectory = DirectoryHelper.ReleaseDirectory + @"FRBDK For Zip\";

            ZipHelper.CreateZip(Results, mDestinationDirectory, sourceFrbdkDirectory, mFrbdkZipFile);
        }
    }
}
