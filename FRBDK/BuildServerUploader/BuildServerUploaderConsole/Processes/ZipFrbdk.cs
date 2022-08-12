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
            string frbdkForZipDirectory = DirectoryHelper.ReleaseDirectory + @"FRBDK For Zip\";

            ZipHelper.CreateZip(Results, mDestinationDirectory, frbdkForZipDirectory, mFrbdkZipFile);
        }
    }
}
