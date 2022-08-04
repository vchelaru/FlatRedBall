using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using GlueControl.Managers;
using GlueControl.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    class CopyPasteManager
    {
        #region Fields/Properties

        List<INameable> CopiedObjects
        {
            get; set;
        } = new List<INameable>();

        List<NamedObjectSave> CopiedNamedObjects
        {
            get; set;
        } = new List<NamedObjectSave>();

        static GlueElement CopiedObjectsOwner;

        #endregion

        public void DoHotkeyLogic(List<INameable> selectedObjects, List<NamedObjectSave> selectedNamedObjects, IStaticPositionable itemGrabbed)
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.IsCtrlDown)
            {
                if (keyboard.KeyPushed(Keys.C))
                {
                    HandleCopy(selectedObjects, selectedNamedObjects);
                }
                if (keyboard.KeyPushed(Keys.V) && CopiedObjects != null)
                {
                    HandlePaste(itemGrabbed, selectedNamedObjects);
                }
            }
        }

        #region Copy

        private void HandleCopy(List<INameable> selectedObjects, List<NamedObjectSave> selectedNamedObjects)
        {
            CopiedObjects.Clear();
            CopiedNamedObjects.Clear();

            CopiedObjects.AddRange(selectedObjects);
            CopiedNamedObjects.AddRange(selectedNamedObjects);

#if HasGum && SupportsEditMode
            string message = "";
            if(selectedNamedObjects.Count == 1)
            {
                message = $"Copied {selectedNamedObjects[0]} to clipboard";
            }
            else
            {
                message = $"Copied {selectedNamedObjects.Count} objects to clipboard";
            }
            FlatRedBall.Forms.Controls.Popups.ToastManager.Show(message);
#endif

            CopiedObjectsOwner = GlueState.Self.CurrentElement;
        }

        #endregion

        #region Paste

        (float x, float y) GetXY(NamedObjectSave nos)
        {
            var xAsObject = nos.InstructionSaves.FirstOrDefault(item => item.Member == "X")?.Value;
            var yAsObject = nos.InstructionSaves.FirstOrDefault(item => item.Member == "Y")?.Value;
            float x = 0;
            float y = 0;
            if (xAsObject is float asFloatX)
            {
                x = asFloatX;
            }
            if (yAsObject is float asFloatY)
            {
                y = asFloatY;
            }
            return (x, y);
        }

        private async void HandlePaste(IStaticPositionable itemGrabbed, List<NamedObjectSave> selectedNamedObjects)
        {
            var currentElement = GlueState.Self.CurrentElement;
            NamedObjectSave newObjectToSelect = null;

            GetOffsetForPasting(itemGrabbed, selectedNamedObjects, out float? offsetX, out float? offsetY);

            List<Task> tasksToWait = new List<Task>();

            Debug.WriteLine($"Looping through CopiedNamedObjects with count {CopiedNamedObjects.Count}");

            var copyResponse = await GlueCommands.Self.GluxCommands.CopyNamedObjectListIntoElement(
                CopiedNamedObjects,
                CopiedObjectsOwner,
                currentElement);

            var newNamedObjects = copyResponse
                .Select(item => item.Data)
                .Where(item => item != null)
                .ToList();

            Debug.WriteLine($"Moving newNameObjects count {newNamedObjects.Count}" +
                $" with offset {offsetX}, {offsetY}");

            List<NosVariableAssignment> variableAssignments = new List<NosVariableAssignment>();
            foreach (var newNos in newNamedObjects)
            {
                if (offsetX != null)
                {
                    (float oldX, float oldY) = GetXY(newNos);
                    var newX = oldX + offsetX;
                    var newY = oldY + offsetY;

                    Debug.WriteLine($"Old X,Y:{oldX},{oldY}");
                    Debug.WriteLine($"New X,Y:{newX},{newY}");


                    if (newX != oldX)
                    {
                        variableAssignments.Add(new NosVariableAssignment
                        {
                            NamedObjectSave = newNos,
                            VariableName = "X",
                            Value = newX
                        });
                    }
                    if (newY != oldY)
                    {
                        variableAssignments.Add(new NosVariableAssignment
                        {
                            NamedObjectSave = newNos,
                            VariableName = "Y",
                            Value = newY
                        });
                    }
                }
            }
            await Managers.GlueCommands.Self.GluxCommands.SetVariableOnList(
                variableAssignments,
                currentElement,
                performSaveAndGenerateCode: true, updateUi: true, echoToGame: true);

            if (newObjectToSelect != null)
            {
                await GlueState.Self.SetCurrentNamedObjectSave(newObjectToSelect, currentElement);
            }
        }

        private void GetOffsetForPasting(IStaticPositionable itemGrabbed, List<NamedObjectSave> selectedNamedObjects, out float? offsetX, out float? offsetY)
        {
            offsetX = null;
            offsetY = null;
            NamedObjectSave matchingNos = null;
            if (itemGrabbed != null)
            {
                var itemGrabbedName = (itemGrabbed as INameable)?.Name;
                matchingNos = selectedNamedObjects.FirstOrDefault(item => item.InstanceName == itemGrabbedName);
            }
            if (matchingNos != null)
            {
                (float originalX, float originalY) = GetXY(matchingNos);

                var asPositionedObject = itemGrabbed as PositionedObject;

                if (asPositionedObject?.Parent == null)
                {
                    offsetX = itemGrabbed.X - originalX;
                    offsetY = itemGrabbed.Y - originalY;
                }
                else
                {
                    offsetX = asPositionedObject.RelativeX - originalX;
                    offsetY = asPositionedObject.RelativeY - originalY;
                }
            }
        }

        #endregion
    }
}
