#ifndef ICONIMAGEDLG_H
#define ICONIMAGEDLG_H

#include <string>
using std::string;

#include "acwin_dialog.h"

class CIconImageDlg : virtual public acWindow::CDialog
{
public:
	CIconImageDlg();

	int DoModal(acWindow::CWindow *parent);

	string fileName;
	int id;
	int xoffset;
	int yoffset;
	int advance;

protected:
	void OnInit();
	void GetOptions();
	void OnBrowse();

	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);
};

#endif