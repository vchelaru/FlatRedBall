/*
   AngelCode Tool Box Library
   Copyright (c) 2004-2008 Andreas Jönsson
  
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
  
   Andreas Jönsson
   andreas@angelcode.com
*/

// To use this class you must link with comctl32.lib

#ifndef ACWIN_STATUSBAR_H
#define ACWIN_STATUSBAR_H

#include "acwin_window.h"
#include <commctrl.h>
#include <string>

namespace acWindow
{

class CStatusBar : public CWindow
{
public:
	CStatusBar();
	~CStatusBar();

	int Create(CWindow *parent);

	void SetStatusText(const char *text, UINT index = 0, DWORD flags = SBT_NOBORDERS);
	void SetParts(UINT numParts, int *widths);

protected:
	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);

	void OnResize();

	int numParts;
	int *widths;
};

}

#endif