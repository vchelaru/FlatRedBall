using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.Managers
{
    public static class DragDropManagerGameWindow
    {
        public static CompilerViewModel CompilerViewModel { get; set; }
        public static async Task HandleDragDropOnGameWindow(ITreeNode treeNode, int gameHostWidth, int gameHostHeight, float screenX, float screenY)
        {

            /////////////////////////Early Out////////////////////////////////
            if (!CompilerViewModel.IsRunning || !CompilerViewModel.IsEditChecked)
            {
                return;
            }
            ScreenSave screen = await CommandSender.GetCurrentInGameScreen();
            if (screen == null)
            {
                return;
            }
            ///////////////////////End Early Out//////////////////////////////


            EntitySave entityToDrop = null;
            StateSave stateToSet = null;
            StateSaveCategory stateCategory = null;

            if (treeNode?.Tag is EntitySave entitySave)
            {
                entityToDrop = entitySave;
            }
            else if(treeNode?.Tag is StateSave stateSave)
            {
                entityToDrop = ObjectFinder.Self.GetElementContaining(stateSave) as EntitySave;
                stateCategory = treeNode.Parent?.Tag as StateSaveCategory;
                stateToSet = stateSave;
            }


            NamedObjectSave newNamedObject = null;

            if(entityToDrop != null)
            { 
                FlatRedBall.Content.Scene.CameraSave cameraSave = null;
                cameraSave = await CommandSender.GetCameraSave();


                if(cameraSave != null)
                {
                    var camera = new FlatRedBall.Camera();
                    cameraSave.SetCamera(camera);
                    camera.OrthogonalWidth = camera.OrthogonalHeight * cameraSave.AspectRatio;
                    camera.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle
                    {
                        X = 0,
                        Y = 0,
                        Width = gameHostWidth,
                        Height = gameHostHeight
                    };
                    float worldX = 0, worldY = 0;
                    FlatRedBall.Math.MathFunctions.WindowToAbsolute((int)screenX, (int)screenY, 
                        ref worldX, ref worldY, 0, camera,
                        FlatRedBall.Camera.CoordinateRelativity.RelativeToWorld);

                    RefreshManager.Self.ForcedNextObjectPosition = new System.Numerics.Vector2(worldX, worldY);
                    var newTreeNode = await DragDropManager.Self.DropEntityOntoElement(entityToDrop, screen);
                    newNamedObject = newTreeNode?.Tag as NamedObjectSave;
                }
            }

            if(newNamedObject != null && stateToSet != null && stateCategory != null)
            {
                var variableName = $"Current{stateCategory.Name}State";
                string stateName = stateToSet.Name;
                GlueCommands.Self.GluxCommands.SetVariableOn(newNamedObject, variableName, stateName);
            }
        }
    }
}
