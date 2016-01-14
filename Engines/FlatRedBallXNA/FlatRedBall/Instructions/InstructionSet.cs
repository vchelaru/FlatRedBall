using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// Represents a set of KeyframeLists which can be applied to objects or
    /// used to perform scripted events.
    /// </summary>
    /// <remarks>
    /// When used to perform scripted events, InstructionSets can be compared
    /// to texture animation.  There are many levels of Lists in an InstructionSet.
    /// The following comparison to AnimationChainLists provides some clarity to the
    /// layers:
    /// <para>
    /// InstructionSet (List of KeyframeLists) : AnimationChainList (List of AnimationChains)
    /// </para>
    /// <para>
    /// KeyframeList (List of Keyframes - aka InstructionLists) : AnimationChain (List of AnimationFrames)
    /// </para>
    /// <para>
    /// InstructionList (Applied to an object to change any properties) : AnimationFrame (Applied to an object to change its appearance);
    /// </para>
    /// </remarks>
    #endregion
    public class InstructionSet : List<KeyframeList>, INameable
    {
        #region Fields
        string mName;
        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Gets a KeyframeList by name.  Returns null if none is found
        /// </summary>
        /// <param name="keyframeListName">Name of the KeyframeList to return.</param>
        /// <returns>Reference to the KeyframeList with the specified name.</returns>
        #endregion
        public KeyframeList this[string keyframeListName]
        {
            get
            {
                for (int i = this.Count - 1; i > -1; i--)
                {
                    if (this[i].Name == keyframeListName)
                    {
                        return this[i];
                    }
                }

                //nothing found, return null
                return null;
            }
        }

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        #endregion
    }
}
