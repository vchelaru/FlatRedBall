using FlatRedBall.Arrow.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.Managers
{
    public class IntentManager : Singleton<IntentManager>
    {
        public void AddRequirementsForIntent(ArrowElementSave element, ArrowIntentSave intent)
        {
            foreach (var component in intent.Components)
            {
                AddRequirementForComponent(element, component);
            }

        }

        private void AddRequirementForComponent(ArrowElementSave element, ArrowIntentComponentSave component)
        {
            switch (component.GlueItemType)
            {
                case GlueItemType.Entity:

                    break;
                case GlueItemType.File:
                    AddFileForComponent(element, component);
                    break;
                case GlueItemType.NamedObject:

                    break;
                case GlueItemType.Screen:

                    break;
                case GlueItemType.Undefined:

                    break;
            }
        }

        private void AddFileForComponent(ArrowElementSave element, ArrowIntentComponentSave component)
        {

        }
    }
}
