#include <windows.h>
#include <commdlg.h>	// GetOpenFileName()
#include <stdlib.h>		// _MAX_PATH
#include <cderr.h>		// error codes for CommDlgExtendedError()
#include <sstream>

#include "imagewnd.h"
#include "resource.h"
#include "charwin.h"

using namespace std;
using namespace acWindow;

static const char * const winTitle = "Preview";

cImageWnd::cImageWnd() : CWindow()
{
	scale = 1.0;
	viewAlpha = false;
	viewTiled = false;
	parent = 0;

	page    = 0;
	chnl    = 0;
	fontGen = 0;
}

void cImageWnd::CopyImage(cImage *img)
{
	originalImage.Create(img->width, img->height);

	memcpy(originalImage.pixels, img->pixels, img->width*img->height*4);

	ProcessImage();
}

int cImageWnd::Create(CWindow *parent, CFontGen *fontGen)
{
	this->parent  = parent;
	this->fontGen = fontGen;

	HICON hIcon = (HICON)LoadImage(GetModuleHandle(0), MAKEINTRESOURCE(IDI_ICON1), IMAGE_ICON, LR_DEFAULTSIZE, LR_DEFAULTSIZE, LR_SHARED);
	HICON hIconSmall = (HICON)LoadImage(GetModuleHandle(0), MAKEINTRESOURCE(IDI_ICON1), IMAGE_ICON, 16, 16, LR_SHARED);

	int r = RegisterClass("ImageWnd", 0, 0, hIcon, hIconSmall, AC_REGDEFCURSOR);
	if( r < 0 ) return r;

	RECT rc = {0, 0, originalImage.width, originalImage.height};
	AdjustWindowRect(&rc, WS_OVERLAPPEDWINDOW, TRUE);

	// Determine the size of the work area, so we don't make the window too large
	RECT wa;
	SystemParametersInfo(SPI_GETWORKAREA, 0, &wa, 0);

	if( rc.right - rc.left > wa.right - wa.left )
		rc.right = rc.left + wa.right - wa.left;
	if( rc.bottom - rc.top > wa.bottom - wa.top )
		rc.bottom = rc.top + wa.bottom - wa.top;

	stringstream title;
	title << winTitle << " : " << page+1 << "/" << fontGen->GetNumPages();

	if( fontGen->Is4ChnlPacked() )
		title << " : " << 1 + chnl;

	r = CWindow::Create(title.str().c_str(), rc.right-rc.left, rc.bottom-rc.top, WS_OVERLAPPEDWINDOW, 0, parent, "ImageWnd");
	if( r < 0 ) return r;

	SetMenu(MAKEINTRESOURCE(IDR_IMG_MENU));

	SetAccelerator(MAKEINTRESOURCE(IDR_IMG_ACCEL));

	ShowWindow(hWnd, SW_SHOW);

	return 0;
}

LRESULT cImageWnd::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	switch( msg )
	{
	case WM_PAINT:
		OnPaint();
		return 0;

	case WM_SIZE:
		OnSize();
		return 0;

	case WM_HSCROLL:
		OnScroll(SB_HORZ, LOWORD(wParam));
		return 0;

	case WM_VSCROLL:
		OnScroll(SB_VERT, LOWORD(wParam));
		return 0;

	case WM_INITMENUPOPUP:
		OnInitMenuPopup((HMENU)wParam, LOWORD(lParam), HIWORD(lParam));
		return 0;

	case WM_COMMAND:
		switch( LOWORD(wParam) )
		{
/*		case ID_FILE_CLOSE:
			SendMessage(WM_CLOSE, 0, 0);
			return 0;

		case ID_FILE_SAVEAS:
			OnSaveAs();
			return 0;*/

		case IDC_VIEW_SCALE_1_8:
			OnScale(0.125);
			return 0;

		case IDC_VIEW_SCALE_1_4:
			OnScale(0.25);
			return 0;
			
		case IDC_VIEW_SCALE_1_2:
			OnScale(0.5);
			return 0;

		case IDC_VIEW_SCALE_1_1:
			OnScale(1);
			return 0;

		case IDC_VIEW_SCALE_2_1:
			OnScale(2);
			return 0;

		case IDC_VIEW_SCALE_4_1:
			OnScale(4);
			return 0;
			
		case IDC_VIEW_SCALE_8_1:
			OnScale(8);
			return 0;

		case IDC_VIEW_NEXTPAGE:
			OnChangePage(1);
			return 0;

		case IDC_VIEW_PREVPAGE:
			OnChangePage(-1);
			return 0;

/*		case ID_VIEW_CHANNELS_COLOR:
		case ID_VIEW_CHANNELS_ALPHA:
			viewAlpha = LOWORD(wParam) == ID_VIEW_CHANNELS_ALPHA;
			ProcessImage();
			Draw();
			Invalidate(false);
			return 0;

		case ID_VIEW_TILED:
			viewTiled = !viewTiled;
			Draw();
			Invalidate(false);
			return 0;*/
		}
	}

	return DefWndProc(msg, wParam, lParam);
}

void cImageWnd::OnPaint()
{
	PAINTSTRUCT ps;
	memset(&ps, 0, sizeof(PAINTSTRUCT));

	HDC dc = BeginPaint(hWnd, &ps);

	RECT rc;
	GetClientRect(hWnd, &rc);
	buffer.CopyToDC(dc, 0, 0, rc.right, rc.bottom);

	EndPaint(hWnd, &ps);
}

void cImageWnd::OnChangePage(int direction)
{
	if( fontGen->Is4ChnlPacked() )
	{
		chnl += direction;
		if( chnl >= 4 )
		{
			page++;
			chnl = 0;
		}
		else if( chnl < 0 )
		{
			page--;
			chnl = 3;
		}
	}
	else
		page += direction;

	if( page >= (signed)fontGen->GetNumPages() )
		page = 0;

	if( page < 0 )
		page = fontGen->GetNumPages() - 1;

	CopyImage(fontGen->GetPageImage(page, chnl));

	Draw();

	stringstream title;
	title << winTitle << " : " << page+1 << "/" << fontGen->GetNumPages();

	if( fontGen->Is4ChnlPacked() )
		title << " : " << 1 + chnl;

	UpdateWindowText(title.str().c_str());

	InvalidateRect(hWnd, 0, TRUE);
}

void cImageWnd::OnSize()
{
	// Do we need to add scroll bars?
	UpdateScrollBars();

	// Resize back buffer
	RECT rc;
	GetClientRect(hWnd, &rc);
	buffer.Create(rc.right, rc.bottom);
	Draw();

	// Show back buffer
	Invalidate(false);
}

void cImageWnd::UpdateScrollBars()
{
	RECT rc;

	int imgWidth = int(originalImage.width * scale);
	int imgHeight = int(originalImage.height * scale);

	GetClientRect(hWnd, &rc);
	ShowScrollBar(hWnd, SB_HORZ, rc.right < imgWidth);

	GetClientRect(hWnd, &rc);
	ShowScrollBar(hWnd, SB_VERT, rc.bottom < imgHeight);

	// Set scroll bar range
	GetClientRect(hWnd, &rc);
	SCROLLINFO info;
	info.cbSize = sizeof(SCROLLINFO);
	info.fMask = SIF_RANGE | SIF_POS;
	if( imgWidth - rc.right > 0 )
	{
		GetScrollInfo(hWnd, SB_HORZ, &info);

		info.nMin = 0;
		info.nMax = imgWidth - rc.right;
		info.nPos = info.nPos > info.nMax ? info.nMax : info.nPos;
	}
	else
	{
		info.nMin = 0;
		info.nMax = 0;
		info.nPos = 0;
	}
	SetScrollInfo(hWnd, SB_HORZ, &info, TRUE);
	
	if( imgHeight - rc.bottom > 0 )
	{
		GetScrollInfo(hWnd, SB_HORZ, &info);

		info.nMin = 0;
		info.nMax = imgHeight - rc.bottom;
		info.nPos = info.nPos > info.nMax ? info.nMax : info.nPos;
	}
	else
	{
		info.nMin = 0;
		info.nMax = 0;
		info.nPos = 0;
	}
	SetScrollInfo(hWnd, SB_VERT, &info, TRUE);
}

void cImageWnd::Draw()
{
	// Clear background
	for( int n = 0; n < buffer.width * buffer.height; n++ )
	{
		buffer.pixels[n] = 0x555555;
	}

	int imgWidth = int(originalImage.width * scale);
	int imgHeight = int(originalImage.height * scale);

	int x1 = buffer.width/2 - imgWidth/2 - 1;
	int x2 = x1 + imgWidth + 1; 
	int y1 = buffer.height/2 - imgHeight/2 - 1;
	int y2 = y1 + imgHeight + 1; 

	if( x1 < 0 ) x1 = -GetHorzScroll() - 1;
	if( y1 < 0 ) y1 = -GetVertScroll() - 1;

	// Draw vertical lines
	if( x1 >= 0 )
	{
		int min = y1 < 0 ? 0 : y1;
		int max = y2 + 1 > buffer.height ? buffer.height : y2 + 1;
		for( int n = min; n < max; n++ )
		{
			buffer.pixels[x1 + n*buffer.width] = 0;
		}
	}

	if( x2 < buffer.width )
	{
		int min = y1 < 0 ? 0 : y1;
		int max = y2 + 1 > buffer.height ? buffer.height : y2 + 1;
		for( int n = min; n < max; n++ )
		{
			buffer.pixels[x2 + n*buffer.width] = 0;
		}
	}

	// Draw horizontal lines
	if( y1 >= 0 )
	{
		int min = x1 < 0 ? 0 : x1;
		int max = x2 + 1 > buffer.width ? buffer.width : x2 + 1;
		for( int n = min; n < max; n++ )
		{
			buffer.pixels[n + y1*buffer.width] = 0;
		}
	}

	if( y2 < buffer.height )
	{
		int min = x1 < 0 ? 0 : x1;
		int max = x2 + 1 > buffer.width ? buffer.width : x2 + 1;
		for( int n = min; n < max; n++ )
		{
			buffer.pixels[n + y2*buffer.width] = 0;
		}
	}

	// Copy image to the backbuffer
	if( !viewTiled )
		DrawImage(x1+1, y1+1);
	else
	{
		x1++;
		y1++;
		while( x1 > 0 ) x1 -= imgWidth;
		while( y1 > 0 ) y1 -= imgHeight;
		int sx = x1;

		while( y1 < buffer.height )
		{
			x1 = sx;
			while( x1 < buffer.width )
			{
				DrawImage(x1, y1);

				x1 += imgWidth;
			}

			y1 += imgHeight;
		}
	}
}

void cImageWnd::DrawImage(int dx, int dy)
{
	int ssx = 0;
	int sy = 0;

	if( dx < 0 )
	{
		ssx += -dx;
		dx = 0;
	}
	if( dy < 0 )
	{
		sy += -dy;
		dy = 0;
	}

	int imgWidth = int(originalImage.width * scale);
	int imgHeight = int(originalImage.height * scale);

	int y2 = dy + imgHeight - sy;
	int x2 = dx + imgWidth - ssx;
	if( y2 > buffer.height ) y2 = buffer.height;
	if( x2 > buffer.width ) x2 = buffer.width;

	// Copy image to the backbuffer
	if( scale > 1 )
	{
		int iScale = int(scale);
		for( int y = dy; y < y2; y++ )
		{
			int sx = ssx;
			for( int x = dx; x < x2; x++ )
			{
				buffer.pixels[x + y*buffer.width] = image.pixels[sx/iScale + sy/iScale*image.width];
				sx++;
			}
			sy++;
		}
	}
	else
	{
		for( int y = dy; y < y2; y++ )
		{
			int sx = ssx;
			for( int x = dx; x < x2; x++ )
			{
				buffer.pixels[x + y*buffer.width] = image.pixels[sx + sy*image.width];
				sx++;
			}
			sy++;
		}
	}
}

int cImageWnd::GetHorzScroll()
{
	SCROLLINFO info;
	info.cbSize = sizeof(SCROLLINFO);
	info.fMask = SIF_POS;
	if( GetScrollInfo(hWnd, SB_HORZ, &info) )
		return info.nPos;

	return 0;
}

int cImageWnd::GetVertScroll()
{
	SCROLLINFO info;
	info.cbSize = sizeof(SCROLLINFO);
	info.fMask = SIF_POS;
	if( GetScrollInfo(hWnd, SB_VERT, &info) )
		return info.nPos;

	return 0;
}

void cImageWnd::OnScroll(UINT scrollBar, UINT action)
{
	SCROLLINFO info;
	info.cbSize = sizeof(SCROLLINFO);
	info.fMask = SIF_POS | SIF_TRACKPOS;
	if( GetScrollInfo(hWnd, scrollBar, &info) )
	{
		switch( action )
		{
		case SB_PAGEUP:
			info.nPos -= image.width/8 ? image.width/8 : 1;
			break;

		case SB_PAGEDOWN:
			info.nPos += image.width/8 ? image.width/8 : 1;
			break;

		case SB_LINEUP:
			info.nPos -= image.width/64 ? image.width/64 : 1;
			break;

		case SB_LINEDOWN:
			info.nPos += image.width/64 ? image.width/64 : 1;
			break;

		case SB_THUMBTRACK:
		case SB_THUMBPOSITION:
			info.nPos = info.nTrackPos;
			break;
		}

		info.fMask = SIF_POS;
		SetScrollInfo(hWnd, scrollBar, &info, TRUE);
	}

	Draw();
	Invalidate(false);
}

void cImageWnd::OnScale(float newScale)
{
	scale = newScale;

	ProcessImage();

	if( !IsZoomed(hWnd) )
	{
		// Remove the scroll bars
		ShowScrollBar(hWnd, SB_HORZ, 0);
		ShowScrollBar(hWnd, SB_VERT, 0);

		// Resize the window to fit the new size
		RECT rc = {0, 0, int(originalImage.width*scale), int(originalImage.height*scale)};
		AdjustWindowRect(&rc, WS_OVERLAPPEDWINDOW, TRUE);

		// Determine the size of the work area, so we don't make the window too large
		RECT wa;
		SystemParametersInfo(SPI_GETWORKAREA, 0, &wa, 0);

		if( rc.right - rc.left > wa.right - wa.left )
			rc.right = rc.left + wa.right - wa.left;
		if( rc.bottom - rc.top > wa.bottom - wa.top )
			rc.bottom = rc.top + wa.bottom - wa.top;

		SetWindowPos(hWnd, NULL, 0,0, rc.right-rc.left, rc.bottom-rc.top, SWP_NOMOVE | SWP_NOZORDER);
	}
	else
	{
		// Update the window without resizing it
		UpdateScrollBars();

		Draw();
		Invalidate(false);
	}
}

void cImageWnd::OnInitMenuPopup(HMENU hMenu, int pos, BOOL isWindowMenu)
{
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_1_8, MF_BYCOMMAND | (scale == 0.125 ? MF_CHECKED : MF_UNCHECKED));
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_1_4, MF_BYCOMMAND | (scale == 0.25 ? MF_CHECKED : MF_UNCHECKED));
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_1_2, MF_BYCOMMAND | (scale == 0.5 ? MF_CHECKED : MF_UNCHECKED));
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_1_1, MF_BYCOMMAND | (scale == 1 ? MF_CHECKED : MF_UNCHECKED));
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_2_1, MF_BYCOMMAND | (scale == 2 ? MF_CHECKED : MF_UNCHECKED));
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_4_1, MF_BYCOMMAND | (scale == 4 ? MF_CHECKED : MF_UNCHECKED));
	CheckMenuItem(hMenu, IDC_VIEW_SCALE_8_1, MF_BYCOMMAND | (scale == 8 ? MF_CHECKED : MF_UNCHECKED));

//	CheckMenuItem(hMenu, ID_VIEW_CHANNELS_COLOR, MF_BYCOMMAND | (!viewAlpha ? MF_CHECKED : MF_UNCHECKED));
//	CheckMenuItem(hMenu, ID_VIEW_CHANNELS_ALPHA, MF_BYCOMMAND | (viewAlpha ? MF_CHECKED : MF_UNCHECKED));
//	CheckMenuItem(hMenu, ID_VIEW_TILED, MF_BYCOMMAND | (viewTiled ? MF_CHECKED : MF_UNCHECKED));

	if( fontGen->GetNumPages() > 1 || fontGen->Is4ChnlPacked() )
	{
		EnableMenuItem(hMenu, IDC_VIEW_PREVPAGE, MF_BYCOMMAND | MF_ENABLED);
		EnableMenuItem(hMenu, IDC_VIEW_NEXTPAGE, MF_BYCOMMAND | MF_ENABLED);
	}
	else
	{
		EnableMenuItem(hMenu, IDC_VIEW_PREVPAGE, MF_BYCOMMAND | MF_GRAYED);
		EnableMenuItem(hMenu, IDC_VIEW_NEXTPAGE, MF_BYCOMMAND | MF_GRAYED);
	}
}

void cImageWnd::ProcessImage()
{
	cImage tmp;
	cImage *src;
	if( viewAlpha )
	{
		tmp.Create(originalImage.width, originalImage.height);
		for( int c = 0; c < tmp.height*tmp.width; c++ )
		{
			DWORD a = originalImage.pixels[c]>>24;
			tmp.pixels[c] = a | (a<<8) | (a<<16) | (a<<24);
		}
		src = &tmp;
	}
	else
		src = &originalImage;

	if( scale < 1 ) 
	{
		int imgWidth = int(originalImage.width*scale);
		int imgHeight = int(originalImage.height*scale);

		image.Create(imgWidth, imgHeight);

		int count = int(1/scale);

		for( int y = 0; y < imgHeight; y++ )
		{
			for( int x = 0; x < imgWidth; x++ )
			{
				// Get the average for the pixel
				int a = 0, r = 0, g = 0, b = 0;
				for( int yc = 0; yc < count; yc++ )
				{
					for( int xc = 0; xc < count; xc++ )
					{
						BYTE *p = (BYTE*)&src->pixels[xc+x*count + (yc + y*count)*src->width];
						b += p[0];
						g += p[1];
						r += p[2];
						a += p[3];
					}
				}
				a /= count*count;
				r /= count*count;
				g /= count*count;
				b /= count*count;

				BYTE *p = (BYTE*)&image.pixels[x+y*image.width];
				p[0] = b;
				p[1] = g;
				p[2] = r;
				p[3] = a;
			}
		}
	}
	else
	{
		image.Create(src->width, src->height);

		memcpy(image.pixels, src->pixels, 4*src->width*src->height);
	}
}

