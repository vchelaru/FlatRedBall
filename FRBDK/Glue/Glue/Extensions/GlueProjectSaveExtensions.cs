using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class GlueProjectSaveExtensions
    {
        public static void TestSave(this GlueProjectSave glueProjectSave, string tag)
        {
            string serializedToString;

            GlueProjectSave whatToSave = null;
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                whatToSave = glueProjectSave.ConvertToPartial(tag);
            });

            if(whatToSave != null)
            {
                if(glueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                {
                    // The settings really don't matter, but let's simulate the real save here by using the same settings
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                    serializedToString = JsonConvert.SerializeObject(glueProjectSave, Formatting.Indented, settings);
                }
                else
                {
                    FileManager.XmlSerialize(whatToSave, out serializedToString);
                }
            }
        }

        public static List<FilePath> GetAllSerializedFiles(this GlueProjectSave glueProjectSave, FilePath target)
        {
            var files = new List<FilePath>();
            files.Add(target);

            if(glueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
            {
                var directory = target.GetDirectoryContainingThis();

                foreach(var screen in glueProjectSave.Screens)
                {
                    files.Add(directory + screen.Name + "." + GlueProjectSave.ScreenExtension);
                }
                foreach(var entity in glueProjectSave.Entities)
                {
                    files.Add(directory + entity.Name + "." + GlueProjectSave.EntityExtension);
                }
            }

            return files;
        }

        public static bool Save(this GlueProjectSave glueProjectSave, string tag, string fileName, out Exception lastException)
        {
            int failures = 0;
            // This gives Glue 2 chances to save....but not like weekend sales at RC Willey
            // whoa actually 3!
            const int maxFailures = 3;

            bool succeeded = false;
            lastException = null;

            while (failures < maxFailures)
            {
                try
                {
                    glueProjectSave.SaveMainAndElementsToFile(fileName, tag);

                    succeeded = true;
                    break;
                }
                catch (IOException ioe)
                {
                    lastException = ioe;
                    System.Threading.Thread.Sleep(200);
                    failures++;
                }
                catch (UnauthorizedAccessException uae)
                {
                    lastException = uae;
                    System.Threading.Thread.Sleep(200);
                    failures++;
                }
            }

            return succeeded;
        }

        public static void SaveGlujFile(this GlueProjectSave glueProjectSave, string tag, FilePath filePath)
        {
            if(glueProjectSave.FileVersion < (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
            {
                throw new ArgumentException("The GlueProjectSave must have SeparateJsonFilesForElements version or greater");
            }

            GlueProjectSave clone = null;

            GlueCommands.Self.TryMultipleTimes(() => clone = glueProjectSave.ConvertToPartial(tag));

            PrepareWildcardsForSaving(filePath, clone);

            clone.EntityReferences = clone.Entities.Select(item => new GlueElementFileReference { Name = item.Name }).ToList();
            clone.ScreenReferences = clone.Screens.Select(item => new GlueElementFileReference { Name = item.Name }).ToList();

            clone.Entities.Clear();
            clone.Screens.Clear();

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            var serialized = JsonConvert.SerializeObject(clone, Formatting.Indented, settings);
            PluginManager.ReactToGlueJsonSaveAsync(serialized);
            FileManager.SaveText(serialized, filePath.FullPath);
        }

        public static void SaveMainAndElementsToFile(this GlueProjectSave glueProjectSave, FilePath filePath, string tag)
        {
            GlueProjectSave clone = null;
            
            GlueCommands.Self.TryMultipleTimes(() => clone = glueProjectSave.ConvertToPartial(tag));

            if(clone.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
            {
                PrepareWildcardsForSaving(filePath, clone);
                
                SaveAndRemoveElements(filePath, clone);
            }

            var fileName = filePath.FullPath;

            string convertedFileName = fileName.ConvertToPartialName(tag);
            if(glueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                var serialized = JsonConvert.SerializeObject(clone, Formatting.Indented, settings);
                PluginManager.ReactToGlueJsonSaveAsync(serialized);
                FileManager.SaveText(serialized, fileName);

            }
            else
            {
                FileManager.XmlSerialize(clone, convertedFileName);
            }
        }

        private static void PrepareWildcardsForSaving(FilePath filePath, GlueProjectSave clone)
        {
            clone.GlobalFiles.RemoveAll(item => item.IsCreatedByWildcard);
            clone.GlobalFiles.AddRange(clone.GlobalFileWildcards);
            //clone.GlobalFileWildcards.Clear();
            // if we null it, it won't show up, right?
            clone.GlobalFileWildcards = null;

            foreach (var screen in clone.Screens)
            {
                // add wildcards here...
            }
            foreach (var entity in clone.Entities)
            {
                // add wildcards here...
            }
        }

        private static void SaveAndRemoveElements(FilePath filePath, GlueProjectSave clone)
        {
            clone.EntityReferences = clone.Entities.Select(item => new GlueElementFileReference { Name = item.Name }).ToList();
            clone.ScreenReferences = clone.Screens.Select(item => new GlueElementFileReference { Name = item.Name }).ToList();

            var glueDirectory = filePath.GetDirectoryContainingThis();

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;

            // Ignoring defaults will make the json much smaller, but is it okay?

            foreach (var entity in clone.Entities)
            {
                var serialized = JsonConvert.SerializeObject(entity, settings);

                var locationToSave = glueDirectory + entity.Name + "." + GlueProjectSave.EntityExtension;

                PluginManager.ReactToEntityJsonSaveAsync(entity.Name, serialized);
                FileManager.SaveText(serialized, locationToSave);
            }

            foreach (var screen in clone.Screens)
            {
                var serialized = JsonConvert.SerializeObject(screen, settings);

                var locationToSave = glueDirectory + screen.Name + "." + GlueProjectSave.ScreenExtension;

                PluginManager.ReactToScreenJsonSaveAsync(screen.Name, serialized);
                GlueCommands.Self.TryMultipleTimes(() => FileManager.SaveText(serialized, locationToSave));
            }

            clone.Entities.Clear();
            clone.Screens.Clear();
        }

        private static GlueProjectSave ConvertToPartial(this GlueProjectSave glueProjectSave, string tag)
        {
            GlueProjectSave returnValue;
            if (tag == "GLUE")
            {
                //Remove other elements
                if(glueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                {
                    var serialized = JsonConvert.SerializeObject(glueProjectSave);
                    returnValue = JsonConvert.DeserializeObject<GlueProjectSave>(serialized);
                }
                else
                {
                    returnValue = glueProjectSave.Clone();
                }

                //Entities
                returnValue.Entities.RemoveAll(t => !t.Tags.Contains(tag) && t.Tags.Count != 0);

                //Screens
                returnValue.Screens.RemoveAll(t => !t.Tags.Contains(tag) && t.Tags.Count != 0);
            }
            else
            {
                returnValue = new GlueProjectSave();


                throw new NotImplementedException("Need to add, not remove. I believe the following code is wrong:");

                ////Entities
                //returnValue.Entities.RemoveAll(t => !t.Tags.Contains(tag));

                ////Screens
                //returnValue.Screens.RemoveAll(t => !t.Tags.Contains(tag));
            }

            return returnValue;


        }

        private static string ConvertToPartialName(this string fileName, string tag)
        {
            if (tag == "GLUE")
                return fileName;

            var newFileName = fileName.Remove(fileName.Length - 5);
            newFileName += "." + tag + ".Generated.glux";

            return newFileName;
        }

        public static GlueProjectSave Load(FilePath fileName)
        {
            GlueProjectSave mainGlueProjectSave = null;
            if(fileName.Extension == "glux")
            {
                mainGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName.FullPath);
            }
            else if(fileName.Extension == "gluj")
            {
                // During the conversion, there may still be XML files using version 9, so check for that:
                if(fileName.Exists())
                {
                    var text = System.IO.File.ReadAllText(fileName.FullPath);
                    PluginManager.ReactToGlueJsonLoadAsync(text);
                    mainGlueProjectSave = JsonConvert.DeserializeObject<GlueProjectSave>(text);
                }
                else if(System.IO.File.Exists( fileName.RemoveExtension() + ".glux"))
                {
                    mainGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName.RemoveExtension() + ".glux");
                }
            }
            mainGlueProjectSave = mainGlueProjectSave.MarkTags("GLUE"); 

            var files =
                Directory.GetFiles(fileName.GetDirectoryContainingThis() + @"\");



            foreach (var file in files.Where(item=>item.ToLower().EndsWith(".generated.glux") || item.ToLower().EndsWith(".generated.gluj")))
            {
                string withoutExtension = FileManager.RemoveExtension(file);
                string withoutGenerated = FileManager.RemoveExtension(withoutExtension);

                if (withoutGenerated == null) continue;
                var tag = FileManager.GetExtension(withoutGenerated);

                mainGlueProjectSave.Merge(FileManager.XmlDeserialize<GlueProjectSave>(file).MarkTags(tag));
            }

            if(mainGlueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
            {
                LoadReferencedScreensAndEntities(fileName, mainGlueProjectSave);

                WildcardReferencedFileSaveLogic.LoadWildcardReferencedFiles(fileName, mainGlueProjectSave);
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
                    PluginManager.ReactToScreenJsonLoadAsync(screenReference.Name, fileContents);
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
                    PluginManager.ReactToEntityJsonLoadAsync(entityReference.Name, fileContents);
                    var deserialized = JsonConvert.DeserializeObject<EntitySave>(fileContents);

                    main.Entities.Add(deserialized);
                }
            }

            main.ScreenReferences.Clear();
            main.EntityReferences.Clear();
        }





        public static void Save(this GlueProjectSave glueprojectsave, string tag, string fileName)
        {
            glueprojectsave.SaveMainAndElementsToFile(fileName, tag);
        }

        private static GlueProjectSave Clone(this GlueProjectSave obj)
        {
            var toReturn = FileManager.CloneObject(obj);

            return toReturn;
        }

        private static void Merge(this GlueProjectSave origSave, GlueProjectSave newSave)
        {
            //Entities
            foreach (var entitySave in newSave.Entities)
            {
                var save = entitySave;
                if (origSave.Entities.All(t => t.Name != save.Name))
                {
                    origSave.Entities.Add(save);
                }
                else
                {

                    //Do stuff for when it already exists
                }
            }

            foreach(var entityReference in newSave.EntityReferences)
            {
                var contains = origSave.EntityReferences.Any(item => item.Name == entityReference.Name);
                if(!contains)
                {
                    origSave.EntityReferences.Add(entityReference);
                }
            }

            //Screens
            foreach (var screenSave in newSave.Screens)
            {
                var save = screenSave;
                if (origSave.Screens.All(t => t.Name != save.Name))
                {
                    origSave.Screens.Add(save);
                }
                else
                {
                    //Do stuff for when it already exists
                }
            }

            foreach(var screenReference in newSave.ScreenReferences)
            {
                var contains = origSave.ScreenReferences.Any(item => item.Name == screenReference.Name);
                if(!contains)
                {
                    origSave.ScreenReferences.Add(screenReference);
                }
            }
        }

        private static GlueProjectSave MarkTags(this GlueProjectSave origSave, string tag)
        {
            //Entities
            foreach (var entitySave in origSave.Entities)
            {
                entitySave.Source = tag;
                entitySave.Tags.Clear();
                entitySave.Tags.Add(tag);

                // Eventually need to add this
                //foreach (var rfs in entitySave.ReferencedFiles)
                //{
                //    rfs.Tags.Clear();
                //    rfs.Tags.Add(tag);
                //}
            }

            //Screens
            foreach (var screenSave in origSave.Screens)
            {
                screenSave.Source = tag;
                screenSave.Tags.Clear();
                screenSave.Tags.Add(tag);
            }

            return origSave;
        }
    }
}
