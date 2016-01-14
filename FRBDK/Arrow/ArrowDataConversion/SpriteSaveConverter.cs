using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace ArrowDataConversion
{
    public class SpriteSaveConverter : GeneralSaveConverter
    {
                
        public NamedObjectSave SpriteSaveToNamedObjectSave(FlatRedBall.Content.Scene.SpriteSave sprite)
        {
            NamedObjectSave toReturn = new NamedObjectSave();

            toReturn.SourceType = SourceType.FlatRedBallType;
            toReturn.SourceClassType = "Sprite";
            toReturn.InstanceName = sprite.Name;

            AddVariablesForAllProperties(sprite, toReturn);

            return toReturn;
        }

        protected override void AddVariableToNos(NamedObjectSave toReturn, string memberName, object currentValue)
        {
            if (memberName == "Texture" && !string.IsNullOrEmpty((string)currentValue))
            {
                currentValue = FileManager.RemoveExtension( FileManager.RemovePath(currentValue as string));
            }
            base.AddVariableToNos(toReturn, memberName, currentValue);

        }
    }
}
