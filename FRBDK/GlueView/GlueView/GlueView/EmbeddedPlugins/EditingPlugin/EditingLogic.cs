using FlatRedBall;
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
                PositionedObject objectToMove = GetEffectivePositionedObject();
                var cursor = GuiManager.Cursor;

                if (objectToMove.Parent == null)
                {
                    objectToMove.X += x;
                    objectToMove.Y += y;
                }
                else
                {
                    objectToMove.RelativeX += x;
                    objectToMove.RelativeY += y;
                }
                didMove = true;
            }

        }

        private static PositionedObject GetEffectivePositionedObject()
        {
            PositionedObject objectToMove = grabbedElement;
            if (grabbedElement.DirectObjectReference is PositionedObject)
            {
                objectToMove = grabbedElement.DirectObjectReference as PositionedObject;
            }

            return objectToMove;
        }

        internal static void HandleClick()
        {
            bool shouldSave = false;
            if(grabbedElement != null && didMove)
            {

                var nos = grabbedElement.AssociatedNamedObjectSave;

                if (nos != null)
                {
                    shouldSave = true;

                    PositionedObject objectToMove = GetEffectivePositionedObject();

                    var xVariable = nos.InstructionSaves.FirstOrDefault(item => item.Member == "X");
                    var yVariable = nos.InstructionSaves.FirstOrDefault(item => item.Member == "Y");

                    if (xVariable == null)
                    {
                        xVariable = new FlatRedBall.Glue.SaveClasses.CustomVariableInNamedObject();
                        xVariable.Member = "X";
                        nos.InstructionSaves.Add(xVariable);
                    }
                    if (yVariable == null)
                    {
                        yVariable = new FlatRedBall.Glue.SaveClasses.CustomVariableInNamedObject();
                        yVariable.Member = "Y";
                        nos.InstructionSaves.Add(yVariable);
                    }

                    xVariable.Value = objectToMove.Parent == null ? objectToMove.X : objectToMove.RelativeX;
                    yVariable.Value = objectToMove.Parent == null ? objectToMove.Y : objectToMove.RelativeY;
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