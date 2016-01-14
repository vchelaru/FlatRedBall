#ifndef ABOUT_H
#define ABOUT_H

#include "acwin_dialog.h"
#include "acwin_static.h"

class CAbout : virtual public acWindow::CDialog
{
public:
	CAbout();

	int DoModal(CWindow *parent);

protected:
	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);
	void GoUrl(UINT id);

	acWindow::CStatic urlAngelCode;
	acWindow::CStatic urlLibPng;
	acWindow::CStatic urlLibJpeg;
	acWindow::CStatic urlZLib;
	acWindow::CStatic urlSquish;
};

#endif