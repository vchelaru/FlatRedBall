using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CompilerPlugin
{
    class Win32ProcessStarter
    {
        const int STARTF_USESHOWWINDOW = 1;
        const int SW_SHOWNOACTIVATE = 4;
        const int SW_SHOWMINNOACTIVE = 7;
        const int CREATE_NEW_CONSOLE = 0x00000010;

        // from https://stackoverflow.com/questions/12586957/how-do-i-open-a-process-so-that-it-doesnt-have-focus

        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        private static void StartProcessNoActivate(string cmdLine, string runInDirectory)
        {

            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.dwFlags = STARTF_USESHOWWINDOW;

            // If we use this, the game will be minimized...
            //si.wShowWindow = SW_SHOWMINNOACTIVE;
            // If we do this, it won't steal focus:
            si.wShowWindow = SW_SHOWNOACTIVATE;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero, false,
                CREATE_NEW_CONSOLE, IntPtr.Zero,
                runInDirectory, ref si, out pi);

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }

        public static void StartProcessPreventFocus(string runArguments, string exeLocation)
        {
            exeLocation = exeLocation.Replace("\\", "/");

            var lastSlash = exeLocation.LastIndexOf("/");
            var directory = exeLocation.Substring(0, lastSlash);

            if (!string.IsNullOrWhiteSpace(runArguments))
            {
                StartProcessNoActivate(exeLocation + " " + runArguments, directory);
            }
            else
            {
                StartProcessNoActivate(exeLocation, directory);
            }
        }
    }

}
