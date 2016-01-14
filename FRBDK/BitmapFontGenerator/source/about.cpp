#include <string>
#include "about.h"
#include "resource.h"

using namespace acWindow;

CAbout::CAbout() : CDialog()
{

}

int CAbout::DoModal(CWindow *parent)
{
	return CDialog::DoModal(MAKEINTRESOURCE(IDD_ABOUT), parent);
}

LRESULT CAbout::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	switch( msg )
	{
	case WM_INITDIALOG:
		urlAngelCode.Subclass(GetDlgItem(hWnd, IDC_URL1));
		urlLibPng.Subclass(GetDlgItem(hWnd, IDC_URL_LIBPNG));
		urlLibJpeg.Subclass(GetDlgItem(hWnd, IDC_LIBJPEG));
		urlZLib.Subclass(GetDlgItem(hWnd, IDC_URL_ZLIB));
		urlSquish.Subclass(GetDlgItem(hWnd, IDC_SQUISH));
		urlAngelCode.MakeUrl("http://www.angelcode.com");
		urlLibPng.MakeUrl("http://www.libpng.org");
		urlLibJpeg.MakeUrl("http://www.ijg.org");
		urlZLib.MakeUrl("http://www.zlib.net");
		urlSquish.MakeUrl("http://code.google.com/p/libsquish/");
		break;

	case WM_COMMAND:
		switch( LOWORD(wParam) )
		{
		case IDOK:
			EndDialog(hWnd, IDOK);
			break;

		case IDCANCEL:
			EndDialog(hWnd, IDCANCEL);
			break;
		}
		break;
	}

	return DefWndProc(msg, wParam, lParam);
}


