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

        public override void GetAllChangedMemberInstructions(List<InstructionList> list, bool createNewList)
        {
            int countBefore = list.Count;

            base.GetAllChangedMemberInstructions(list, createNewList);

            // Vic says:  The GetPaintedChanges method assumes that the SpriteGrids have the same 
            // offsets.  If not, then let's not get the painted changes

                InstructionList changes = GetPaintedChanges();

                if (changes.Count != 0)
                {
                    if (list.Count == 0 || (createNewList && countBefore == list.Count))
                    {
                        list.Add(changes);
                    }
                    else
                    {
                        list[list.Count - 1].AddRange(changes);
                    }

                }
        }

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

        private InstructionList GetPaintedChanges()
        {
            InstructionList listToReturn = new InstructionList();

            foreach (KeyValuePair<SpriteGrid, SpriteGrid> kvp in mObjectsWatching)
            {
                SpriteGrid referencedGrid = kvp.Key;
                SpriteGrid clonedGrid = kvp.Value;

                // Vic says:  If the grid offset values aren't the same, then 
                // the comparing methods will throw exceptions.  Eventually we may
                // want to handle this IF we allow the user to both paint and change
                // the position of SpriteGrids in the same frame.  This currently can't
                // be done, and it would be a pain to handle, so I'm going to take the easy
                // way out here.
                if (referencedGrid.Blueprint.X == clonedGrid.Blueprint.X &&
                    referencedGrid.Blueprint.Y == clonedGrid.Blueprint.Y &&
                    referencedGrid.Blueprint.Z == clonedGrid.Blueprint.Z)
                {
                    // Get all of the differences between the two SpriteGrids
                    List<TextureLocation<Texture2D>> textureDifferences =
                        referencedGrid.TextureGrid.GetTextureLocationDifferences(clonedGrid.TextureGrid);

                    List<TextureLocation<FloatRectangle>> textureCoordinateDifferences =
                        referencedGrid.DisplayRegionGrid.GetTextureLocationDifferences(clonedGrid.DisplayRegionGrid);

                    List<TextureLocation<AnimationChain>> animationChainDifferences =
                        referencedGrid.AnimationChainGrid.GetTextureLocationDifferences(clonedGrid.AnimationChainGrid);

                    if (textureDifferences.Count != 0 || textureCoordinateDifferences.Count != 0 || animationChainDifferences.Count != 0)
                    {
                        if (textureDifferences.Count != 0)
                        {
                            SpriteGridTexturePaintInstruction sgtpi = new SpriteGridTexturePaintInstruction(
                                referencedGrid, textureDifferences);

                            listToReturn.Add(sgtpi);
                        }
                        if (textureCoordinateDifferences.Count != 0)
                        {
                            SpriteGridDisplayRegionPaintInstruction sgdrpi = new SpriteGridDisplayRegionPaintInstruction(
                                referencedGrid, textureCoordinateDifferences);

                            listToReturn.Add(sgdrpi);
                        }

                        if (animationChainDifferences.Count != 0)
                        {
                            SpriteGridAnimationChainPaintInstruction sgacpi = new SpriteGridAnimationChainPaintInstruction(
                                referencedGrid, animationChainDifferences);

                            listToReturn.Add(sgacpi);
                        }
                    }
                }
            }

            return listToReturn;

        }
    }
}
