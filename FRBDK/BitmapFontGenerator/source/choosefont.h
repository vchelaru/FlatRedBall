#ifndef CHOOSEFONT_H
#define CHOOSEFONT_H

#include <string>
using std::string;

#include "acwin_dialog.h"

class CChooseFont : virtual public acWindow::CDialog
{
public:
	CChooseFont();

	int DoModal(acWindow::CWindow *parent);

	string font;
	int charSet;
	int fontSize;
	int antiAliasing;
	bool useSmoothing;
	bool isBold;
	bool isItalic;
	bool useUnicode;
	int scaleH;
	bool outputInvalidCharGlyph;
	int outlineThickness;

protected:
	void OnInit();
	void GetOptions();

	void OnFontChange();

	void EnableWidgets();

	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);
};

#endif