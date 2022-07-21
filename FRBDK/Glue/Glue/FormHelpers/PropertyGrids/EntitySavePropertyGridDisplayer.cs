using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class EntitySavePropertyGridDisplayer : ElementPropertyGridDisplayer
    {
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {

                UpdateIncludedAndExcluded(value as EntitySave);
                base.Instance = value;
                SetAfterMemberChangedEvents();
            }
        }




        private void UpdateIncludedAndExcluded(EntitySave instance)
        {
            UpdateIncludedAndExcludedBase(instance);


            bool shouldIncludeItemType = true;

            if (!string.IsNullOrEmpty(instance.BaseEntity) && instance.GetInheritsFromIVisible() && 
                // If it inherits from a FRB type, it is an IVisible, but we should still show this property
                !instance.InheritsFromFrbType())
            {
                // Since the base does, the derived does automatically so always show true:
                Func<object> getMethod = () => true;
                IncludeMember(nameof(EntitySave.ImplementsIVisible), typeof(bool), 
                    memberChangeAction:null, getMember:getMethod, 
                    converter:null, attributes: base.ReadOnlyAttribute());
            }

            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.RemoveIsScrollableEntityList)
            {
                ExcludeMember(nameof(EntitySave.IsScrollableEntityList));
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity) && !instance.GetHasImplementsCollidableProperty())
            {
                ExcludeMember(nameof(EntitySave.ImplementsICollidable));
            }


            if (!instance.CreatedByOtherEntities)
            {
                ExcludeMember(nameof(EntitySave.PooledByFactory));

            }

            if (!instance.IsScrollableEntityList)
            {
                shouldIncludeItemType = false;
                ExcludeMember(nameof(EntitySave.VerticalOrHorizontal));
                ExcludeMember(nameof(EntitySave.ListTopBound));
                ExcludeMember(nameof(EntitySave.ListBottomBound));
                // These don't have properties so....do we show them? Not sure, but
                // scrollable entities are gone for GLUX/J v16
                ExcludeMember("ListLeftBound");
                ExcludeMember("ListRightBound");

                ExcludeMember(nameof(EntitySave.SpacingBetweenItems));
            }

            // We used to only support inheriting from Entities, but now we support
            // inheriting from FRB types for performance reasons.
            //IncludeMember("BaseEntity", typeof(EntitySave), new AvailableEntityTypeConverter(instance));
            var converter = new AvailableClassGenericTypeConverter();
            // Don't let it inherit from itself:
            converter.EntitiesToExclude.Add(instance);
            IncludeMember(nameof(EntitySave.BaseEntity), typeof(EntitySave), converter);

            if (shouldIncludeItemType)
            {
                IncludeMember("ItemType", typeof(EntitySave), new AvailableEntityTypeConverter(instance));
            }
            else
            {
                ExcludeMember("ItemType");
            }
            IncludeMember("Name", typeof(string), SetClassName, GetClassName, null, this.CategoryAttribute("\tEnitity"));


        }


    }
}
