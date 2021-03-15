using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Content
{
    #region XML Docs
    /// <summary>
    /// Class that defines a source and destination file relationship.  This can be used
    /// by tools (such as Glue) which maintain the source/destination relationship between files.
    /// </summary>
    #endregion
    public class SourceReferencingFile
    {
        #region XML Docs
        /// <summary>
        /// The source file - the file which will be built to create the destination.
        /// </summary>
        #endregion
        public string SourceFile;

        #region XML Docs
        /// <summary>
        /// The destination file - the file which will be created when the source is built.
        /// </summary>
        #endregion
        public string DestinationFile;


        public string ObjectName;


        #region XML Docs
        /// <summary>
        /// Instantiates a new SourceReferencingFile instance.
        /// </summary>
        #endregion
        public SourceReferencingFile()
        {

        }

        #region XML Docs
        /// <summary>
        /// Instantiates a new SourceReferencingFile instance using the argument source and destination files.
        /// </summary>
        /// <param name="sourceFile">The source file name to use.</param>
        /// <param name="destinationFile">The destination file name to use.</param>
        #endregion
        public SourceReferencingFile(string sourceFile, string destinationFile)
        {
            SourceFile = sourceFile;
            DestinationFile = destinationFile;
        }

        public bool HasTheSameFilesAs(SourceReferencingFile otherReferencingFile)
        {
            return SourceFile == otherReferencingFile.SourceFile &&
                DestinationFile == otherReferencingFile.DestinationFile;
        }

        public override string ToString()
        {
            return "Source: " + SourceFile + ", Destination: " + DestinationFile;
        }


    }
}
