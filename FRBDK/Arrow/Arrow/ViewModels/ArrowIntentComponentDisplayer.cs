using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WpfDataUi.DataTypes;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowIntentComponentDisplayer
    {
        public TypeMemberDisplayProperties GetTypedMemberDisplayProperties()
        {
            TypeMemberDisplayProperties toReturn = new TypeMemberDisplayProperties();

            AddPropertyHiddenDelegate("IsFileRequirement", IsHiddenDelegate, toReturn);

            AddPropertyHiddenDelegate("RequiredExtension", IsFilePropertyHidden, toReturn);
            AddPropertyHiddenDelegate("LoadedOnlyWhenReferenced", IsFilePropertyHidden, toReturn);


            toReturn.AddIgnore("UiDisplayName");


            return toReturn;
        }


        void AddPropertyHiddenDelegate(string property, Func<InstanceMember, bool> hiddenDelegate, TypeMemberDisplayProperties allProperties)
        {
            InstanceMemberDisplayProperties imdp = new InstanceMemberDisplayProperties();
            imdp.Name = property;
            imdp.IsHiddenDelegate = hiddenDelegate;

            allProperties.DisplayProperties.Add(imdp);
        }


        bool IsHiddenDelegate(InstanceMember member)
        {
            ArrowIntentComponentVm owner = member.Instance as ArrowIntentComponentVm;

            return owner.GlueItemType != DataTypes.GlueItemType.NamedObject;


            //return false;
            //return ((InstanceMember)component).GlueItemType != DataTypes.GlueItemType.NamedObject;
        }

        bool IsFilePropertyHidden(InstanceMember member)
        {
            ArrowIntentComponentVm owner = member.Instance as ArrowIntentComponentVm;

            bool isFile = owner.GlueItemType == DataTypes.GlueItemType.File ||
                ((owner.GlueItemType == DataTypes.GlueItemType.NamedObject) && owner.IsFileRequirement == DataTypes.CharacteristicRequirement.MustBe);

            return !isFile;

        }

    }
}
