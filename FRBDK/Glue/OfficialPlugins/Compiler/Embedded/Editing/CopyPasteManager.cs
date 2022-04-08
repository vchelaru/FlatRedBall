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
                    HandlePaste(itemGrabbed);
                }
            }
        }

        #region Paste

        private void HandlePaste(PositionedObject itemGrabbed)
        {
            List<PositionedObject> newObjects = new List<PositionedObject>();
            List<Dtos.AddObjectDto> addedItems = new List<Dtos.AddObjectDto>();

            for (int i = 0; i < CopiedObjects.Count; i++)
            {
                var copiedRuntimeObject = CopiedObjects[i];
                var copiedGlueObject = CopiedNamedObjects[i];
                HandlePasteIndividualObject(newObjects, addedItems, copiedRuntimeObject, copiedGlueObject);
            }

            // If we have something grabbed, then don't select the new items in Glue
            foreach (var item in addedItems)
            {
                item.SelectNewObject = itemGrabbed == null;
            }

            GlueControlManager.Self.SendToGlue(addedItems);

            // If the user is dragging objects around and pasting them, then we won't select
            // pasted objects. If the user does a simple copy/paste without dragging, then select
            // the new object.
            var shouldSelectNewObjectsInGame = itemGrabbed == null;

            if (shouldSelectNewObjectsInGame)
            {
                var allNamedObjects = EditingManager.Self.CurrentGlueElement.AllNamedObjects.ToArray();

                var isFirst = true;
                foreach (var newObject in newObjects)
                {
                    var matchingNos = allNamedObjects.FirstOrDefault(item => item.InstanceName == newObject.Name);
                    EditingManager.Self.Select(matchingNos, addToExistingSelection: !isFirst);
                    isFirst = false;
                }
            }
            else
            {
                if (CopiedObjects.Count > 0)
                {
                    // If at least one object was copied, then we sent that one object over to Glue. Glue will
                    // automatically select newly-created objects, but we don't want that to happen when we copy/paste,
                    // so we re-send the select command on the first selected item. If only one item is selected, this will
                    // work perfectly. If not, then the first item is sent over, which is as good as we can do since Glue doesn't
                    // support multi-selection.
                    EditingManager.Self.RaiseObjectSelected();
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
