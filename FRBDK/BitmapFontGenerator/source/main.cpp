#include <windows.h>
#include <crtdbg.h>
#include <string>
#include <iostream>
using namespace std;

#include "dynamic_funcs.h"
#include "charwin.h"

const char *getArgValue(const char *cmdLine, string &value)
{
	cmdLine += strspn(cmdLine, " \t");
	int end;
	if( *cmdLine == '"' )
	{
		end = strcspn(++cmdLine, "\"");
		if( cmdLine[end] == '"' )
			value.assign(cmdLine, end);
		end++;
	}
	else
	{
		end = strcspn(cmdLine, " ");
		value.assign(cmdLine, end);
	}
	cmdLine += end;
	return cmdLine;
}

// Returns true if the GUI should be opened
bool processCmdLine(const char *cmdLine)
{
	string outputFile;
	string configFile = "bmfont.bmfc"; // Use the last configuration from the GUI as default
	string textFile;

	bool hasError = false;

	int start = strspn(cmdLine, " \t");
	if( start == 0 && *cmdLine == 0 )
	{
		// No arguments, open the GUI
		return true;
	}

	// Attach to the console and assign the streams
	AttachConsole(ATTACH_PARENT_PROCESS);
	freopen("CONOUT$","w",stdout);
	freopen("CONOUT$","w",stderr);

	cmdLine += start;
	if( *cmdLine == '-' )
	{
		while( *cmdLine == '-' )
		{
			cmdLine++;
			if( *cmdLine == 'o' )
				cmdLine = getArgValue(++cmdLine, outputFile);
			else if( *cmdLine == 'c' )
				cmdLine = getArgValue(++cmdLine, configFile);
			else if( *cmdLine == 't' )
				cmdLine = getArgValue(++cmdLine, textFile);
			else
			{
				hasError = true;
				break;
			}
	
			cmdLine += strspn(cmdLine, " \t");
		}
	}
	else 
		hasError = true;

	if( hasError || outputFile == "" )
	{
		cerr << "Incorrect arguments. See documentation for instructions." << endl;
		return false;
	}

	CFontGen *fontGen = new CFontGen();

	cout << "Loading config." << endl;
	fontGen->LoadConfiguration(configFile.c_str());

	if( textFile != "" )
	{
		cout << "Selecting characters from file." << endl;
		fontGen->SelectCharsFromFile(textFile.c_str());
	}

	cout << "Generating pages." << endl;
	fontGen->GeneratePages(false);

	cout << "Saving font." << endl;
	fontGen->SaveFont(outputFile.c_str());

	delete fontGen;
	cout << "Finished." << endl;

	return false;
}

int WINAPI WinMain(HINSTANCE hInst, 
				   HINSTANCE hPrevInst, 
				   LPSTR     cmdLine, 
				   int       showFlag)
{
	// Turn on memory leak detection
	// Find the memory leak with _CrtSetBreakAlloc(n) where n is the number reported
	_CrtSetDbgFlag(_CRTDBG_LEAK_CHECK_DF|_CRTDBG_ALLOC_MEM_DF);
	_CrtSetReportMode(_CRT_ASSERT,_CRTDBG_MODE_FILE);
	_CrtSetReportFile(_CRT_ASSERT,_CRTDBG_FILE_STDERR);

	Init();

	bool openGui = processCmdLine(cmdLine);

	if( openGui )
	{
		CCharWin *wnd = new CCharWin;
		wnd->Create(512, 512);
		while( !acWindow::CWindow::CheckMessages(true) ) {}
		delete wnd;
	}

	Uninit();

	return 0;
}
