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

        // By default we want this to be true for portability
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
