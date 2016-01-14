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


#include "acwin_window.h"
#include <winuser.h>
#include <assert.h>
#include <map>

namespace acWindow
{

//=============================================================================

// The association between window handle and window object is stored in this 
// map structure. It could have been stored in for example the GWLP_USERDATA
// attribute of the window (in fact I used to do that before), but in some
// situations this attribute is already used for something else. For example
// Input Method Editors installed on some international versions of Windows, 
// i.e. Japanese and Chinese. Because of that care must be taken when using
// the GWLP_USERDATA to make sure the value retrieved is really what is 
// expected. I thought it was easier to switch to a map structure instead of
// implementing these validations, and the overhead with the map lookup should
// be insignificant unless very many windows are in use.

typedef std::map<HWND, CWindow*> HTOMap;
static HTOMap g_handleToObject;

//==============================================================================

CWindow::CWindow()
{
	hWnd   = 0;
	hAccel = 0;
	hMenu  = 0;

	originalProc = 0;
}

CWindow::~CWindow()
{
	if( hWnd && IsWindow(hWnd) )
		DestroyWindow(hWnd);

	if( hMenu ) 
		DestroyMenu(hMenu);
}

//=============================================================================

// static
CWindow *CWindow::FromHandle(HWND hWnd)
{
	CWindow *wnd = 0;

	// wnd = (CWindow *)GetWindowLongPtr(hWnd, GWLP_USERDATA);

	HTOMap::iterator i = g_handleToObject.find(hWnd);
	if( i != g_handleToObject.end() )
		wnd = i->second;

	assert( !wnd || wnd->GetHandle() == hWnd );

	return wnd;
}

//=============================================================================

int CWindow::Create(const char *title, int width, int height, DWORD style, DWORD styleEx, CWindow *parent, const char *className)
{
	if( className == 0 )
	{
		// Register a default window class
		int r = RegisterClass("Basic Window", 0, AC_REGDEFBRUSH, AC_REGDEFICON, AC_REGDEFICON, AC_REGDEFCURSOR);
		if( FAILED(r) )
			return r;

		className = "Basic Window";
	}

	HWND hParent = 0;
	if( parent ) hParent = parent->hWnd;

	HINSTANCE hInst = GetModuleHandle(0);

    // Create the window
	// We set up a hook procedure to connect our class instance with the window handle
	HookCreate(this);
    HWND hWndNew = CreateWindowEx( styleEx, className, 
		                           title, style, CW_USEDEFAULT, CW_USEDEFAULT,
						           width,height,hParent,NULL,hInst,0 ); 
	if( hWndNew == NULL )
		return EWND_CREATE_WINDOW_FAILED;
	
	assert( hWnd == hWndNew );

	return 0;
}

//=============================================================================

int CWindow::SetAccelerator(const char *accel)
{
	// Get the module handle
	HINSTANCE hInst = GetModuleHandle(0);
	
	// Load the accelerator table
	if( accel != 0 )
	{
		hAccel = LoadAccelerators(hInst, accel);
		if( hAccel == 0 )
			return EWND_LOAD_RESOURCE_FAILED;
	}
	else
		hAccel = 0;

	// Note that accelerator tables loaded from resources
	// are automatically freed at termination

	return 0;
}

int CWindow::SetAccelerator(ACCEL *accels, int numItems)
{
	if( hAccel )
	{
		DestroyAcceleratorTable(hAccel);
		hAccel = 0;
	}

	if( accels && numItems )
	{
		hAccel = CreateAcceleratorTable(accels, numItems);
		if( !hAccel )
			return -1;
	}

	return 0;
}

//=============================================================================

int CWindow::SetMenu(const char *menu)
{
	// Get the module handle
	HINSTANCE hInst = GetModuleHandle(0);

	// Destroy old menu
	if( hMenu != 0 )
	{
		::SetMenu(hWnd, 0);
		DestroyMenu(hMenu);
	}
	hMenu = 0;

	// Load the menu
	if( menu != 0 )
	{
		hMenu = LoadMenu(hInst, menu);
		if( hMenu == 0 )
			return EWND_LOAD_RESOURCE_FAILED;

		::SetMenu(hWnd, hMenu);
	}

	return 0;
}

//=============================================================================

// static
int CWindow::RegisterClass(const char *className, UINT style, HBRUSH newBgBrush, HICON newIcon, HICON newSmallIcon,
						   HCURSOR newCursor)
{
	// Get the module handle
	HINSTANCE hInst = GetModuleHandle(0);

	// See if the class has been registered before
	WNDCLASSEX wndClass;
	if( GetClassInfoEx(hInst, className, &wndClass) )
		return 0;

	// Register the new class
	HBRUSH  hBgBrush   = newBgBrush;
	HICON   hIcon      = newIcon;
	HCURSOR hCursor    = newCursor;
	HICON   hIconSmall = newSmallIcon;

	if( size_t(hIcon)      == -1 ) hIcon      = LoadIcon(NULL, IDI_APPLICATION);
	if( size_t(hIconSmall) == -1 ) hIconSmall = 0;
	if( size_t(hCursor)    == -1 ) hCursor    = LoadCursor(NULL, IDC_ARROW);
	if( size_t(hBgBrush)   == -1 ) hBgBrush   = (HBRUSH)::GetStockObject(BLACK_BRUSH);

	// Register the window class
	wndClass.cbSize        = sizeof(WNDCLASSEX);
	wndClass.style         = style;
	wndClass.lpfnWndProc   = (WNDPROC)WndProc;
	wndClass.cbClsExtra    = 0;
	wndClass.cbWndExtra    = 0;
	wndClass.hInstance     = hInst;
	wndClass.hIcon         = hIcon;
	wndClass.hCursor       = hCursor;
	wndClass.hbrBackground = hBgBrush;
	wndClass.lpszMenuName  = 0;
	wndClass.lpszClassName = className;
	wndClass.hIconSm       = hIconSmall;

	ATOM windowClass = RegisterClassEx( &wndClass );
	if( !windowClass )
		return EWND_REGISTER_CLASS_FAILED;

	return 0;
}

//=============================================================================

int CWindow::Attach(HWND newHWnd)
{
	if( hWnd ) return EWND_ALREADY_ATTACHED;
	if( FromHandle(newHWnd) ) return EWND_OTHER_IS_ALREADY_ATTACHED;

	hWnd = newHWnd;
	// SetWindowLongPtr(hWnd, GWLP_USERDATA, (LONG_PTR)this);
	g_handleToObject.insert(HTOMap::value_type(hWnd, this));

	return 0;
}

int CWindow::Subclass(HWND newHWnd)
{
	if( hWnd ) return EWND_ALREADY_ATTACHED;
	if( FromHandle(newHWnd) ) return EWND_OTHER_IS_ALREADY_ATTACHED;

	originalProc = (WNDPROC)SetWindowLongPtr(newHWnd, GWLP_WNDPROC, (LONG_PTR)WndProc);

	Attach(newHWnd);

	return 0;
}

int CWindow::Detach()
{
	if( hWnd == 0 ) return EWND_NOT_ATTACHED;

	// SetWindowLong(hWnd, GWLP_USERDATA, 0);
	g_handleToObject.erase(hWnd);

	if( originalProc ) 
		SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)originalProc);

	hWnd = 0;

	return 0;
}

//=============================================================================

// static
// If wait is true then the function will wait until a message arrives.
// The function returns true if it receives WM_QUIT.
bool CWindow::CheckMessages(bool wait)
{
	if( wait )
		WaitMessage();

    MSG msg;
	while( PeekMessage(&msg, NULL, 0, 0, PM_REMOVE) )
    {
		if( msg.message == WM_QUIT )
			return true;

		// Let window and parents translate the message
		HWND hWnd = msg.hwnd;
		int isTranslated = 0;
		while( hWnd )
		{
			CWindow *wnd = (CWindow *)FromHandle(hWnd);
			if( wnd )
			{
				if( isTranslated = wnd->TranslateMessage(&msg) ) 
					break;
			}

			hWnd = GetParent(hWnd);
		}

		if( isTranslated != 1 )
		{
			// Translate key messages to character messages
			::TranslateMessage(&msg);

			// Send the messages to the window procedure
			DispatchMessage(&msg);
		}
	}

    return false;
}

//=============================================================================

int CWindow::TranslateMessage(MSG *msg)
{
	if( hAccel && TranslateAccelerator(hWnd, hAccel, msg) )
		return 1;

	// Return -1 if you do not wish to allow parent window to translate the message

	return 0;
}

//=============================================================================

HWND CWindow::GetHandle()
{
	return hWnd;
}

void CWindow::Invalidate(BOOL erase)
{
	InvalidateRect(hWnd, 0, erase);
}

//=============================================================================

LRESULT CWindow::DefWndProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	if( originalProc )
		return CallWindowProc(originalProc, hWnd, msg, wParam, lParam);

	return DefWindowProc(hWnd, msg, wParam, lParam);
}

LRESULT CWindow::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	// Let the default window procedure handle 
	// any message that we don't care about
	return DefWndProc(msg, wParam, lParam);
}

// static
// Message handler which passes messages to the application class.
LRESULT CALLBACK CWindow::WndProc(HWND hWnd, UINT msg, 
							      WPARAM wParam, LPARAM lParam)
{
	CWindow *wnd;

	// Extract the object from the window handle.
	wnd = (CWindow *)FromHandle(hWnd);
	if( wnd )
	{
		// Let the object handle it.
		return wnd->MsgProc(msg, wParam, lParam);
	}

	// If no class instance is found we call the default procedure
	return DefWindowProc(hWnd, msg, wParam, lParam);
}

//=============================================================================

// TODO: Make thread safe
HHOOK    CWindow::hCreateHook = 0;
CWindow *CWindow::wndCreator  = 0;

// static
int CWindow::HookCreate(CWindow *wnd)
{
	if( wnd != 0 )
	{
		wndCreator  = wnd;
		hCreateHook = SetWindowsHookEx(WH_CBT, CreateProc, 0, GetCurrentThreadId());
		if( hCreateHook == 0 )
		{
			// Failed, use GetLastError() to know what happened
			return EWND_HOOK_FAILED;
		}
	}
	else
	{
		if( hCreateHook && !UnhookWindowsHookEx(hCreateHook) )
		{
			// Failed, use GetLastError() to know what happened
			return EWND_HOOK_FAILED;
		}
		hCreateHook = 0;
		wndCreator  = 0;
	}

	return 0;
}

// static
LRESULT CALLBACK CWindow::CreateProc(int nCode, WPARAM wParam, LPARAM lParam)
{
	if( nCode == HCBT_CREATEWND )
	{
		// Connect the new window with the class instance
		HWND hWnd = (HWND)wParam;
		CBT_CREATEWND *cw = (CBT_CREATEWND*)lParam;

		if( wndCreator )
			wndCreator->Attach(hWnd);

		// Unregister the hook procedure here
		HookCreate(0);

		// Return 0 to allow continued creation
		return 0;
	}

	return CallNextHookEx(hCreateHook, nCode, wParam, lParam);
}

//=============================================================================

// WM_DRAWITEM is sent to the parent for owner drawn 
// controls and the parent calls this method on the child.
// Return true, if the message is handled
BOOL CWindow::DrawItem(DRAWITEMSTRUCT *di)
{
	return FALSE;
}

//=============================================================================

bool CWindow::IsVisible()
{
	LONG style = GetWindowLong(hWnd, GWL_STYLE);
	if( style & WS_VISIBLE )
		return true;

	return false;
}

//=============================================================================

// This function avoids flickering by only updating if the text is different
void CWindow::UpdateWindowText(const char *text)
{
	int len = GetWindowTextLength(hWnd);
	char *str = new char[len+1];
	
	GetWindowText(hWnd, str, len);
	if( strcmp(text, str) != 0 )
		SetWindowText(hWnd, text);

	delete[] str;
}

// This function allows you to hide the system menu button. This makes the window
// appear like a dialog box without the system menu button but still with the close
// button. The system menu can still be opened by right clicking the caption bar, 
// just like for dialogs.
void CWindow::HideSystemMenuButton()
{
	// Make sure the window uses the WS_EX_DLGMODALFRAME style, otherwise it won't work
	SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_DLGMODALFRAME);

	// Remove the icon handles to hide the menu button
	SetClassLong(hWnd, GCL_HICON, NULL);
	SetClassLong(hWnd, GCL_HICONSM, NULL);

	// Update the style cache so that the window can be redrawn correctly
	SetWindowPos(hWnd, 0, 0, 0, 0, 0,  SWP_FRAMECHANGED|SWP_NOSIZE|SWP_NOMOVE);	
}

}
