using System;
using Microsoft.Xna.Framework.Content.Pipeline;
using FlatRedBall.IO;
using System.IO;

namespace FlatRedBall.Content.AnimationChain
{
    [ContentImporter(".achx",
        DisplayName="AnimationChain - FlatRedBall",
        DefaultProcessor="AnimationChainArrayProcessor")]
    public class AnimationChainArrayImporter : ContentImporter<AnimationChainListSaveContent>
    {
        public override AnimationChainListSaveContent Import(string filename, ContentImporterContext context)
        {
            AnimationChainListSaveContent ach = AnimationChainListSaveContent.FromFile(filename);
            return ach;
            
        }
    }
}
