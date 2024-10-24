#if false
#define SUPPORTS_LIGHTS
#endif

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Content.SpriteGrid;

using FileManager = FlatRedBall.IO.FileManager;
using FlatRedBall.Math;
using FlatRedBall.Content.Saves;
using System.ComponentModel;


#if SUPPORTS_LIGHTS
using FlatRedBall.Graphics.Lighting;
#endif


using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace FlatRedBall.Content
{
    public class SpriteEditorSceneBase<T, U, N, TextSaveType>
    {
        #region Fields

        [XmlElementAttribute("Sprite")]
        public List<T> SpriteList = new List<T>();

        /// <summary>
        /// DynamicSpriteList for compatability with FlatRedBall Managed DirectX.
        /// </summary>
        /// <remarks>
        /// The DynamicSpriteList and SpriteList will be combined into one list later
        /// in the content pipeline.
        /// </remarks>
        [XmlElementAttribute("DynamicSprite")]
        public List<T> DynamicSpriteList = new List<T>();

        public CameraSave Camera;

        [DefaultValue(false)]
        public bool Snapping;

        public float PixelSize;

        [XmlElementAttribute("SpriteGrid")]
        public List<U> SpriteGridList = new List<U>();

        [XmlElementAttribute("SpriteFrame")]
        public List<N> SpriteFrameSaveList = new List<N>();

        [XmlElementAttribute("Text")]
        public List<TextSaveType> TextSaveList = new List<TextSaveType>();

        public String SpriteEditorSceneProperties;

        public bool AssetsRelativeToSceneFile;

        [XmlIgnore]
        protected string mSceneDirectory;

        [XmlIgnore]
        protected string mFileName;

        public CoordinateSystem CoordinateSystem;

        [XmlIgnore]
        protected bool mAllowLoadingModelsFromFile = false;    

        #endregion

        #region Properties

        [XmlIgnore]
        public string FileName
        {
            get { return mFileName; }
            set { mFileName = value; }
        }

        #endregion

        #region Methods

        #region Constructor
        public SpriteEditorSceneBase()
        {
            SpriteList = new List<T>();
            DynamicSpriteList = new List<T>();

            Camera = new CameraSave();
            Camera.Z = -40 * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;

            Snapping = false;

            PixelSize = 0;

            AssetsRelativeToSceneFile = true;

            // This is so that the default is LeftHanded when loading from .scnx.
            // If this property is present in the XML this will get overwritten.
            // If the user is instantiating a SpriteEditorScene to save a .scnx from
            // FlatRedBall XNA, the Save method will overwrite this so that the coordinate
            // system is RightHanded.
            // Update Feb 15, 2012
            // People are starting to
            // work with the SpriteEditorScene
            // class in-code and this is causing
            // issues.  We need to make it always
            // use right-handed.  It's been long enough
            // since FRB XNA has been out (maybe 4 years)
            // to where we can assume all .scnx files will
            // have the CoordinateSystem property on them.
            //this.CoordinateSystem = FlatRedBall.Math.CoordinateSystem.LeftHanded;
            this.CoordinateSystem = FlatRedBall.Math.CoordinateSystem.RightHanded;

        }
        #endregion

        #endregion
    }
}
