#ifndef IMAGEMGR_H
#define IMAGEMGR_H

#include "acwin_window.h"
#include "acwin_listview.h"

class CCharWin;
class CFontGen;

class CImageMgr : public acWindow::CWindow
{
public:
	CImageMgr();
	~CImageMgr();

	int Create(CCharWin *parent, CFontGen *gen);

	void RefreshList();

protected:
    LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);

	void OnSize();
	void OnImportImage();
	void OnInitMenuPopup(HMENU menu, int pos, BOOL isWindowMenu);
	void OnDeleteSelected();
	void OnEditImage();

	CCharWin *parent;
	CFontGen *fontGen;
	acWindow::CListView  *listView;
};

#endif