using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

namespace FlatRedBall.Content.Instructions
{
    #region XML Docs
    /// <summary>
    /// Save class for a Keyframe (A list of Instructions).
    /// </summary>
    #endregion
    public class KeyframeSave
    {
        #region Fields
        public string Name;

        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

        #endregion
    }

}