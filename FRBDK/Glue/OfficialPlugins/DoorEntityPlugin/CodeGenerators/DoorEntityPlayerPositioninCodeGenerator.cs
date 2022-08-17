using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.DoorEntityPlugin.CodeGenerators
{
    internal class DoorEntityPlayerPositioninCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation => CodeLocation.AfterStandardGenerated;

        bool HasDoorEntity => ObjectFinder.Self.GlueProject.Entities.Any(item => item.Name == "Entities\\DoorEntity");

        // This has to be in AddToManagers so that all the lists have been filled.
        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            if(!HasDoorEntity)
            {
                return codeBlock;
            }
            var allNamedObjects = element.AllNamedObjects;

            var player = allNamedObjects.FirstOrDefault(item => item.ClassType == "Player");

            if(player != null && element is ScreenSave && player.DefinedByBase == false)
            {
                // This 
                var ifBlock = codeBlock.If("Entities.DoorEntity.NextDestinationObject != null");

                var listItems = element.AllNamedObjects.Where(item => item.IsList).ToArray();

                var itemsNotInLists = element.NamedObjects.Where(item =>
                    item.GetAssetTypeInfo()?.IsPositionedObject == true ||
                    item.SourceType == SourceType.Entity);

                ifBlock.Line("var found = false;");

                foreach(var listItem in listItems)
                {
                    var ifForThisList = ifBlock.If("found == false");
                    var forBlock = ifForThisList.For($"int i = 0; i < {listItem.FieldName}.Count; i++");
                    {
                        var innerIf = forBlock.If($"{listItem.FieldName}[i].Name == Entities.DoorEntity.NextDestinationObject");
                        {
                            innerIf.Line($"{player.FieldName}.Position = {listItem.FieldName}[i].Position;");
                            innerIf.Line("found = true;");
                            innerIf.Line("break;");

                        }
                    }
                }

                foreach(var item in itemsNotInLists)
                {
                    var itemIf = ifBlock.If($"found == false && Entities.DoorEntity.NextDestinationObject == \"{item.InstanceName}\"");
                    itemIf.Line($"{player.FieldName}.Position = {item.FieldName}.Position;");
                    itemIf.Line("found = true;");

                }

                var positionIf = codeBlock.ElseIf("Entities.DoorEntity.NextDestinationX != null && Entities.DoorEntity.NextDestinationY != null");
                positionIf.Line($"{player.FieldName}.X = Entities.DoorEntity.NextDestinationX.Value;");
                positionIf.Line($"{player.FieldName}.Y = Entities.DoorEntity.NextDestinationY.Value;");
            }

            return codeBlock;
        }
    }
}
