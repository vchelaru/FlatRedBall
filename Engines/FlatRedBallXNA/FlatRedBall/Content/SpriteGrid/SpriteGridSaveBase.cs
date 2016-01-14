using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Graphics;

namespace FlatRedBall.Content.SpriteGrid
{
    #region XML Docs
    /// <summary>
    /// Base class for SpriteGridSave and SpriteGridSaveContent.
    /// </summary>
    /// <typeparam name="T">The type of the blueprint.</typeparam>
    #endregion
    public class SpriteGridSaveBase<T>
    {
        #region Fields

        public T Blueprint;

        public String BaseTexture;

        public string[][] GridTexturesArray;

        public AnimationChainGridSave AnimationChainGridSave;
        public DisplayRegionGridSave DisplayRegionGridSave;

        [XmlElementAttribute("FirstPaintedX")]
        public List<float> FirstPaintedX = new List<float>();

        public float FirstPaintedY;

        #region XML Docs
        /// <summary>
        /// Specifies the grid to use.
        /// </summary>
        /// <remarks>
        /// If the grid is 'y', use an XY grid.  Otherwise an XZ.
        /// </remarks>
        #endregion
        public char Axis;

        public float GridSpacing;

        #region XML Docs
        /// <summary>
        /// The name of the SpriteGridSave.
        /// </summary>
        #endregion
        public string Name;

        #region Bounds

        public float XRightBound;
        public float XLeftBound;

        public float YTopBound;
        public float YBottomBound;

        public float ZCloseBound;
        public float ZFarBound;

        public OrderingMode OrderingMode = OrderingMode.DistanceFromCamera;
        #endregion

        #region XML Docs
        /// <summary>
        /// This is used by the content pipeline to know which directory to look in
        /// for Textures.  Otherwise this variable is unused.
        /// </summary>
        #endregion
        [XmlIgnore]
        public string mFileName;

        public bool CreatesAutomaticallyUpdatedSprites = true;

        public bool CreatesParticleSprites = false;

        public bool DrawDefaultTile = true;

        public bool DrawableBatch = false;

        #endregion

        #region Properties

        public string FileName
        {
            get { return mFileName; }
        }

        #endregion

        #region Methods

        public SpriteGridSaveBase() 
        {
            Name = "";
        }

        #endregion
    }
}
