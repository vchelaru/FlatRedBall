using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Instructions;
#if FRB_XNA
using Microsoft.Xna.Framework.Graphics;
#endif
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Animation;
using EditorObjects.Undo.Instructions;
using FlatRedBall;

namespace EditorObjects.Undo.PropertyComparers
{
    public class SpriteGridPropertyComparer : PropertyComparer<SpriteGrid>
    {
        #region Constructor

        public SpriteGridPropertyComparer()
            : base()
        {
            AddMemberWatching<float>("XRightBound");
            AddMemberWatching<float>("XLeftBound");

            AddMemberWatching<float>("YTopBound");
            AddMemberWatching<float>("YBottomBound");

            AddMemberWatching<float>("ZCloseBound");
            AddMemberWatching<float>("ZFarBound");
        }

        #endregion

        public override void UpdateWatchedObject(SpriteGrid objectToUpdate)
        {
            // Let the default property comparer handle simple
            // properties
            base.UpdateWatchedObject(objectToUpdate);

            // Now let's copy over the state of the painted 
            // grids.
            SpriteGrid clonedInstance = mObjectsWatching[objectToUpdate];

            // Let's set all of its values here
            clonedInstance.TextureGrid.SetFrom(objectToUpdate.TextureGrid);
            clonedInstance.DisplayRegionGrid.SetFrom(objectToUpdate.DisplayRegionGrid);
            clonedInstance.AnimationChainGrid.SetFrom(objectToUpdate.AnimationChainGrid);

        }

    }
}
