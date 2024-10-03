using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using TileGraphicsPlugin;

namespace TiledPlugin.Managers
{
    static class NewObjectLogic
    {
        internal static void HandleNewObject(NamedObjectSave newNamedObject)
        {
            var ati = newNamedObject.GetAssetTypeInfo();

            var isTileShapeCollection = ati == AssetTypeInfoAdder.Self
                .TileShapeCollectionAssetTypeInfo;

            if(isTileShapeCollection)
            {
                var owner = newNamedObject.GetContainer();

                var isGameScreen =
                    owner is ScreenSave && owner.GetStrippedName() == "GameScreen";
                if (isGameScreen)
                {
                    newNamedObject.SetByDerived = true;
                    // Added Nov 28, 2022 since these are often
                    // referenced outside of game screen
                    newNamedObject.HasPublicProperty = true;

                    var allDerived = ObjectFinder.Self.GetAllDerivedElementsRecursive(owner);
                    foreach(var derived in allDerived)
                    {
                        GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(derived);
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(derived);
                    }
                }
            }
        }
    }
}
