using System;
using System.Collections.Generic;
using System.IO;

namespace MacBuildTool
{
	public enum RelativeType
	{
		Relative,
		Absolute
	}

	public static class FileManager
	{

		public static List<string> GetAllFilesInDirectory(string directory)
		{
			List<string> arrayToReturn = new List<string>();
			
			if (directory.EndsWith(@"\") == false && directory.EndsWith("/") == false)
				directory += @"\";
			
			
			string[] files = System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
			
			arrayToReturn.AddRange(files);
			
			
			return arrayToReturn;
		}

		public static string MakeRelative(string pathToMakeRelative, string pathToMakeRelativeTo)
		{
			if (string.IsNullOrEmpty(pathToMakeRelative) == false)
			{
				pathToMakeRelative = FileManager.Standardize(pathToMakeRelative, null);
				pathToMakeRelativeTo = FileManager.Standardize(pathToMakeRelativeTo, null);
				if (!pathToMakeRelativeTo.EndsWith("/"))
				{
					pathToMakeRelativeTo += "/";
				}
				
				// Use the old method if we can
				if (pathToMakeRelative.ToLowerInvariant().StartsWith(pathToMakeRelativeTo.ToLowerInvariant()))
				{
					pathToMakeRelative = pathToMakeRelative.Substring(pathToMakeRelativeTo.Length);
				}
				else
				{
					// Otherwise, we have to use the new method to identify the common root
					
					// Split the path strings
					string[] path = pathToMakeRelative.ToLowerInvariant().Split('/');
					string[] relpath = pathToMakeRelativeTo.ToLowerInvariant().Split('/');
					
					string relativepath = string.Empty;
					
					// build the new path
					int start = 0;
					// November 1, 2011
					// Do we want to do this:
					// March 26, 2012
					// Yes!  Found a bug
					// while working on wahoo's
					// tools that we need to check
					// "start" against the length of
					// the string arrays.
					//while (start < path.Length && start < relpath.Length && path[start] == relpath[start])
					//while (path[start] == relpath[start])
					while (start < path.Length && start < relpath.Length && path[start] == relpath[start])
					{
						start++;
					}
					
					// If start is 0, they aren't on the same drive, so there is no way to make the path relative without it being absolute
					if (start != 0)
					{
						// add .. for every directory left in the relative path, this is the shared root
						for (int i = start; i < relpath.Length; i++)
						{
							if (relpath[i] != string.Empty)
								relativepath += @"../";
						}
						
						// if the current relative path is still empty, and there are more than one entries left in the path,
						// the file is in a subdirectory.  Start with ./
						if (relativepath == string.Empty && path.Length - start > 0)
						{
							relativepath += @"./";
						}
						
						// add the rest of the path
						for (int i = start; i < path.Length; i++)
						{
							relativepath += path[i];
							if (i < path.Length - 1) relativepath += "/";
						}
						
						pathToMakeRelative = relativepath;
					}
				}
			}
			return pathToMakeRelative;


		}


		public static bool IsRelative(string fileName)
		{
			if (fileName == null)
			{
				throw new System.ArgumentException("Cannot check if a null file name is relative.");
			}
			
			
			
#if USES_DOT_SLASH_ABOLUTE_FILES
			if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
			{
				return false;
			}
			// If it's isolated storage, then it's not relative:
			else if (fileName.Contains(IsolatedStoragePrefix))
			{
				return false;
			}
			else
			{
				return true;
			}
#else
			// a non-relative directory will have a letter than a : at the beginning.
			// for example c:/file.bmp.  If other cases arise, this may need to be changed.
			// Aha!  If it starts with \\ then it's a network file.  Thsi is absolute.
//			return !(fileName.Length > 1 && fileName[1] == ':') && fileName.StartsWith("\\\\") == false;

			return !fileName.ToLower().StartsWith("users/");

#endif
		}

		public static string RemoveDotDotSlash(string fileNameToFix)
		{
			if (fileNameToFix.Contains(".."))
			{
				// First let's get rid of any ..'s that are in the middle
				// for example:
				//
				// "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
				//
				// would become
				// 
				// "content/background/outdoorsanim/outdoorsanim.achx"
				
				int indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");
				
				bool shouldLoop = indexOfNextDotDotSlash > 0;
				
				while (shouldLoop)
				{
					int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);
					
					fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);
					
					indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");
					
					shouldLoop = indexOfNextDotDotSlash > 0;
					
					
				}
			}
			return fileNameToFix;
		}

		public static string Standardize(string fileNameToFix, string relativePath)
		{
			bool makeAbsolute = true;

			if (fileNameToFix == null)
				return null;
			
//			if (!PreserveCase)
//			{
//				fileNameToFix = fileNameToFix.ToLowerInvariant();
//			}
			
			bool isNetwork = fileNameToFix.StartsWith("\\\\");
			
//			ReplaceSlashes(ref fileNameToFix);
			
			if (makeAbsolute && !isNetwork)
			{
				// Not sure what this is all about, but everything should be standardized:
				//#if SILVERLIGHT
				//                if (IsRelative(fileNameToFix) && mRelativeDirectory.Length > 1)
				//                    fileNameToFix = mRelativeDirectory + fileNameToFix;
				
				//#else
				
				if (IsRelative(fileNameToFix))
				{
					fileNameToFix = (relativePath + fileNameToFix);
				}
				
				//#endif
			}



			
#if !XBOX360
			fileNameToFix = RemoveDotDotSlash(fileNameToFix);
			
			// 1/2/2011:
			// I'm not sure
			// why the following
			// exists.  If the above
			// fails, we need to throw
			// an exception:
			
			// Check it again because we may have gotten rid of all instances
			//if(fileNameToFix.Contains(".."))
			//{
			//    Uri myUri = new Uri(fileNameToFix );
			//    fileNameToFix = myUri.AbsolutePath;
			
			//    fileNameToFix = fileNameToFix.Replace("%20", " ");
			//}
			if (fileNameToFix.StartsWith("..") && makeAbsolute)
			{
				throw new InvalidOperationException("Tried to remove all ../ but ended up with this: " + fileNameToFix);
			}
			
#endif
			
			// It's possible that there will be double forward slashes.
			fileNameToFix = fileNameToFix.Replace("//", "/");
			
			return fileNameToFix;
		}

		
		public static string GetDirectory(string fileName, RelativeType relativeType)
		{
			int lastIndex = System.Math.Max(
				fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
			
			if (lastIndex == fileName.Length - 1)
			{
				// If this happens then fileName is actually a directory.
				// So we should return the parent directory of the argument.
				
				
				
				lastIndex = System.Math.Max(
					fileName.LastIndexOf('/', fileName.Length - 2),
					fileName.LastIndexOf('\\', fileName.Length - 2));
			}
			
			if (lastIndex != -1)
			{
				bool isFtp = false;
				
#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONOGAME
				isFtp = FtpManager.IsFtp(fileName);
#endif
				
				if (FileManager.IsUrl(fileName) || isFtp)
				{
					// don't standardize URLs - they're case sensitive!!!
					return fileName.Substring(0, lastIndex + 1);
					
				}
				else
				{
					if (relativeType == RelativeType.Absolute)
					{
						return FileManager.Standardize(fileName.Substring(0, lastIndex + 1), null);
					}
					else
					{
						return FileManager.Standardize(fileName.Substring(0, lastIndex + 1), "");
					}
				}
			}
			else
				return ""; // there was no directory found.
			
		}

		public static bool IsUrl(string fileName)
		{
			return fileName.IndexOf("http:") == 0;
		}

		public static string RemoveDirectory(string fileName)
		{
			string toReturn = fileName;

			RemovePath(ref toReturn);

			return toReturn;
		}

		public static void RemovePath(ref string fileName)
		{
			int indexOf1 = fileName.LastIndexOf('/', fileName.Length - 1, fileName.Length);
			if (indexOf1 == fileName.Length - 1 && fileName.Length > 1)
			{
				indexOf1 = fileName.LastIndexOf('/', fileName.Length - 2, fileName.Length - 1);
			}
			
			int indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 1, fileName.Length);
			if (indexOf2 == fileName.Length - 1 && fileName.Length > 1)
			{
				indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 2, fileName.Length - 1);
			}
			
			
			if (indexOf1 > indexOf2)
				fileName = fileName.Remove(0, indexOf1 + 1);
			else if (indexOf2 != -1)
				fileName = fileName.Remove(0, indexOf2 + 1);
		}
	}
}

