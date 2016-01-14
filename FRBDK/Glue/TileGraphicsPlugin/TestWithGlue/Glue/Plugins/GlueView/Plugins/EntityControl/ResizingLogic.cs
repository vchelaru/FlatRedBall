using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using GlueViewTestPlugins.EntityControl.Handles;

namespace GlueViewTestPlugins.EntityControl
{
    public class ResizingLogic
    {

        public void ApplyChangeVariables(PositionedObject objectToResize, float cursorXChange, float cursorYChange,
            float xChangeCoef, float yChangeCoef)
        {
            //IScalable
            if (objectToResize is IScalable)
            {
                IScalable scalable = (IScalable)objectToResize;
                //Scale
                if (scalable.ScaleX + cursorXChange < 0)
                {
                    scalable.ScaleX = 0;
                    xChangeCoef = 0;
                }
                else
                {
                    scalable.ScaleX += cursorXChange;
                }

                if (scalable.ScaleY + cursorYChange < 0)
                {
                    scalable.ScaleY = 0;
                    yChangeCoef = 0;
                }
                else
                {
                    scalable.ScaleY += cursorYChange;
                }
            }
            else if (objectToResize is ElementRuntime &&
                ((ElementRuntime)objectToResize).AssociatedNamedObjectSave != null &&
                ((ElementRuntime)objectToResize).AssociatedNamedObjectSave.GetIsScalableEntity())
            {
                ElementRuntime currentElement = objectToResize as ElementRuntime;
                float scaleX = (float)currentElement.AssociatedNamedObjectSave.GetEffectiveValue("ScaleX");
                float scaleY = (float)currentElement.AssociatedNamedObjectSave.GetEffectiveValue("ScaleY");

                IElement entitySave = currentElement.AssociatedIElement;
                CustomVariable variable = entitySave.GetCustomVariable("ScaleX").Clone();

                variable.DefaultValue = scaleX + cursorXChange;
                currentElement.AssociatedNamedObjectSave.GetCustomVariable("ScaleX").Value = scaleX + cursorXChange;
                currentElement.SetCustomVariable(variable);


                variable = entitySave.GetCustomVariable("ScaleY").Clone();
                variable.DefaultValue = scaleY + cursorYChange;
                currentElement.AssociatedNamedObjectSave.GetCustomVariable("ScaleY").Value = variable.DefaultValue;
                currentElement.SetCustomVariable(variable);

            }

            AdjustPositionsAfterScaling(objectToResize, cursorXChange, cursorYChange, xChangeCoef, yChangeCoef);

        }

        public void ApplyCircleResize(ref float cursorXChange, ref float cursorYChange, ref float xChangeCoef, ref float yChangeCoef, float selectedScaleXCoefficient, float selectedScaleYCoefficient, float oppositeX, float oppositeY, float cursorX, float cursorY, Circle circle, float handleX, float handleY)
        {
            if (selectedScaleXCoefficient != 0 && selectedScaleYCoefficient != 0)
            {
                float change = 0;
                float radius = circle.Radius;

                float opX = oppositeX;
                float opY = oppositeY;

				//If the cursor is past the highlight line
				if (((handleX > opX && cursorX < opX) || (handleX < opX && cursorX > opX)) || ((handleY > opY && cursorY < opY) || (handleY < opY && cursorY > opY)))
				{
					//Do nothing for now
				}
				else if (Math.Abs(cursorX - opX) > Math.Abs(cursorY - opY))
                {
                    change = (Math.Abs(cursorX - opX) - radius * 2) / 2;
                }
                else
                {
					change = (Math.Abs(cursorY - opY) - radius * 2) / 2;
                }

                if (circle.Radius + change < 0)
                {
                    circle.Radius = 0;
                    xChangeCoef = 0;
                    yChangeCoef = 0;
                }
                else
                {
                    circle.Radius += change;
                }

                cursorXChange = change;
                cursorYChange = change;
            }
            else
            {
                if (circle.Radius + cursorXChange < 0)
                {
                    circle.Radius = 0;
                    xChangeCoef = 0;
                }
                else
                {
                    circle.Radius += cursorXChange;
                }

                if (circle.Radius + cursorYChange < 0)
                {
                    circle.Radius = 0;
                    yChangeCoef = 0;
                }
                else
                {
                    circle.Radius += cursorYChange;
                }
            }



            AdjustPositionsAfterScaling(circle, cursorXChange, cursorYChange, xChangeCoef, yChangeCoef);
        }

        private static void AdjustPositionsAfterScaling(PositionedObject pObj, float xChange, float yChange, float xChangeCoef, float yChangeCoef)
        {
            //Move to line up with the scale properly
            if (pObj.Parent == null)
            {
                pObj.X += xChange * xChangeCoef;
                pObj.Y += yChange * yChangeCoef;

            }
            else
            {
                pObj.RelativeX += xChange * xChangeCoef;
                pObj.RelativeY += yChange * yChangeCoef;
                pObj.ForceUpdateDependencies();
            }
        }



    }
}
