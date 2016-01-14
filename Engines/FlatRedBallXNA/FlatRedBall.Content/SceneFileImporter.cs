using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework.Content.Pipeline;
using System.IO;

namespace FlatRedBall.Content
{
    [ContentImporter(
        ".scnx",
        DefaultProcessor="SceneFileProcessor", 
        DisplayName="Scene - FlatRedBall")]
    class SceneFileImporter : ContentImporter<SpriteEditorSceneContent>
    {
        public override SpriteEditorSceneContent Import(string filename, ContentImporterContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            //Deserialize from filesystem into a SpriteEditorSceneFile
            SpriteEditorSceneContent ses = SpriteEditorSceneContent.FromFile(filename);
            ses.ScenePath = Path.GetDirectoryName(filename);
            return ses;
        }
    }
}
