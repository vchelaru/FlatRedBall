using FlatRedBall.Graphics.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.TileGraphics
{
    public partial class LayeredTileMapAnimation
    {
        Dictionary<string, AnimationChainContainer> mAnimationChainContainers = new Dictionary<string, AnimationChainContainer>();

        public LayeredTileMapAnimation(Dictionary<string, AnimationChain> animationChainAssociations)
        {
            foreach(var kvp in animationChainAssociations)
            {
                AnimationChainContainer container = new AnimationChainContainer(kvp.Value);

                mAnimationChainContainers.Add(kvp.Key, container);

            }
        }

        public void Activity(LayeredTileMap layeredTileMap)
        {
            foreach (var kvp in mAnimationChainContainers)
            {
                AnimationChainContainer container = kvp.Value;


                int indexBefore = container.CurrentFrameIndex;
                container.Activity(TimeManager.SecondDifference);
                if (container.CurrentFrameIndex != indexBefore)
                {
                    ReactToChangedAnimationFrame(kvp.Key, kvp.Value, layeredTileMap);

                }
            }
        }


        private void ReactToChangedAnimationFrame(string spriteName, AnimationChainContainer animationChainContainer, LayeredTileMap layeredTileMap)
        {
            AnimationFrame animationFrame = animationChainContainer.CurrentFrame;
            Microsoft.Xna.Framework.Vector4 textureValues = new Microsoft.Xna.Framework.Vector4();
            foreach (var mapLayer in layeredTileMap.MapLayers)
            {
                var nameDictionary = mapLayer.NamedTileOrderedIndexes;

                if (nameDictionary.ContainsKey(spriteName))
                {
                    var indexes = nameDictionary[spriteName];

                    foreach (int value in indexes)
                    {
                        textureValues.X = animationFrame.LeftCoordinate;
                        textureValues.Y = animationFrame.RightCoordinate;
                        textureValues.Z = animationFrame.TopCoordinate;
                        textureValues.W = animationFrame.BottomCoordinate;

                        var flipFlags = mapLayer.FlipFlagArray[value];

                        if ((flipFlags & TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedHorizontallyFlag) == TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedHorizontallyFlag)
                        {
                            var temp = textureValues.Y;
                            textureValues.Y = textureValues.X;
                            textureValues.X = temp;
                        }

                        if ((flipFlags & TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedVerticallyFlag) == TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedVerticallyFlag)
                        {
                            var temp = textureValues.Z;
                            textureValues.Z = textureValues.W;
                            textureValues.W = temp;
                        }

                        mapLayer.PaintTileTextureCoordinates(value,
                            textureValues.X, textureValues.Z,
                            textureValues.Y, textureValues.W);

                        // not sure why it's done this way, copied from MapDrawableBatch...
                        if ((flipFlags & TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedDiagonallyFlag) == TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedDiagonallyFlag)
                        {
                            mapLayer.ApplyDiagonalFlip(value);
                        }
                    }
                }
            }
        }
    }
}
