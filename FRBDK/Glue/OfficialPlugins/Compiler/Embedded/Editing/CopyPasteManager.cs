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

        public void DoHotkeyLogic(List<INameable> selectedObjects, List<NamedObjectSave> selectedNamedObjects, PositionedObject itemGrabbed)
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.IsCtrlDown)
            {
                if (keyboard.KeyPushed(Keys.C))
                {
                    CopiedObjects.Clear();
                    CopiedNamedObjects.Clear();

                    CopiedObjects.AddRange(selectedObjects);
                    CopiedNamedObjects.AddRange(selectedNamedObjects);

                    CopiedObjectsOwner = GlueState.Self.CurrentElement;
                }
                if (keyboard.KeyPushed(Keys.V) && CopiedObjects != null)
                {
                    HandlePaste(itemGrabbed, selectedNamedObjects);
                }
            }
        }

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

        private async void HandlePaste(PositionedObject itemGrabbed, List<NamedObjectSave> selectedNamedObjects)
        {
            var currentElement = GlueState.Self.CurrentElement;
            NamedObjectSave newObjectToSelect = null;

            GetoffsetForPasting(itemGrabbed, selectedNamedObjects, out float? offsetX, out float? offsetY);

            ConcurrentQueue<NamedObjectSave> newNamedObjects = new ConcurrentQueue<NamedObjectSave>();

            async Task SendCopyToEditor(NamedObjectSave originalNamedObject)
            {
                var response = await GlueCommands.Self.GluxCommands.CopyNamedObjectIntoElement(
                    originalNamedObject, CopiedObjectsOwner, currentElement,
                    performSaveAndGenerateCode: false);
                if (response.Succeeded)
                {
                    var newNos = response.Data;
                    newNamedObjects.Enqueue(newNos);
                    if (itemGrabbed == null)
                    {
                        newObjectToSelect = newNos;
                    }
                }
            }


            List<Task> tasksToWait = new List<Task>();
            foreach (var originalNamedObject in CopiedNamedObjects)
            {
                var task = SendCopyToEditor(originalNamedObject);
                tasksToWait.Add(task);
            }
            tasksToWait.Add(GlueCommands.Self.GluxCommands.SaveGlux());
            tasksToWait.Add(GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(currentElement));

            await Task.WhenAll(tasksToWait);

            foreach (var newNos in newNamedObjects)
            {
                if (offsetX != null)
                {
                    (float oldX, float oldY) = GetXY(newNos);
                    var newX = oldX + offsetX;
                    var newY = oldY + offsetY;
                    if (newX != oldX)
                    {
                        await GlueCommands.Self.GluxCommands.SetVariableOn(newNos, currentElement, "X", newX);
                    }
                    if (newY != oldY)
                    {
                        await GlueCommands.Self.GluxCommands.SetVariableOn(newNos, currentElement, "Y", newY);
                    }
                }
            }

            if (newObjectToSelect != null)
            {
                await GlueState.Self.SetCurrentNamedObjectSave(newObjectToSelect, currentElement);
            }
        }

        private void GetoffsetForPasting(PositionedObject itemGrabbed, List<NamedObjectSave> selectedNamedObjects, out float? offsetX, out float? offsetY)
        {
            offsetX = null;
            offsetY = null;
            NamedObjectSave matchingNos = null;
            if (itemGrabbed != null)
            {
                matchingNos = selectedNamedObjects.FirstOrDefault(item => item.InstanceName == itemGrabbed.Name);
            }
            if (matchingNos != null)
            {
                (float originalX, float originalY) = GetXY(matchingNos);

                if (itemGrabbed.Parent == null)
                {
                    offsetX = itemGrabbed.X - originalX;
                    offsetY = itemGrabbed.Y - originalY;
                }
                else
                {
                    offsetX = itemGrabbed.RelativeX - originalX;
                    offsetY = itemGrabbed.RelativeY - originalY;
                }
            }
        }

        private static void HandlePasteIndividualObject(List<PositionedObject> newObjects, List<Dtos.AddObjectDto> addedItems,
            INameable copiedObject, NamedObjectSave copiedGlueNamedObjectSave)
        {
            PositionedObject instance = null;

            var copiedObjectName = copiedObject.Name;

            if (copiedObject is Circle originalCircle)
            {
                instance = InstanceLogic.Self.HandleCreateCircleByGame(originalCircle, copiedObjectName, addedItems);
            }
            else if (copiedObject is AxisAlignedRectangle originalRectangle)
            {
                instance = InstanceLogic.Self.HandleCreateAxisAlignedRectangleByGame(originalRectangle, copiedObjectName, addedItems);
            }
            else if (copiedObject is Polygon originalPolygon)
            {
                instance = InstanceLogic.Self.HandleCreatePolygonByGame(originalPolygon, copiedObjectName, addedItems);
            }
            else if (copiedObject is Sprite originalSprite)
            {
                instance = InstanceLogic.Self.HandleCreateSpriteByName(originalSprite, copiedObjectName, addedItems);
            }
            else if (copiedObject is Text originalText)
            {
                instance = InstanceLogic.Self.HandleCreateTextByName(originalText, copiedObjectName, addedItems);
            }
            else if (copiedObject is PositionedObject asPositionedObject) // positioned object, so entity?
            {
                var type = copiedObject.GetType().FullName;
                if (copiedObject is Runtime.DynamicEntity dynamicEntity)
                {
                    type = dynamicEntity.EditModeType;
                }
                // for now assume names are unique, not qualified
                instance = InstanceLogic.Self.CreateInstanceByGame(
                    type,
                    asPositionedObject, addedItems);
                instance.CreationSource = "Glue";
                instance.Velocity = Vector3.Zero;
                instance.Acceleration = Vector3.Zero;

                // apply any changes that have been made to the entity:
                int currentAddObjectIndex = CommandReceiver.GlobalGlueToGameCommands.Count;

                for (int i = 0; i < currentAddObjectIndex; i++)
                {
                    var dto = CommandReceiver.GlobalGlueToGameCommands[i];
                    if (dto is Dtos.AddObjectDto addObjectDtoRerun)
                    {
                        InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(addObjectDtoRerun, currentAddObjectIndex, instance);
                    }
                    else if (dto is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                    {
                        GlueControl.Editing.VariableAssignmentLogic.SetVariable(glueVariableSetDataRerun, instance);
                    }
                }
            }

            if (instance != null)
            {
                newObjects.Add(instance);
                var entityViewingScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.EntityViewingScreen;
                var parent = entityViewingScreen?.CurrentEntity as PositionedObject;
                if (parent != null)
                {
                    instance.AttachTo(parent);
                }
                var isPastedInNewObject = CopiedObjectsOwner?.Name != GlueState.Self.CurrentElement?.Name;

                if (isPastedInNewObject)
                {
                    var dto = addedItems.LastOrDefault();
                    instance.X = Camera.Main.X;
                    instance.Y = Camera.Main.Y;
                    // move it and set its values
                    var xInstruction = dto.InstructionSaves.FirstOrDefault(item => item.Member == "X");
                    var yInstruction = dto.InstructionSaves.FirstOrDefault(item => item.Member == "Y");

                    void AddFloatValue(string memberName, float value)
                    {
                        dto.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
                        {
                            Member = memberName,
                            Type = "float",
                            Value = value
                        });
                    }

                    if (entityViewingScreen != null)
                    {
                        instance.Z = parent.Z;
                        instance.SetRelativeFromAbsolute();
                        if (xInstruction != null)
                        {
                            xInstruction.Value = instance.RelativeX;
                        }
                        else
                        {
                            AddFloatValue("X", instance.RelativeX);
                        }
                        if (yInstruction != null)
                        {
                            yInstruction.Value = instance.RelativeY;
                        }
                        else
                        {
                            AddFloatValue("Y", instance.RelativeY);
                        }

                    }
                    else
                    {
                        if (xInstruction != null)
                        {
                            xInstruction.Value = instance.X;
                        }
                        else
                        {
                            AddFloatValue("X", instance.X);
                        }
                        if (yInstruction != null)
                        {
                            yInstruction.Value = instance.Y;
                        }
                        else
                        {
                            AddFloatValue("Y", instance.Y);

                        }
                    }

                }
            }


        }

        #endregion
    }
}
