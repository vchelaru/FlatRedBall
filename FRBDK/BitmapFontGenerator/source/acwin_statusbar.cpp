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

#include "acwin_statusbar.h"
#include <assert.h>

namespace acWindow
{

CStatusBar::CStatusBar() : CWindow()
{
	numParts = 0;
	widths   = 0;
}

CStatusBar::~CStatusBar()
{
	if( widths )
		delete[] widths;
}

int CStatusBar::Create(CWindow *parent)
{
	// Initialize the common control
	INITCOMMONCONTROLSEX icc;
	icc.dwSize = sizeof(icc);
	icc.dwICC = ICC_BAR_CLASSES;
	InitCommonControlsEx(&icc);

	HWND parentWnd = parent ? parent->GetHandle() : 0;
	HWND statusBar = CreateStatusWindow(WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_CLIPCHILDREN, "", parentWnd, 0);
	if( Subclass(statusBar) < 0 )
		return -1;

	SetStatusText("");

	return 0;
}

LRESULT CStatusBar::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	switch( msg )
	{
	case WM_DESTROY:
		DefWndProc(msg, wParam, lParam);
		Detach();
		return 0;

	case WM_SIZE:
		DefWndProc(msg, wParam, lParam);
		OnResize();
		return 0;
	}

	return DefWndProc(msg, wParam, lParam);
}

// flags 
//  0              - sunken border
//  SBT_NOBORDERS  - no border
//  SBT_OWNERDRAW  - text is drawn by parent
//  SBT_POPOUT     - raised border
//  SBT_RTLREADING - right-to-left reading
void CStatusBar::SetStatusText(const char *text, UINT index, DWORD flags)
{
	if( index > 255 ) index = 255;
	SendMessage(hWnd, SB_SETTEXT, index | flags, (LPARAM)text);
}

void CStatusBar::SetParts(UINT inNumParts, int *inWidths)
{
	numParts = inNumParts;
	if( numParts > 255 ) numParts = 255;

	if( widths ) delete[] widths;
	widths = new int[numParts];

	// Copy the widths
	memcpy(widths, inWidths, numParts*4);

	OnResize();
}

void CStatusBar::OnResize()
{
	// Calculate the size of the window so we can adjust the location of each part
	RECT rc;
	GetWindowRect(hWnd, &rc);
	int w = rc.right-rc.left;

	// Determine how many resizables parts there are, and 
	// how much space should be shared between them
	int countResizables = 0;
	int n;
	for( n = 0; n < numParts; n++ )
	{
		if( widths[n] == -1 )
			countResizables++;
		else
			w -= widths[n];
	}

	if( w < 0 ) w = 0;

	// Resize the parts
	int rights[256];
	int right = 0;
	for( n = 0; n < numParts; n++ )
	{
		if( widths[n] == -1 )
			right += w/countResizables;
		else
			right += widths[n];

		rights[n] = right;
	}

	SendMessage(hWnd, SB_SETPARTS, numParts, (LPARAM)rights);
}

}
