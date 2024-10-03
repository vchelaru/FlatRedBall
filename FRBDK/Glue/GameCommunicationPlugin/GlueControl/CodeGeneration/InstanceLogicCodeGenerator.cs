using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.CodeGeneration
{
    static class InstanceLogicCodeGenerator
    {
        public static string GetStringContents()
        {
            return @"
namespace " + GlueState.Self.ProjectNamespace + @".GlueControl
{
    public class InstanceLogic
    {
        public void Update()
        {
            if(GuiManager.Cursor.PrimaryClick)
            {
                Performance.IEntityFactory factory = Factories.EnemyFactory.Self;
                var cursor = GuiManager.Cursor;
                factory.CreateNew(cursor.WorldX, cursor.WorldY);
            }
        }
    }
}
";
        }
    }
}
