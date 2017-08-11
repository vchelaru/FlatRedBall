using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Managers
{
    public class RecursionManager : Singleton<RecursionManager>
    {
        public bool CanContainInstanceOf(IElement container, string typeInQuestion)
        {
            EntitySave typeAsEntitySave = Elements.ObjectFinder.Self.GetEntitySave(typeInQuestion);

            if (typeAsEntitySave != null)
            {
                // If the container is the same type or base type of the typeAsEntitySave, don't allow it
                if (typeAsEntitySave == container || typeAsEntitySave.InheritsFrom(container.Name))
                {
                    return false;
                }
            }
            return true;
        }



    }
}
