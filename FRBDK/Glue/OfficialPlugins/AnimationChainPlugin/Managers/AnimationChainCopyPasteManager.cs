using AsepriteDotNet;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        static string copiedXml = string.Empty;
        static CopiedType CopiedType = CopiedType.AnimationChains;


        public static void HandleCopy(ViewModels.AchxViewModel viewModel)
        {
            if (viewModel.SelectedShape != null)
            {
                // todo - handle shapes...
            }
            else if (viewModel.SelectedAnimationFrame != null)
            {
                var frameBacking = viewModel.SelectedAnimationFrame.BackingModel;
                FileManager.XmlSerialize(frameBacking, out copiedXml);
                CopiedType = CopiedType.AnimationFrames;
            }
            else if (viewModel.CurrentAnimationChain != null)
            {
                var animationChainBacking = viewModel.CurrentAnimationChain.BackingModel;
                FileManager.XmlSerialize(animationChainBacking, out copiedXml);
                CopiedType = CopiedType.AnimationChains;
            }
        }

        public static void HandlePaste(ViewModels.AchxViewModel viewModel)
        {
            /////////////early out/////////////////////
            if (string.IsNullOrEmpty(copiedXml))
            {
                return;
            }
            //////////end early out////////////////////

            switch (CopiedType)
            {
                case CopiedType.AnimationChains:
                    {
                        var deserialized = FileManager.XmlDeserializeFromString<AnimationChainSave>(copiedXml);



                    }
                    break;
                case CopiedType.AnimationFrames:
                    {
                        var chainVmToAddTo = viewModel.CurrentAnimationChain;
                        if(chainVmToAddTo != null)
                        {
                            var deserialized = FileManager.XmlDeserializeFromString<AnimationFrameSave>(copiedXml);
                            // add it to the backing model first, so that when it's added to the VM, the save picks up the add:
                            chainVmToAddTo.BackingModel.Frames.Add(deserialized);

                            var newFrame = chainVmToAddTo.AddAnimationFrame(deserialized);
                            viewModel.CurrentAnimationFrame = newFrame;
                            //chainToAddTo.
                        }
                    }

                    break;
            }

        }
    }
}
