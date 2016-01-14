#ifndef FONTCHAR_H
#define FONTCHAR_H

#include "ac_image.h"

class CFontChar
{
public:
	CFontChar();
	~CFontChar();

	void DrawChar(HFONT font, int id, int scaleH, int aa, bool useUnicode, bool useSmoothing);
	void DrawInvalidCharGlyph(HFONT font, int scaleH, int aa, bool useUnicode, bool useSmoothing);
	void AddOutline(int thickness);

	void DrawGlyph(HFONT font, int glyph, int scaleH, int aa, bool useUnicode, bool useSmoothing);

	void CreateFromImage(int id, cImage *image, int xoffset, int yoffset, int advance);

	int m_id;

	int m_x;
	int m_y;
	int m_width;
	int m_height;
	int m_yoffset;
	int m_xoffset;
	int m_advance;
	int m_page;
	int m_chnl;

	bool HasOutline();
	BYTE GetPixelValue(int x, int y, int encoding);

	bool m_colored;
	bool m_isChar;

	cImage *m_charImg;
};

#endif