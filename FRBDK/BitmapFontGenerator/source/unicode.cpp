#include <windows.h>
#include <stdio.h>
#include <assert.h>

#include "dynamic_funcs.h"
#include "ac_string_util.h"
#include "acutil_unicode.h"

#include "unicode.h"

// These are the defined character sets from the Unicode 5.1 standard
const UnicodeSubset_t UnicodeSubsets[] = {
// Plane 0 - Base Multilingual Plane
{"Latin + Latin Supplement"                 , 0x0000  , 0x00FF},
{"Latin Extended A"                         , 0x0100  , 0x017F},
{"Latin Extended B"                         , 0x0180  , 0x024F},
{"IPA Extensions"                           , 0x0250  , 0x02AF},
{"Spacing Modifier Letters"                 , 0x02B0  , 0x02FF},
{"Combining Diacritical Marks"              , 0x0300  , 0x036F},
{"Greek and Coptic"                         , 0x0370  , 0x03FF},
{"Cyrillic"                                 , 0x0400  , 0x04FF},
{"Cyrillic Supplement"                      , 0x0500  , 0x052F},
{"Armenian"                                 , 0x0530  , 0x058F},
{"Hebrew"                                   , 0x0590  , 0x05FF},
{"Arabic"                                   , 0x0600  , 0x06FF},
{"Syriac"                                   , 0x0700  , 0x074F},
{"Arabic Supplement"                        , 0x0750  , 0x077F},
{"Thaana"                                   , 0x0780  , 0x07BF},
{"N'Ko"                                     , 0x07C0  , 0x07FF},
{"(0x0800 - 0x08FF)"                        , 0x0800  , 0x08FF},
{"Devanagari"                               , 0x0900  , 0x097F},
{"Bengali"                                  , 0x0980  , 0x09FF},
{"Gurmukhi"                                 , 0x0A00  , 0x0A7F},
{"Gujarati"                                 , 0x0A80  , 0x0AFF},
{"Oriya"                                    , 0x0B00  , 0x0B7F},
{"Tamil"                                    , 0x0B80  , 0x0BFF},
{"Telugu"                                   , 0x0C00  , 0x0C7F},
{"Kannada"                                  , 0x0C80  , 0x0CFF},
{"Malayalam"                                , 0x0D00  , 0x0D7F},
{"Sinhala"                                  , 0x0D80  , 0x0DFF},
{"Thai"                                     , 0x0E00  , 0x0E7F},
{"Lao"                                      , 0x0E80  , 0x0EFF},
{"Tibetan"                                  , 0x0F00  , 0x0FFF},
{"Myanmar"                                  , 0x1000  , 0x109F},
{"Georgian"                                 , 0x10A0  , 0x10FF},
{"Hangul Jamo"                              , 0x1100  , 0x11FF},
{"Ethiopic"                                 , 0x1200  , 0x12BF},
{"(0x12C0 - 0x137F)"                        , 0x12C0  , 0x137F},
{"Ethiopic Supplement"                      , 0x1380  , 0x139F},
{"Cherokee"                                 , 0x13A0  , 0x13FF},
{"Canadian Aboriginal Syllabics"            , 0x1400  , 0x167F},
{"Ogham"                                    , 0x1680  , 0x169F},
{"Runic"                                    , 0x16A0  , 0x16FF},
{"Tagalog"                                  , 0x1700  , 0x171F},
{"Hanunoo"                                  , 0x1720  , 0x173F},
{"Buhid"                                    , 0x1740  , 0x175F},
{"Tagbanwa"                                 , 0x1760  , 0x177F},
{"Khmer"                                    , 0x1780  , 0x17FF},
{"Mongolian"                                , 0x1800  , 0x18AF},
{"(0x18B0 - 0x18FF)"                        , 0x18B0  , 0x18FF},
{"Limbu"                                    , 0x1900  , 0x194F},
{"Tai Le"                                   , 0x1950  , 0x197F},
{"New Tai Lue"                              , 0x1980  , 0x19DF},
{"Khmer Symbols"                            , 0x19E0  , 0x19FF},
{"Buginese"                                 , 0x1A00  , 0x1A1F},
{"(0x1A20 - 0x1BFF)"                        , 0x1A20  , 0x1AFF},
{"Balinese"                                 , 0x1B00  , 0x1B7F},
{"Sundanese"                                , 0x1B80  , 0x1BBF},
{"(0x1BC0 - 0x1BFF)"                        , 0x1BC0  , 0x1BFF},
{"Lepcha"                                   , 0x1C00  , 0x1C4F},
{"Ol Chiki"                                 , 0x1C50  , 0x1C7F},
{"(0x1C80 - 0x1DBF)"                        , 0x1C80  , 0x1CFF},
{"Phonetic Extensions"                      , 0x1D00  , 0x1D7F},
{"Phonetic Extensions Supplement"           , 0x1D80  , 0x1DBF},
{"Combining Diacritical Marks Supplement"   , 0x1DC0  , 0x1DFF},
{"Latin Extended Additional"                , 0x1E00  , 0x1EFF},
{"Greek Extended"                           , 0x1F00  , 0x1FFF},
{"General Punctuation"                      , 0x2000  , 0x206F},
{"Subscripts and Superscripts"              , 0x2070  , 0x209F},
{"Currency Symbols"                         , 0x20A0  , 0x20CF},
{"Combining Diacritical Marks for Symbols"  , 0x20D0  , 0x20FF},
{"Letterlike Symbols"                       , 0x2100  , 0x214F},
{"Number Forms"                             , 0x2150  , 0x218F},
{"Arrows"                                   , 0x2190  , 0x21FF},
{"Mathematical Operators"                   , 0x2200  , 0x22FF},
{"Miscellaneous Technical"                  , 0x2300  , 0x23FF},
{"Control Pictures"                         , 0x2400  , 0x243F},
{"Optical Character Recognition"            , 0x2440  , 0x245F},
{"Enclosed Alphanumerics"                   , 0x2460  , 0x24FF},
{"Box Drawing"                              , 0x2500  , 0x257F},
{"Block Elements"                           , 0x2580  , 0x259F},
{"Geometric Shapes"                         , 0x25A0  , 0x25FF},
{"Miscellaneous Symbols"                    , 0x2600  , 0x26FF},
{"Dingbats"                                 , 0x2700  , 0x27BF},
{"Miscellaneous Mathematical Symbols A"     , 0x27C0  , 0x27EF},
{"Supplemental Arrows A"                    , 0x27F0  , 0x27FF},
{"Braille"                                  , 0x2800  , 0x28FF},
{"Supplemental Arrows B"                    , 0x2900  , 0x297F},
{"Miscellaneous Mathematical Symbols B"     , 0x2980  , 0x29FF},
{"Supplemental Mathematical Operators"      , 0x2A00  , 0x2AFF},
{"Miscellaneous Symbols and Arrows"         , 0x2B00  , 0x2BFF},
{"Glagolitic"                               , 0x2C00  , 0x2C5F},
{"Latin Extended C"                         , 0x2C60  , 0x2C7F},
{"Coptic"                                   , 0x2C80  , 0x2CFF}, 
{"Georgian Supplement"                      , 0x2D00  , 0x2D2F},
{"Tifinagh"                                 , 0x2D30  , 0x2D7F},
{"Ethiopic Extended"                        , 0x2D80  , 0x2DDF},
{"Cyrillic Extended A"                      , 0x2DE0  , 0x2DFF},
{"Supplemental Punctuation"                 , 0x2E00  , 0x2E7F},
{"CJK Radicals Supplement"                  , 0x2E80  , 0x2EFF},
{"KangXi Radicals"                          , 0x2F00  , 0x2FDF},
{"(0x2FE0 - 0x2FEF)"                        , 0x2FE0  , 0x2FEF},
{"Ideographic Description"                  , 0x2FF0  , 0x2FFF},
{"CJK Symbols and Punctuation"              , 0x3000  , 0x303F},
{"Hiragana"                                 , 0x3040  , 0x309F},
{"Katakana"                                 , 0x30A0  , 0x30FF},
{"Bopomofo"                                 , 0x3100  , 0x312F},
{"Hangul Compatibility Jamo"                , 0x3130  , 0x318F},
{"Kanbun"                                   , 0x3190  , 0x319F},
{"Extended Bopomofo"                        , 0x31A0  , 0x31BF},
{"CJK Strokes"                              , 0x31C0  , 0x31EF},
{"Katakana Phonetic Extensions"             , 0x31F0  , 0x31FF},
{"Enclosed CJK Letters and Months"          , 0x3200  , 0x32FF},
{"CJK Compatibility"                        , 0x3300  , 0x33FF},
{"CJK Unified Ideographs Extension A"       , 0x3400  , 0x4DBF},
{"Yijing Hexagram Symbols"                  , 0x4DC0  , 0x4DFF},
{"CJK Unified Ideographs"                   , 0x4E00  , 0x9FFF},
{"Yi"                                       , 0xA000  , 0xA48F},
{"Yi Radicals"                              , 0xA490  , 0xA4CF},
{"(0xA4D0 - 0xA4FF)"                        , 0xA4D0  , 0xA4FF},
{"Vai"                                      , 0xA500  , 0xA59F},
{"(0xA600 - 0xA63F)"                        , 0xA600  , 0xA63F},
{"Cyrillic Extended B"                      , 0xA640  , 0xA69F},
{"(0xA6A0 - 0xA6FF)"                        , 0xA6A0  , 0xA6FF},
{"Modifier Tone Letters"                    , 0xA700  , 0xA71F},
{"Latin Extended D"                         , 0xA720  , 0xA7FF},
{"Syloti Nagri"                             , 0xA800  , 0xA82F},
{"(0xA830 - 0xA83F)"                        , 0xA830  , 0xA83F},
{"Phags-Pa"                                 , 0xA840  , 0xA87F},
{"Saurashtra"                               , 0xA880  , 0xA8DF},
{"(0xA8E0 - 0xA8FF)"                        , 0xA8E0  , 0xA8FF},
{"Kayah Li"                                 , 0xA900  , 0xA92F},
{"Rejang"                                   , 0xA930  , 0xA95F},
{"(0xA960 - 0xA9FF)"                        , 0xA960  , 0xA9FF},
{"Cham"                                     , 0xAA00  , 0xAA5F},
{"(0xAA60 - 0xABFF)"                        , 0xAA60  , 0xABFF},
{"Hangul"                                   , 0xAC00  , 0xD7A3},
{"(0xD7A4 - 0xD7FF)"                        , 0xD7A4  , 0xD7FF},
{"(High Surrogates)"                        , 0xD800  , 0xDBFF},
{"(Low Surrogates)"                         , 0xDC00  , 0xDFFF},
{"Private Use Area"                         , 0xE000  , 0xF8FF},
{"CJK Compatibility Ideographs"             , 0xF900  , 0xFAFF},
{"Alphabetical Presentation Forms"          , 0xFB00  , 0xFB4F},
{"Arabic Presentation Forms A"              , 0xFB50  , 0xFDFF},
{"Variation Selectors"                      , 0xFE00  , 0xFE0F},
{"Vertical Forms"                           , 0xFE10  , 0xFE1F},
{"Combining Half Marks"                     , 0xFE20  , 0xFE2F},
{"CJK Compatibility Forms"                  , 0xFE30  , 0xFE4F},
{"Small Form Variants"                      , 0xFE50  , 0xFE6F}, 
{"Arabic Presentation Forms B"              , 0xFE70  , 0xFEFF},
{"Halfwidth and Fullwidth Forms"            , 0xFF00  , 0xFFEF},
{"Specials"                                 , 0xFFF0  , 0xFFFD},
{"(Reserved)"                               , 0xFFFE  , 0xFFFF},

// Plane 1
{"Linear B Syllabary"                       , 0x10000 , 0x1007F},
{"Linear B Ideograms"                       , 0x10080 , 0x100FF},
{"Aegean Numbers"                           , 0x10100 , 0x1013F},
{"Ancient Greek Numbers"                    , 0x10140 , 0x1018F},
{"Ancient Symbols"                          , 0x10190 , 0x101CF},
{"Phaistos Disc"                            , 0x101D0 , 0x101FF},
{"(0x10200 - 0x1027F)"                      , 0x10200 , 0x1027F},
{"Lycian"                                   , 0x10280 , 0x1029F},
{"Carian"                                   , 0x102A0 , 0x102DF},
{"(0x102E0 - 0x102FF)"                      , 0x102E0 , 0x102FF},
{"Old Italic"                               , 0x10300 , 0x1032F},
{"Gothic"                                   , 0x10330 , 0x1034F}, 
{"(0x10350 - 0x1037F)"                      , 0x10350 , 0x1037F},
{"Ugaritic"                                 , 0x10380 , 0x1039F},
{"Old Persian"                              , 0x103A0 , 0x103DF},
{"(0x103E0 - 0x103FF)"                      , 0x103E0 , 0x103FF},
{"Deseret"                                  , 0x10400 , 0x1044F}, 
{"Shavian"                                  , 0x10450 , 0x1047F},
{"Osmanya"                                  , 0x10480 , 0x104AF},
{"(0x104B0 - 0x107FF)"                      , 0x104B0 , 0x107FF},
{"Cypriot Syllabary"                        , 0x10800 , 0x1083F},
{"(0x10840 - 0x108FF)"                      , 0x10840 , 0x108FF},
{"Phoenician"                               , 0x10900 , 0x1091F},
{"Lydian"                                   , 0x10920 , 0x1093F},
{"(0x10940 - 0x109FF)"                      , 0x10940 , 0x109FF},
{"Kharoshthi"                               , 0x10A00 , 0x10A5F},
{"(0x10A60 - 0x11FFF)"                      , 0x10920 , 0x11FFF},
{"Cuneiform"                                , 0x12000 , 0x123FF},
{"Cuneiform Numbers and Punctuation"        , 0x12400 , 0x1247F},
{"(0x12480 - 0x1CFFF)"                      , 0x12480 , 0x1CFFF},
{"Byzantine Musical Symbols"                , 0x1D000 , 0x1D0FF},
{"Musical Symbols"                          , 0x1D100 , 0x1D1FF},
{"Ancient Greek Musical Notation"           , 0x1D200 , 0x1D24F},
{"(0x1D250 - 0x1D3FF)"                      , 0x1D250 , 0x1D2FF},
{"Tai Xuan Jing Symbols"                    , 0x1D300 , 0x1D35F},
{"Counting Rod Numerals"                    , 0x1D360 , 0x1D37F},
{"(0x1D380 - 0x1D3FF)"                      , 0x1D380 , 0x1D3FF},
{"Mathematical Alphanumeric Symbols"        , 0x1D400 , 0x1D7FF},
{"(0x1D800 - 0x1EFFF)"                      , 0x1D800 , 0x1EFFF},
{"Mahjong Tiles"                            , 0x1F000 , 0x1F02F},
{"Domino Tiles"                             , 0x1F030 , 0x1F09F},
{"(0x1F0A0 - 0x1FFFF)"                      , 0x1F0A0 , 0x1FFFD},
{"(Reserved)"                               , 0x1FFFE , 0x1FFFF},

// Plane 2
{"CJK Unified Ideographs Extension B"       , 0x20000 , 0x2A6DF},
{"(0x2A6E0 - 0x2F7FF)"                      , 0x2A6E0 , 0x2F7FF},
{"CJK Compatibility Ideographs Supplement"  , 0x2F800 , 0x2FA1F}, 
{"(0x2FA20 - 0x2FFFD)"                      , 0x2FA20 , 0x2FFFD},
{"(Reserved)"                               , 0x2FFFE , 0x2FFFF},

// Plane 3
{"(0x30000 - 0x3FFFD)"                      , 0x30000 , 0x3FFFD},
{"(Reserved)"                               , 0x3FFFE , 0x3FFFF},

// Plane 4
{"(0x40000 - 0x4FFFD)"                      , 0x40000 , 0x4FFFD},
{"(Reserved)"                               , 0x4FFFE , 0x4FFFF},

// Plane 5
{"(0x50000 - 0x5FFFD)"                      , 0x50000 , 0x5FFFD},
{"(Reserved)"                               , 0x5FFFE , 0x5FFFF},

// Plane 6
{"(0x60000 - 0x6FFFD)"                      , 0x60000 , 0x6FFFD},
{"(Reserved)"                               , 0x6FFFE , 0x6FFFF},

// Plane 7
{"(0x70000 - 0x7FFFD)"                      , 0x70000 , 0x7FFFD},
{"(Reserved)"                               , 0x7FFFE , 0x7FFFF},

// Plane 8
{"(0x80000 - 0x8FFFD)"                      , 0x80000 , 0x8FFFD},
{"(Reserved)"                               , 0x8FFFE , 0x8FFFF},

// Plane 9
{"(0x90000 - 0x9FFFD)"                      , 0x90000 , 0x9FFFD},
{"(Reserved)"                               , 0x9FFFE , 0x9FFFF},

// Plane 10
{"(0xA0000 - 0xAFFFD)"                      , 0xA0000 , 0xAFFFD},
{"(Reserved)"                               , 0xAFFFE , 0xAFFFF},

// Plane 11
{"(0xB0000 - 0xBFFFD)"                      , 0xB0000 , 0xBFFFD},
{"(Reserved)"                               , 0xBFFFE , 0xBFFFF},

// Plane 12
{"(0xC0000 - 0xCFFFD)"                      , 0xC0000 , 0xCFFFD},
{"(Reserved)"                               , 0xCFFFE , 0xCFFFF},

// Plane 13
{"(0xD0000 - 0xDFFFD)"                      , 0xD0000 , 0xDFFFD},
{"(Reserved)"                               , 0xDFFFE , 0xDFFFF},

// Plane 14
{"Tags"                                     , 0xE0000 , 0xE007F},
{"(0xE0080 - 0xE00FF)"                      , 0xE0080 , 0xE00FF},
{"Variation Selectors Supplement"           , 0xE0100 , 0xE01EF},
{"(0xE01F0 - 0xEFFFD)"                      , 0xE01F0 , 0xEFFFD},
{"(Reserved)"                               , 0xEFFFE , 0xEFFFF},

// Plane 15
{"Private Use (Plane 15)"                   , 0xF0000 , 0xFFFFD}, 
{"(Reserved)"                               , 0xFFFFE , 0xFFFFF},

// Plane 16
{"Private Use (Plane 16)"                   , 0x100000, 0x10FFFD},
{"(Reserved)"                               , 0x10FFFE, 0x10FFFF},
};

const int numUnicodeSubsets = sizeof(UnicodeSubsets)/sizeof(UnicodeSubset_t);

int GetSubsetFromChar(unsigned int chr)
{
	for( int n = 0; n < numUnicodeSubsets; n++ )
	{
		if( chr >= (unsigned)UnicodeSubsets[n].beginChar && chr <= (unsigned)UnicodeSubsets[n].endChar )
			return n;
	}

	return -1;
}

string GetCharSetName(int charSet)
{
	string str;

	switch( charSet )
	{
	case ANSI_CHARSET:
		str = "ANSI";
		break;
	case DEFAULT_CHARSET:
		str = "DEFAULT";
		break;
	case SYMBOL_CHARSET:          
		str = "SYMBOL";
		break;
	case SHIFTJIS_CHARSET:        
		str = "SHIFTJIS";
		break;
	case HANGUL_CHARSET:          
		str = "HANGUL";
		break;
	case GB2312_CHARSET:          
		str = "GB2312";
		break;
	case CHINESEBIG5_CHARSET:     
		str = "CHINESEBIG5";
		break;
	case OEM_CHARSET:             
		str = "OEM";
		break;
	case 130: // JOHAB_CHARSET           
		str = "JOHAB";
		break;
	case 177: // HEBREW_CHARSET          
		str = "HEBREW";
		break;
	case 178: // ARABIC_CHARSET          
		str = "ARABIC";
		break;
	case 161: // GREEK_CHARSET           
		str = "GREEK";
		break;
	case 162: // TURKISH_CHARSET         
		str = "TURKISH";
		break;
	case 163: // VIETNAMESE_CHARSET      
		str = "VIETNAMESE";
		break;
	case 222: // THAI_CHARSET            
		str = "THAI";
		break;
	case 238: // EASTEUROPE_CHARSET      
		str = "EASTEUROPE";
		break;
	case 204: // RUSSIAN_CHARSET         
		str = "RUSSIAN";
		break;
	case 77:  // MAC_CHARSET             
		str = "MAC";
		break;
	case 186: // BALTIC_CHARSET          
		str = "BALTIC";
		break;

	default:
		str = acStringFormat("%d", charSet);
	}
	
	return str;
}

int GetCharSet(const char *charSetName)
{
	string str = charSetName;
	int set = 0;

	if( str == "ANSI" )             set = 0;
	else if( str == "DEFAULT" )     set = 1;
	else if( str == "SYMBOL" )      set = 2;
	else if( str == "SHIFTJIS" )    set = 128;
	else if( str == "HANGUL" )      set = 129;
	else if( str == "GB2312" )      set = 134;
	else if( str == "CHINESEBIG5" ) set = 136;
	else if( str == "OEM" )         set = 255;
	else if( str == "JOHAB" )       set = 130;
	else if( str == "HEBREW" )      set = 177;
	else if( str == "ARABIC" )      set = 178;
	else if( str == "GREEK" )       set = 161;
	else if( str == "TURKISH" )     set = 162;
	else if( str == "VIETNAMESE" )  set = 163;
	else if( str == "THAI" )        set = 222;
	else if( str == "EASTEUROPE" )  set = 238;
	else if( str == "RUSSIAN" )     set = 204;
	else if( str == "MAC" )         set = 77;
	else if( str == "BALTIC" )      set = 186;
	else set = acStringScanInt(charSetName, 10, 0);

	return set;
}

int GetUnicodeGlyphIndex(HDC dc, SCRIPT_CACHE *sc, UINT ch)
{
	SCRIPT_CACHE mySc = 0;
	if( sc == 0 ) sc = &mySc;

	if( ch < 0x10000 )
	{
		// GetGlyphIndices seems to work better than ScriptShape in 
		// the base plane. It reports less missing characters as existing
		WCHAR buf[] = {ch};
		WORD idx;
		int r = fGetGlyphIndicesW(dc, buf, 1, &idx, GGI_MARK_NONEXISTING_GLYPHS);
		if( r == GDI_ERROR || idx == 0xFFFF )
			return -1;

		return idx;
	}

	// Convert the unicode character to a UTF16 encoded 
	// buffer that Uniscribe understands
	WCHAR buf[2];
	int length = acUtility::EncodeUTF16(ch, (unsigned char*)buf, 0);

	// Call ScriptItemize to analyze the unicode string 
	// to find it's logical pieces
	SCRIPT_ITEM items[2];
	int nitems;
	HRESULT hr;
	hr = ScriptItemize(buf, length/2, 2, 0, 0, items, &nitems);
	if( FAILED(hr) )
		return -1;

	// Call ScriptShape to determine the glyphs that will 
	// be used to render each character
	WORD glyphs[10];
	WORD cluster[10];
	int nglyphs;
	SCRIPT_VISATTR attr[10];
	hr = ScriptShape(dc, sc, &buf[0], length/2, 10, &items[0].a, glyphs, cluster, attr, &nglyphs);
	if( FAILED(hr) )
		return -1;

	if( mySc ) 
		ScriptFreeCache(&mySc);

	// Was the glyph found?
	return glyphs[0];
}

int DoesUnicodeCharExist(HDC dc, SCRIPT_CACHE *sc, UINT ch)
{
	int idx = GetUnicodeGlyphIndex(dc, sc, ch);
	
	if( idx < 0 ) 
		return 0;

	SCRIPT_FONTPROPERTIES props;
	props.cBytes = sizeof(props);
	ScriptGetFontProperties(dc, sc, &props);	
	
	if( idx != props.wgDefault )
		return 1;

	return 0;
}

int GetUnicodeCharABCWidths(HDC dc, SCRIPT_CACHE *sc, UINT ch, ABC *abc)
{
	SCRIPT_CACHE mySc = 0;
	if( sc == 0 ) sc = &mySc;

	int idx = GetUnicodeGlyphIndex(dc, sc, ch);
	if( idx < 0 ) 
	{
		if( mySc ) ScriptFreeCache(&mySc);
		return -1;
	}

	HRESULT hr = ScriptGetGlyphABCWidth(dc, sc, idx, abc);
	if( FAILED(hr) )
	{
		if( mySc ) ScriptFreeCache(&mySc);
		return -1;
	}

	if( mySc ) ScriptFreeCache(&mySc);
	return 0;
}

//=================================================================================
// The functions below are all for extracting kerning data from the GPOS table
//
// Reference: http://www.microsoft.com/typography/otspec/gpos.htm
//            http://www.microsoft.com/typography/otspec/otff.htm
//            http://partners.adobe.com/public/developer/opentype/index_table_formats2.html
//

#define TAG(a,b,c,d) ((a) | ((b) << 8) | ((c) << 16) | ((d) << 24))
#define SWAP32(x) ((((x)&0xFF)<<24)|(((x)&0xFF00)<<8)|(((x)&0xFF0000)>>8)|(((x)&0xFF000000)>>24))
#define SWAP16(x) ((((x)&0xFF)<<8)|(((x)&0xFF00)>>8))

#define GETUSHORT(x) WORD(SWAP16(*(WORD*)(x)))
#define GETSHORT(x)  short(SWAP16(*(WORD*)(x)))
#define GETUINT(x)   DWORD(SWAP32(*(DWORD*)(x)))
#define GETINT(x)    int(SWAP32(*(DWORD*)(x)))

UINT GetClassFromClassDef(BYTE *classDef, WORD glyphId)
{
	// Go through the class def to determine in which class the glyph belongs
	WORD classFormat = GETUSHORT(classDef);
	if( classFormat == 1 )
	{
		WORD startGlyph = GETUSHORT(classDef+2);
		WORD glyphCount = GETUSHORT(classDef+4);

		if( startGlyph <= glyphId && glyphId - startGlyph < glyphCount )
			return GETUSHORT(classDef+6+2*(glyphId - startGlyph));
	}
	else if( classFormat == 2 )
	{
		WORD rangeCount = GETUSHORT(classDef+2);
		for( UINT n = 0; n < rangeCount; n++ )
		{
			WORD start = GETUSHORT(classDef+4+6*n);
			WORD end   = GETUSHORT(classDef+6+6*n);
			if( start <= glyphId && end >= glyphId )
				return GETUSHORT(classDef+8+6*n);
		}
	}

	return 0;
}

vector<UINT> GetGlyphsFromClassDef(BYTE *classDef, UINT classId)
{
	// Find the class, and return all the glyphs that are part of it
	vector<UINT> glyphs;

	WORD classFormat = GETUSHORT(classDef);
	if( classFormat == 1 )
	{
		WORD startGlyph = GETUSHORT(classDef+2);
		WORD glyphCount = GETUSHORT(classDef+4);

		for( UINT n = 0; n < glyphCount; n++ )
		{
			if( GETUSHORT(classDef+6+2*n) == classId )
				glyphs.push_back(startGlyph + n);
		}
	}
	else if( classFormat == 2 )
	{
		WORD rangeCount = GETUSHORT(classDef+2);
		for( UINT n = 0; n < rangeCount; n++ )
		{
			WORD start = GETUSHORT(classDef+4+6*n);
			WORD end   = GETUSHORT(classDef+6+6*n);
			if( GETUSHORT(classDef+8+6*n) == classId )
			{
				for( UINT g = start; g <= end; g++ )
					glyphs.push_back(g);
			}
		}
	}

	return glyphs;
}

float DetermineDesignUnitToFontUnitFactor(HDC dc)
{
	OUTLINETEXTMETRIC tm;
	GetOutlineTextMetrics(dc, sizeof(tm), &tm);

	DWORD head = TAG('h','e','a','d');
	UINT size = GetFontData(dc, head, 0, 0, 0);
	if( size != GDI_ERROR )
	{
		vector<BYTE> buffer;
		buffer.resize(size);
		GetFontData(dc, head, 0, &buffer[0], size);

		SHORT xMin = GETUSHORT(&buffer[36]);
		SHORT yMin = GETUSHORT(&buffer[38]);
		SHORT xMax = GETUSHORT(&buffer[40]);
		SHORT yMax = GETUSHORT(&buffer[42]);

		float factor = float(tm.otmrcFontBox.top-tm.otmrcFontBox.bottom)/float(yMax-yMin);
		return factor;
	}

	return 1;
}

void AddKerningPairToList(HDC dc, UINT glyphId1, UINT glyphId2, int kerning, vector<KERNINGPAIR> &pairs, float scaleFactor, UINT *glyphIdToChar)
{
	assert(kerning != 0);

	// Add the kerning pair to the list
	KERNINGPAIR pair;
	pair.wFirst      = glyphIdToChar[glyphId1];
	pair.wSecond     = glyphIdToChar[glyphId2];
	if( pair.wFirst == 0 || pair.wSecond == 0 )
		return;

	// Convert from design units to the selected font size
	float kern = kerning*scaleFactor;
	if( kern < 0 )
		pair.iKernAmount = int(kern-0.5f);
	else
		pair.iKernAmount = int(kern+0.5f);

	// Skip 0 kernings
	if( pair.iKernAmount == 0 )
		return;

	if( pairs.capacity() == pairs.size() && pairs.size() > 1 )
		pairs.reserve(pairs.size() * 2);
	pairs.push_back(pair);
}

UINT GetSizeOfValueType(WORD valueType)
{
	UINT size = 0;
	// TODO: Are these the only flags?
	if( valueType & 1 ) // x placement
		size += 2;
	if( valueType & 2 ) // y placement
		size += 2;
	if( valueType & 4 ) // x advance
		size += 2;
	if( valueType & 8 ) // y advance
		size += 2;
	if( valueType & 16 ) // offset to device x placement
		size += 2;
	if( valueType & 32 ) // offset to device y placement
		size += 2;
	if( valueType & 64 ) // offset to device x advance
		size += 2;
	if( valueType & 128 ) // offset to device y advance
		size += 2;
	return size;
}

short GetXAdvance(BYTE *value, WORD valueType)
{
	if( !(valueType & 4) ) 
		return 0;

	UINT offset = 0;
	if( valueType & 1 ) offset += 2;
	if( valueType & 2 ) offset += 2;

	return GETSHORT(value+offset);
}

void ProcessPairAdjustmentFormat1(HDC dc, BYTE *subTable, vector<KERNINGPAIR> &pairs, UINT *glyphIdToChar, float scaleFactor)
{
	// Defines kerning between two individual glyphs

	WORD coverageOffset = GETUSHORT(subTable+2);
	WORD valueFormat1   = GETUSHORT(subTable+4);
	assert( valueFormat1 == 4 ); // TODO: Must support others
	WORD valueFormat2   = GETUSHORT(subTable+6);
	assert( valueFormat2 == 0 ); // TODO: Must support others
	WORD pairSetCount   = GETUSHORT(subTable+8);

	UINT valuePairSize = GetSizeOfValueType(valueFormat1) + GetSizeOfValueType(valueFormat2);

	// The first glyph id in the pair is found in the coverage table
	// The second glyph id in the pair is found in the PairSet records
	
	BYTE *coverage = subTable + coverageOffset;
	WORD coverageFormat = GETUSHORT(coverage);
	if( coverageFormat == 1 )
	{
		WORD glyphCount = GETUSHORT(coverage+2);
		assert(glyphCount == pairSetCount);
		for( DWORD g = 0; g < glyphCount; g++ )
		{
			WORD glyphId1 = GETUSHORT(coverage+4+2*g);
			if( glyphIdToChar[glyphId1] == 0 )
				continue;

			// For each of the glyph ids we need to search the 
			// PairSets for the matching kerning pairs
			WORD pairSetOffset = GETUSHORT(subTable+10+g*2);
			BYTE *pairSet = subTable + pairSetOffset;
			WORD pairValueCount = GETUSHORT(pairSet);
			for( UINT p = 0; p < pairValueCount; p++ )
			{
				BYTE *pairValue = pairSet + 2 + p*(2+valuePairSize);

				WORD glyphId2 = GETUSHORT(pairValue);
				short xAdv1 = GetXAdvance(pairValue+2, valueFormat1);

				if( xAdv1 != 0 )
				{
					AddKerningPairToList(dc, glyphId1, glyphId2, xAdv1, pairs, scaleFactor, glyphIdToChar);
				}
			}
		}
	}
	else
	{
		WORD rangeCount = GETUSHORT(coverage+2);

		// TODO: Implement this
		assert( false );
	}
}

void ProcessPairAdjustmentFormat2(HDC dc, BYTE *subTable, vector<KERNINGPAIR> &pairs, UINT *glyphIdToChar, float scaleFactor)
{
	// Defines kerning between two classes of glyphs

	WORD coverageOffset  = GETUSHORT(subTable+2);
	WORD valueFormat1    = GETUSHORT(subTable+4);
	assert( valueFormat1 == 4 ); // TODO: must support others
	WORD valueFormat2    = GETUSHORT(subTable+6);
	assert( valueFormat2 == 0 ); // TODO: must support others
	WORD classDefOffset1 = GETUSHORT(subTable+8);
	WORD classDefOffset2 = GETUSHORT(subTable+10);
	WORD classCount1     = GETUSHORT(subTable+12);
	WORD classCount2     = GETUSHORT(subTable+14);

	WORD valuePairSize = GetSizeOfValueType(valueFormat1) + GetSizeOfValueType(valueFormat2);

	// The first glyph id in the pair is found in the coverage table
	// The second glyph id is determined from the class definitions

	vector<WORD> glyph1;

	BYTE *coverage = subTable + coverageOffset;
	WORD coverageFormat = GETUSHORT(coverage);
	if( coverageFormat == 1 )
	{
		WORD glyphCount = GETUSHORT(coverage+2);
		glyph1.reserve(glyphCount);

		for( DWORD g = 0; g < glyphCount; g++ )
		{
			WORD glyphId = GETUSHORT(coverage+4+2*g);
			if( glyphIdToChar[glyphId] )
				glyph1.push_back(glyphId);
		}
	}
	else
	{
		WORD rangeCount = SWAP16(*(WORD*)(coverage+2));

		// Expand the ranges into the glyph1 array
		for( UINT n = 0; n < rangeCount; n++ )
		{
			WORD start = GETUSHORT(coverage+4+n*6);
			WORD end   = GETUSHORT(coverage+6+n*6);
			WORD startCoverageIndex = GETUSHORT(coverage+8+n*6);

			// TODO: Reserve space for all the glyph ids to reduce number of individual allocations

			for( UINT g = start; g <= end; g++ )
			{
				if( glyphIdToChar[g] )
					glyph1.push_back(g);
			}
		}
	}

	for( UINT g = 0; g < glyph1.size(); g++ )
	{
		// What class is this glyph?
		// Need a function for obtaining the class from a ClassDef
		UINT c1 = GetClassFromClassDef(subTable + classDefOffset1, glyph1[g]);

		assert( c1 < classCount1 );
	
		if( c1 < classCount1 )
		{
			BYTE *c1List = subTable + 16 + c1*classCount2*valuePairSize;

			// For each of the classes 
			for( UINT c2 = 0; c2 < classCount2; c2++ )
			{
				// Enumerate the glyphs that are part of the classes
				vector<UINT> glyph2 = GetGlyphsFromClassDef(subTable + classDefOffset2, c2);

				BYTE *valuePair = c1List + valuePairSize*c2;

				short xAdv1 = GetXAdvance(valuePair, valueFormat1);
				if( xAdv1 != 0 )
				{
					// Add a kerning pair for each combination of glyphs in each of the classes
					for( UINT n = 0; n < glyph2.size(); n++ )
					{
						AddKerningPairToList(dc, glyph1[g], glyph2[n], xAdv1, pairs, scaleFactor, glyphIdToChar);
					}
				}
			}
		}
	}
}

void ProcessKernFeature(HDC dc, BYTE *featureRecord, BYTE *featureList, BYTE *lookupList, vector<KERNINGPAIR> &pairs, UINT *glyphIdToChar, float scaleFactor)
{
	WORD offset = GETUSHORT(featureRecord+4);

	BYTE *feature = featureList + offset;
	WORD featureParams = GETUSHORT(feature);
	WORD lookupCount = GETUSHORT(feature+2);
	WORD allLookupCount = GETUSHORT(lookupList);
	
	for( DWORD i = 0; i < lookupCount; i++ )
	{
		WORD lookupIndex = GETUSHORT(feature+4+i*2);

		// Determine the features that apply (look for kerning pairs)

		// Find the adjustments in the lookup table
		if( lookupIndex < allLookupCount )
		{
			WORD lookupOffset = GETUSHORT(lookupList + 2 + lookupIndex*2);

			BYTE *lookup = lookupList + lookupOffset;

			WORD lookupType = GETUSHORT(lookup);
			WORD lookupFlag = GETUSHORT(lookup+2);
			WORD subTableCount = GETUSHORT(lookup+4);

			for( UINT s = 0; s < subTableCount; s++ )
			{
				WORD offset = GETUSHORT(lookup + 6 + s*2);
				BYTE *subTable = lookup + offset;

				WORD realLookupType = lookupType;

				if( lookupType == 9 ) // extension positioning
				{
					WORD posFormat = GETUSHORT(subTable);
					assert( posFormat == 1 ); // reserved
					WORD extensionLookupType = GETUSHORT(subTable+2);
					DWORD extensionOffset = GETUINT(subTable+4);

					realLookupType = extensionLookupType;
					subTable = subTable + extensionOffset;
				}

				if( realLookupType == 2 ) // pair adjustment
				{
					WORD posFormat    = GETUSHORT(subTable);
					if( posFormat == 1 )
						ProcessPairAdjustmentFormat1(dc, subTable, pairs, glyphIdToChar, scaleFactor);
					else if( posFormat == 2 )
						ProcessPairAdjustmentFormat2(dc, subTable, pairs, glyphIdToChar, scaleFactor);
					else
						assert(false);
				}
			}
		}
	}
}

void GetKerningPairsFromGPOS(HDC dc, vector<KERNINGPAIR> &pairs, vector<UINT> &chars)
{
	// Determine the factor for scaling down the values from the design units to the font size
	float scaleFactor = DetermineDesignUnitToFontUnitFactor(dc);

	// TODO: support non unicode as well
	// Build a glyphId to char map
	UINT glyphIdToChar[65535] = {0};
	for( UINT n = 0; n < chars.size(); n++ )
	{
		SCRIPT_CACHE sc;
		int glyphId = GetUnicodeGlyphIndex(dc, &sc, chars[n]);
		if( glyphId >= 0 )
			glyphIdToChar[glyphId] = chars[n];
	}

	// Load the GPOS table from the TrueType font file
	vector<BYTE> buffer;
	DWORD GPOS = TAG('G','P','O','S');
	DWORD size = GetFontData(dc, GPOS, 0, 0, 0);
	if( size != GDI_ERROR )
	{
		buffer.resize(size);
		size = GetFontData(dc, GPOS, 0, &buffer[0], size);
	}
	if( size == GDI_ERROR || size == 0 )
		return;

	// Get the GPOS header info
	DWORD version          = GETUINT(&buffer[0]);
	assert( version == 0x00010000 );
	WORD scriptListOffset  = GETUSHORT(&buffer[4]);
	WORD featureListOffset = GETUSHORT(&buffer[6]);
	WORD lookupListOffset  = GETUSHORT(&buffer[8]);

	BYTE *scriptList  = &buffer[0] + scriptListOffset;
	BYTE *featureList = &buffer[0] + featureListOffset;
	BYTE *lookupList  = &buffer[0] + lookupListOffset;

	// Locate the default script in the script list table
	WORD scriptCount = GETUSHORT(scriptList);
	WORD offset = 0;
	for( UINT c = 0; c < scriptCount; c++ )
	{
		BYTE *scriptRecord = scriptList + 2 + c*6;
		DWORD tag = *(DWORD*)scriptRecord;
		if( tag == TAG('D','F','L','T') )
		{
			offset = GETUSHORT(scriptRecord+4);
			break;
		}				
	}

	if( offset == 0 )
		return;

	// Use the default language
	BYTE *script = scriptList + offset;
	WORD defaultLangSysOffset = GETUSHORT(script);
	WORD langSysCount = GETUSHORT(script+2);
	if( defaultLangSysOffset == 0 )
		return;

	BYTE *langSys = script + defaultLangSysOffset;
		
	WORD lookupOrder = GETUSHORT(langSys);
	assert( lookupOrder == 0 ); // reserved for future use
	WORD reqFeatureIndex = GETUSHORT(langSys+2); // Can be 0xFFFF if not used
	WORD featureCount    = GETUSHORT(langSys+4);

	// Find all kerning pairs from all the features that apply
	WORD allFeatureCount = GETUSHORT(featureList);
	for( UINT c = 0; c < featureCount; c++ )
	{
		WORD featureIndex = GETUSHORT(langSys+6+c*2);

		if( featureIndex < allFeatureCount )
		{
			BYTE *featureRecord = featureList + 2 + 6*featureIndex;

			DWORD tag = *(DWORD*)(featureRecord);
			if( tag == TAG('k','e','r','n') )
			{
				ProcessKernFeature(dc, featureRecord, featureList, lookupList, pairs, glyphIdToChar, scaleFactor);
			}
		}
	}
}
