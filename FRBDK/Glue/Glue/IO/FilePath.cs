using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.IO
{
    public class FilePath
    {
        #region Fields

        public string Original { get; private set; }

        #endregion

        #region Operators

        public static bool operator ==(FilePath f1, FilePath f2)
        {
            return f1?.Standardized == f2?.Standardized;
        }

        public static bool operator !=(FilePath f1, FilePath f2)
        {
            return (f1 == f2) == false;
        }

        public static implicit operator FilePath(string s)
        {
            // Code to convert the book into an XML structure
            return new FilePath(s);
        }

        #endregion

        #region Properties

        public string Extension
        {
            get
            {
                return FileManager.GetExtension(Original);
            }
        }

        public string StandardizedNoPathNoExtension
        {
            get
            {
                return FileManager.RemovePath(FileManager.RemoveExtension(Standardized));
            }
        }

        public string NoPathNoExtension
        {
            get
            {
                return FileManager.RemovePath(FileManager.RemoveExtension(FullPath));
            }
        }

        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(Original))
                {
                    return FileManager.RemoveDotDotSlash(StandardizeInternal(""));
                }
                else
                {
                    return FileManager.RemoveDotDotSlash(StandardizeInternal(Original));
                }
            }
        }

        public string Standardized
        {
            get
            {
                if (string.IsNullOrEmpty(Original))
                {
                    return FileManager.RemoveDotDotSlash(StandardizeInternal("")).ToLowerInvariant();
                }
                else
                {
                    return FileManager.RemoveDotDotSlash(StandardizeInternal(Original)).ToLowerInvariant();
                }
            }
        }

        public string StandardizedCaseSensitive
        {
            get
            {
                return FileManager.RemoveDotDotSlash(StandardizeInternal(Original));
            }
        }

        #endregion

        /// <summary>
        /// Creates a file path for the original. If this is an absolute file, then it is stored as such and the Standaridzed property will return the same absolute file. If it is lower-case, then Standardized prepends the current relative directory.
        /// </summary>
        /// <param name="path"></param>
        public FilePath(string path)
        {
            Original = path;
        }

        public override bool Equals(object obj)
        {
            if (obj is FilePath)
            {
                var path = obj as FilePath;
                return path != null &&
                       Standardized == path.Standardized;
            }
            else if (obj is string)
            {
                var path = new FilePath(obj as string);
                return path != null &&
                       Standardized == path.Standardized;
            }
            else
            {
                return false;
            }
        }

        public FilePath GetDirectoryContainingThis()
        {
            return FlatRedBall.IO.FileManager.GetDirectory(this.StandardizedCaseSensitive);
        }

        public override int GetHashCode()
        {
            var hashCode = 354063820;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Standardized);
            return hashCode;
        }

        public bool Exists()
        {
            return System.IO.File.Exists(this.StandardizedCaseSensitive);
        }

        public bool IsRootOf(FilePath otherFilePath)
        {
            return otherFilePath.Standardized.StartsWith(this.Standardized);
        }

        public FilePath RemoveExtension()
        {
            var fileString = FileManager.RemoveExtension(Original);

            return fileString;
        }

        public override string ToString()
        {
            return StandardizedCaseSensitive;
        }

        static void ReplaceSlashes(ref string stringToReplace)
        {
            bool isNetwork = false;
            if (stringToReplace.StartsWith("\\\\"))
            {
                stringToReplace = stringToReplace.Substring(2);
                isNetwork = true;
            }

            stringToReplace = stringToReplace.Replace("\\", "/");

            if (isNetwork)
            {
                stringToReplace = "\\\\" + stringToReplace;
            }
        }

        private string StandardizeInternal(string fileNameToFix)
        {
            if (fileNameToFix == null)
                return null;

            bool isNetwork = fileNameToFix.StartsWith("\\\\");

            ReplaceSlashes(ref fileNameToFix);

            if (!isNetwork)
            {
                // Not sure what this is all about, but everything should be standardized:
                //#if SILVERLIGHT
                //                if (IsRelative(fileNameToFix) && mRelativeDirectory.Length > 1)
                //                    fileNameToFix = mRelativeDirectory + fileNameToFix;

                //#else

                if (FileManager.IsRelative(fileNameToFix))
                {
                    fileNameToFix = (FileManager.RelativeDirectory + fileNameToFix);
                    ReplaceSlashes(ref fileNameToFix);
                }

                //#endif
            }

#if !XBOX360
            fileNameToFix = FileManager.RemoveDotDotSlash(fileNameToFix);

            if (fileNameToFix.StartsWith(".."))
            {
                throw new InvalidOperationException("Tried to remove all ../ but ended up with this: " + fileNameToFix);
            }

#endif
            // It's possible that there will be double forward slashes.
            fileNameToFix = fileNameToFix.Replace("//", "/");

            return fileNameToFix;
        }
    }
}
