using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Glue.SaveClasses;

namespace ArrowDataConversion
{
    public class ArrowProjectToGlueProjectConverter
    {
        public GlueProjectSave ToGlueProjectSave(ArrowProjectSave arrowProject)
        {
            GlueProjectSave toReturn = new GlueProjectSave();

            ArrowElementToGlueConverter elementToElementConverter = new ArrowElementToGlueConverter();

            foreach (var element in arrowProject.Elements)
            {
                IElement glueElement = elementToElementConverter.ToGlueIElement(element);

                if (glueElement is ScreenSave)
                {
                    toReturn.Screens.Add(glueElement as ScreenSave);
                }
                else
                {
                    toReturn.Entities.Add(glueElement as EntitySave);
                }

            }



            return toReturn;


        }
    }
}
