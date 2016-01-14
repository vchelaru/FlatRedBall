#include "choosefont.h"
#include "resource.h"
#include "commctrl.h"
#include "unicode.h"

using namespace acWindow;

typedef DWORD (_stdcall *GetGlyphIndices_t)(HDC hdc, LPCTSTR lpstr, int c, LPWORD pgi, DWORD fl);
extern GetGlyphIndices_t fGetGlyphIndicesA;
extern GetGlyphIndices_t fGetGlyphIndicesW;

int CALLBACK ChooseFontCallback(
  ENUMLOGFONTEX *lpelfe,    // logical-font data
  NEWTEXTMETRICEX *lpntme,  // physical-font data
  DWORD FontType,           // type of font
  LPARAM lParam             // application-defined data
);

int CALLBACK ChooseFontCallback2(
  ENUMLOGFONTEX *lpelfe,    // logical-font data
  NEWTEXTMETRICEX *lpntme,  // physical-font data
  DWORD FontType,           // type of font
  LPARAM lParam             // application-defined data
);

CChooseFont::CChooseFont() : CDialog()
{

}

int CChooseFont::DoModal(CWindow *parent)
{
	return CDialog::DoModal(MAKEINTRESOURCE(IDD_CHOOSEFONT), parent);
}

LRESULT CChooseFont::MsgProc(UINT msg, WPARAM wParam, LPARAM lParam)
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

		case IDC_FONT:
			if( HIWORD(wParam) == CBN_SELCHANGE )
				OnFontChange();
			break;

		case IDC_ENABLEAA:
		case IDC_USEUNICODE:
		case IDC_USEOEM:
			EnableWidgets();
			break;
		}
		break;
	}

	return DefWndProc(msg, wParam, lParam);
}

void CChooseFont::EnableWidgets()
{
	// ASCII charset combo box
	EnableWindow(GetDlgItem(hWnd, IDC_CHARSET), !IsDlgButtonChecked(hWnd, IDC_USEUNICODE));

	// Supersampling
	EnableWindow(GetDlgItem(hWnd, IDC_ANTIALIASING), IsDlgButtonChecked(hWnd, IDC_ENABLEAA));
}

void CChooseFont::OnFontChange()
{
	char buf[256];
	GetDlgItemText(hWnd, IDC_CHARSET, buf, 256);
	if( strcmp(buf, "") != 0 )
		charSet = GetCharSet(buf);

	SendDlgItemMessage(hWnd, IDC_CHARSET, CB_RESETCONTENT, 0, 0);

	int idx = SendDlgItemMessage(hWnd, IDC_FONT, CB_GETCURSEL, 0, 0);
	if( idx != CB_ERR )
	{
		// Enumerate the charsets for the font
		LOGFONT lf;
		lf.lfCharSet = DEFAULT_CHARSET;
		lf.lfPitchAndFamily = 0;

		char buf[256];
		SendDlgItemMessage(hWnd, IDC_FONT, CB_GETLBTEXT, idx, (LPARAM)buf);

		strncpy(lf.lfFaceName, buf, LF_FACESIZE-1);
		lf.lfFaceName[LF_FACESIZE-1] = 0;

		HDC dc = GetDC(0);
		EnumFontFamiliesEx(dc, &lf, (FONTENUMPROC)ChooseFontCallback2, (LPARAM)this, 0);
		ReleaseDC(0, dc);
	}

	string str = GetCharSetName(charSet);
	int r = SendDlgItemMessage(hWnd, IDC_CHARSET, CB_SELECTSTRING, -1, (LPARAM)str.c_str());
	if( r == CB_ERR )
		SendDlgItemMessage(hWnd, IDC_CHARSET, CB_SETCURSEL, 0, 0);
}

void CChooseFont::OnInit()
{
	HDC dc;
	string str;

	// Get the device context for the display
	dc = GetDC(0);

	// Enumerate all fonts and types
	LOGFONT lf;
	lf.lfCharSet = DEFAULT_CHARSET; 
	lf.lfFaceName[0] = '\0';
	lf.lfPitchAndFamily = 0;

	EnumFontFamiliesEx(dc, &lf, (FONTENUMPROC)ChooseFontCallback, (LPARAM)this, 0);

	ReleaseDC(0, dc);

	SendDlgItemMessage(hWnd, IDC_FONT, CB_SELECTSTRING, -1, (LPARAM)font.c_str());

	OnFontChange();

	str = GetCharSetName(charSet);
	SendDlgItemMessage(hWnd, IDC_CHARSET, CB_SELECTSTRING, -1, (LPARAM)str.c_str());

	if( fontSize < 0 )
	{
		SetDlgItemInt(hWnd, IDC_FONTSIZE, -fontSize, FALSE);
		CheckDlgButton(hWnd, IDC_MATCHCHARHEIGHT, BST_CHECKED);
	}
	else
	{
		SetDlgItemInt(hWnd, IDC_FONTSIZE, fontSize, FALSE);
	}

	SendDlgItemMessage(hWnd, IDC_SIZESPIN, UDM_SETRANGE, 0, (LPARAM)MAKELONG(255, 1));
	SendDlgItemMessage(hWnd, IDC_SPINSAMPLING, UDM_SETRANGE, 0, (LPARAM)MAKELONG(4, 2)); 
	SendDlgItemMessage(hWnd, IDC_SCALEH_SPIN, UDM_SETRANGE, 0, (LPARAM)MAKELONG(200, 50)); 

	CheckDlgButton(hWnd, IDC_BOLD, isBold ? BST_CHECKED : BST_UNCHECKED);
	CheckDlgButton(hWnd, IDC_ITALIC, isItalic ? BST_CHECKED : BST_UNCHECKED);

	CheckDlgButton(hWnd, IDC_SMOOTH, useSmoothing ? BST_CHECKED : BST_UNCHECKED);

	CheckDlgButton(hWnd, IDC_INVALIDCHAR, outputInvalidCharGlyph ? BST_CHECKED : BST_UNCHECKED);

	CheckDlgButton(hWnd, IDC_USEUNICODE, useUnicode ? BST_CHECKED : BST_UNCHECKED);
	CheckDlgButton(hWnd, IDC_USEOEM, !useUnicode ? BST_CHECKED : BST_UNCHECKED);

	SetDlgItemInt(hWnd, IDC_ANTIALIASING, antiAliasing == 1 ? 2 : antiAliasing, FALSE);
	CheckDlgButton(hWnd, IDC_ENABLEAA, antiAliasing > 1 ? BST_CHECKED : BST_UNCHECKED);

	SetDlgItemInt(hWnd, IDC_SCALEH, scaleH, FALSE);



	// Outline options
	SetDlgItemInt(hWnd, IDC_OUTLINETHICKNESS, outlineThickness, FALSE);

	SendDlgItemMessage(hWnd, IDC_SPINTHICKNESS, UDM_SETRANGE, 0, (LPARAM)MAKELONG(32, 0)); 

	EnableWidgets();
}

int CALLBACK ChooseFontCallback(
  ENUMLOGFONTEX *lpelfe,    // logical-font data
  NEWTEXTMETRICEX *lpntme,  // physical-font data
  DWORD FontType,           // type of font
  LPARAM lParam             // application-defined data
)
{
	CDialog *dlg = (CChooseFont *)lParam;

	// Add font name to combobox
	int idx = SendDlgItemMessage(dlg->GetHandle(), IDC_FONT, CB_FINDSTRINGEXACT, 0, (LPARAM)(char*)lpelfe->elfLogFont.lfFaceName);
	if( idx == CB_ERR )
		SendDlgItemMessage(dlg->GetHandle(), IDC_FONT, CB_ADDSTRING, 0, (LPARAM)(char*)lpelfe->elfLogFont.lfFaceName);

	return 1;
}

int CALLBACK ChooseFontCallback2(
  ENUMLOGFONTEX *lpelfe,
  NEWTEXTMETRICEX *lpntme,
  DWORD FontType,
  LPARAM lParam
)
{
	CChooseFont *dlg = (CChooseFont *)lParam;

	string str = GetCharSetName(lpelfe->elfLogFont.lfCharSet);

	// Add charset to combobox
	int idx = SendDlgItemMessage(dlg->GetHandle(), IDC_CHARSET, CB_FINDSTRINGEXACT, 0, (LPARAM)str.c_str());
	if( idx == CB_ERR )
		SendDlgItemMessage(dlg->GetHandle(), IDC_CHARSET, CB_ADDSTRING, 0, (LPARAM)str.c_str());

	return 1;
}

void CChooseFont::GetOptions()
{
	char buf[256];

	GetDlgItemText(hWnd, IDC_FONT, buf, 256);
	font = buf;

	GetDlgItemText(hWnd, IDC_CHARSET, buf, 256);
	charSet = GetCharSet(buf);

	fontSize = GetDlgItemInt(hWnd, IDC_FONTSIZE, 0, FALSE);
	if( IsDlgButtonChecked(hWnd, IDC_MATCHCHARHEIGHT) == BST_CHECKED )
		fontSize = -fontSize;

	if( IsDlgButtonChecked(hWnd, IDC_ENABLEAA) == BST_CHECKED )
	{
		antiAliasing = GetDlgItemInt(hWnd, IDC_ANTIALIASING, 0, FALSE);
		if( antiAliasing < 2 ) antiAliasing = 2;
		if( antiAliasing > 4 ) antiAliasing = 4;
	}
	else
		antiAliasing = 1;

	scaleH = GetDlgItemInt(hWnd, IDC_SCALEH, 0, FALSE);
	if( scaleH < 1 ) scaleH = 1;

	isBold = IsDlgButtonChecked(hWnd, IDC_BOLD) == BST_CHECKED;
	isItalic = IsDlgButtonChecked(hWnd, IDC_ITALIC) == BST_CHECKED;

	useSmoothing = IsDlgButtonChecked(hWnd, IDC_SMOOTH) == BST_CHECKED;
	
	outputInvalidCharGlyph = IsDlgButtonChecked(hWnd, IDC_INVALIDCHAR) == BST_CHECKED;

	if( IsDlgButtonChecked(hWnd, IDC_USEUNICODE) ) useUnicode = true; else useUnicode = false;

	outlineThickness = GetDlgItemInt(hWnd, IDC_OUTLINETHICKNESS, 0, FALSE);
}






