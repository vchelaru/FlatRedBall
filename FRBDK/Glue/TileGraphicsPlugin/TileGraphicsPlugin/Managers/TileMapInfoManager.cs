using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileGraphicsPlugin.Managers
{
    public class TileMapInfoManager : FlatRedBall.Glue.Managers.Singleton<TileMapInfoManager>
    {

        public void AddAndModifyTileMapInfoClass()
        {
            var glux = GlueState.Self.CurrentGlueProject;

            CustomClassSave tileMapInfoClass = glux.CustomClasses.FirstOrDefault(item => item.Name == "TileMapInfo");

            bool wasAnythingAdded = false;

            if (tileMapInfoClass == null)
            {
                tileMapInfoClass = new CustomClassSave();

                tileMapInfoClass.Name = "TileMapInfo";

                glux.CustomClasses.Add(tileMapInfoClass);
                wasAnythingAdded = true;
            }

            //if (TryAdd(TilesetController.HasCollisionVariableName, "bool", false, tileMapInfoClass))
            //{
            //    wasAnythingAdded = true;
            //}

            //if (TryAdd(TilesetController.EntityToCreatePropertyName, "string", null, tileMapInfoClass))
            //{
            //    wasAnythingAdded = true;
            //}

            if(TryAdd("Name", "string", null, tileMapInfoClass))
            {
                wasAnythingAdded = true;
            }

            // Feb 2020
            // force it to eliminate any old AnimationFrameSaveBase
            var shouldForce = tileMapInfoClass.RequiredProperties.Any(item => item.Member == "EmbeddedAnimation" && item.Type?.Contains("AnimationFrameSaveBase") == true);

            // March 30 2021
            // This is noisy and always saves the project. Vic is trying to figure out why Glue always reloads on .NET Core, so removing this


            if (TryAdd("EmbeddedAnimation", "System.Collections.Generic.List<FlatRedBall.Content.AnimationChain.AnimationFrameSave>", null, tileMapInfoClass, shouldForce))
            {
                wasAnythingAdded = true;
            }

            if (wasAnythingAdded)
            {
                // Let's generate this asap:
                GlueCommands.Self.GenerateCodeCommands.GenerateCustomClassesCode();
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        bool TryAdd(string memberName, string type, object defaultValue, CustomClassSave tileMapInfoClass, bool forceAdd = false)
        {
            if(forceAdd)
            {
                tileMapInfoClass.RequiredProperties.RemoveAll(item => item.Member == memberName);
            }

            bool shouldAdd = tileMapInfoClass.RequiredProperties.Any(item=>item.Member == memberName) == false;
            if(shouldAdd)
            {
                InstructionSave instruction = new InstructionSave();
                instruction.Member = memberName;
                instruction.Type = type;
                instruction.Value = null;
                tileMapInfoClass.RequiredProperties.Add(instruction);
                
            }

            return shouldAdd;
        }

    }
}
