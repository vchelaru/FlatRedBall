/*
   AngelCode Tool Box Library
   Copyright (c) 2004-2009 Andreas Jonsson
  
   This software is provided 'as-is', without any express or implied 
   warranty. In no event will the authors be held liable for any 
   damages arising from the use of this software.

   Permission is granted to anyone to use this software for any 
   purpose, including commercial applications, and to alter it and 
   redistribute it freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you 
      must not claim that you wrote the original software. If you use
      this software in a product, an acknowledgment in the product 
      documentation would be appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and 
      must not be misrepresented as being the original software.

   3. This notice may not be removed or altered from any source 
      distribution.
  
   Andreas Jonsson
   andreas@angelcode.com
*/


// 2009-03-22 Fixed crash when passing null pointer to AskForOpenFileName and AskForSaveFileName

#include "acwin_filedialog.h"

using namespace std;

namespace acWindow
{

CFileDialog::CFileDialog()
{
	filterIndex = 0;
	szFile[0]   = 0;
	numFilters  = 0;
}

void CFileDialog::AddFilter(const char *desc, const char *ext, bool isDefault)
{
	fileFilter += desc;
	fileFilter += '\0';
	fileFilter += ext;
	fileFilter += '\0';

	numFilters++;

	if( isDefault )
		filterIndex = numFilters;
}

void CFileDialog::SetDefaultFilter(int index)
{
	filterIndex = index;
}

string CFileDialog::GetFileName()
{
	return string(szFile);
}

int CFileDialog::AskForOpenFileName(CWindow *owner)
{
	OPENFILENAME ofn;

	ZeroMemory(&ofn, sizeof(OPENFILENAME));
	ofn.lStructSize     = sizeof(OPENFILENAME);
	ofn.hwndOwner       = owner ? owner->GetHandle() : 0;
	ofn.lpstrFile       = szFile;
	ofn.nMaxFile        = sizeof(szFile);
	ofn.lpstrFilter     = fileFilter.c_str();
	ofn.nFilterIndex    = filterIndex;
	ofn.lpstrFileTitle  = NULL;
	ofn.nMaxFileTitle   = 0;
	ofn.lpstrInitialDir = 0;
	ofn.Flags = OFN_FILEMUSTEXIST | OFN_NONETWORKBUTTON;
	// TODO: ofn.FlagsEx = OFN_EX_NOPLACESBAR;

	BOOL ret = GetOpenFileName(&ofn);
	if( ret )
		filterIndex = ofn.nFilterIndex;

	return ret;
}

int CFileDialog::AskForSaveFileName(CWindow *owner)
{
	OPENFILENAME ofn;

	ZeroMemory(&ofn, sizeof(OPENFILENAME));
	ofn.lStructSize		= sizeof(OPENFILENAME);
	ofn.hwndOwner		= owner ? owner->GetHandle() : 0;
	ofn.lpstrFile		= szFile;
	ofn.nMaxFile		= sizeof(szFile);
	ofn.lpstrFilter     = fileFilter.c_str();
	ofn.nFilterIndex	= filterIndex;
	ofn.lpstrFileTitle	= NULL;
	ofn.nMaxFileTitle	= 0;
	ofn.lpstrInitialDir	= 0;
	ofn.Flags = OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT | OFN_NONETWORKBUTTON | OFN_HIDEREADONLY;
	// TODO: ofn.FlagsEx = OFN_EX_NOPLACESBAR;

	// Open a SaveAs dialog to get a filename from the user
	BOOL ret = GetSaveFileName(&ofn);
	if( ret )
		filterIndex = ofn.nFilterIndex;

	return ret;
}

int CFileDialog::GetSelectedFilter()
{
	return filterIndex;
}

void CFileDialog::SetFileName(const char *name)
{
	strncpy(szFile, name, 260);
}

}
