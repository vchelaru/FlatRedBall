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
        public static string[] NativelyHandledExtensions = new string[]
        {
            "png"
        };
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

            var fileExists = File.Exists(executablePath);
            if(!fileExists)
                executablePath = "";

            // Ensure to not return the default OpenWith.exe associated executable in Windows 8 or higher
            if (!String.IsNullOrEmpty(executablePath) && fileExists && !executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                if (executablePath.EndsWith("openwith.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return null; // 'OpenWith.exe' is th windows 8 or higher default for unknown extensions. I don't want to have it as associted file
                }
                return executablePath;
            }
            return executablePath;
        }

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        const uint E_POINTER = 0x80004003;

        private static string FileExtentionInfo(AssocStr assocStr, string doctype, string verb)
        {
            uint pcchOut = 0;
            var response = AssocQueryString(AssocF.Verify, assocStr, doctype, verb, null, ref pcchOut);
            // not sure why, but when asking for a .png when there is no association, 2147943555 is returned...

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
            None = 0,
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
            IgnoreBaseClass = 0x200,
            Init_IgnoreUnknown = 0x400,
            Init_Fixed_ProgId = 0x800,
            Is_Protocol = 0x1000,
            Init_For_File = 0x2000
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