using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Managers;
using System.Reflection;
using System.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace TileGraphicsPlugin
{
    public class BuildToolAssociationAdder
    {
        public void AddIfNecessary(BuildToolAssociation association)
        {
            var found = GlueState.Self.GlueSettingsSave.BuildToolAssociations.FirstOrDefault(
                possible => possible.ToString().ToLowerInvariant() == association.ToString().ToLowerInvariant());

            if (found == null)
            {
                GlueState.Self.GlueSettingsSave.BuildToolAssociations.Add(association);
                GlueCommands.Self.GluxCommands.SaveSettings();
            }
        }
    }
}
