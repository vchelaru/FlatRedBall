using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.DataTypes;

namespace TileGraphicsPlugin.Managers
{
    /// <summary>
    /// Saves the XML file that is used by Tiled to determine object type and variables.
    /// </summary>
    public class TiledObjectTypeCreator
    {

        public void RefreshFile()
        {
            var fileName = GetTiledObjectTypeFileName();

            var entities = GetEntitiesForTiledObjectTypeFile();
            var whatToSave = CreateTiledObjectTypeListFrom(entities);

            string fileContents;


            // fix for https://github.com/vchelaru/FlatRedBall/issues/396
            // Fix from : https://stackoverflow.com/questions/21140292/how-to-define-the-culture-that-the-xmlserializer-uses
            var oldCulture = CultureInfo.CurrentCulture;
            var newCulture = (CultureInfo)oldCulture.Clone();
            newCulture.NumberFormat.NumberDecimalSeparator = "."; //Force use . insted of ,
            System.Threading.Thread.CurrentThread.CurrentCulture = newCulture;

            FlatRedBall.IO.FileManager.XmlSerialize(whatToSave, out fileContents);

            System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;


            // Not sure how to fix this other than this hacky solution:
            fileContents = fileContents.Replace("<TiledObjectTypeSave ", "<objecttype ");
            fileContents = fileContents.Replace("</TiledObjectTypeSave>", "</objecttype>");
            fileContents = fileContents.Replace("<ArrayOfTiledObjectTypeSave", "<objecttypes");
            fileContents = fileContents.Replace("</ArrayOfTiledObjectTypeSave>", "</objecttypes>");

            try
            {
                GlueCommands.Self.TryMultipleTimes(() => FlatRedBall.IO.FileManager.SaveText(fileContents, fileName.FullPath));
            }
            catch(System.IO.IOException)
            {
                // It's probably in use, so just output that it wasn't saved, we'll try again later
                GlueCommands.Self.PrintOutput("Could not save Tiled XML file because it is in use. Will try again later. Reload the Glue project" +
                    "to force a regeneration.");
            }
        }

        public static FilePath GetTiledObjectTypeFileName()
        {
            // put this in the content directory
            var directory = GlueState.Self.ContentDirectory;

            return directory + "TiledObjects.Generated.xml";
        }

        private IEnumerable<EntitySave> GetEntitiesForTiledObjectTypeFile()
        {
            var allEntitiesInGlux = GlueState.Self.CurrentGlueProject.Entities;

            return allEntitiesInGlux
                .Where(item=>item.CreatedByOtherEntities);
        }

        private List<TiledObjectTypeSave> CreateTiledObjectTypeListFrom(IEnumerable<EntitySave> entities)
        {
            List<TiledObjectTypeSave> toReturn = new List<TiledObjectTypeSave>();


            foreach(var entity in entities)
            {
                var objectType = CreateTiledObjectTypeFrom(entity);

                toReturn.Add(objectType);
            }



            return toReturn;
        }

        private TiledObjectTypeSave CreateTiledObjectTypeFrom(EntitySave entity)
        {
            TiledObjectTypeSave tiledObjectType = new DataTypes.TiledObjectTypeSave();
            tiledObjectType.Name = FlatRedBall.IO.FileManager.RemovePath(entity.Name);

            foreach(var variable in entity.CustomVariables)
            {
                bool shouldInlude = GetIfShouldInclude(variable);

                if(shouldInlude)
                {
                    var propertySave = CreatePropertySaveFrom(variable);
                    tiledObjectType.Properties.Add(propertySave);
                }
            }

            return tiledObjectType;
        }

        private bool GetIfShouldInclude(CustomVariable variable)
        {
            if(variable.Name == "X" ||
                variable.Name == "Y" ||
                variable.Name == "Z")
            {
                return false;
            }
            return true;
        }

        private TiledObjectTypePropertySave CreatePropertySaveFrom(CustomVariable variable)
        {
            var toReturn = new TiledObjectTypePropertySave();

            toReturn.name = variable.Name;
            toReturn.Type = GetTmxFriendlyType( variable.Type);
            if(variable.DefaultValue is float asFloat)
            {
                toReturn.DefaultAsString = GetTmxFriendlyValue(asFloat.ToString(CultureInfo.InvariantCulture), variable.Type);
            }
            if (variable.DefaultValue is double asDouble)
            {
                toReturn.DefaultAsString = GetTmxFriendlyValue(asDouble.ToString(CultureInfo.InvariantCulture), variable.Type);
            }
            else
            {
                toReturn.DefaultAsString = GetTmxFriendlyValue(variable.DefaultValue?.ToString(), variable.Type) ;
            }

            return toReturn;
        }

        private string GetTmxFriendlyValue(string value, string type)
        {
            if(type == "bool")
            {
                return value?.ToLower();
            }
            else
            {
                return value;
            }
        }

        private string GetTmxFriendlyType(string type)
        {
            if(type == "double" || type == "decimal")
            {
                return "float";
            }
            else
            {
                return type;
            }
        }
    }
}
