using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Content.Scene;

using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using Microsoft.Xna.Framework.Graphics;


namespace FlatRedBall.Content.SpriteGrid
{
    public class AnimationChainGridSave
    {
        #region Fields
        public ReferencedAnimationChain[][] ReferenceGrid;

        [XmlElementAttribute("FirstPaintedX")]
        public List<float> FirstPaintedX = new List<float>();

        public float FirstPaintedY;
        #endregion

        #region Methods

        public TextureGrid<FlatRedBall.Graphics.Animation.AnimationChain> ToAnimationChainGrid(string contentManagerName,
            float gridSpacing)
        {
            TextureGrid<FlatRedBall.Graphics.Animation.AnimationChain> toReturn = new TextureGrid<FlatRedBall.Graphics.Animation.AnimationChain>();

            toReturn.FirstPaintedX = FirstPaintedX;
            toReturn.LastPaintedX = new List<float>();

            toReturn.FirstPaintedY = FirstPaintedY;
            toReturn.LastPaintedY = toReturn.FirstPaintedY + (ReferenceGrid.Length - 1) * gridSpacing;

            toReturn.GridSpacingX = gridSpacing;
            toReturn.GridSpacingY = gridSpacing;

            int yOn = 0;

            toReturn.BaseTexture = null;

            Dictionary<string, FlatRedBall.Graphics.Animation.AnimationChainList> animationChainListCache =
                new Dictionary<string, FlatRedBall.Graphics.Animation.AnimationChainList>();


            foreach (ReferencedAnimationChain[] racArray in ReferenceGrid)
            {
                List<FlatRedBall.Graphics.Animation.AnimationChain> newAnimationChainList =
                    new List<FlatRedBall.Graphics.Animation.AnimationChain>();
                toReturn.Textures.Add(newAnimationChainList);

                toReturn.LastPaintedX.Add(toReturn.FirstPaintedX[yOn] + gridSpacing * (racArray.Length - 1));

                foreach (ReferencedAnimationChain rac in racArray)
                {
                    FlatRedBall.Graphics.Animation.AnimationChainList acl = null;
                    if (!string.IsNullOrEmpty(rac.AnimationChainListFileName) && animationChainListCache.ContainsKey(rac.AnimationChainListFileName) == false)
                    {
                        AnimationChainListSave acls = AnimationChainListSave.FromFile(rac.AnimationChainListFileName);
                        animationChainListCache.Add(rac.AnimationChainListFileName, 
                            acls.ToAnimationChainList(contentManagerName));
                    }

                    if (string.IsNullOrEmpty(rac.AnimationChainListFileName))
                    {
                        acl = null;
                        newAnimationChainList.Add(null);
                    }
                    else
                    {
                        acl = animationChainListCache[rac.AnimationChainListFileName];
                        newAnimationChainList.Add(acl[rac.AnimationChainName]);
                    }

                }

                yOn++;

            }


            return toReturn;

        }

        public static AnimationChainGridSave FromAnimationChainGrid(
            TextureGrid<FlatRedBall.Graphics.Animation.AnimationChain> animationChainGrid)
        {
            AnimationChainGridSave ags = new AnimationChainGridSave();

            int numberOfTextures = animationChainGrid.Textures.Count;

            ags.ReferenceGrid = new ReferencedAnimationChain[numberOfTextures][];

            ags.FirstPaintedX = animationChainGrid.FirstPaintedX;
            ags.FirstPaintedY = animationChainGrid.FirstPaintedY;

            for (int i = 0; i < animationChainGrid.Textures.Count; i++)
            {
                ags.ReferenceGrid[i] = new ReferencedAnimationChain[animationChainGrid[i].Count];

                for (int j = 0; j < animationChainGrid.Textures[i].Count; j++)
                {
                    if (animationChainGrid.Textures[i][j] != null)
                    {
                        ags.ReferenceGrid[i][j] = ReferencedAnimationChain.FromAnimationChain(
                            animationChainGrid.Textures[i][j]);
                    }
                }
            }

            return ags;

        }

        public void MakeRelative()
        {
            for (int i = 0; i < ReferenceGrid.Length; i++)
            {
                for (int j = 0; j < ReferenceGrid[i].Length; j++)
                {
                    ReferenceGrid[i][j].AnimationChainListFileName = FileManager.MakeRelative(ReferenceGrid[i][j].AnimationChainListFileName);
                }
            }

        }

        public static AnimationChainGridSave FromXElement(System.Xml.Linq.XElement element)
        {
                        
            AnimationChainGridSave acs = new AnimationChainGridSave();

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "FirstPaintedY":
                        acs.FirstPaintedY = SceneSave.AsFloat(subElement);
                        break;
                    case "ReferenceGrid":
                        throw new NotImplementedException();
                        //acs.ReferenceGrid = ToAnimationChainReferenceArrayArray(subElement);
                        break;
                    default:
                        throw new NotImplementedException("Node not understood: " + subElement.Name.LocalName);
                }
            }

            return acs;
        }

        #endregion
    }
}
