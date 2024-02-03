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

        internal static void Initialize(DataUiGrid propertyGrid)
        {
            PropertyGrid = propertyGrid;
        }

        internal static void ShowInPropertyGrid(AnimationChainViewModel selectedAnimationChain)
        {
            PropertyGrid.Instance = selectedAnimationChain;
            MemberCategoryManager.SetMemberCategories(PropertyGrid, selectedAnimationChain);
            PropertyGrid.Refresh();
        }

        internal static void ShowInPropertyGrid(AnimationFrameViewModel selectedAnimationFrame)
        {
            PropertyGrid.Instance = selectedAnimationFrame;
            MemberCategoryManager.SetMemberCategories(PropertyGrid, selectedAnimationFrame);
            PropertyGrid.Refresh();
        }

        internal static void ShowInPropertyGrid(CircleViewModel circle)
        {
            PropertyGrid.Instance = circle;
            MemberCategoryManager.SetMemberCategories(PropertyGrid, circle);
            PropertyGrid.Refresh();
        }

    }
}
