#include <process.h>
#include <windows.h>
//#pragma comment(linker,"/NODEFAULTLIB")
#pragma comment(linker,"/MERGE:.rdata=.text")

// We'll use our own entry point to skip a lot of unnecessary 
// initialization in the CRT library, thus decreasing the file size
#pragma comment(linker,"/ENTRY:NewWinMain")
void NewWinMain()
{ 	
	char buffer[1024];
	STARTUPINFO si; 	
	PROCESS_INFORMATION pi;
	DWORD exitcode = 0;

	// Get the command line and substitute bmfont.com for bmfont.exe
	char stopchar = ' ';
	char *cmdline = GetCommandLine();
	char *newline = cmdline;
	if( cmdline[0] == '\"' )
		stopchar = '\"';

	do { cmdline++; } while( (cmdline[0] != stopchar) && (cmdline[0] != 0) );

	// Substitute the extension if there, otherwise add it
	if( (cmdline[-3] == 'c' || cmdline[-3] == 'C') && 
		(cmdline[-2] == 'o' || cmdline[-2] == 'O') && 
		(cmdline[-1] == 'm' || cmdline[-1] == 'M') )
	{
		cmdline[-3] = 'e';
		cmdline[-2] = 'x';
		cmdline[-1] = 'e';
	}
	else
	{
		int n;
		for( n = 0; n < int(cmdline-newline); n++ )
			buffer[n] = newline[n];

		buffer[n++] = '.';
		buffer[n++] = 'e';
		buffer[n++] = 'x';
		buffer[n++] = 'e';

		// Copy the rest of the cmdline
		while( *cmdline != 0 )
			buffer[n++] = *cmdline++;

		buffer[n] = 0;

		newline = buffer;
	}

	// Create a new process with the real BMFont app
	memset(&si, 0, sizeof(si));
	si.cb = sizeof(si);

	if( CreateProcess(NULL, newline, NULL, 
		              NULL, FALSE, 0, NULL, 
					  NULL, &si, &pi) )
	{
		// Wait for the process to terminate
		WaitForSingleObject(pi.hProcess, INFINITE);
		CloseHandle( pi.hProcess );
		CloseHandle( pi.hThread );
	}
	else  
		exitcode = GetLastError();

	// Terminate this process
	ExitProcess(exitcode);
}

