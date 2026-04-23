using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;

namespace AnimationEditor.Core
{
    public class ObjectFinder : Singleton<ObjectFinder>
    {
        public AnimationFrameSave GetAnimationFrameContaining(AxisAlignedRectangleSave rectangle)
        {
            foreach (var chain in ProjectManager.Self.AnimationChainListSave.AnimationChains)
            {
                foreach (var frame in chain.Frames)
                {
                    if (frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Contains(rectangle))
                        return frame;
                }
            }
            return null;
        }

        public AnimationFrameSave GetAnimationFrameContaining(CircleSave circle)
        {
            foreach (var chain in ProjectManager.Self.AnimationChainListSave.AnimationChains)
            {
                foreach (var frame in chain.Frames)
                {
                    if (frame.ShapeCollectionSave.CircleSaves.Contains(circle))
                        return frame;
                }
            }
            return null;
        }

        public AnimationChainSave GetAnimationChainContaining(AnimationFrameSave frame)
        {
            foreach (var chain in ProjectManager.Self.AnimationChainListSave.AnimationChains)
            {
                foreach (var possibleFrame in chain.Frames)
                {
                    if (possibleFrame == frame)
                        return chain;
                }
            }
            return null;
        }
    }
}
