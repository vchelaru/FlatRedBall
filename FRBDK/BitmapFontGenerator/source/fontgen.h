#ifndef FONTGEN_H
#define FONTGEN_H

#include <windows.h>

#include <string>
using std::string;
#include <vector>
using std::vector;

#include "fontpage.h"

static const int maxUnicodeChar = 0x10FFFF;
class CFontChar;

struct SSubset
{
	SSubset() {charBegin = 0; charEnd = 0; available = false; selected = -1;}

	string name;
	int    charBegin;
	int    charEnd;
	bool   available;
	int    selected;
};

struct SIconImage
{
	SIconImage() {image = 0;}
	~SIconImage() {if( image ) delete image;}
	string  fileName;
	int     id;
	cImage *image;
	int     xoffset;
	int     yoffset;
	int     advance;
};

enum EChnlValues
{
	e_glyph,
	e_outline,
	e_glyph_outline,
	e_zero,
	e_one,
};

class CFontGen
{
public:
	CFontGen();
	~CFontGen();

	// Unicode subsets
	unsigned int   GetNumUnicodeSubsets();
	const SSubset *GetUnicodeSubset(unsigned int set);

	// Generate font pages asynchronously
	int     GeneratePages(bool async = true);
	void    Abort();
	int     GetStatus();
	int     GetStatusCounter();

	// Icon images
	int     AddIconImage(const char *file, int id, int xoffset, int yoffset, int advance);
	int     GetIconImageCount();
	int     GetIconImageInfo(int n, string &filename, int &id, int &xoffset, int &yoffset, int &advance);
	int     DeleteIconImage(int id);
	int     UpdateIconImage(int oldId, int id, const char *file, int xoffset, int yoffset, int advance);
	void    ClearIconImages();
	bool    IsImage(int id);

	// Select individual characters
	bool    IsSelected(int charIdx);
	int     SetSelected(int charIdx, bool set);
	int     SelectSubset(int subset, bool set);
	int     ClearAll();
	int     IsSubsetSelected(int subset);
	bool    IsDisabled(int charIdx);
	bool    DidNotFit(int charIdx);
	bool    IsOutputInvalidCharGlyphSet();
	int     SetOutputInvalidCharGlyph(bool set);
	int     GetNumCharsSelected();
	int     GetNumCharsAvailable();
	int     SelectCharsFromFile(const char *filename);

	// Failed characters
	int     GetNumFailedChars();
	int     FindNextFailedCharacterSubset(int startSubset);
	void    ClearFailedCharacters();
	int     SubsetFromChar(int ch);

	// Font properties
	string  GetFontName();           int SetFontName(string &name);
	int     GetCharSet();            int SetCharSet(int charSet);
	int     GetFontSize();           int SetFontSize(int fontSize);
	bool    IsBold();                int SetBold(bool set);
	bool    IsItalic();              int SetItalic(bool set);
	int     GetAntiAliasingLevel();  int SetAntiAliasingLevel(int level);
	bool    IsUsingSmoothing();      int SetUseSmoothing(bool set);
	bool    IsUsingUnicode();        int SetUseUnicode(bool set);

	// Character padding and spacing
	int     GetPaddingDown();        int SetPaddingDown(int pad);
	int     GetPaddingUp();          int SetPaddingUp(int pad);
	int     GetPaddingLeft();        int SetPaddingLeft(int pad);
	int     GetPaddingRight();       int SetPaddingRight(int pad);
	int     GetSpacingHoriz();       int SetSpacingHoriz(int space);
	int     GetSpacingVert();        int SetSpacingVert(int space);
	int     GetScaleHeight();        int SetScaleHeight(int scale);
									
	// Output font file				
	int     GetOutWidth();           int SetOutWidth(int width);
	int     GetOutHeight();          int SetOutHeight(int height);
	int     GetOutBitDepth();        int SetOutBitDepth(int bitDepth);
	int     GetFontDescFormat();     int SetFontDescFormat(int format);
	bool    Is4ChnlPacked();         int Set4ChnlPacked(bool set);
	string  GetTextureFormat();      int SetTextureFormat(string &format);
	int     GetTextureCompression(); int SetTextureCompression(int compression);
	int     GetAlphaChnl();          int SetAlphaChnl(int value);
	int     GetRedChnl();            int SetRedChnl(int value);
	int     GetGreenChnl();          int SetGreenChnl(int value);
	int     GetBlueChnl();           int SetBlueChnl(int value);
	bool    IsAlphaInverted();       int SetAlphaInverted(bool set);
	bool    IsRedInverted();         int SetRedInverted(bool set);
	bool    IsGreenInverted();       int SetGreenInverted(bool set);
	bool    IsBlueInverted();        int SetBlueInverted(bool set);

	// Outline
	int     GetOutlineThickness();   int SetOutlineThickness(int thickness);

	// Call this after updating the font properties
	int     Prepare();

	// A helper function for creating the font object
	HFONT   CreateFont(int fontSize);

	// Visualize pages
	int     GetNumPages();
	cImage *GetPageImage(int page, int channel);

	// Save the font to disk
	int     SaveFont(const char *filename);

	// Configuration
	int     SaveConfiguration(const char *filename);
	int     LoadConfiguration(const char *filename);

protected:
	friend class CFontPage;

	void ResetFont();
	void ClearPages();
	int  CreatePage();
	void ClearSubsets();
	void DetermineExistingChars();

	static void __cdecl GenerateThread(CFontGen *fontGen);
	void InternalGeneratePages();

	bool fontChanged;

	bool isWorking;
	bool stopWorking;
	int  status;
	int  counter;
	bool disableBoxChars;
	bool outputInvalidCharGlyph;
	bool arePagesGenerated;

	// Font properties
	string fontName;
	int    charSet;
	int    fontSize;
	int    aa;
	int    scaleH;
	bool   useSmoothing;
	bool   isBold;
	bool   isItalic;
	bool   useUnicode;

	// Char alignment options
	int  paddingDown;
	int  paddingUp;
	int  paddingRight;
	int  paddingLeft;	
	int  spacingHoriz;
	int  spacingVert;

	// File output options
	int    outWidth;
	int    outHeight;
	int    outBitDepth;
	int    fontDescFormat;
	bool   fourChnlPacked;
	string textureFormat;
	int    textureCompression;
	int    alphaChnl;
	int    redChnl;
	int    greenChnl;
	int    blueChnl;
	bool   invA;
	bool   invR;
	bool   invG;
	bool   invB;

	// Outline
	int    outlineThickness;

	// Characters
	int  numCharsSelected;
	int  numCharsAvailable;
	bool disabled[maxUnicodeChar+1];
	bool selected[maxUnicodeChar+1];
	bool noFit[maxUnicodeChar+1];
	CFontChar *chars[maxUnicodeChar+1];
	CFontChar *invalidCharGlyph;

	// Font textures
	vector<CFontPage *> pages;

	// Icon images
	vector<SIconImage *> iconImages;

	// Available character subsets
	vector<SSubset *> subsets;
	unsigned int lastFoundSubset;
};

#endif