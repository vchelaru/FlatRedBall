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


#include "acwin_dialog.h"

namespace acWindow
{

CDialog::CDialog() : CWindow()
{
}

int CDialog::Create(const char *templateName, CWindow *parent)
{
	HINSTANCE hInst = GetModuleHandle(0);

	HWND hParent = 0;
	if( parent ) hParent = parent->GetHandle();

	CWindow::HookCreate(this);
	HWND hWndNew = CreateDialog(hInst, templateName, hParent, (DLGPROC)DlgProc);
	if( hWndNew == 0 )
		return EDLG_CREATE_DIALOG_FAILED;

	return 0;
}

int CDialog::DoModal(const char *templateName, CWindow *parent)
{
	HINSTANCE hInst = GetModuleHandle(0);

	HWND hParent = 0;
	if( parent ) hParent = parent->GetHandle();

	CWindow::HookCreate(this);
	INT_PTR r = DialogBox(hInst, templateName, hParent, (DLGPROC)DlgProc);

	return int(r);
}

// static
INT_PTR CALLBACK CDialog::DlgProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	CDialog *dlg = (CDialog*)FromHandle(hWnd);
	if( dlg )
		return dlg->MsgProc(msg, wParam, lParam);

	return FALSE;
}

int CDialog::TranslateMessage(MSG *msg)
{
	if( hAccel && TranslateAccelerator(hWnd, hAccel, msg) )
		return 1;

	if( msg->message >= WM_KEYFIRST && msg->message <= WM_KEYLAST )
		if( IsDialogMessage(hWnd, msg) ) return 1;

	return 0;
}

LRESULT CDialog::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	return DefWndProc(msg, wParam, lParam);
}

// Return FALSE if the message is not handled, TRUE otherwise
LRESULT CDialog::DefWndProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	if( originalProc )
		return CallWindowProc(originalProc, hWnd, msg, wParam, lParam);

	return FALSE;
}

int CDialog::Subclass(HWND newHWnd)
{
	if( hWnd ) return EWND_ALREADY_ATTACHED;
	if( FromHandle(newHWnd) ) return EWND_OTHER_IS_ALREADY_ATTACHED;

	originalProc = (WNDPROC)SetWindowLongPtr(newHWnd, DWL_DLGPROC, (LONG_PTR)DlgProc);

	Attach(newHWnd);

	return 0;
}

void CDialog::UpdateDlgItemText(UINT itemId, const char *text)
{
	int len = GetWindowTextLength(GetDlgItem(hWnd, itemId));
	char *str = new char[len+1];
	
	GetDlgItemText(hWnd, itemId, str, len);
	if( strcmp(text, str) != 0 )
		SetDlgItemText(hWnd, itemId, text);

	delete[] str;
}

}
