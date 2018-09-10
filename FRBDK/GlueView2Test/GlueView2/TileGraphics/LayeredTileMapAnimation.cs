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

            foreach (var mapLayer in layeredTileMap.MapLayers)
            {
                var nameDictionary = mapLayer.NamedTileOrderedIndexes;

                if (nameDictionary.ContainsKey(spriteName))
                {
                    var indexes = nameDictionary[spriteName];

                    foreach (int value in indexes)
                    {
                        mapLayer.PaintTileTextureCoordinates(value, animationFrame.LeftCoordinate, animationFrame.TopCoordinate,
                            animationFrame.RightCoordinate, animationFrame.BottomCoordinate);
                    }
                }
            }
        }
    }
}
