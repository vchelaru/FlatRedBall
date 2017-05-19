using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Content.AnimationChain
{


    #region XML Docs
    /// <summary>
    /// Base class for AnimationChainListSave and AnimationChainListSaveContent.
    /// </summary>
    #endregion
    public class AnimationChainListSaveBase<AnimationChainSaveType>
    {

        #region Fields

        [XmlIgnore]
        protected string mFileName;

        /// <summary>
        /// Whether files (usually image files) referenced by this object (and .achx) are
        /// relative to the .achx itself. If false, then file references will be stored as absolute. 
        /// If true, then file reference,s will be stored relative to the .achx itself. This value should
        /// be true so that a .achx can be moved to a different file system or computer and still
        /// have valid references.
        /// </summary>
        public bool FileRelativeTextures = true;

        public FlatRedBall.TimeMeasurementUnit TimeMeasurementUnit;
        public FlatRedBall.Graphics.TextureCoordinateType CoordinateType = Graphics.TextureCoordinateType.UV;

		[XmlElementAttribute("AnimationChain")]
		public List<AnimationChainSaveType> AnimationChains;

        #endregion


        #region Properties
        [XmlIgnore]
        public string FileName
        {
			set { mFileName = value; }
            get { return mFileName; }
        }
        #endregion
    }
}
