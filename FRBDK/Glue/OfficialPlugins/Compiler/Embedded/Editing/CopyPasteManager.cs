using FlatRedBall;
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
        PositionedObject CopiedPositionedObject
        {
            get; set;
        }

        public void DoHotkeyLogic(PositionedObject selectedObject)
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.IsCtrlDown)
            {
                if (keyboard.KeyPushed(Keys.C))
                {
                    CopiedPositionedObject = selectedObject;
                }
                if (keyboard.KeyPushed(Keys.V) && CopiedPositionedObject != null)
                {
                    if (CopiedPositionedObject is Circle originalCircle)
                    {
                        //var newCircle = originalCircle.Clone();

                    }
                    else if (CopiedPositionedObject is AxisAlignedRectangle originalRectangle)
                    {

                    }
                    else if (CopiedPositionedObject is Polygon originalPolygon)
                    {

                    }
                    else if (CopiedPositionedObject is Sprite originalSprite)
                    {

                    }
                    else // positioned object, so entity?
                    {
                        // for now assume names are unique, not qualified
                        var instance = InstanceLogic.Self.CreateInstanceByGame(
                            CopiedPositionedObject.GetType().Name,
                            CopiedPositionedObject.X,
                            CopiedPositionedObject.Y);
                        instance.CreationSource = "Glue";
                        instance.Velocity = Vector3.Zero;
                        instance.Acceleration = Vector3.Zero;
                    }
                }
            }
        }
    }
}
