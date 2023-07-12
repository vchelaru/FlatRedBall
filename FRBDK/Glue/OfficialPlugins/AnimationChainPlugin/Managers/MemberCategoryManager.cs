using GlueFormsCore.ViewModels;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal static class MemberCategoryManager
    {
        public static void SetMemberCategories(DataUiGrid grid, AnimationChainViewModel selectedAnimationChain)
        {
            grid.Categories.Clear();

            grid.Categories.AddRange(CreateMemberCategories(selectedAnimationChain));
        }

        public static void SetMemberCategories(DataUiGrid grid, AnimationFrameViewModel selectedAnimationFrame)
        {
            var propertiesToShow = new string[]
            {
                nameof(AnimationFrameViewModel.LengthInSeconds),

            };

            var category = grid.Categories[0];
            for(int i = category.Members.Count-1; i> -1; i--)
            {
                var member = category.Members[i];
                if(propertiesToShow.Contains(member.Name) == false)
                {
                    category.Members.RemoveAt(i);
                }
            }

            foreach(var member in category.Members)
            {
                // for now....
                member.IsReadOnly = true;
            }

            grid.InsertSpacesInCamelCaseMemberNames();
        }

        //private static List<MemberCategory> CreateMemberCategories(AnimationFrameViewModel animationFrameViewModel)
        //{
        //    List<MemberCategory> toReturn = new List<MemberCategory>();

        //    // todo - add more here...

        //    return toReturn;
        //}

        private static List<MemberCategory> CreateMemberCategories(AnimationChainViewModel selectedAnimationChain)
        {
            List<MemberCategory> toReturn = new List<MemberCategory>();

            var mainCategory = new MemberCategory();
            toReturn.Add(mainCategory);

            mainCategory.Members.Add(GetNameMember(selectedAnimationChain));
            mainCategory.Members.Add(GetDurationMember(selectedAnimationChain));

            return toReturn;
        }

        private static InstanceMember GetNameMember(AnimationChainViewModel selectedAnimationChain)
        {
            var nameInstanceMember = new InstanceMember("Name", selectedAnimationChain);
            nameInstanceMember.CustomGetEvent += (instance) => ((AnimationChainViewModel)instance)?.Name;
            nameInstanceMember.CustomGetTypeEvent += (instance) => typeof(string);
            //nameInstanceMember.CustomSetEvent += (instance, value) => ((AnimationChainViewModel)instance).Name = (string)value;
            return nameInstanceMember;
        }

        public static InstanceMember GetDurationMember(AnimationChainViewModel selectedAnimationChain)
        {
            var nameInstanceMember = new InstanceMember("Duration", selectedAnimationChain);
            nameInstanceMember.CustomGetEvent += (instance) => ((AnimationChainViewModel)instance)?.LengthInSeconds;
            nameInstanceMember.CustomGetTypeEvent += (instance) => typeof(float);
            //nameInstanceMember.CustomSetEvent += (instance, value) => ((AnimationChainViewModel)instance).Name = (string)value;
            return nameInstanceMember;
        }
    }
}
