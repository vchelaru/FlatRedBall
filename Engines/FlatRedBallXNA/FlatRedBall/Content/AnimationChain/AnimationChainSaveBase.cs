using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Content.AnimationChain
{
    #region XML Docs
    /// <summary>
    /// The base class for AnimationChainSave and AnimationChainSaveContent.
    /// </summary>
    #endregion
#if !UWP && !WINDOWS_8
    [Serializable]
#endif
    public class AnimationChainSaveBase<AnimationFrameType>
    {
        #region Fields

        public string Name;
        public uint ColorKey;


        /// <summary>
        /// This is used if the AnimationChain actually comes from 
        /// a file like a .gif.
        /// </summary>
        public string ParentFile;

		[XmlElementAttribute("Frame")]
		public List<AnimationFrameType> Frames = new List<AnimationFrameType>();


        #endregion



    }
}
