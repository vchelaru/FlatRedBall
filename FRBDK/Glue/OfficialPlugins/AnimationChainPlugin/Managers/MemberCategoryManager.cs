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
        #region Animation Chain

        public static void SetMemberCategories(DataUiGrid grid, AnimationChainViewModel selectedAnimationChain)
        {
            grid.Categories.Clear();

            grid.Categories.AddRange(CreateMemberCategories(selectedAnimationChain));
        }

        private static List<MemberCategory> CreateMemberCategories(AnimationChainViewModel selectedAnimationChain)
        {
            List<MemberCategory> toReturn = new List<MemberCategory>();

            var mainCategory = new MemberCategory();
            toReturn.Add(mainCategory);

            Add(nameof(AnimationChainViewModel.Name));
            Add(nameof(AnimationChainViewModel.Duration));

            return toReturn;

            void Add(string propertyName)
            {
                var member = new InstanceMember(propertyName, selectedAnimationChain);
                member.IsReadOnly = true;
                mainCategory.Members.Add(member);
            }
        }

        #endregion

        #region Animation Frame

        public static void SetMemberCategories(DataUiGrid grid, AnimationFrameViewModel selectedAnimationFrame)
        {
            List<MemberCategory> list = new List<MemberCategory>();

            var mainCategory = new MemberCategory();
            list.Add(mainCategory);


            Add(nameof(AnimationFrameViewModel.StrippedTextureName));
            Add(nameof(AnimationFrameViewModel.RelativeX));
            Add(nameof(AnimationFrameViewModel.RelativeY));
            Add(nameof(AnimationFrameViewModel.X), canWrite:true);
            Add(nameof(AnimationFrameViewModel.Y), canWrite: true);
            Add(nameof(AnimationFrameViewModel.Width), canWrite: true);
            Add(nameof(AnimationFrameViewModel.Height), canWrite: true);
            Add(nameof(AnimationFrameViewModel.LengthInSeconds), canWrite: true);
            Add(nameof(AnimationFrameViewModel.FlipHorizontal), canWrite:true);
            Add(nameof(AnimationFrameViewModel.FlipVertical), canWrite: true);

            void Add(string propertyName, bool canWrite = false)
            {
                var member = new InstanceMember(propertyName, selectedAnimationFrame);
                member.IsReadOnly = !canWrite;
                mainCategory.Members.Add(member);
            }

            grid.InsertSpacesInCamelCaseMemberNames();

            grid.Categories.Clear();
            grid.Categories.AddRange(list);

            grid.InsertSpacesInCamelCaseMemberNames();
        }

        //private static List<MemberCategory> CreateMemberCategories(AnimationFrameViewModel animationFrameViewModel)
        //{
        //    List<MemberCategory> toReturn = new List<MemberCategory>();

        //    // todo - add more here...

        //    return toReturn;
        //}

        #endregion


    }
}
