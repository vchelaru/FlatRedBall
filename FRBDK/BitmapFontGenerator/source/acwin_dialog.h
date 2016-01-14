/*
   AngelCode Tool Box Library
   Copyright (c) 2004-2007 Andreas Jönsson
  
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


#ifndef ACWIN_DIALOG_H
#define ACWIN_DIALOG_H

#include "acwin_window.h"

namespace acWindow
{
#define EDLG_CREATE_DIALOG_FAILED -1

class CDialog : public CWindow
{
public:
	CDialog();

	int Create(const char *templateName, CWindow *parent);
	int DoModal(const char *templateName, CWindow *parent);

	int Subclass(HWND hWnd);

protected:
	LRESULT DefWndProc(UINT msg, WPARAM wParam, LPARAM lParam);
	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);
	int TranslateMessage(MSG *msg);
	void UpdateDlgItemText(UINT itemId, const char *text);

private:
	static INT_PTR CALLBACK DlgProc(HWND, UINT, WPARAM, LPARAM);
};

}

#endif