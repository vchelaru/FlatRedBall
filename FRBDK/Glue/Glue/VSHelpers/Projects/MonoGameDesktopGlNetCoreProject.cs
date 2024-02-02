using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class MonoGameDesktopGlNetCoreProject : MonoGameDesktopGlBaseProject
    {

        const string FlatRedBallDll = "FlatRedBallDesktopGL.dll";

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                {
                    FlatRedBallDll
                };
            }
        }

        public override string NeededVisualStudioVersion
        {
            get { return "14.0"; }

        }

        public MonoGameDesktopGlNetCoreProject(Project project) : base(project)
        {
        }



    }
}
