#ifndef UNICODE_H
#define UNICODE_H

#include <string>
#include <vector>
#include <Usp10.h>
using std::string;
using std::vector;

// Interesting links
//
// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/intl/unicode_63ub.asp
// http://unicode.org/charts/
// http://www.i18nguy.com/surrogates.html
// http://msdn2.microsoft.com/en-us/library/ms776414.aspx
// http://www.code2000.net
// http://www.mihai-nita.net/article.php?artID=charmapex
// http://www.microsoft.com/typography/otspec/cmap.htm

struct UnicodeSubset_t
{
	const char *name;
	unsigned int beginChar;
	unsigned int endChar;
};

extern const UnicodeSubset_t UnicodeSubsets[];
extern const int numUnicodeSubsets;

string GetCharSetName(int charSet);
int GetCharSet(const char *charSetName);
int GetSubsetFromChar(unsigned int chr);

int DoesUnicodeCharExist(HDC dc, SCRIPT_CACHE *sc, UINT ch);
int GetUnicodeCharABCWidths(HDC dc, SCRIPT_CACHE *sc, UINT ch, ABC *abc);
int GetUnicodeGlyphIndex(HDC dc, SCRIPT_CACHE *sc, UINT ch);

void GetKerningPairsFromGPOS(HDC dc, vector<KERNINGPAIR> &pairs, vector<UINT> &chars);

#endif