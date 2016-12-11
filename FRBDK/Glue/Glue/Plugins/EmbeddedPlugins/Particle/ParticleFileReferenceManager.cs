using EditorObjects.Parsing;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Particle
{
    class ParticleFileReferenceManager
    {
        public void GetFilesReferencedBy(string file, EditorObjects.Parsing.TopLevelOrRecursive depth, List<string> listToFill)
        {
            if(CanFileReferenceContent(file))
            {
                listToFill.AddRange(
                    ContentParser.GetFilesReferencedByAsset(file));
            }
        }

        public bool CanFileReferenceContent(string file) => FileManager.GetExtension(file) == "emix";
    }
}
