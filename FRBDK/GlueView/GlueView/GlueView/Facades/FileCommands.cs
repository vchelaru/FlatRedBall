using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView.Facades
{
    public class FileCommands
    {
        public ReferencedFileSave GetReferencedFile(string fileName)
        {
            ////////////////Early Out//////////////////////////////////
            var invalidPathChars = Path.GetInvalidPathChars();
            if (invalidPathChars.Any(item => fileName.Contains(item)))
            {
                // This isn't a RFS, because it's got a bad path. Early out here so that FileManager.IsRelative doesn't throw an exception
                return null;
            }

            //////////////End Early Out////////////////////////////////


            fileName = fileName.ToLower();

            if (FileManager.IsRelative(fileName))
            {

                fileName = GetAbsoluteFileName(fileName, isContent: true).Standardized;

            }

            fileName = FileManager.Standardize(fileName).ToLower();


            if (GlueViewState.Self.CurrentGlueProject != null)
            {
                var allRfs = GlueViewState.Self.GetAllReferencedFiles();

                foreach (ReferencedFileSave rfs in allRfs)
                {
                    var absoluteRfsFile = GetAbsoluteFileName(rfs.Name);

                    if (absoluteRfsFile == fileName)
                    {
                        return rfs;
                    }
                }
            }

            return null;
        }

        public FilePath GetAbsoluteFileName(ReferencedFileSave rfs)
        {
            if(rfs == null)
            {
                throw new ArgumentNullException(nameof(rfs));
            }
            return GetAbsoluteFileName(rfs.Name, true);
        }

        public FilePath GetAbsoluteFileName(string relativePath, bool isContent = true)
        {
            if (FileManager.IsRelative(relativePath))
            {

                if (isContent)
                {
                    return GlueViewState.Self.ContentDirectory + relativePath;
                }
                else
                {
                    return GlueViewState.Self.CurrentGlueProjectDirectory + relativePath;
                }
            }

            return relativePath;
        }
    }


}
