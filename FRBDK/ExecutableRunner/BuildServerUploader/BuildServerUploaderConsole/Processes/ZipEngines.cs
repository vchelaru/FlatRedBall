namespace BuildServerUploaderConsole.Processes
{
    public class ZipEngine : ProcessStep
    {
        public ZipEngine(IResults results)
            : base(
                @"Zip copied Engine files", results)
        { }

        public override void ExecuteStep()
        {
            string sourceZipDirectory = Program.DefaultDirectory + @"\..\..\ReleaseFiles\SingleDlls\";
            string destZipDirectory = Program.DefaultDirectory + @"\..\..\ReleaseFiles\";

            ZipHelper.CreateZip(Results, destZipDirectory, sourceZipDirectory, "SingleDlls");
        }
    }
}
