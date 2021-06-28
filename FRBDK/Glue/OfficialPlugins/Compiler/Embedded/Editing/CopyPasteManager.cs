using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Editing
{
    class CopyPasteManager
    {
        List<PositionedObject> CopiedPositionedObjects
        {
            get; set;
        } = new List<PositionedObject>();

        public void DoHotkeyLogic(PositionedObjectList<PositionedObject> selectedObjects)
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.IsCtrlDown)
            {
                if (keyboard.KeyPushed(Keys.C))
                {
                    CopiedPositionedObjects.Clear();
                    CopiedPositionedObjects.AddRange(selectedObjects);
                }
                if (keyboard.KeyPushed(Keys.V) && CopiedPositionedObjects != null)
                {
                    foreach(var copiedObject in CopiedPositionedObjects)
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

                        }
                        else if (copiedObject is Sprite originalSprite)
                        {
                            InstanceLogic.Self.HandleCreateSpriteByName(originalSprite);
                        }
                        else // positioned object, so entity?
                        {
                            // for now assume names are unique, not qualified
                            var instance = InstanceLogic.Self.CreateInstanceByGame(
                                copiedObject.GetType().Name,
                                copiedObject.X,
                                copiedObject.Y);
                            instance.CreationSource = "Glue";
                            instance.Velocity = Vector3.Zero;
                            instance.Acceleration = Vector3.Zero;
                        }

                    }
                }
            }
        }
    }
}
