// ExecutableRunner.cpp : Defines the entry point for the console application.
//

#define UNICODE

#include <windows.h>
#include <stdio.h>

#include <TCHAR.h>


#include <string>
#include <mbstring.h>
//#include <shellapi.h>

#include <sys/types.h>
#include <sys/stat.h>
#include <wchar.h>
#include <errno.h>
#include <conio.h>
using namespace std;

#define PATH_SIZE 512

// Headers
void RunExecutable(_TCHAR* path, _TCHAR* program);
bool IsDotNet2Installed();
int CheckDotNetVersion(int major, int minor, int build);



int _tmain(int argc, _TCHAR* argv[])
{

    _TCHAR path[PATH_SIZE];

	// find the index of the last "\" or "/"

	int length = _tcsclen(argv[0]);
    int endOfPath = -1;

	for(int i = length-1; i > -1; i--)
	{
		if(argv[0][i] == _T('/') || argv[0][i] == _T('\\'))
		{
			endOfPath = i;
			argv[0][i] = _T('\0');
			break;
		}
	}

	_tcscpy_s(path, PATH_SIZE, argv[0]);

	_tprintf( _T("Checking for .NET 2.0\n"));
	
	if(IsDotNet2Installed() == false)
	{
		_tprintf( _T(".NET 2.0 not found.  .NET 2.0 is needed to run any FlatRedBall application.  \nPress any key to install...\n"));
		getch();
		_tprintf( _T("\nPlease Wait...\n"));
		RunExecutable(path, _T("data\\dotnetfx.exe"));
		_tcscpy_s(path, PATH_SIZE, argv[0]);

		// check to see if .NET is installed now

		// for now, give the application some time to load and sleep for a bit before showing the next message.
		Sleep(14000);
		_tprintf( _T("\nPress any key to check if .NET 2.0 has been installed correctly.\n"));
		getch();

		if(IsDotNet2Installed())
		{
			_tprintf( _T(".NET 2.0 installed successfully.\n"));
		}
		else
		{
			_tprintf( _T(".NET 2.0 installation failed.  Press any key to exit.\n"));
			getch();
			return 0;
		}
	}
	else
	{
		_tprintf( _T(".NET 2.0 already installed.\n"));
	}

	// if we get to this point, .NET is installed so run the DX installer
	_tprintf( _T("\nManaged DirectX is needed to run any FlatRedBall application.\nTo check for Managed DirectX, press any key.\n"));
	getch();
	_tprintf( _T("\nPlease Wait...\n"));

	RunExecutable(path, _T("data\\dxwebsetup.exe"));
	_tcscpy_s(path, PATH_SIZE, argv[0]);

	Sleep(14000);

	_tprintf( _T("Prerequisite installation complete.  Press any key to continue IGB3 installation.\n"));
	getch();
	RunExecutable(path, _T("data\\IGB3Installer.exe"));
	_tcscpy_s(path, PATH_SIZE, argv[0]);

	_tprintf( _T("Running IGB3Installer...\n"));

	Sleep(3000);

	return 0;

}


void RunExecutable(_TCHAR* path, _TCHAR* program)
{
	
	_tcscat(path, _T("\\"));
	_tcscat(path, program);


	ShellExecute(NULL, _T("open"), path, _T(""), _T(""), SW_SHOW);
	
}

bool IsDotNet2Installed()
{
	return CheckDotNetVersion(2, 0, 50727);
}


int CheckDotNetVersion(int major, int minor, int build)
{
   //
   //Found flag
   //
   int found = 0;
	
   
   //
   //Variables for registry functions parameter construction
   //
   _TCHAR root[1024];
#ifdef ANFP
   char bld[1024];
#endif

   //
   //Registry key
   //
   HKEY hkey;

   //
   //Registry type, value, result buffer
   //
   DWORD dwType = REG_SZ;
#ifdef ANFP
   DWORD dwSize;
   char buffer[1024];
#endif


   //
   //Registry functions parameter construction
   //
#ifdef ANFP
   wsprintf(root, "SOFTWARE\\Microsoft\\.NETFramework\\policy\\v%d.%d", major, minor);
   wsprintf(bld, "%d", build);
#else
   _stprintf(root, _T("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v%d.%d.%d"), major, minor, build);
#endif

   //
   //Open registry key
   //
   if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, root, 0, KEY_QUERY_VALUE, &hkey))
   {
#ifdef ANFP
      //
      //Read registry value
      //
      if (ERROR_SUCCESS == RegQueryValueEx(hkey, bld, NULL, &dwType, (unsigned char*) &buffer, &dwSize))
         found = TRUE;
#else
      found = TRUE;
#endif

      //
      //Close registry key
      //
      RegCloseKey(hkey);
   }


   
   return found;
}