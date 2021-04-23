using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Particle
{
    class ParticleFileReferenceManager
    {
        public void GetFilesReferencedBy(string file, EditorObjects.Parsing.TopLevelOrRecursive depth, List<FilePath> listToFill)
        {
            if(CanFileReferenceContent(file))
            {
                listToFill.AddRange(
                    ContentParser.GetFilesReferencedByAsset(file));
            }
        }

        public bool CanFileReferenceContent(string file) => FileManager.GetExtension(file) == "emix";

        internal GeneralResponse FillWithReferencedFiles(FilePath file, List<FilePath> referencedFiles)
        {
            if (CanFileReferenceContent(file.FullPath))
            {
                var referencedFilesInner = ContentParser.GetFilesReferencedByAsset(file.FullPath);
                referencedFiles.AddRange(referencedFilesInner);

            }
            return GeneralResponse.SuccessfulResponse;
        }
    }
}
