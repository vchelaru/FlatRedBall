#ifndef IMAGEWND_H
#define IMAGEWND_H

#include "acwin_window.h"
#include "ac_image.h"

class CFontGen;

class cImageWnd : public acWindow::CWindow
{
public:
	cImageWnd();

	int Create(acWindow::CWindow *parent, CFontGen *fontGen);
	void CopyImage(cImage *img);

protected:
	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);

	void OnPaint();
	void OnSaveAs();
	void OnSize();
	void OnScroll(UINT scrollBar, UINT action);
	void OnScale(float newScale);
	void OnInitMenuPopup(HMENU hMenu, int pos, BOOL isWindowMenu);
	void OnChangePage(int direction);

	void UpdateScrollBars();
	void ProcessImage();

	int GetHorzScroll();
	int GetVertScroll();

	void Draw();
	void DrawImage(int x, int y);

	cImage buffer;
	cImage originalImage;
	cImage image;
	float scale;
	bool viewAlpha;
	bool viewTiled;

	acWindow::CWindow *parent;
	CFontGen *fontGen;

	int page;
	int chnl;
};

#endif