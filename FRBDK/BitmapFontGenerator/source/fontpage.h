#ifndef FONTPAGE_H
#define FONTPAGE_H

#include <vector>
#include "ac_image.h"

class CFontChar;
class CFontGen;

struct SHole
{
	int x;
	int y;
	int w;
	int h;
	int chnl;
};

class CFontPage
{
public:
	CFontPage(CFontGen *gen, int id, int width, int height, int spacingH, int spacingV);
	~CFontPage();

	void    SetPadding(int left, int up, int right, int down);
	void    SetIntendedFormat(int bitDepth, bool fourChnlPacked, int a, int r, int g, int b);

	void    AddChars(CFontChar **chars, int count);

	void    GeneratePreviewTexture(int channel);
	void    GenerateOutputTexture();

	cImage *GetPageImage();

protected:
	void    AddChar(int x, int y, CFontChar *ch, int channel);
	int     AddChar(CFontChar *ch, int channel);
	void    SortList(CFontChar **ch, int *indices, int count);
	void    AddCharsToPage(CFontChar **ch, int count, bool colored, int channel);
	int     GetNextIdealImageWidth();

	CFontGen *gen;

	int pageId;
	cImage *pageImg;
	int *heights[4];
	int currX;
	int spacingH;
	int spacingV;

	int paddingRight;
	int paddingLeft;
	int paddingUp;
	int paddingDown;

	int  bitDepth;
	bool fourChnlPacked;
	int  alphaChnl;
	int  redChnl;
	int  greenChnl;
	int  blueChnl;

	std::vector<CFontChar*> chars;
	std::vector<SHole> holes;
};

#endif