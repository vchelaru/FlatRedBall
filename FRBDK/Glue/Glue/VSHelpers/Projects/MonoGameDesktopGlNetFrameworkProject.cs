using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class MonoGameDesktopGlNetFrameworkProject : MonoGameDesktopGlBaseProject
    {

        const string FlatRedBallDll = "FlatRedBallDesktopGL.dll";

        public override string NeededVisualStudioVersion
        {
            get { return "14.0"; }

        }

        public MonoGameDesktopGlNetFrameworkProject(Project project) : base(project)
        {
        }



    }
}
