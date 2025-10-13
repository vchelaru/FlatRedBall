using OfficialPlugins.AnimationChainPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    public static class PropertyGridManager
    {
        static DataUiGrid PropertyGrid;
        private static MemberCategoryManager _memberCategoryManager;

        internal static void Initialize(DataUiGrid propertyGrid, MemberCategoryManager memberCategoryManager)
        {
            PropertyGrid = propertyGrid;
            _memberCategoryManager = memberCategoryManager;
        }

        internal static void ShowInPropertyGrid(AnimationChainViewModel selectedAnimationChain, AchxViewModel allAnimations)
        {
            PropertyGrid.Instance = selectedAnimationChain;
            _memberCategoryManager.SetMemberCategories(PropertyGrid, selectedAnimationChain, allAnimations);
            PropertyGrid.Refresh();
        }

        internal static void ShowInPropertyGrid(AnimationFrameViewModel selectedAnimationFrame)
        {
            PropertyGrid.Instance = selectedAnimationFrame;
            _memberCategoryManager.SetMemberCategories(PropertyGrid, selectedAnimationFrame);
            PropertyGrid.Refresh();
        }

        internal static void ShowInPropertyGrid(CircleViewModel circle)
        {
            PropertyGrid.Instance = circle;
            _memberCategoryManager.SetMemberCategories(PropertyGrid, circle);
            PropertyGrid.Refresh();
        }

        internal static void RefreshGrid()
        {
            PropertyGrid.Refresh();
        }
    }
}
