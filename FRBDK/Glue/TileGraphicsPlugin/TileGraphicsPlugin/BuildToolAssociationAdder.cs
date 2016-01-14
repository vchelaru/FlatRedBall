using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Managers;
using System.Reflection;
using System.IO;

namespace TileGraphicsPlugin
{
    public class BuildToolAssociationAdder
    {
        public void AddIfNecessary(BuildToolAssociation association)
        {
            var found = BuildToolAssociationManager.Self.ProjectSpecificBuildTools.BuildToolList.FirstOrDefault(
                possible => possible.ToString().ToLowerInvariant() == association.ToString().ToLowerInvariant());

            if (found == null)
            {
                BuildToolAssociationManager.Self.ProjectSpecificBuildTools.BuildToolList.Add(association);

                BuildToolAssociationManager.Self.SaveProjectSpecificBuildTools();

            }
        }
    }
}
