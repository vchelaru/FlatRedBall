using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.IO
{
    public class FilePath : IComparable
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
            if(s == null)
            {
                return null;
            }
            // Code to convert the book into an XML structure
            return new FilePath(s);
        }

        #endregion

        #region Properties

        string extensionCache;
        public string Extension
        {
            get
            {
                if (extensionCache == null)
                {
                    extensionCache = FileManager.GetExtension(Original);
                }
                return extensionCache;
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

        public string NoPath => FileManager.RemovePath(FullPath);

        string fullPathCache;
        public string FullPath
        {
            get
            {
                if(fullPathCache == null)
                {
                    fullPathCache = string.IsNullOrEmpty(Original)
                        ? FileManager.RemoveDotDotSlash(StandardizeInternal(""))
                        : FileManager.RemoveDotDotSlash(StandardizeInternal(Original));
                }
                return fullPathCache;
            }
        }

        string standardizedCache;
        public string Standardized
        {
            get
            {
                if(standardizedCache == null)
                {
                    standardizedCache = string.IsNullOrEmpty(Original)
                        ? FileManager.RemoveDotDotSlash(StandardizeInternal("")).ToLowerInvariant()
                        : FileManager.RemoveDotDotSlash(StandardizeInternal(Original)).ToLowerInvariant();
                }
                return standardizedCache;
            }
        }

        string standardizedCaseSensitive;
        public string StandardizedCaseSensitive
        {
            get
            {
                if(standardizedCaseSensitive == null)
                {
                    standardizedCaseSensitive = FileManager.RemoveDotDotSlash(StandardizeInternal(Original));
                }
                return standardizedCaseSensitive;
            }
        }

        #endregion

        /// <summary>
        /// Creates a file path for the original.
        /// If this is an absolute file, then it is stored as such and the Standaridzed property will return the same absolute file. 
        /// If it is relative, then Standardized prepends the current relative directory.
        /// </summary>
        /// <param name="path">the absolute or relative path</param>
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
            var standardized = this.StandardizedCaseSensitive;
            if(standardized.EndsWith("/"))
            {
                return System.IO.Directory.Exists(this.StandardizedCaseSensitive);
            }
            else
            {
                // Update - this may be a directory like "c:/SomeDirectory/" or "c:/SomeDirectory/". We don't know, so we have to check both directory and file:
                return System.IO.File.Exists(this.StandardizedCaseSensitive) ||
                    System.IO.Directory.Exists(this.StandardizedCaseSensitive);
            }
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
                if (FileManager.IsRelative(fileNameToFix))
                {
                    fileNameToFix = (FileManager.RelativeDirectory + fileNameToFix);
                    ReplaceSlashes(ref fileNameToFix);
                }
            }

            var beforeFix = fileNameToFix;
            fileNameToFix = FileManager.RemoveDotDotSlash(fileNameToFix);

            if (fileNameToFix.StartsWith(".."))
            {
                throw new InvalidOperationException($"Tried to remove all ../ from {beforeFix} but ended up with this: " + fileNameToFix);
            }

            // It's possible that there will be double forward slashes.
            fileNameToFix = fileNameToFix.Replace("//", "/");

            return fileNameToFix;
        }

        public int CompareTo(object obj)
        {
            if(obj is FilePath otherAsFilePath)
            {
                return this.FullPath.CompareTo(otherAsFilePath?.FullPath);
            }
            else if(obj is string asString)
            {
                return this?.FullPath.CompareTo(asString) ?? 0;
            }
            else
            {
                return 0;
            }
        }

        public string RelativeTo(FilePath otherFilePath)
        {
            return FileManager.MakeRelative(this.FullPath, otherFilePath.FullPath);
        }
    }
}
