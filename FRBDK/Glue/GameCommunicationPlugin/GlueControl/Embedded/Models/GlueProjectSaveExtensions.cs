using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GlueControl.Models
{
    public static class GlueProjectSaveExtensions
    {
        public static GlueProjectSave Load(FilePath fileName)
        {
            GlueProjectSave mainGlueProjectSave = null;
            if (fileName.Extension == "glux")
            {
                mainGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName.FullPath);
            }
            else if (fileName.Extension == "gluj")
            {
                // During the conversion, there may still be XML files using version 9, so check for that:
                if (fileName.Exists())
                {
                    var text = System.IO.File.ReadAllText(fileName.FullPath);
                    mainGlueProjectSave = JsonConvert.DeserializeObject<GlueProjectSave>(text);
                }
                else if (System.IO.File.Exists(fileName.RemoveExtension() + ".glux"))
                {
                    mainGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName.RemoveExtension() + ".glux");
                }
            }
            // don't do this in the editor
            //mainGlueProjectSave = mainGlueProjectSave.MarkTags("GLUE");

            var files =
                Directory.GetFiles(fileName.GetDirectoryContainingThis() + @"\");



            foreach (var file in files.Where(item => 
                         item.EndsWith(".generated.glux", StringComparison.OrdinalIgnoreCase) 
                         || item.EndsWith(".generated.gluj", StringComparison.OrdinalIgnoreCase)))
            {
                string withoutExtension = FileManager.RemoveExtension(file);
                string withoutGenerated = FileManager.RemoveExtension(withoutExtension);

                if (withoutGenerated == null) continue;
                var tag = FileManager.GetExtension(withoutGenerated);

                //mainGlueProjectSave.Merge(FileManager.XmlDeserialize<GlueProjectSave>(file)
                //.MarkTags(tag));
                ;
            }

            if (mainGlueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
            {
                LoadReferencedScreensAndEntities(fileName, mainGlueProjectSave);

                // todo - when we eventually support referenced file saves
                //WildcardReferencedFileSaveLogic.LoadWildcardReferencedFiles(fileName, mainGlueProjectSave);
            }

            return mainGlueProjectSave;
        }

        private static void LoadReferencedScreensAndEntities(FilePath glujFilePath, GlueProjectSave main)
        {
            var glueDirectory = glujFilePath.GetDirectoryContainingThis();
            foreach (var screenReference in main.ScreenReferences)
            {
                var path = new FilePath(glueDirectory + screenReference.Name + "." + GlueProjectSave.ScreenExtension);

                if (path.Exists())
                {
                    var fileContents = System.IO.File.ReadAllText(path.FullPath);
                    var deserialized = JsonConvert.DeserializeObject<ScreenSave>(fileContents);

                    main.Screens.Add(deserialized);
                }
            }

            foreach (var entityReference in main.EntityReferences)
            {
                var path = new FilePath(glueDirectory + entityReference.Name + "." + GlueProjectSave.EntityExtension);

                if (path.Exists())
                {
                    var fileContents = System.IO.File.ReadAllText(path.FullPath);
                    var deserialized = JsonConvert.DeserializeObject<EntitySave>(fileContents);

                    main.Entities.Add(deserialized);
                }
            }

            main.ScreenReferences.Clear();
            main.EntityReferences.Clear();
        }


    }
}
