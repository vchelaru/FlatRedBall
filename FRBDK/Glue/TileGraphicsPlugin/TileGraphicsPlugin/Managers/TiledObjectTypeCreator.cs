using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.DataTypes;

namespace TileGraphicsPlugin.Managers
{
    public class TiledObjectTypeCreator
    {

        public void RefreshFile()
        {
            var fileName = GetTiledObjectTypeFileName();

            var entities = GetEntitiesForTiledObjectTypeFile();
            var whatToSave = CreateTiledObjectTypeListFrom(entities);

            string fileContents;

            FlatRedBall.IO.FileManager.XmlSerialize(whatToSave, out fileContents);

            // Not sure how to fix this other than this hacky solution:
            fileContents = fileContents.Replace("<TiledObjectTypeSave ", "<objecttype ");
            fileContents = fileContents.Replace("</TiledObjectTypeSave>", "</objecttype>");
            fileContents = fileContents.Replace("<ArrayOfTiledObjectTypeSave", "<objecttypes");
            fileContents = fileContents.Replace("</ArrayOfTiledObjectTypeSave>", "</objecttypes>");
            FlatRedBall.IO.FileManager.SaveText(fileContents, fileName.FullPath);
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
            toReturn.DefaultAsString = GetTmxFriendlyValue(variable.DefaultValue?.ToString(), variable.Type) ;

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
            if(type == "double")
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
