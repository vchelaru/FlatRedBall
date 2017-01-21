using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Parsing;

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
                IncludeMember("ImplementsIVisible", typeof(EntitySave), null, base.ReadOnlyAttribute());
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity) && !instance.GetHasImplementsCollidableProperty())
            {
                ExcludeMember("ImplementsICollidable");
            }


            if (!instance.CreatedByOtherEntities)
            {
                ExcludeMember("PooledByFactory");

            }

            if (!instance.IsScrollableEntityList)
            {
                shouldIncludeItemType = false;
                ExcludeMember("VerticalOrHorizontal");
                ExcludeMember("ListTopBound");
                ExcludeMember("ListBottomBound");
                ExcludeMember("ListLeftBound");
                ExcludeMember("ListRightBound");

                ExcludeMember("SpacingBetweenItems");
            }
            else
            {
                //if (this.VerticalOrHorizontal == SaveClasses.VerticalOrHorizontal.Horizontal)
                //{
                //    pdc = PropertyDescriptorHelper.RemoveProperty(pdc, "ListTopBound");
                //    pdc = PropertyDescriptorHelper.RemoveProperty(pdc, "ListBottomBound");
                //}
                //else
                //{
                //    pdc = PropertyDescriptorHelper.RemoveProperty(pdc, "ListLeftBound");
                //    pdc = PropertyDescriptorHelper.RemoveProperty(pdc, "ListRightBound");
                //}
            }

            // We used to only support inheriting from Entities, but now we support
            // inheriting from FRB types for performance reasons.
            //IncludeMember("BaseEntity", typeof(EntitySave), new AvailableEntityTypeConverter(instance));
            var converter = new AvailableClassGenericTypeConverter();
            // Don't let it inherit from itself:
            converter.EntitiesToExclude.Add(instance);
            IncludeMember("BaseEntity", typeof(EntitySave), converter);

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
