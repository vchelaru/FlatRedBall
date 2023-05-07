using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.ViewModels;

namespace TiledPluginCore.Errors
{
    internal class TileShapeCollectionMapShapeCollectionNameMismatchViewModel : ErrorViewModel
    {
        NamedObjectSave namedObject;
        GlueElement owner;

        public override string UniqueId => Details;


        public TileShapeCollectionMapShapeCollectionNameMismatchViewModel(NamedObjectSave namedObject)
        {
            this.namedObject = namedObject;
            this.owner = ObjectFinder.Self.GetElementContaining(namedObject);

            var collisionType = namedObject.Properties.GetValue<string>(
                nameof(TileShapeCollectionPropertiesViewModel.TmxCollisionName));
            

            Details = $"{namedObject} is referencing a tile collision type named " +
                $"{collisionType}. This object should be named {collisionType}, " +
                $"or the property should be changed in the .TSX file to be named {namedObject.InstanceName}.";
        }

        public override bool GetIfIsFixed()
        {
            // This is fixed if:
            // The element no longer exists:
            var project = GlueState.Self.CurrentGlueProject;
            if (owner is ScreenSave screenSave && project.Screens.Contains(screenSave) == false)
            {
                return true;
            }
            if (owner is EntitySave entitySave && project.Entities.Contains(entitySave) == false)
            {
                return true;
            }

            // If the NOS has been removed:
            if (owner.AllNamedObjects.Contains(namedObject) == false)
            {
                return true;
            }

            // If the nos is defined by base
            if(namedObject.DefinedByBase)
            {
                return true;
            }

            var collisionCreationOptions = namedObject.Properties.GetValue<CollisionCreationOptions>(nameof(CollisionCreationOptions));

            if(collisionCreationOptions != CollisionCreationOptions.FromMapCollision)
            {
                return true;
            }

            var collisionType = namedObject.Properties.GetValue<string>(
                nameof(TileShapeCollectionPropertiesViewModel.TmxCollisionName));

            if(collisionType == namedObject.InstanceName)
            {
                return true;
            }

            return false;
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = namedObject;
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            // gonna be lazy:
            return filePath.Extension == "tmx";
        }

    }
}
