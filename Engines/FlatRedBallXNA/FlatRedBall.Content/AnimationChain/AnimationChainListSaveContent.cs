using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.IO;

namespace FlatRedBall.Content.AnimationChain
{
    [XmlRoot("AnimationChainArraySave")]
    public class AnimationChainListSaveContent : AnimationChainListSaveBase<AnimationChainSaveContent>
    {

        public static AnimationChainListSaveContent FromFile(string fileName)
        {
            AnimationChainListSaveContent AnimationChainListSave =
                FileManager.XmlDeserialize<AnimationChainListSaveContent>(fileName);

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.MakeAbsolute(fileName);

            AnimationChainListSave.mFileName = fileName;

            return AnimationChainListSave;
        }
    }
}
