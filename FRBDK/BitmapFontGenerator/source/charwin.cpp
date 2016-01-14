#include <math.h>
#include <windows.h>
#include <wingdi.h>
#include <assert.h>
#include <sstream>
#include <Usp10.h>

#include "dynamic_funcs.h"
#include "charwin.h"
#include "choosefont.h"
#include "exportdlg.h"
#include "imagewnd.h"
#include "resource.h"
#include "about.h"
#include "ac_string_util.h"
#include "acwin_filedialog.h"
#include "imagemgr.h"
#include "acutil_unicode.h"
#include "unicode.h"

using namespace std;
using namespace acWindow;

#define LISTVIEW_WIDTH 250


CCharWin::CCharWin() : CWindow()
{
	fontGen       = 0;

	wnd       = 0;
	listView  = 0;
	statusBar = 0;
	imageMgr  = 0;

	isGenerating           = false;
	whenGenerateIsFinished = 0;
	unicodeSubset          = 0;
}

CCharWin::~CCharWin()
{
	if( imageMgr )
	{
		delete imageMgr;
		imageMgr = 0;
	}

	if( wnd )
	{
		delete wnd;
		wnd = 0;
	}

	if( listView )
	{
		delete listView;
		listView = 0;
	}

	if( statusBar )
	{
		delete statusBar;
		statusBar = 0;
	}

	if( fontGen )
	{
		fontGen->Abort();
		delete fontGen;
	}
}

int CCharWin::GetCharFromPos(int posX, int posY)
{
	int ch = -1;

	RECT rc;
	GetCharGridRect(&rc);

	if( posX < 0 || posX >= rc.right ||
		posY < 0 || posY >= rc.bottom )
		return -1;

	int x = 16*posX/rc.right; 
	int y = 16*posY/rc.bottom;

	if( fontGen->IsUsingUnicode() )
	{
		ch = fontGen->GetUnicodeSubset(unicodeSubset)->charBegin + y*16 + x;
		if( ch > fontGen->GetUnicodeSubset(unicodeSubset)->charEnd )
			return -1;
	}
	else
		ch = y*16 + x;

	return ch;
}

void CCharWin::OnTimer()
{
	// Update the status bar
	UpdateStatus();

	if( listView )
	{
		// Update the check box for the currently viewed subset.
		// This is for when the user selects individual characters in the subset and eventually changes,
		// the state for the unicode subset (nothing selected, partially selected, fully selected).
		int item = listView->FindItemByParam(-1, unicodeSubset);
		if( item >= 0 )
		{
			// Is subset selected?
			int check = fontGen->IsSubsetSelected(unicodeSubset)+1;
			if( listView->GetItemStateImage(item) != check )
				listView->SetItemStateImage(item, check);
		}
	}

	if( isGenerating )
	{
		// Poll the status of the generation, so we can continue the action after it has completed
		if( fontGen->GetStatus() == 0 )
		{
			isGenerating = false;
			SetCursor(LoadCursor(0, IDC_ARROW));
			if( whenGenerateIsFinished == 1 )
			{
				// Open the visualization window
				VisualizeAfterFinishedGenerating();
			}
			else if( whenGenerateIsFinished == 2 )
			{
				// Save the font
				SaveFontAfterFinishedGenerating();
			}
		}
	}
}

void CCharWin::UpdateStatus()
{
	if( statusBar )
	{
		string str;
		if( fontGen->GetStatus() == 0 )
			str = acStringFormat("selected characters: %d/%d", fontGen->GetNumCharsSelected(), fontGen->GetNumCharsAvailable()); 
		else if( fontGen->GetStatus() == 1 )
			str = acStringFormat("generating characters %d/%d", fontGen->GetStatusCounter(), fontGen->GetNumCharsSelected());
		else if( fontGen->GetStatus() == 2 )
			str = acStringFormat("adding characters to texture(s) %d/%d", fontGen->GetStatusCounter(), fontGen->GetNumCharsSelected());
		statusBar->SetStatusText(str.c_str());

		// Display the current charset
		if( fontGen->IsUsingUnicode() )
			str = fontGen->GetUnicodeSubset(unicodeSubset)->name;
		else
			str = "ASCII";
		statusBar->SetStatusText(str.c_str(), 1, 0);
	}
}

// This is called when a global action takes place, e.g.
// change of font, clear all option, or select chars from file.
void CCharWin::UpdateSubsetsSelection()
{
	if( listView )
	{
		int item = -1;
		while( (item = listView->GetNextItem(item, LVNI_ALL)) >= 0 )
		{
			// Is subset selected?
			int subset;
			listView->GetItemParam(item, (LPARAM*)&subset);

			int check = fontGen->IsSubsetSelected(subset)+1;
			if( listView->GetItemStateImage(item) != check )
				listView->SetItemStateImage(item, check);
		}
	}
}

string CCharWin::GetDefaultConfig()
{
	char buffer[256];
	GetModuleFileName(0, buffer, 255);

	string file = buffer;
	int n = file.rfind("\\");
	file = file.substr(0, n);
	file += "\\bmfont.bmfc";

	return file;
}

int CCharWin::Create(int width, int height)
{
	fontGen = new CFontGen();

	HICON hIcon = (HICON)LoadImage(GetModuleHandle(0), MAKEINTRESOURCE(IDI_ICON1), IMAGE_ICON, LR_DEFAULTSIZE, LR_DEFAULTSIZE, LR_SHARED);
	HICON hIconSmall = (HICON)LoadImage(GetModuleHandle(0), MAKEINTRESOURCE(IDI_ICON1), IMAGE_ICON, 16, 16, LR_SHARED);

	int r = RegisterClass("CharWnd", 0, 0, hIcon, hIconSmall, AC_REGDEFCURSOR);
	if( r < 0 ) return r;

	r = CWindow::Create("Bitmap font generator", width + LISTVIEW_WIDTH, height, WS_OVERLAPPEDWINDOW | WS_VISIBLE, 0, 0, "CharWnd");
	if( r < 0 ) return r;

	SetMenu(MAKEINTRESOURCE(IDR_MENU));

	SetAccelerator(MAKEINTRESOURCE(IDR_ACCELERATOR));

	statusBar = new CStatusBar();
	statusBar->Create(this);
	int widths[] = {-1, 200, 120};
	statusBar->SetParts(3, widths);

	fontGen->LoadConfiguration(GetDefaultConfig().c_str());

	PrepareView();

	SetTimer(hWnd, 0, 100, 0);

	return 0;
}

void CCharWin::PrepareView()
{
	fontGen->Prepare();

	if( fontGen->IsUsingUnicode() )
    {
		// Create the right hand list view with the unicode 
		if( listView == 0 )
		{
			listView = new CListView();

			RECT rc;
			GetClientRect(hWnd, &rc);

			DWORD style = WS_CHILD | LVS_REPORT | WS_VISIBLE | 
						LVS_SHOWSELALWAYS | WS_CLIPSIBLINGS | LVS_NOCOLUMNHEADER;
			DWORD exStyle = LVS_EX_GRIDLINES | LVS_EX_FULLROWSELECT | LVS_EX_CHECKBOXES;

			rc.left = rc.right - LISTVIEW_WIDTH;

			listView->Create(style, exStyle, &rc, this, 0);
			listView->PassRightButtonToParent(true);

			// Make the list view use three-state checkboxes
			HIMAGELIST imageList = ImageList_LoadImage(GetModuleHandle(0), MAKEINTRESOURCE(IDB_STATEIMAGES), 16, 3, CLR_DEFAULT, IMAGE_BITMAP, LR_CREATEDIBSECTION);
			ListView_SetImageList(listView->GetHandle(), imageList, LVSIL_STATE);

			listView->InsertColumn(0, "Subset", LISTVIEW_WIDTH);
		}

		// Synchronize the list view items with the available unicode subsets
		unsigned numSets = fontGen->GetNumUnicodeSubsets();
		int lastInLV = -1;
		int lvi = -1;
		for(;;)
		{
			lvi = listView->GetNextItem(lvi, 0);
			if( lvi == -1 )
			{
				// There are no more items in the list view
				// Insert all the remaining unicode subsets
				for( lastInLV++; lastInLV < (signed)numSets; lastInLV++ )
				{
					const SSubset *set = fontGen->GetUnicodeSubset(lastInLV);
					if( set->available )
					{
						string name = acStringFormat("%0.6X  %s", set->charBegin, set->name.c_str());
						listView->InsertItem(numSets, name.c_str(), lastInLV);
					}
				}

				// We're finished
				break;
			}
			else
			{
				// Add all subsets up to the current
				int subset;
				listView->GetItemParam(lvi, (LPARAM*)&subset);

				for( lastInLV++; lastInLV < subset; lastInLV++ )
				{
					const SSubset *set = fontGen->GetUnicodeSubset(lastInLV);
					if( set->available )
					{
						string name = acStringFormat("%0.6X  %s", set->charBegin, set->name.c_str());
						listView->InsertItem(lvi++, name.c_str(), lastInLV);
					}
				}

				// Should the current item be removed?
				if( !fontGen->GetUnicodeSubset(subset)->available )
					listView->DeleteItem(lvi--);
			}
		}

		UpdateSubsetsSelection();
	}
	else
	{
		if( listView )
		{
			DestroyWindow(listView->GetHandle());
			delete listView;
			listView = 0;
		}

		unicodeSubset = 0;
	}
}



void CCharWin::Draw()
{
	PAINTSTRUCT ps;
	HDC dc = BeginPaint(hWnd, &ps);

	RECT rc;
	GetCharGridRect(&rc);

	HBRUSH br = (HBRUSH)GetStockObject(LTGRAY_BRUSH);
	FillRect(dc, &rc, br);

	HPEN pen = CreatePen(PS_SOLID, 1, RGB(255,255,255));
	HPEN oldPen = (HPEN)SelectObject(dc, pen);

	for( int n = 1; n < 16; n++ )
	{
		int x = n*rc.right/16;
		MoveToEx(dc, x, 0, 0);
		LineTo(dc, x, rc.bottom);

		int y = n*rc.bottom/16;
		MoveToEx(dc, 0, y, 0);
		LineTo(dc, rc.right, y);
	}

	HFONT font = fontGen->CreateFont(rc.bottom/16-4);
	HFONT oldFont = (HFONT)SelectObject(dc, font);
	SetTextColor(dc, RGB(255,255,255));
	SetBkColor(dc, RGB(0,0,0));
	SetBkMode(dc, TRANSPARENT);
	TEXTMETRIC tm;
	GetTextMetrics(dc, &tm);

	HPEN red = CreatePen(PS_SOLID, 3, RGB(255, 0, 0));
	SelectObject(dc, red);

	if( fontGen->IsUsingUnicode() )
		DrawUnicode(dc, rc, tm);
	else
		DrawAnsi(dc, rc, tm);

	SelectObject(dc, oldFont);
	DeleteObject(font);

	SelectObject(dc, oldPen);
	DeleteObject(red);
	DeleteObject(pen);

	EndPaint(hWnd, &ps);
}

void CCharWin::DrawUnicode(HDC dc, RECT &rc, TEXTMETRIC &tm)
{
	const SSubset *subset = fontGen->GetUnicodeSubset(unicodeSubset);
	int offset = subset->charBegin;

	SCRIPT_CACHE sc = 0;

	for( int y = 0; y < 16; y++ )
	{
		for( int x = 0; x < 16; x++ )
		{
			int idx = y*16+x + offset;
			
			if( idx <= subset->charEnd )
			{
				int cy = (rc.bottom/16 - tm.tmHeight)/2 + 1;

				// Determine X position
				ABC abc;
				int cx = rc.right/16/2;
				if( GetUnicodeCharABCWidths(dc, &sc, idx, &abc) >= 0 )
					cx -= abc.abcB/2 + abc.abcA;

				if( fontGen->IsSelected(idx) )
					SetTextColor(dc, RGB(0,0,0));
				else
					SetTextColor(dc, RGB(64,64,64));

				if( fontGen->IsDisabled(idx) || 
					!fontGen->IsSelected(idx) )
				{
					RECT box;
					box.left = x*rc.right/16 + 1;
					box.right = (x+1)*rc.right/16;
					box.top = y*rc.bottom/16 + 1;
					box.bottom = (y+1)*rc.bottom/16;
					if( fontGen->IsDisabled(idx) )
					{
						HBRUSH br = CreateHatchBrush(HS_DIAGCROSS, RGB(128,128,128));
						FillRect(dc, &box, br);
						DeleteObject(br);
					}
					else
						FillRect(dc, &box, (HBRUSH)GetStockObject(GRAY_BRUSH));
				}

				if( fontGen->DidNotFit(idx) )
				{
					MoveToEx(dc, x*rc.right/16 + 1, y*rc.bottom/16 + 1, 0);
					LineTo(dc, (x+1)*rc.right/16 - 1, y*rc.bottom/16 + 1);
					LineTo(dc, (x+1)*rc.right/16 - 1, (y+1)*rc.bottom/16 - 1);
					LineTo(dc, x*rc.right/16 + 1, (y+1)*rc.bottom/16 - 1);
					LineTo(dc, x*rc.right/16 + 1, y*rc.bottom/16 + 1);
				}

				if( !fontGen->IsDisabled(idx) )
				{
					WCHAR ch[2];
					int length = acUtility::EncodeUTF16(idx, (unsigned char*)ch, 0);
					TextOutW(dc, x*rc.right/16 + cx, y*rc.bottom/16+cy, ch, length/2);
				}

				if( fontGen->IsImage(idx) )
				{
					RECT box;
					box.left = (x+1)*rc.right/16 - 5;
					box.right = (x+1)*rc.right/16;
					box.top = (y+1)*rc.bottom/16 - 5;
					box.bottom = (y+1)*rc.bottom/16;
					HBRUSH br = CreateSolidBrush(RGB(128,255,255));
					FillRect(dc, &box, br);
					DeleteObject(br);
				}
			}
			else
			{
				RECT box;
				box.left = x*rc.right/16 + 1;
				box.right = (x+1)*rc.right/16;
				box.top = y*rc.bottom/16 + 1;
				box.bottom = (y+1)*rc.bottom/16;
				FillRect(dc, &box, (HBRUSH)GetStockObject(DKGRAY_BRUSH));
			}
		}
	}

	// Clean up the cache created by Uniscribe
	if( sc != 0 )
		ScriptFreeCache(&sc);
}

void CCharWin::DrawAnsi(HDC dc, RECT &rc, TEXTMETRIC &tm)
{
	for( int y = 0; y < 16; y++ )
	{
		for( int x = 0; x < 16; x++ )
		{
			int idx = y*16 + x;

			char ch[2];
			ch[0] = idx;
			ch[1] = '\0';
			
			int cy = (rc.bottom/16 - tm.tmHeight)/2 + 1;

			int cx;
			if( !GetCharWidth32(dc, idx, idx, &cx) )
			{
				ABC abc;
				if( !GetCharABCWidths(dc, idx, idx, &abc) )
					cx = 0;
				else
					cx = abc.abcB;
			}

			cx = (rc.right/16 - cx)/2;

			if( fontGen->IsSelected(y*16 + x) )
				SetTextColor(dc, RGB(0,0,0));
			else
				SetTextColor(dc, RGB(64,64,64));

			if( fontGen->IsDisabled(y*16+x) || !fontGen->IsSelected(y*16+x) )
			{
				RECT box;
				box.left = x*rc.right/16 + 1;
				box.right = (x+1)*rc.right/16;
				box.top = y*rc.bottom/16 + 1;
				box.bottom = (y+1)*rc.bottom/16;
				if( fontGen->IsDisabled(y*16+x) )
				{
					HBRUSH br = CreateHatchBrush(HS_DIAGCROSS, RGB(128,128,128));
					FillRect(dc, &box, br);
					DeleteObject(br);
				}
				else
					FillRect(dc, &box, (HBRUSH)GetStockObject(GRAY_BRUSH));
			}

			if( fontGen->DidNotFit(y*16 + x) )
			{
				MoveToEx(dc, x*rc.right/16 + 1, y*rc.bottom/16 + 1, 0);
				LineTo(dc, (x+1)*rc.right/16 - 1, y*rc.bottom/16 + 1);
				LineTo(dc, (x+1)*rc.right/16 - 1, (y+1)*rc.bottom/16 - 1);
				LineTo(dc, x*rc.right/16 + 1, (y+1)*rc.bottom/16 - 1);
				LineTo(dc, x*rc.right/16 + 1, y*rc.bottom/16 + 1);
			}

			if( !fontGen->IsDisabled(y*16+x) )
				TextOut(dc, x*rc.right/16 + cx, y*rc.bottom/16+cy, ch, 1);

			if( fontGen->IsImage(idx) )
			{
				RECT box;
				box.left = (x+1)*rc.right/16 - 5;
				box.right = (x+1)*rc.right/16;
				box.top = (y+1)*rc.bottom/16 - 5;
				box.bottom = (y+1)*rc.bottom/16;
				HBRUSH br = CreateSolidBrush(RGB(128,255,255));
				FillRect(dc, &box, br);
				DeleteObject(br);
			}
		}
	}
}

void CCharWin::OnInitMenuPopup(HMENU menu, int pos, BOOL isWindowMenu)
{
	if( fontGen->GetStatus() == 0 )
	{
		EnableMenuItem(menu, ID_OPTIONS_SELECTFONT       , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_OPTIONS_EXPORTOPTIONS    , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_OPTIONS_VISUALIZE        , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_OPTIONS_SAVEAS           , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_OPTIONS_LOADCONFIGURATION, MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_OPTIONS_ABORT            , MF_BYCOMMAND | MF_GRAYED);

		EnableMenuItem(menu, ID_EDIT_SELECTALL          , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_POPUP_SELECTSUBSET      , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_POPUP_UNSELECTSUBSET    , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_EDIT_CLEARALL           , MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_EDIT_SELECTCHARSFROMFILE, MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(menu, ID_EDIT_OPENIMAGEMANAGER   , MF_BYCOMMAND | MF_ENABLED);
	}
	else
	{
		EnableMenuItem(menu, ID_OPTIONS_SELECTFONT       , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_OPTIONS_EXPORTOPTIONS    , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_OPTIONS_VISUALIZE        , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_OPTIONS_SAVEAS           , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_OPTIONS_LOADCONFIGURATION, MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_OPTIONS_ABORT            , MF_BYCOMMAND | MF_ENABLED);

		EnableMenuItem(menu, ID_EDIT_SELECTALL          , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_POPUP_SELECTSUBSET      , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_POPUP_UNSELECTSUBSET    , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_EDIT_CLEARALL           , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_EDIT_SELECTCHARSFROMFILE, MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_EDIT_OPENIMAGEMANAGER   , MF_BYCOMMAND | MF_GRAYED);
	}

	if( fontGen->GetNumFailedChars() )
	{
		if( fontGen->IsUsingUnicode() )
			EnableMenuItem(menu, ID_EDIT_FINDNEXTFAILEDCHAR, MF_BYCOMMAND | (fontGen->IsUsingUnicode() ? MF_ENABLED : MF_GRAYED));
		EnableMenuItem(menu, ID_EDIT_CLEARFAILEDCHARACTERS, MF_BYCOMMAND | MF_ENABLED);
	}
	else
	{
		EnableMenuItem(menu, ID_EDIT_FINDNEXTFAILEDCHAR   , MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(menu, ID_EDIT_CLEARFAILEDCHARACTERS, MF_BYCOMMAND | MF_GRAYED);
	}

	if( imageMgr )
	{
		CheckMenuItem(menu, ID_EDIT_OPENIMAGEMANAGER, MF_BYCOMMAND | MF_CHECKED);
	}
	else
	{
		CheckMenuItem(menu, ID_EDIT_OPENIMAGEMANAGER, MF_BYCOMMAND | MF_UNCHECKED);
	}
}

LRESULT CCharWin::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	int x, y;
	RECT rc;
	RECT box;
	int ch;

	switch( msg )
	{
	case WM_TIMER:
		OnTimer();
		return 0;

	case WM_LBUTTONDOWN:
		ch = GetCharFromPos(LOWORD(lParam), HIWORD(lParam));
		if( ch >= 0 )
		{
			selectMode = !fontGen->IsSelected(ch);
			fontGen->SetSelected(ch, selectMode);

			GetCharGridRect(&rc);
			x = 16*LOWORD(lParam)/rc.right; 
			y = 16*HIWORD(lParam)/rc.bottom;

			box.left = x*rc.right/16 + 1;
			box.right = (x+1)*rc.right/16;
			box.top = y*rc.bottom/16 + 1;
			box.bottom = (y+1)*rc.bottom/16;
			InvalidateRect(hWnd, &box, FALSE);
		}
		return 0;

	case WM_RBUTTONDOWN:
		OnRButtonDown(LOWORD(lParam), HIWORD(lParam));
		return 0;

	case WM_MOUSEMOVE:
		ch = GetCharFromPos(LOWORD(lParam), HIWORD(lParam));
		if( wParam & MK_LBUTTON )
		{
			if( ch >= 0 )
			{
				if( fontGen->IsSelected(ch) != selectMode &&
					!fontGen->IsDisabled(ch) )
				{
					fontGen->SetSelected(ch, selectMode);

					GetCharGridRect(&rc);
					x = 16*LOWORD(lParam)/rc.right;
					y = 16*HIWORD(lParam)/rc.bottom;

					box.left = x*rc.right/16 + 1;
					box.right = (x+1)*rc.right/16;
					box.top = y*rc.bottom/16 + 1;
					box.bottom = (y+1)*rc.bottom/16;
					InvalidateRect(hWnd, &box, FALSE);
				}
			}
		}

		// Determine which character the mouse is over
		if( ch >= 0 )
		{
			string str = acStringFormat("%d : %X", ch, ch);
			statusBar->SetStatusText(str.c_str(), 2, 0);
		}
		else
			statusBar->SetStatusText("", 2, 0);
		return 0;

	case WM_SIZE:
		OnSize();
		return 0;

	case WM_CLOSE:
		whenGenerateIsFinished = 0;
		fontGen->Abort();
		DestroyWindow(hWnd);
		return 0;

	case WM_DESTROY:
		fontGen->SaveConfiguration(GetDefaultConfig().c_str());
		PostQuitMessage(0);
		return 0;

	case WM_PAINT:
		Draw();
		return 0;

	case WM_INITMENUPOPUP:
		OnInitMenuPopup((HMENU)wParam, LOWORD(lParam), HIWORD(lParam));
		return 0;

	case WM_COMMAND:
		switch( LOWORD(wParam) )
		{
		case ID_APP_EXIT:
			whenGenerateIsFinished = 0;
			fontGen->Abort();
			DestroyWindow(hWnd);
			return 0;

		case ID_OPTIONS_ABOUT:
			OnAbout();
			return 0;

		case ID_OPTIONS_VISUALIZE:
			OnVisualize();
			return 0;

		case ID_OPTIONS_SELECTFONT:
			OnChooseFont();
			return 0;

		case ID_OPTIONS_EXPORTOPTIONS:
			OnExportOptions();
			return 0;

		case ID_OPTIONS_SAVEAS:
			OnSaveAs();
			return 0;

		case ID_OPTIONS_LOADCONFIGURATION:
			OnLoadConfiguration();
			return 0;

		case ID_OPTIONS_SAVECONFIGURATIONAS:
			OnSaveConfiguration();
			return 0;

		case ID_OPTIONS_ABORT:
			whenGenerateIsFinished = 0;
			fontGen->Abort();
			return 0;

		case ID_EDIT_SELECTALL:
			{
				fontGen->SelectSubset(fontGen->IsUsingUnicode() ? unicodeSubset : 0,
								      !fontGen->IsSubsetSelected(fontGen->IsUsingUnicode() ? unicodeSubset : 0));

				Invalidate(FALSE);
			}
			return 0;

		case ID_POPUP_SELECTSUBSET:
		case ID_POPUP_UNSELECTSUBSET:
			{
				if( fontGen->IsUsingUnicode() )
				{
					int item = -1;
					while( (item = listView->GetNextItem(item, LVNI_SELECTED)) != -1 )
					{
						int subset;
						listView->GetItemParam(item, (LPARAM*)&subset);
						fontGen->SelectSubset(subset, (LOWORD(wParam) == ID_POPUP_SELECTSUBSET));
						int check = fontGen->IsSubsetSelected(subset) + 1; 
						if( listView->GetItemStateImage(item) != check )
							listView->SetItemStateImage(item, check);
					}
				}
				else
					fontGen->SelectSubset(0, (LOWORD(wParam) == ID_POPUP_SELECTSUBSET));
				Invalidate(FALSE);
				UpdateStatus();
			}
			return 0;

		case ID_EDIT_CLEARALL:
			fontGen->ClearAll();
			Invalidate(FALSE);
			UpdateSubsetsSelection();
			UpdateStatus();
			return 0;

		case ID_EDIT_SELECTCHARSFROMFILE:
			OnSelectCharsFromFile();
			return 0;

		case ID_EDIT_FINDNEXTFAILEDCHAR:
			{
				unicodeSubset = fontGen->FindNextFailedCharacterSubset(unicodeSubset);
				int li = listView->FindItem(-1, fontGen->GetUnicodeSubset(unicodeSubset)->name.c_str());
				SetFocus(listView->GetHandle());
				listView->SetItemState(-1, LVIS_FOCUSED|LVIS_SELECTED, 0);
				if( li >= 0 )
				{
					listView->SetItemState(li, LVIS_FOCUSED|LVIS_SELECTED, LVIS_FOCUSED|LVIS_SELECTED);
					listView->EnsureVisible(li);
				}
				Invalidate(FALSE);
			}
			return 0;

		case ID_EDIT_CLEARFAILEDCHARACTERS:
			fontGen->ClearFailedCharacters();
			Invalidate(FALSE);
			return 0;

		case ID_EDIT_OPENIMAGEMANAGER:
			if( imageMgr ) 
			{
				delete imageMgr;
				imageMgr = 0;
			}
			else
			{
				imageMgr = new CImageMgr();
				imageMgr->Create(this, fontGen);
			}
			return 0;
		}
		break;

	case WM_SETCURSOR:
		if( isGenerating )
		{
			SetCursor(LoadCursor(0, IDC_WAIT));
			return TRUE;
		}
		break;

	case WM_NOTIFY:
		switch( ((NMHDR*)lParam)->code )
		{
		case LVN_ITEMCHANGED:
			{
				NMLISTVIEW *nm = (NMLISTVIEW*)lParam;
				if( (nm->uChanged & LVIF_STATE) )
				{
					if( nm->uNewState & LVIS_FOCUSED )
					{
						// A new subset was selected
						listView->GetItemParam(nm->iItem, (LPARAM*)&unicodeSubset);

						// Redraw
						Invalidate(FALSE);
					}
					if( (nm->uNewState & LVIS_STATEIMAGEMASK) != (nm->uOldState & LVIS_STATEIMAGEMASK) )
					{
						int subset;
						listView->GetItemParam(nm->iItem, (LPARAM*)&subset);
						int state = listView->GetItemStateImage(nm->iItem);
						if( fontGen->IsSubsetSelected(subset)+1 != state )
						{
							fontGen->SelectSubset(subset, state != 1);

							// Update the state image
							int check = fontGen->IsSubsetSelected(subset)+1;
							listView->SetItemStateImage(nm->iItem, check);

							// Redraw if this is the current subset
							if( subset == unicodeSubset )
								Invalidate(FALSE);
						}
					}
				}
			}
			return 0;
		}
	}

	return DefWndProc(msg, wParam, lParam);
}

void CCharWin::OnRButtonDown(int x, int y)
{
	HMENU menu = LoadMenu(0, MAKEINTRESOURCE(IDR_POPUP));
	HMENU subMenu = GetSubMenu(menu, 0);

	POINT pt = {x,y};
	ClientToScreen(hWnd, &pt);

	TrackPopupMenuEx(subMenu, TPM_LEFTALIGN|TPM_RIGHTBUTTON, pt.x, pt.y, hWnd, 0);
}

void CCharWin::OnSize()
{
	InvalidateRect(hWnd, 0, 0);

	if( listView )
	{
		RECT rc;
		GetClientRect(hWnd, &rc);
		rc.left = rc.right-LISTVIEW_WIDTH;

		if( statusBar )
		{
			RECT rcsb;
			GetWindowRect(statusBar->GetHandle(), &rcsb);
			rc.bottom -= rcsb.bottom - rcsb.top;
		}

		MoveWindow(listView->GetHandle(), rc.left, rc.top, rc.right-rc.left, rc.bottom-rc.top, TRUE);
	}

	if( statusBar ) 
		SendMessage(statusBar->GetHandle(), WM_SIZE, 0, 0);
}

void CCharWin::GetCharGridRect(RECT *rc)
{
	GetClientRect(hWnd, rc);

	if( listView )
	{
		RECT rclv;
		GetWindowRect(listView->GetHandle(), &rclv);
		rc->right -= rclv.right - rclv.left;
	}

	if( statusBar )
	{
		RECT rcsb;
		GetWindowRect(statusBar->GetHandle(), &rcsb);
		rc->bottom -= rcsb.bottom - rcsb.top;
	}
}

void CCharWin::OnAbout()
{
	CAbout dlg;

	dlg.DoModal(this);
}

void CCharWin::OnChooseFont()
{
	CChooseFont dlg;

	dlg.font            = fontGen->GetFontName();
	dlg.charSet         = fontGen->GetCharSet();
	dlg.fontSize        = fontGen->GetFontSize();
	dlg.isBold          = fontGen->IsBold();
	dlg.isItalic        = fontGen->IsItalic();
	dlg.antiAliasing    = fontGen->GetAntiAliasingLevel();
	dlg.useSmoothing    = fontGen->IsUsingSmoothing();
	dlg.scaleH          = fontGen->GetScaleHeight();
	dlg.useUnicode      = fontGen->IsUsingUnicode();
	dlg.outputInvalidCharGlyph = fontGen->IsOutputInvalidCharGlyphSet();
	dlg.outlineThickness = fontGen->GetOutlineThickness();

	if( dlg.DoModal(this) == IDOK )
	{
		fontGen->SetFontName(dlg.font);
		fontGen->SetCharSet(dlg.charSet);
		fontGen->SetFontSize(dlg.fontSize);
		fontGen->SetBold(dlg.isBold);
		fontGen->SetItalic(dlg.isItalic);
		fontGen->SetAntiAliasingLevel(dlg.antiAliasing);
		fontGen->SetUseSmoothing(dlg.useSmoothing);
		fontGen->SetScaleHeight(dlg.scaleH);
		fontGen->SetUseUnicode(dlg.useUnicode);

		fontGen->SetOutputInvalidCharGlyph(dlg.outputInvalidCharGlyph);

		fontGen->SetOutlineThickness(dlg.outlineThickness);

		PrepareView();
		InvalidateRect(hWnd, 0, FALSE);
	}
}

void CCharWin::OnExportOptions()
{
	CExportDlg dlg;

	dlg.paddingDown     = fontGen->GetPaddingDown();
	dlg.paddingRight    = fontGen->GetPaddingRight();
	dlg.paddingLeft     = fontGen->GetPaddingLeft();
	dlg.paddingUp       = fontGen->GetPaddingUp();
	dlg.spacingHoriz    = fontGen->GetSpacingHoriz();
	dlg.spacingVert     = fontGen->GetSpacingVert();

	dlg.width              = fontGen->GetOutWidth();
	dlg.height             = fontGen->GetOutHeight();
	dlg.bitDepth           = fontGen->GetOutBitDepth();
	dlg.fourChnlPacked     = fontGen->Is4ChnlPacked();
	dlg.textureFormat      = fontGen->GetTextureFormat();
	dlg.textureCompression = fontGen->GetTextureCompression();
	dlg.alphaChnl          = fontGen->GetAlphaChnl();
	dlg.redChnl            = fontGen->GetRedChnl();
	dlg.greenChnl          = fontGen->GetGreenChnl();
	dlg.blueChnl           = fontGen->GetBlueChnl();
	dlg.invA               = fontGen->IsAlphaInverted();
	dlg.invR               = fontGen->IsRedInverted();
	dlg.invG               = fontGen->IsGreenInverted();
	dlg.invB               = fontGen->IsBlueInverted();

	dlg.fontDescFormat  = fontGen->GetFontDescFormat();

	if( dlg.DoModal(this) == IDOK )
	{
		fontGen->SetPaddingDown(dlg.paddingDown);
		fontGen->SetPaddingUp(dlg.paddingUp);
		fontGen->SetPaddingLeft(dlg.paddingLeft);
		fontGen->SetPaddingRight(dlg.paddingRight);
		fontGen->SetSpacingHoriz(dlg.spacingHoriz);
		fontGen->SetSpacingVert(dlg.spacingVert);

		fontGen->SetOutWidth(dlg.width);
		fontGen->SetOutHeight(dlg.height);
		fontGen->SetOutBitDepth(dlg.bitDepth);
		fontGen->Set4ChnlPacked(dlg.fourChnlPacked);
		fontGen->SetTextureFormat(dlg.textureFormat);
		fontGen->SetTextureCompression(dlg.textureCompression);
		fontGen->SetAlphaChnl(dlg.alphaChnl);
		fontGen->SetRedChnl(dlg.redChnl);
		fontGen->SetGreenChnl(dlg.greenChnl);
		fontGen->SetBlueChnl(dlg.blueChnl);

		fontGen->SetAlphaInverted(dlg.invA);
		fontGen->SetRedInverted(dlg.invR);
		fontGen->SetGreenInverted(dlg.invG);
		fontGen->SetBlueInverted(dlg.invB);

		fontGen->SetFontDescFormat(dlg.fontDescFormat);
	}
}

void CCharWin::VisualizeAfterFinishedGenerating()
{
	if( fontGen->GetNumPages() > 0 )
	{
		if( wnd ) 
		{
			DestroyWindow(wnd->GetHandle());
			delete wnd;
		}
		wnd = new cImageWnd;
		
		wnd->CopyImage(fontGen->GetPageImage(0, 0));
		wnd->Create(this, fontGen);

		int countFailed = fontGen->GetNumFailedChars();
		if( countFailed )
		{
			stringstream s;
			s << countFailed << " characters did not fit the textures.";
			MessageBox(hWnd, s.str().c_str(), "Warning", MB_OK);
		}
	}
	else
		MessageBox(hWnd, "No output!", "Notification", MB_OK|MB_ICONEXCLAMATION);
}


void CCharWin::OnVisualize()
{
	if( isGenerating ) return;

	// First start the generation
	fontGen->GeneratePages();

	// Let the timer know what to do when the generation is finished
	whenGenerateIsFinished = 1;
	isGenerating = true;

	SetCursor(LoadCursor(0, IDC_WAIT));
}


void CCharWin::SaveFontAfterFinishedGenerating()
{
	// Save the generated font
	fontGen->SaveFont(saveFontName.c_str());

	int countFailed = fontGen->GetNumFailedChars();
	if( countFailed )
	{
		stringstream s;
		s << countFailed << " characters did not fit the textures.";
		MessageBox(hWnd, s.str().c_str(), "Warning", MB_OK);
	}
}

void CCharWin::OnLoadConfiguration()
{
	if( isGenerating ) return;

	CFileDialog dlg;
	dlg.AddFilter("All files (*.*)", "*.*");
	dlg.AddFilter("BMFont config (*.bmfc)", "*.bmfc", true);

	if( dlg.AskForOpenFileName(this) )
	{
		fontGen->LoadConfiguration(dlg.GetFileName().c_str());

		PrepareView();
		InvalidateRect(hWnd, 0, FALSE);
	}
}

void CCharWin::OnSaveConfiguration()
{
	CFileDialog dlg;
	dlg.AddFilter("All files (*.*)", "*.*");
	dlg.AddFilter("BMFont config (*.bmfc)", "*.bmfc", true);

	// Open a SaveAs dialog to get a filename from the user
	if( dlg.AskForSaveFileName(this) )
	{
		fontGen->SaveConfiguration(dlg.GetFileName().c_str());
	}
}

void CCharWin::OnSelectCharsFromFile()
{
	if( isGenerating ) return;

	CFileDialog dlg;
	dlg.AddFilter("All files (*.*)", "*.*", true);

	if( dlg.AskForOpenFileName(this) )
	{
		fontGen->SelectCharsFromFile(dlg.GetFileName().c_str());
		int countMissing = fontGen->GetNumFailedChars();
		if( countMissing )
		{
			stringstream s;
			s << countMissing << " characters from the file are not available in the font";
			MessageBox(hWnd, s.str().c_str(), "Warning", MB_OK);
		}

		UpdateSubsetsSelection();
		Invalidate(FALSE);
	}
}

void CCharWin::OnSaveAs()
{
	if( isGenerating ) return;

	CFileDialog dlg;
	dlg.AddFilter("All files (*.*)", "*.*");
	dlg.AddFilter("Bitmap font (*.fnt)", "*.fnt", true);

	// Open a SaveAs dialog to get a filename from the user
	if( dlg.AskForSaveFileName(this) )
	{
		saveFontName = dlg.GetFileName();

		// First start the generation
		fontGen->GeneratePages();

		// Let the time know what to do when the generation is finished
		whenGenerateIsFinished = 2;
		isGenerating = true;

		SetCursor(LoadCursor(0, IDC_WAIT));
	}
}

