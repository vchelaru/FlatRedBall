#ifndef CHARWIN_H
#define CHARWIN_H

#include <string>
using std::string;
#include <vector>
using std::vector;

#include "acwin_window.h"
#include "acwin_listview.h"
#include "acwin_statusbar.h"
#include "imagewnd.h"
#include "fontgen.h"

class CImageMgr;

class CCharWin : public acWindow::CWindow
{
public:
	CCharWin();
	~CCharWin();

	int Create(int width, int height);
	void Draw();

	void GetCharGridRect(RECT *rc);

protected:
	friend class CImageMgr;

	void OnChooseFont();
	void OnExportOptions();
	void OnVisualize();
	void OnSaveAs();
	void OnLoadConfiguration();
	void OnSaveConfiguration();
	void OnAbout();
	void OnSize();
	void OnInitMenuPopup(HMENU menu, int pos, BOOL isWindowMenu);
	void OnRButtonDown(int x, int y);
	void OnSelectCharsFromFile();
	void OnTimer();

	void PrepareView();

	void DrawUnicode(HDC dc, RECT &rc, TEXTMETRIC &tm);
	void DrawAnsi(HDC dc, RECT &rc, TEXTMETRIC &tm);

	void UpdateStatus();
	void UpdateSubsetsSelection();

	void VisualizeAfterFinishedGenerating();
	void SaveFontAfterFinishedGenerating();

	int GetCharFromPos(int x, int y);

	string GetDefaultConfig();

    LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);

	cImageWnd            *wnd;
	acWindow::CListView  *listView;
	acWindow::CStatusBar *statusBar;
	CImageMgr            *imageMgr;

	bool selectMode;
	int unicodeSubset;
	CFontGen *fontGen;
	bool isGenerating;
	int whenGenerateIsFinished;
	string saveFontName;
};

#endif