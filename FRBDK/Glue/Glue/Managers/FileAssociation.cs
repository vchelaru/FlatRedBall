using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HQ.Util.Unmanaged
{

    // pulled from:
    // https://stackoverflow.com/questions/770023/how-do-i-get-file-type-information-based-on-extension-not-mime-in-c-sharp

    /// <summary>
    /// Usage:  string executablePath = FileAssociation.GetExecFileAssociatedToExtension(pathExtension, "open");
    /// </summary>
    public static class WindowsFileAssociation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="verb"></param>
        /// <returns>Return null if not found</returns>
        public static string GetExecFileAssociatedToExtension(string ext, string verb = null)
        {
            if (ext[0] != '.')
            {
                ext = "." + ext;
            }

            string executablePath = FileExtentionInfo(AssocStr.Executable, ext, verb); // Will only work for 'open' verb
            if (string.IsNullOrEmpty(executablePath))
            {
                executablePath = FileExtentionInfo(AssocStr.Command, ext, verb); // required to find command of any other verb than 'open'

                // Extract only the path
                if (!string.IsNullOrEmpty(executablePath) && executablePath.Length > 1)
                {
                    if (executablePath[0] == '"')
                    {
                        executablePath = executablePath.Split('\"')[1];
                    }
                    else if (executablePath[0] == '\'')
                    {
                        executablePath = executablePath.Split('\'')[1];
                    }
                }
            }

            // Ensure to not return the default OpenWith.exe associated executable in Windows 8 or higher
            if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath) &&
                !executablePath.ToLower().EndsWith(".dll"))
            {
                if (executablePath.ToLower().EndsWith("openwith.exe"))
                {
                    return null; // 'OpenWith.exe' is th windows 8 or higher default for unknown extensions. I don't want to have it as associted file
                }
                return executablePath;
            }
            return executablePath;
        }

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        private static string FileExtentionInfo(AssocStr assocStr, string doctype, string verb)
        {
            uint pcchOut = 0;
            AssocQueryString(AssocF.Verify, assocStr, doctype, verb, null, ref pcchOut);

            //Debug.Assert(pcchOut != 0);
            if (pcchOut == 0)
            {
                return "";
            }

            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            AssocQueryString(AssocF.Verify, assocStr, doctype, verb, pszOut, ref pcchOut);
            return pszOut.ToString();
        }

        [Flags]
        public enum AssocF
        {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }



    }
}