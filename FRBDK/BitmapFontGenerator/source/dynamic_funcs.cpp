#include <windows.h>
#include "dynamic_funcs.h"

// Since this code requires Win2000 or later, we'll load it dynamically
static HMODULE dll_gdi32 = 0;

GetGlyphIndicesA_t     fGetGlyphIndicesA     = 0;
GetGlyphIndicesW_t     fGetGlyphIndicesW     = 0;
GetFontUnicodeRanges_t fGetFontUnicodeRanges = 0;

void Init()
{
#ifdef LOAD_GDI32
	dll_gdi32 = LoadLibrary("gdi32.dll");
	if( dll_gdi32 != 0 ) 
	{
		fGetGlyphIndicesA     = (GetGlyphIndicesA_t)GetProcAddress(dll_gdi32, "GetGlyphIndicesA");
		fGetGlyphIndicesW     = (GetGlyphIndicesW_t)GetProcAddress(dll_gdi32, "GetGlyphIndicesW");
		fGetFontUnicodeRanges = (GetFontUnicodeRanges_t)GetProcAddress(dll_gdi32, "GetFontUnicodeRanges");
	}
#else
	fGetGlyphIndicesA     = GetGlyphIndicesA;
	fGetGlyphIndicesW     = GetGlyphIndicesW;
	fGetFontUnicodeRanges = GetFontUnicodeRanges;
#endif
}

void Uninit()
{
#ifdef LOAD_GDI32
	if( dll_gdi32 != 0 )
		FreeLibrary(dll_gdi32);
#endif
}