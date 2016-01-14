/*
   AngelCode Tool Box Library
   Copyright (c) 2004-2008 Andreas Jonsson
  
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

#ifndef ACWIN_LISTVIEW_H
#define ACWIN_LISTVIEW_H

#include "acwin_window.h"
#include <commctrl.h>
#include <string>

namespace acWindow
{

class CListView : public CWindow
{
public:
	CListView();

	int Create(DWORD style, DWORD exStyle, RECT *rc, CWindow *parent, UINT id);

	int InsertColumn(UINT col, const char *name, UINT width);
	int InsertItem(UINT item);
	int InsertItem(UINT item, const char *text, long param = 0);
	int DeleteItem(UINT item);

	int FindItem(int start, const char *text);
	int FindItemByParam(int start, LPARAM param);
	void EnsureVisible(UINT item);

	int GetNextItem(int start, UINT flags);
	int GetItemParam(UINT item, LPARAM *param);
	int GetItemText(UINT item, std::string *text);
	int GetItemImage(UINT item);
	void SetItemText(UINT item, const char *text);
	void SetItemState(int item, UINT mask, UINT state);
	void SetItemImage(UINT item, int image);

	void GetItemSubRect(UINT item, UINT col, RECT *rc);

	int  GetItemStateImage(UINT item);
	void SetItemStateImage(UINT item, int image);

	void PassRightButtonToParent(bool pass);

protected:
	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);

	bool doPassRightButtonToParent;
};

}

// 2008-05-09 Added FindItemByParam

#endif