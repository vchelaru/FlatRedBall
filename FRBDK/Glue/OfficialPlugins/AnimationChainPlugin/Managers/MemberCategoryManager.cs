using GlueFormsCore.ViewModels;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi;
using WpfDataUi.Controls;
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

        public static void SetMemberCategories(DataUiGrid grid, AnimationFrameViewModel animationFrame)
        {
            var list = new List<MemberCategory>();

            var currentCategory = new MemberCategory();
            list.Add(currentCategory);


            var member = Add(nameof(animationFrame.RelativeTextureName), canWrite:true, typeof(FileSelectionDisplay));
            member.PropertiesToSetOnDisplayer[nameof(FileSelectionDisplay.Filter)] = "PNG|*.png";
            Add(nameof(animationFrame.LengthInSeconds), canWrite: true);

            currentCategory = new MemberCategory();
            list.Add(currentCategory);
            currentCategory.Name = "Texture Coordinates";

            Add(nameof(animationFrame.X), canWrite:true);
            Add(nameof(animationFrame.Y), canWrite: true);
            Add(nameof(animationFrame.Width), canWrite: true);
            Add(nameof(animationFrame.Height), canWrite: true);

            Add(nameof(animationFrame.FlipHorizontal), canWrite:true);
            Add(nameof(animationFrame.FlipVertical), canWrite: true);

            currentCategory = new MemberCategory();
            list.Add(currentCategory);
            currentCategory.Name = "Offset";

            Add(nameof(animationFrame.RelativeX), canWrite:true);
            Add(nameof(animationFrame.RelativeY), canWrite:true);

            grid.Categories.Clear();
            grid.Categories.AddRange(list);
            grid.InsertSpacesInCamelCaseMemberNames();

            return;

            InstanceMember Add(string propertyName, bool canWrite = false, Type preferredDisplayer = null)
            {
                var member = new InstanceMember(propertyName, animationFrame);
                if(preferredDisplayer != null)
                {
                    member.PreferredDisplayer = preferredDisplayer;
                }
                member.IsReadOnly = !canWrite;
                currentCategory.Members.Add(member);
                return member;
            }
        }

        #endregion

        #region Circle

        public static void SetMemberCategories(DataUiGrid grid, CircleViewModel circle)
        {
            var list = new List<MemberCategory>();

            var mainCategory = new MemberCategory();
            list.Add(mainCategory);

            Add(nameof(circle.Name));
            Add(nameof(circle.X));
            Add(nameof(circle.Y));
            Add(nameof(circle.Radius));

            void Add(string propertyName, bool canWrite = false)
            {
                var member = new InstanceMember(propertyName, circle);
                member.IsReadOnly = !canWrite;
                mainCategory.Members.Add(member);
            }

            grid.Categories.Clear();
            grid.Categories.AddRange(list);
            grid.InsertSpacesInCamelCaseMemberNames();

        }

        #endregion
    }
}
