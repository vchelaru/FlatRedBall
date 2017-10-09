using EditorObjects.IoC;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using GlueBuilder.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueBuilder.Managers
{
    public static class ContainerPopulator
    {
        public static void PopulateDefaultContainers()
        {
            Container.Set<IGlueCommands>(new GlueCommands());
            Container.Set<IGlueState>(new GlueState());
        }
    }
}
