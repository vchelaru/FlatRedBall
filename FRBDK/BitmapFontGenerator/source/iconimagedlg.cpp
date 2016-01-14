#include "iconimagedlg.h"
#include "resource.h"
#include "acwin_filedialog.h"
#include "commctrl.h"

using namespace acWindow;

CIconImageDlg::CIconImageDlg() : CDialog()
{
	int id = 0;
	int xoffset = 0;
	int yoffset = 0;
	int advance = 0;
}

int CIconImageDlg::DoModal(CWindow *parent)
{
	return CDialog::DoModal(MAKEINTRESOURCE(IDD_ICONIMAGE), parent);
}

LRESULT CIconImageDlg::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
{
	switch( msg )
	{
	case WM_INITDIALOG:
		OnInit();
		return TRUE;

	case WM_COMMAND:
		switch( LOWORD(wParam) )
		{
		case IDOK:
			GetOptions();
			EndDialog(hWnd, IDOK);
			break;

		case IDCANCEL:
			EndDialog(hWnd, IDCANCEL);
			break;

		case IDC_BROWSE:
			OnBrowse();
			break;
		}
		break;
	}

	return DefWndProc(msg, wParam, lParam);
}

void CIconImageDlg::OnBrowse()
{
	CFileDialog dlg;
	dlg.AddFilter("All files (*.*)", "*.*");
	dlg.AddFilter("Supported image files (*.bmp;*.jpg;*.tga;*.dds;*.png)", "*.bmp;*.jpg;*.tga;*.dds;*.png", true);

	if( dlg.AskForOpenFileName(this) )
	{
		SetDlgItemText(hWnd, IDC_FILE, dlg.GetFileName().c_str());
	}
}

void CIconImageDlg::OnInit()
{
	SetDlgItemText(hWnd, IDC_FILE, fileName.c_str());
	SetDlgItemInt(hWnd, IDC_ID, id, FALSE);

	SetDlgItemInt(hWnd, IDC_XOFFSET, xoffset, TRUE);
	SendDlgItemMessage(hWnd, IDC_SPIN_XOFFSET, UDM_SETRANGE, 0, (LPARAM)MAKELONG(32767, -32768));

	SetDlgItemInt(hWnd, IDC_YOFFSET, yoffset, TRUE);
	SendDlgItemMessage(hWnd, IDC_SPIN_YOFFSET, UDM_SETRANGE, 0, (LPARAM)MAKELONG(32767, -32768));

	SetDlgItemInt(hWnd, IDC_ADVANCE, advance, TRUE);
	SendDlgItemMessage(hWnd, IDC_SPIN_ADVANCE, UDM_SETRANGE, 0, (LPARAM)MAKELONG(32767, -32768));
}


void CIconImageDlg::GetOptions()
{
	char buf[260];

	GetDlgItemText(hWnd, IDC_FILE, buf, 260);
	fileName = buf;

	id = GetDlgItemInt(hWnd, IDC_ID, 0, FALSE);
	xoffset = GetDlgItemInt(hWnd, IDC_XOFFSET, 0, TRUE);
	yoffset = GetDlgItemInt(hWnd, IDC_YOFFSET, 0, TRUE);
	advance = GetDlgItemInt(hWnd, IDC_ADVANCE, 0, TRUE);
}




