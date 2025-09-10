using AsepriteDotNet;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    public enum CopiedType
    {
        // these names are plural because one day
        // we will also support selecting and copying
        // multiple animations.
        AnimationChains,
        AnimationFrames,
    }
    internal class AnimationChainCopyPasteManager
    {
        public static string CopiedXml { get; set; } = string.Empty;
        public static CopiedType CopiedType { get; set; } = CopiedType.AnimationChains;


        public static void HandleCopy(ViewModels.AchxViewModel viewModel)
        {
            if (viewModel.SelectedShape != null)
            {
                // todo - handle shapes...
            }
            else if (viewModel.SelectedAnimationFrame != null)
            {
                var frameBacking = viewModel.SelectedAnimationFrame.BackingModel;
                FileManager.XmlSerialize(frameBacking, out string copiedXmlTemp);
                CopiedXml = copiedXmlTemp;
                CopiedType = CopiedType.AnimationFrames;
            }
            else if (viewModel.CurrentAnimationChain != null)
            {
                var animationChainBacking = viewModel.CurrentAnimationChain.BackingModel;
                FileManager.XmlSerialize(animationChainBacking, out string copiedXmlTemp);
                CopiedXml = copiedXmlTemp;
                CopiedType = CopiedType.AnimationChains;
            }
        }

        
    }
}
