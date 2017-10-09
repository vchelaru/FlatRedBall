using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueBuilder.Managers
{
    public class ProjectManager : Singleton<ProjectManager>
    {
        public GlueProjectSave CurrentGlueProjectSave
        {
            get;
            set;
        }

        public ProjectBase MainProject
        {
            get;
            set;
        }
    }
}
