using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Arrow.Managers
{
    public class GlueViewCommands
    {
        public void RefreshToCurrentElement()
        {
            var currentElement = ArrowState.Self.CurrentArrowElementSave;

            if (currentElement != null)
            {
                string elementName = currentElement.Name;

                string prefix;

                IElement glueElement = null;

                if (currentElement.ElementType == DataTypes.ElementType.Screen)
                {
                    prefix = "Screens/";
                    glueElement = ArrowState.Self.CurrentGlueProjectSave.Screens.FirstOrDefault(item=>item.Name == prefix + elementName);
                }
                else
                {
                    prefix = "Entities/";
                    glueElement = ArrowState.Self.CurrentGlueProjectSave.Entities.FirstOrDefault(item=>item.Name == prefix + elementName);

                }
                if(glueElement != null)
                {
                    GluxManager.GlueProjectSave = ArrowState.Self.CurrentGlueProjectSave;
                    GluxManager.ShowElement(prefix + elementName);
                }

            }

        }
    }
}
