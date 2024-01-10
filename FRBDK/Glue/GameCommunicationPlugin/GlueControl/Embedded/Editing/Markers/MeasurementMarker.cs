using FlatRedBall.Gui;
using GlueControl.Editing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    public class MeasurementMarker
    {
        Vector3 StartPointGrabbed;

        public void Update()
        {
            var cursor = GuiManager.Cursor;
            if (cursor.SecondaryPush)
            {
                StartPointGrabbed = cursor.WorldPosition.ToVector3();
            }
            if (cursor.SecondaryDown)
            {
                DrawMarker(cursor);
            }
        }

        private void DrawMarker(Cursor cursor)
        {
            // don't use WithX/WithY because those aren't in older versions of FRB and
            // Vic doesn't want to increment the GLUJ version just for that.
            var cursorPosition = cursor.WorldPosition.ToVector3();

            var horizontalEnd = StartPointGrabbed;
            horizontalEnd.X = cursorPosition.X;
            EditorVisuals.Line(StartPointGrabbed, horizontalEnd);

            var verticalEnd = StartPointGrabbed;
            verticalEnd.Y = cursorPosition.Y;
            EditorVisuals.Line(StartPointGrabbed, verticalEnd);

            var differenceX = cursorPosition.X - StartPointGrabbed.X;

            var midpointX = StartPointGrabbed.X + differenceX / 2.0f;
            var midpointPositionX = new Vector3(midpointX, StartPointGrabbed.Y, 0);
            EditorVisuals.Line(midpointPositionX.AddY(6), midpointPositionX.AddY(-6));

            EditorVisuals.Line(midpointPositionX, midpointPositionX.AddY(cursorPosition.Y - StartPointGrabbed.Y), Color.Gray);

            var darkGray = new Color(50, 50, 50, 255);

            midpointX = StartPointGrabbed.X + differenceX / 4.0f;
            midpointPositionX = new Vector3(midpointX, StartPointGrabbed.Y, 0);
            EditorVisuals.Line(midpointPositionX.AddY(3), midpointPositionX.AddY(-3));

            EditorVisuals.Line(midpointPositionX, midpointPositionX.AddY(cursorPosition.Y - StartPointGrabbed.Y), darkGray);



            midpointX = StartPointGrabbed.X + 3 * differenceX / 4.0f;
            midpointPositionX = new Vector3(midpointX, StartPointGrabbed.Y, 0);
            EditorVisuals.Line(midpointPositionX.AddY(3), midpointPositionX.AddY(-3));

            EditorVisuals.Line(midpointPositionX, midpointPositionX.AddY(cursorPosition.Y - StartPointGrabbed.Y), darkGray);




            var differenceY = cursorPosition.Y - StartPointGrabbed.Y;
            var midpointY = StartPointGrabbed.Y + differenceY / 2.0f;
            var midpointPositionY = new Vector3(StartPointGrabbed.X, midpointY, 0);
            EditorVisuals.Line(midpointPositionY.AddX(-6), midpointPositionY.AddX(6));

            EditorVisuals.Line(midpointPositionY, midpointPositionY.AddX(cursorPosition.X - StartPointGrabbed.X), Color.Gray);


            midpointY = StartPointGrabbed.Y + differenceY / 4.0f;
            midpointPositionY = new Vector3(StartPointGrabbed.X, midpointY, 0);
            EditorVisuals.Line(midpointPositionY.AddX(-3), midpointPositionY.AddX(3));

            EditorVisuals.Line(midpointPositionY, midpointPositionY.AddX(cursorPosition.X - StartPointGrabbed.X), darkGray);


            midpointY = StartPointGrabbed.Y + 3 * differenceY / 4.0f;
            midpointPositionY = new Vector3(StartPointGrabbed.X, midpointY, 0);
            EditorVisuals.Line(midpointPositionY.AddX(-3), midpointPositionY.AddX(3));

            EditorVisuals.Line(midpointPositionY, midpointPositionY.AddX(cursorPosition.X - StartPointGrabbed.X), darkGray);


            var angleInRadians = (cursorPosition - StartPointGrabbed).Angle();
            if (angleInRadians != null)
            {

                var text = EditorVisuals.Text($"{angleInRadians:N3} RAD  {MathHelper.ToDegrees(angleInRadians.Value):N0} DEG", StartPointGrabbed);
                if (differenceY > 0)
                {
                    text.VerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Top;
                }
                else
                {
                    text.VerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Bottom;
                }

                EditorVisuals.Arrow(StartPointGrabbed, cursorPosition, Color.Green);
            }

            if (differenceY > 0)
            {
                var endText = EditorVisuals.Text(cursorPosition.ToString(), cursorPosition);

                endText.VerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Bottom;
            }
            else
            {
                var position = cursorPosition.AddY(-20 / CameraLogic.CurrentZoomRatio);

                var endText = EditorVisuals.Text(cursorPosition.ToString(), position);

                endText.VerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Top;
            }

            var widthTextPosition = new Vector3(StartPointGrabbed.X + differenceX / 2.0f, StartPointGrabbed.Y, 0);
            FlatRedBall.Graphics.VerticalAlignment widthTextVerticalAlignment;
            if (differenceY > 0)
            {
                widthTextPosition.Y -= 20 / CameraLogic.CurrentZoomRatio;
                widthTextVerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Top;
            }
            else
            {
                widthTextPosition.Y += 20 / CameraLogic.CurrentZoomRatio;
                widthTextVerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Bottom;
            }
            EditorVisuals.Text($"Width\n{differenceX:N3}", widthTextPosition).VerticalAlignment = widthTextVerticalAlignment;


            var heightTextPosition = new Vector3(StartPointGrabbed.X, StartPointGrabbed.Y + differenceY / 2.0f, 0);
            FlatRedBall.Graphics.HorizontalAlignment heightTextHorizontalAlignment;
            if (differenceX > 0)
            {
                heightTextHorizontalAlignment = FlatRedBall.Graphics.HorizontalAlignment.Right;
                heightTextPosition.X -= 20 / CameraLogic.CurrentZoomRatio;
            }
            else
            {
                heightTextHorizontalAlignment = FlatRedBall.Graphics.HorizontalAlignment.Left;
                heightTextPosition.X += 20 / CameraLogic.CurrentZoomRatio;
            }
            EditorVisuals.Text($"Height\n{differenceY:N3}", heightTextPosition).HorizontalAlignment = heightTextHorizontalAlignment;
        }
    }
}
