using System.Diagnostics;
using System.IO;
using System.Reflection;
using FlatRedBall.Glue;
using FlatRedBall.Glue.AutomatedGlue;
using Microsoft.Build.Evaluation;
using NUnit.Framework;
using FlatRedBall.Glue.IO;

namespace GlueTests
{
    [TestFixture]
    class Test1
    {
        string TestDirectory
        {
            get { return Path.GetDirectoryName(Assembly.GetAssembly(typeof (Test1)).Location) + @"\..\..\..\..\"; }
        }

        [Test]
        public void Test_1()
        {
            var projectFileName = TestDirectory + @"GlueTestProject\GlueTestProject\GlueTestProject\GlueTestProject.csproj";

            //Start up Glue
            AutoGlue.Start();
            ProjectLoader.Self.LoadProject(projectFileName);

            //Build Project
            var pc = new ProjectCollection();

            var fileLogger = new OutputLogger();
            pc.RegisterLogger(fileLogger);

            var p = pc.LoadProject(projectFileName);
            p.SetProperty("Configuration", "UnitTests");
            Assert.AreEqual(true, p.Build(), "Failed to build project.");

            //Run Project
            var proc = new Process
                           {
                               StartInfo =
                                   {
                                       UseShellExecute = false,
                                       FileName =
                                           TestDirectory +
                                           @"GlueTestProject\GlueTestProject\GlueTestProject\bin\x86\Debug\GlueTestProject.exe",
                                       RedirectStandardError = true
                                   }
                           };
            proc.Start();
            proc.WaitForExit();
            Trace.Write(proc.StandardError.ReadToEnd());
            Assert.AreEqual(0, proc.ExitCode);
        }
    }
}
