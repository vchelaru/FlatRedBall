using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.AnimationChain;

namespace EditorObjects.Cleaners
{
    public class AchxCleaner
    {
        public static AnimationChainSave AnimationChainSave = new AnimationChainSave();
        public static AnimationFrameSave AnimationFrameSave = new AnimationFrameSave();

        public static Type AnimationChainSaveType = typeof(AnimationChainSave);
        public static Type AnimationFrameSaveType = typeof(AnimationFrameSave);
    }
}
