using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Facades;

namespace FlatRedBall.Glue.FacadeImplementation
{
    public class ProjectValues : IProjectValues
    {
        public string ContentDirectory
        {
            get
            {
                return ProjectManager.ContentDirectory;
            }
        }


    }
}
