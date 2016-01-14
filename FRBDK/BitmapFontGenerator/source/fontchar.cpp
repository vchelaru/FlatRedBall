#include <math.h>
#include "fontchar.h"
#include "unicode.h"
#include "acutil_unicode.h"
#include "fontgen.h"
#include "dynamic_funcs.h"

CFontChar::CFontChar()
{
	m_charImg = 0;
}

CFontChar::~CFontChar()
{
	if( m_charImg )
		delete m_charImg;
}

void CFontChar::CreateFromImage(int ch, cImage *image, int xoffset, int yoffset, int advance)
{
	m_isChar  = false;
	m_id      = ch;
	m_width   = image->width;
	m_height  = image->height;
	m_advance = m_width + advance;
	m_xoffset = xoffset;
	m_yoffset = yoffset;
	m_colored = true;

	m_charImg = new cImage(image->width, image->height);
	m_charImg->isTopDown = true;

	// Copy the input image to charImg
	memcpy(m_charImg->pixels, image->pixels, m_width*m_height*4);
}

bool CFontChar::HasOutline()
{
	return m_isChar && m_colored;
}

BYTE CFontChar::GetPixelValue(int x, int y, int encoding)
{
	if( m_isChar )
	{	
		if( encoding == e_one ) return 255;
		if( encoding == e_zero ) return 0;

		// Does the character have an outline?
		if( m_colored )
		{
			DWORD color = m_charImg->pixels[y*m_charImg->width+x];
			if( BYTE(color) )
			{
				if( encoding == e_glyph )
					return (BYTE)color;
				else if( encoding == e_outline )
					return 255;
				else if( encoding == e_glyph_outline )
					return 0x80 | (((BYTE)(color))>>1);
			}
			else
			{
				if( encoding == e_glyph )
					return 0;
				else if( encoding == e_outline )
                    return BYTE(color>>24);
				else if( encoding == e_glyph_outline )
					return BYTE(color>>25);
			}
		}
		else
		{
			// Since the character has no outline we 
			// always return the same value
			return (BYTE)m_charImg->pixels[y*m_charImg->width+x];
		}
	}

	return 0;
}

void CFontChar::DrawInvalidCharGlyph(HFONT font, int scaleH, int aa, bool useUnicode, bool useSmoothing)
{
	m_id = -1;
	DrawGlyph(font, 0xFFFF, scaleH, aa, useUnicode, useSmoothing);
}

void CFontChar::DrawChar(HFONT font, int ch, int scaleH, int aa, bool useUnicode, bool useSmoothing)
{
	m_id = ch;
	DrawGlyph(font, ch, scaleH, aa, useUnicode, useSmoothing);
}

void CFontChar::DrawGlyph(HFONT font, int ch, int scaleH, int aa, bool useUnicode, bool useSmoothing)
{
	m_colored = false;
	m_isChar  = true;

	// Create a memory dc
	HDC dc = CreateCompatibleDC(0);

	// Set the font
	HFONT oldFont = (HFONT)SelectObject(dc, font);

	// We need to determine text metrics before applying the world transform, because 
	// the returned text metrics with transform is not consistent. The tmHeight for example
	// is always the same, independently of the scale, but the tmAscent varies slightly,
	// though not proportionally with the scale.
	TEXTMETRIC tm;
	GetTextMetrics(dc, &tm);

	// Compute the height and ascent with scale
	int fontHeight = int(ceilf(tm.tmHeight*float(scaleH)/100.0f));
	int fontAscent = int(ceilf(tm.tmAscent*float(scaleH)/100.0f));

	// Scale the coordinate system so that the font is stretched
	if( SetGraphicsMode(dc, GM_ADVANCED) )
	{
		XFORM mtx;
		mtx.eM11 = 1.0f;
		mtx.eM12 = 0;
		mtx.eM21 = 0;
		mtx.eM22 = float(scaleH)/100.0f;
		mtx.eDx = 0;
		mtx.eDy = 0;
		SetWorldTransform(dc, &mtx);
	}

	// Get the glyph info
	int idx;
	GLYPHMETRICS gm;
	MAT2 mat = {{0,1},{0,0},{0,0},{0,1}};
	DWORD d;
	if( useUnicode )
	{
		idx = GetUnicodeGlyphIndex(dc, 0, ch);
		if( idx < 0 )
		{
			// Get the default character instead
			TEXTMETRICW tm;
			GetTextMetricsW(dc, &tm);
			WORD glyph;
			fGetGlyphIndicesW(dc, &tm.tmDefaultChar, 1, &glyph, 0);
			idx = glyph;
		}
		d = GetGlyphOutlineW(dc,idx,GGO_GLYPH_INDEX|(useSmoothing ? GGO_GRAY8_BITMAP : GGO_BITMAP),&gm,0,0,&mat);
	}
	else
	{
		idx = ch;
		d = GetGlyphOutlineA(dc,idx,(useSmoothing ? GGO_GRAY8_BITMAP : GGO_BITMAP),&gm,0,0,&mat);
	}
	if( d != GDI_ERROR )
	{
		// Create the image that will receive the pixels
		m_width = gm.gmBlackBoxX;
		m_height = gm.gmBlackBoxY;
		m_xoffset = gm.gmptGlyphOrigin.x;
		m_advance = gm.gmCellIncX;
		m_yoffset = fontAscent - gm.gmptGlyphOrigin.y;

		// GetGlyphOutline sometimes returns the incorrect height, usually when the
		// glyph have accentual marks very high up. We can calculate the true height 
		// from the buffer size.
		UINT pitch = m_width;
		if( pitch & 0x3 ) pitch += 4 - (pitch & 0x3);
		if( d / pitch > (unsigned)m_height ) 
		{
			m_yoffset -= d / pitch - m_height;
			m_height = d / pitch;
		}

        // Create the image
		m_charImg = new cImage(m_width, m_height);
		m_charImg->isTopDown = true;
		memset(m_charImg->pixels, 0, m_charImg->width * m_charImg->height * 4);

		// Get the actual bitmap
		if( d > 0 )
		{
			BYTE *tmpPixels = new BYTE[d];
			if( useUnicode )
				d = GetGlyphOutlineW(dc,idx,GGO_GLYPH_INDEX|(useSmoothing ? GGO_GRAY8_BITMAP : GGO_BITMAP),&gm,d,tmpPixels,&mat);
			else
				d = GetGlyphOutlineA(dc,idx,(useSmoothing ? GGO_GRAY8_BITMAP : GGO_BITMAP),&gm,d,tmpPixels,&mat);

			if( useSmoothing )
			{
				// The above outputs the glyph with 65 levels of gray, so we need to convert this to 256 levels of gray
				for( int y = 0; y < m_charImg->height; y++ )
				{
					for( int x = 0; x < m_charImg->width; x++ )
					{
						BYTE v = 255 * tmpPixels[x+y*pitch] / 64;
						m_charImg->pixels[x+y*m_charImg->width] = (v << 24) | (v << 16) | (v << 8) | v;
					}
				}
			}
			else
			{
				UINT pitch = m_charImg->width / 8 + ((m_charImg->width & 0x7) ? 1 : 0);
				if( pitch & 0x3 ) pitch += 4 - (pitch & 0x3);

				// The above outputs a glyph in a monochrome bitmap
				for( int y = 0; y < m_charImg->height; y++ )
				{
					for( int x = 0; x < m_charImg->width; )
					{
						// Transform each byte into 8 pixels
						for( int bit = 7; bit >= 0 && x < m_charImg->width; bit--, x++ )
						{
							m_charImg->pixels[x+y*m_charImg->width] = ((tmpPixels[x/8+y*pitch] >> bit) & 1) ? 0xFFFFFFFF : 0; 
						}
					}
				}
			}

			delete[] tmpPixels;
		}
	}
	else
	{
		// GetGlyphOutline only works for true type fonts, so we need a fallback for other fonts

		// Determine the size needed for the char
		ABC abc;
		if( useUnicode )
		{
			if( GetUnicodeCharABCWidths(dc, 0, ch, &abc) < 0 )
				memset(&abc, 0, sizeof(abc));

			m_width = abc.abcB;
		}
		else
		{
			if( GetCharABCWidths(dc, ch, ch, &abc) )
			{
				m_width = abc.abcB;
			}
			else
			{
				// Use GetCharWidth32() instead
				GetCharWidth32(dc, ch, ch, &m_width);

				abc.abcA = abc.abcC = 0;
				abc.abcB = (unsigned)m_width;
			}
		}
		m_height = fontHeight;
		m_xoffset = abc.abcA;
		m_advance = (abc.abcA + abc.abcB + abc.abcC);
		m_yoffset = 0;

		// Create the image that will receive the pixels
		m_charImg = new cImage(m_width, m_height);
		m_charImg->isTopDown = true;
		m_charImg->Clear(0);

		// Draw the character
		DWORD *pixels;
		BITMAPINFO bmi;
		ZeroMemory(&bmi, sizeof(BITMAPINFO));
		bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bmi.bmiHeader.biWidth = m_charImg->width;
		bmi.bmiHeader.biHeight = -m_charImg->height;
		bmi.bmiHeader.biPlanes = 1;
		bmi.bmiHeader.biBitCount = 32;         
		bmi.bmiHeader.biCompression = BI_RGB;
		bmi.bmiHeader.biSizeImage = m_charImg->width * m_charImg->height * 4;

		HBITMAP bm = CreateDIBSection(dc, &bmi, DIB_RGB_COLORS, (void**)&pixels, 0, 0);
		HBITMAP oldBM = (HBITMAP)SelectObject(dc, bm);

		HBRUSH black = (HBRUSH)GetStockObject(BLACK_BRUSH);

		memset(pixels, 0, bmi.bmiHeader.biSizeImage);

		SetTextColor(dc, RGB(255,255,255));
		SetBkColor(dc, RGB(0,0,0));
		SetBkMode(dc, TRANSPARENT);

		if( useUnicode )
		{
			WCHAR buf[2];
			int length = acUtility::EncodeUTF16(ch, (unsigned char*)buf, 0);

			TextOutW(dc, -abc.abcA, 0, buf, length/2);
		}
		else
			TextOutA(dc, -abc.abcA, 0, (char*)&ch, 1);

		GdiFlush();

		// Retrieve the pixels to the image
		memcpy(m_charImg->pixels, pixels, m_charImg->width*m_charImg->height*4);
		
		// Discount scanlines that are not drawn
		for( int y = 0; y < m_charImg->height; y++ )
		{
			bool empty = true;
			for( int x = 0; x < m_charImg->width; x++ )
			{
				if( m_charImg->pixels[y*m_charImg->width+x] != 0 )
				{
					empty = false;
					break;
				}
			}

			if( empty )
			{
				m_height--;
				m_yoffset++;
			}
			else
				break;
		}

		// Discount scanlines that are not drawn
		for( int y = m_charImg->height-1; y >= m_yoffset; y-- )
		{
			bool empty = true;
			for( int x = 0; x < m_charImg->width; x++ )
			{
				if( m_charImg->pixels[y*m_charImg->width+x] != 0 )
				{
					empty = false;
					break;
				}
			}

			if( empty )
			{
				m_height--;
			}
			else
				break;
		}

		// Remove the empty scanlines
		if( m_yoffset )
		{
			for( int y = 0; y < m_height; y++ )
				for( int x = 0; x < m_width; x++ )
					m_charImg->pixels[y*m_charImg->width+x] = m_charImg->pixels[(y+m_yoffset)*m_charImg->width+x];			
		}
		m_charImg->height = m_height;

		// Clean up
		SelectObject(dc, oldBM);
		DeleteObject(bm);
	}

	// Downscale in case of supersampling
	if( aa > 1 )
	{
		// It's necessary to determine the subpixel that the glyph starts on, i.e. 
		// it may be necessary to add a couple of empty scanlines for the first pixel. This
		// in turn may cause the height to be 1 pixel larger than if just dividing with aa.
		int subpixels = m_yoffset % aa;

		m_width = int(ceilf(float(m_width)/aa));
		m_height = int(ceilf(float(subpixels + m_height)/aa));
		m_xoffset /= aa;
		m_yoffset /= aa;
		m_advance /= aa;

		cImage img;
		img.Create(m_width, m_height);

		if( aa == 2 )
		{
			for( int y = 0; y < img.height; y++ )
			{
				for( int x = 0; x < img.width; x++ )
				{
					int sy = y*2 - subpixels;
					int c = 0;

					if( sy >= 0 )
					{	
						c += m_charImg->pixels[x*2 + sy*m_charImg->width] & 0xFF;
						if( x*2+1 < m_charImg->width ) c += m_charImg->pixels[x*2+1 + sy*m_charImg->width] & 0xFF;
					}

					if( sy+1 < m_charImg->height )
					{
						c += m_charImg->pixels[x*2 + (sy+1)*m_charImg->width] & 0xFF;
						if( x*2+1 < m_charImg->width ) c += m_charImg->pixels[x*2+1 + (sy+1)*m_charImg->width] & 0xFF;
					}

					c /= 4;

					img.pixels[y*img.width + x] = c | (c<<8) | (c<<16) | (c<<24);
				}
			}
		}
		else if( aa == 3 )
		{
			for( int y = 0; y < img.height; y++ )
			{
				for( int x = 0; x < img.width; x++ )
				{
					int sy = y*3 - subpixels;
					int c = 0;

					if( sy >= 0 )
					{
						c += m_charImg->pixels[x*3 + sy*m_charImg->width] & 0xFF;
						if( x*3+1 < m_charImg->width ) c += m_charImg->pixels[x*3+1 + sy*m_charImg->width] & 0xFF;
						if( x*3+2 < m_charImg->width ) c += m_charImg->pixels[x*3+2 + sy*m_charImg->width] & 0xFF;
					}

					if( sy+1 >= 0 && sy+1 < m_charImg->height )
					{
						c += m_charImg->pixels[x*3+0 + (sy+1)*m_charImg->width] & 0xFF;
						if( x*3+1 < m_charImg->width ) c += m_charImg->pixels[x*3+1 + (sy+1)*m_charImg->width] & 0xFF;
						if( x*3+2 < m_charImg->width ) c += m_charImg->pixels[x*3+2 + (sy+1)*m_charImg->width] & 0xFF;
					}

					if( sy+2 < m_charImg->height )
					{
						c += m_charImg->pixels[x*3+0 + (sy+2)*m_charImg->width] & 0xFF;
						if( x*3+1 < m_charImg->width ) c += m_charImg->pixels[x*3+1 + (sy+2)*m_charImg->width] & 0xFF;
						if( x*3+2 < m_charImg->width ) c += m_charImg->pixels[x*3+2 + (sy+2)*m_charImg->width] & 0xFF;
					}

					c /= 9;

					img.pixels[y*img.width + x] = c | (c<<8) | (c<<16) | (c<<24);
				}
			}
		}
		else if( aa == 4 )
		{
			for( int y = 0; y < img.height; y++ )
			{
				for( int x = 0; x < img.width; x++ )
				{
					int sy = y*4 - subpixels;
					int c = 0;

					if( sy >= 0 )
					{
						c += m_charImg->pixels[x*4 + sy*m_charImg->width] & 0xFF;
						if( x*4+1 < m_charImg->width ) c += m_charImg->pixels[x*4+1 + sy*m_charImg->width] & 0xFF;
						if( x*4+2 < m_charImg->width ) c += m_charImg->pixels[x*4+2 + sy*m_charImg->width] & 0xFF;
						if( x*4+3 < m_charImg->width ) c += m_charImg->pixels[x*4+3 + sy*m_charImg->width] & 0xFF;
					}

					if( sy+1 >= 0 && sy+1 < m_charImg->height )
					{
						c += m_charImg->pixels[x*4+0 + (sy+1)*m_charImg->width] & 0xFF;
						if( x*4+1 < m_charImg->width ) c += m_charImg->pixels[x*4+1 + (sy+1)*m_charImg->width] & 0xFF;
						if( x*4+2 < m_charImg->width ) c += m_charImg->pixels[x*4+2 + (sy+1)*m_charImg->width] & 0xFF;
						if( x*4+3 < m_charImg->width ) c += m_charImg->pixels[x*4+3 + (sy+1)*m_charImg->width] & 0xFF;
					}

					if( sy+2 >= 0 && sy+2 < m_charImg->height )
					{
						c += m_charImg->pixels[x*4+0 + (sy+2)*m_charImg->width] & 0xFF;
						if( x*4+1 < m_charImg->width ) c += m_charImg->pixels[x*4+1 + (sy+2)*m_charImg->width] & 0xFF;
						if( x*4+2 < m_charImg->width ) c += m_charImg->pixels[x*4+2 + (sy+2)*m_charImg->width] & 0xFF;
						if( x*4+3 < m_charImg->width ) c += m_charImg->pixels[x*4+3 + (sy+2)*m_charImg->width] & 0xFF;
					}

					if( sy+3 < m_charImg->height )
					{
						c += m_charImg->pixels[x*4+0 + (sy+3)*m_charImg->width] & 0xFF;
						if( x*4+1 < m_charImg->width ) c += m_charImg->pixels[x*4+1 + (sy+3)*m_charImg->width] & 0xFF;
						if( x*4+2 < m_charImg->width ) c += m_charImg->pixels[x*4+2 + (sy+3)*m_charImg->width] & 0xFF;
						if( x*4+3 < m_charImg->width ) c += m_charImg->pixels[x*4+3 + (sy+3)*m_charImg->width] & 0xFF;
					}

					c /= 16;

					img.pixels[y*img.width + x] = c | (c<<8) | (c<<16) | (c<<24);
				}
			}
		}

		// Move the pixels to the charImg member
		m_charImg->width = img.width;
		m_charImg->height = img.height;
		delete[] m_charImg->pixels;
		m_charImg->pixels = img.pixels;
		img.pixels = 0;
	}

	// Clean up
	SelectObject(dc, oldFont);
	DeleteDC(dc);
}

void CFontChar::AddOutline(int thickness)
{
	if( !m_charImg->height || !m_charImg->width )
		return;

	m_colored = true;

	m_width  += thickness*2;
	m_height += thickness*2;
	m_xoffset -= thickness;
	m_yoffset -= thickness;

	cImage img;
	img.Create(m_charImg->width+2*thickness, m_charImg->height+2*thickness);
	img.Clear(0);

	// Create the kernel
	int kernelWidth = thickness*2+1;
	float *kernel = new float[kernelWidth*kernelWidth];

	// Circular kernel with anti-aliasing
	for( int y = 0; y < kernelWidth; y++ )
	{
		for( int x = 0; x < kernelWidth; x++ )
		{
			float val;
			if( x == thickness || y == thickness )
				val = 1;
			else 
			{
				val = thickness+1 - thickness*float((x-thickness)*(x-thickness)+(y-thickness)*(y-thickness))/(thickness*thickness);
				if( val > 1 ) val = 1;
				else if( val < 0 ) val = 0;
			}
			kernel[y*kernelWidth+x] = val;
		}
	}

	// Create the outline
	for( int y1 = 0; y1 < m_charImg->height; y1++ )
	{
		for( int x1 = 0; x1 < m_charImg->width; x1++ )
		{
			DWORD cs = m_charImg->pixels[y1*m_charImg->width+x1] & 0xFF;
			for( int y2 = 0; y2 < kernelWidth; y2++ )
			{
				for( int x2 = 0; x2 < kernelWidth; x2++ )
				{
					if( x2 == thickness && y2 == thickness )
					{
						if( cs )
							img.pixels[(y1+y2)*img.width+(x1+x2)] = 0xFF000000|(cs<<16)|(cs<<8)|cs;
					}
					else
					{
						DWORD val = DWORD(cs*kernel[y2*kernelWidth+x2])<<24;
						DWORD cd = img.pixels[(y1+y2)*img.width+(x1+x2)];
						if( val > cd )
							img.pixels[(y1+y2)*img.width+(x1+x2)] = val;
					}
				}
			}
		}
	}

	delete[] kernel;

	// Move the new image to charImg
	m_charImg->width = img.width;
	m_charImg->height = img.height;
	delete[] m_charImg->pixels;
	m_charImg->pixels = img.pixels;
	img.pixels = 0;
}