#ifndef EXPORTDLG_H
#define EXPORTDLG_H

#include <string>
using std::string;

#include "acwin_dialog.h"

class CExportDlg : virtual public acWindow::CDialog
{
public:
	CExportDlg();

	int DoModal(acWindow::CWindow *parent);

	int paddingUp;
	int paddingDown;
	int paddingRight;
	int paddingLeft;
	int spacingHoriz;
	int spacingVert;

	int width;
	int height;
	int bitDepth;
	bool fourChnlPacked;
	int alphaChnl;
	int redChnl;
	int greenChnl;
	int blueChnl;
	bool invA;
	bool invR;
	bool invG;
	bool invB;

	int fontDescFormat;

	string textureFormat;
	int textureCompression;

protected:
	void OnInit();
	void GetOptions();

	void OnTextureChange();
	void OnPresetChange();

	void EnableWidgets();

	LRESULT MsgProc(UINT msg, WPARAM wParam, LPARAM lParam);
};

#endif