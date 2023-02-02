using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Content.Scene;

using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Content.SpriteGrid
{
    public class DisplayRegionGridSave
    {
        #region Fields
        public FloatRectangle?[][] ReferenceGrid;

        [XmlElementAttribute("FirstPaintedX")]
        public List<float> FirstPaintedX = new List<float>();

        public float FirstPaintedY;
        #endregion

        public TextureGrid<FloatRectangle?> ToDisplayRegionGrid(float gridSpacing)
        {
            TextureGrid<FloatRectangle?> toReturn = new TextureGrid<FloatRectangle?>();

            toReturn.FirstPaintedX = FirstPaintedX;
            toReturn.LastPaintedX = new List<float>();

            toReturn.FirstPaintedY = FirstPaintedY;
            toReturn.LastPaintedY = toReturn.FirstPaintedY + (ReferenceGrid.Length - 1) * gridSpacing;

            toReturn.GridSpacingX = gridSpacing;
            toReturn.GridSpacingY = gridSpacing;

            int yOn = 0;

            //toReturn.BaseTexture = new FloatRectangle(0,1,0,1);

            foreach (FloatRectangle?[] frArray in ReferenceGrid)
            {
                List<FloatRectangle?> newFloatRectangleList =
                    new List<FloatRectangle?>();
                toReturn.Textures.Add(newFloatRectangleList);

                toReturn.LastPaintedX.Add(toReturn.FirstPaintedX[yOn] + gridSpacing * (frArray.Length - 1));

                foreach (FloatRectangle? rectangle in frArray)
                {
                    newFloatRectangleList.Add(rectangle);
                }

                yOn++;
            }


            return toReturn;

        }

        public static DisplayRegionGridSave FromDisplayRegionGrid(
            TextureGrid<FloatRectangle?> displayRegionGrid)
        {
            DisplayRegionGridSave dgs = new DisplayRegionGridSave();

            dgs.ReferenceGrid = new FloatRectangle?[displayRegionGrid.Textures.Count][];

            dgs.FirstPaintedX = displayRegionGrid.FirstPaintedX;
            dgs.FirstPaintedY = displayRegionGrid.FirstPaintedY;

            for (int i = 0; i < displayRegionGrid.Textures.Count; i++)
            {
                dgs.ReferenceGrid[i] = new FloatRectangle?[displayRegionGrid[i].Count];

                for (int j = 0; j < displayRegionGrid.Textures[i].Count; j++)
                {
                    if (displayRegionGrid.Textures[i][j] != null)
                    {
                        dgs.ReferenceGrid[i][j] = displayRegionGrid.Textures[i][j];
                    }
                }
            }

            return dgs;

        }

#if !FRB_MDX
        internal static DisplayRegionGridSave FromXElement(System.Xml.Linq.XElement element)
        {
            DisplayRegionGridSave drgs = new DisplayRegionGridSave();

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "ReferenceGrid":
                        drgs.ReferenceGrid = ToFloatRectangleArrayArray(subElement);

                        break;
                    case "FirstPaintedX":
                        drgs.FirstPaintedX = SceneSave.AsFloatList(subElement);
                        break;
                    case "FirstPaintedY":
                        drgs.FirstPaintedY = SceneSave.AsFloat(subElement);
                        break;
                    default:
                        throw new NotImplementedException(subElement.Name.LocalName);
                        //break;
                }
            }

            return drgs;
        }

        static FloatRectangle?[][] ToFloatRectangleArrayArray(System.Xml.Linq.XElement element)
        {
            List<List<FloatRectangle?>> frReferenceListList = new List<List<FloatRectangle?>>();

            foreach (var subElement in element.Elements())
            {
                List<FloatRectangle?> newList = new List<FloatRectangle?>();

                frReferenceListList.Add(newList);
                foreach (var subSubElement in subElement.Elements())
                {
                    FloatRectangle? newRectangle = ToFloatRectangle(subSubElement);
                    newList.Add(newRectangle);
                }


            }

            FloatRectangle?[][] toReturn = new FloatRectangle?[frReferenceListList.Count][];

            for (int i = 0; i < frReferenceListList.Count; i++)
            {
                toReturn[i] = frReferenceListList[i].ToArray();

            }

            return toReturn;
        }

        static FloatRectangle ToFloatRectangle(System.Xml.Linq.XElement element)
        {
            FloatRectangle toReturn = new FloatRectangle();

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "Bottom":
                        toReturn.Bottom = SceneSave.AsFloat(subElement);
                        break;
                    case "Left":
                        toReturn.Left = SceneSave.AsFloat(subElement);
                        break;
                    case "Right":
                        toReturn.Right = SceneSave.AsFloat(subElement);
                        break;
                    case "Top":
                        toReturn.Top = SceneSave.AsFloat(subElement);
                        break;

                    default:
                        throw new NotImplementedException(subElement.Name.LocalName);
                        //break;
                }


            }


            return toReturn;

        }
#endif
    }
}
