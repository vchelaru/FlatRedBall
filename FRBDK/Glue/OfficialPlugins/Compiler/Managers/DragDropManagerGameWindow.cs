using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Managers;
using OfficialPlugins.Compiler.ViewModels;
using OfficialPlugins.GameHost.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace OfficialPlugins.Compiler.Managers
{
    public class DragDropManagerGameWindow
    {
        private RefreshManager _refreshManager;

        public CompilerViewModel CompilerViewModel { get; set; }

        public DragDropManagerGameWindow(RefreshManager refreshManager)
        {
            _refreshManager = refreshManager;
        }



        public async void HandleDragDropTimerElapsed(GameHostView gameHostView)
        {
            try
            {
                // These suck - they dont' return anything if the user is over only teh wpf item:
                //var point = System.Windows.Input.Mouse.GetPosition(gameHostView);
                //var position = gameHostView.PointToScreen(point);
                var winformsPoint = System.Windows.Forms.Control.MousePosition;

                var controlForDragDrop = gameHostView.WinformsHost;

                var locationFromScreen = controlForDragDrop.PointToScreen(new Point(0, 0));
                // Transform screen point to WPF device independent point
                PresentationSource source = PresentationSource.FromVisual(controlForDragDrop);
                System.Windows.Point targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);

                var isCursorOverTab = winformsPoint.X >= targetPoints.X &&
                    winformsPoint.Y >= targetPoints.Y &&
                    winformsPoint.X <= (targetPoints.X + controlForDragDrop.ActualWidth) &&
                    winformsPoint.Y <= (targetPoints.Y + controlForDragDrop.ActualHeight);


                CompilerViewModel.HasDraggedTreeNodeOverView = GlueState.Self.DraggedTreeNode != null && isCursorOverTab;

                if (CompilerViewModel.HasDraggedTreeNodeOverView &&
                    (System.Windows.Forms.Control.MouseButtons & System.Windows.Forms.MouseButtons.Left) == 0)
                {
                    // user is not holding the mouse button down
                    float screenX = (float)(winformsPoint.X - targetPoints.X);
                    float screenY = (float)(winformsPoint.Y - targetPoints.Y);

                    var draggedNode = GlueState.Self.DraggedTreeNode;
                    // set this before calling HandleDragDropOnGameWindow to prevent a double-drop because the node is not null
                    GlueState.Self.DraggedTreeNode = null;

                    int gameHostWidth = (int)controlForDragDrop.ActualWidth;
                    int gameHostHeight = (int)controlForDragDrop.ActualHeight;

                    await HandleDragDropOnGameWindow(draggedNode, gameHostWidth, gameHostHeight, screenX, screenY);
                }

            }
            // This can get called before the control is created, so tolerate exceptions
            catch { }
        }

        public async Task HandleDragDropOnGameWindow(ITreeNode treeNode, int gameHostWidth, 
            int gameHostHeight, float screenX, float screenY)
        {

            /////////////////////////Early Out////////////////////////////////
            if (!CompilerViewModel.IsRunning || !CompilerViewModel.IsEditChecked)
            {
                return;
            }
            GlueElement element = await CommandSender.GetCurrentInGameScreen();
            if(element == null)
            {
                element = GlueState.Self.CurrentElement;
            }
            if (element == null)
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

                    _refreshManager.ForcedNextObjectPosition = new System.Numerics.Vector2(worldX, worldY);
                    var newTreeNode = await DragDropManager.Self.DropEntityOntoElement(entityToDrop, element);
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
