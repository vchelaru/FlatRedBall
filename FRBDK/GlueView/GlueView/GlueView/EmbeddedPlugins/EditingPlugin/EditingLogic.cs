using FlatRedBall.Glue;
using FlatRedBall.Gui;
using GlueView.Facades;
using System;
using System.Linq;

namespace GlueView.EmbeddedPlugins.EditingPlugin
{
    internal class EditingLogic
    {
        static ElementRuntime grabbedElement;
        static bool didMove = false;

        internal static void HandleMouseMove()
        {
            if(grabbedElement != null)
            {
                var cursor = GuiManager.Cursor;

                MoveGrabbedElementBy(cursor.WorldXChangeAt(grabbedElement.Z), cursor.WorldYChangeAt(grabbedElement.Z));

            }
        }

        private static void MoveGrabbedElementBy(float x, float y)
        {
            if(x != 0 || y != 0)
            {

                var cursor = GuiManager.Cursor;

                if(grabbedElement.Parent == null)
                {
                    grabbedElement.X += x;
                    grabbedElement.Y += y;

                    var directObject = grabbedElement.DirectObjectReference;

                    if(directObject is FlatRedBall.PositionedObject)
                    {
                        var asPO = directObject as FlatRedBall.PositionedObject;
                        asPO.X += x;
                        asPO.Y += y;
                    }

                }
                else
                {
                    grabbedElement.RelativeX += x;
                    grabbedElement.RelativeY += y;

                    var directObject = grabbedElement.DirectObjectReference;

                    if (directObject is FlatRedBall.PositionedObject)
                    {
                        var asPO = directObject as FlatRedBall.PositionedObject;
                        asPO.RelativeX += x;
                        asPO.RelativeY += y;
                    }
                }
                didMove = true;
            }

        }

        internal static void HandleClick()
        {
            bool shouldSave = false;
            if(grabbedElement != null && didMove)
            {

                var nos = grabbedElement.AssociatedNamedObjectSave;

                if (nos != null)
                {

                    var xVariable = nos.InstructionSaves.FirstOrDefault(item => item.Member == "X");
                    var yVariable = nos.InstructionSaves.FirstOrDefault(item => item.Member == "Y");


                    if (xVariable != null)
                    {
                        xVariable.Value = grabbedElement.Parent == null ? grabbedElement.X : grabbedElement.RelativeX;
                        shouldSave = true;
                    }
                    else
                    {
                        xVariable = new FlatRedBall.Glue.SaveClasses.CustomVariableInNamedObject();
                        xVariable.Member = "X";
                        xVariable.Value = grabbedElement.Parent == null ? grabbedElement.X : grabbedElement.RelativeX;
                        nos.InstructionSaves.Add(xVariable);
                        shouldSave = true;
                    }
                    if (yVariable != null)
                    {
                        yVariable.Value = grabbedElement.Parent == null ? grabbedElement.Y : grabbedElement.RelativeY;
                        shouldSave = true;
                    }
                    else
                    {
                        yVariable = new FlatRedBall.Glue.SaveClasses.CustomVariableInNamedObject();
                        yVariable.Member = "Y";
                        xVariable.Value = grabbedElement.Parent == null ? grabbedElement.Y : grabbedElement.RelativeY;
                        nos.InstructionSaves.Add(yVariable);
                        shouldSave = true;
                    }
                }
            }

            if (shouldSave)
            {
                var currentElement = GlueViewState.Self.CurrentElement;
                GlueViewCommands.Self.GlueProjectSaveCommands.SaveGlux();
            }

            grabbedElement = null;

        }

        internal static void HandlePush()
        {
            didMove = false;
            grabbedElement = GlueViewState.Self.CursorState.GetElementRuntimeOver();
        }
    }
}