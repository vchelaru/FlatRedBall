using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
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
        List<INameable> CopiedObjects
        {
            get; set;
        } = new List<INameable>();

        public void DoHotkeyLogic(List<INameable> selectedObjects)
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.IsCtrlDown)
            {
                if (keyboard.KeyPushed(Keys.C))
                {
                    CopiedObjects.Clear();
                    CopiedObjects.AddRange(selectedObjects);
                }
                if (keyboard.KeyPushed(Keys.V) && CopiedObjects != null)
                {
                    HandlePaste();
                }
            }
        }

        private void HandlePaste()
        {
            foreach (var copiedObject in CopiedObjects)
            {
                if (copiedObject is Circle originalCircle)
                {
                    InstanceLogic.Self.HandleCreateCircleByGame(originalCircle);
                }
                else if (copiedObject is AxisAlignedRectangle originalRectangle)
                {
                    InstanceLogic.Self.HandleCreateAxisAlignedRectangleByGame(originalRectangle);
                }
                else if (copiedObject is Polygon originalPolygon)
                {
                    InstanceLogic.Self.HandleCreatePolygonByGame(originalPolygon);
                }
                else if (copiedObject is Sprite originalSprite)
                {
                    InstanceLogic.Self.HandleCreateSpriteByName(originalSprite);
                }
                else if (copiedObject is Text originalText)
                {
                    InstanceLogic.Self.HandleCreateTextByName(originalText);
                }
                else if (copiedObject is PositionedObject asPositionedObject) // positioned object, so entity?
                {
                    var type = copiedObject.GetType().FullName;
                    if (copiedObject is Runtime.DynamicEntity dynamicEntity)
                    {
                        type = dynamicEntity.EditModeType;
                    }
                    // for now assume names are unique, not qualified
                    var instance = InstanceLogic.Self.CreateInstanceByGame(
                        type,
                        asPositionedObject);
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

            }

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
}
