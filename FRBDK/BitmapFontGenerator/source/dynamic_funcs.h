#ifndef DYNAMIC_FUNCS_H
#define DYNAMIC_FUNCS_H

#ifndef GGI_MARK_NONEXISTING_GLYPHS

#define GGI_MARK_NONEXISTING_GLYPHS  0X0001

typedef struct tagWCRANGE {
  WCHAR  wcLow;
  USHORT cGlyphs;
} WCRANGE;

typedef struct tagGLYPHSET {
  DWORD    cbThis;
  DWORD    flAccel;
  DWORD    cGlyphsSupported;
  DWORD    cRanges;
  WCRANGE  ranges[1];
} GLYPHSET;

// Load the functions from GDI32
#define LOAD_GDI32

#endif

typedef DWORD (_stdcall *GetGlyphIndicesA_t)(HDC hdc, LPCSTR lpstr, int c, LPWORD pgi, DWORD fl);
typedef DWORD (_stdcall *GetGlyphIndicesW_t)(HDC hdc, LPCWSTR lpstr, int c, LPWORD pgi, DWORD fl);
typedef DWORD (_stdcall *GetFontUnicodeRanges_t)(HDC hdc, GLYPHSET *gs);

extern GetGlyphIndicesA_t fGetGlyphIndicesA;
extern GetGlyphIndicesW_t fGetGlyphIndicesW;
extern GetFontUnicodeRanges_t fGetFontUnicodeRanges;

void Init();
void Uninit();

#endif