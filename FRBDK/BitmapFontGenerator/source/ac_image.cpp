#include <string.h>
#include "ac_image.h"

#define FAIL(r) {returnCode = (r); goto cleanup;}

cImage::cImage()
{
	pixels = 0;
	width  = 0;
	height = 0;
	isTopDown = true;
}

cImage::cImage(int width, int height)
{
	pixels = new PIXEL[width*height];
	if( pixels )
	{
		this->width = width;
		this->height = height;
	}
	else
	{
		this->width = 0;
		this->height = 0;
	}
}

cImage::~cImage()
{
	if( pixels )
		delete[] pixels;
}

int cImage::CopyToDC(HDC dc, int x, int y, int w, int h)
{
	if( pixels == 0 )
		return 0;

	BITMAPINFO bmi;
	GetBitmapInfoHeader((BITMAPINFOHEADER *)&bmi);

	StretchDIBits(dc, x, y, w, h, 0, 0, width, height, pixels, &bmi, DIB_RGB_COLORS, SRCCOPY);

	return 0;
}

void cImage::GetBitmapInfoHeader(BITMAPINFOHEADER *bmih)
{
	bmih->biSize          = sizeof(BITMAPINFOHEADER);
	bmih->biBitCount      = 32;
	bmih->biWidth         = width;
	bmih->biHeight        = isTopDown ? -height : height; 
	bmih->biCompression   = BI_RGB;
	bmih->biPlanes        = 1;
	bmih->biSizeImage     = 0;
	bmih->biClrImportant  = 0;
	bmih->biClrUsed       = 0;
	bmih->biXPelsPerMeter = 0;
	bmih->biYPelsPerMeter = 0;
}

int cImage::Create(int w, int h)
{
	if( pixels ) delete[] pixels;
	pixels = new PIXEL[w*h];
	width  = w;
	height = h;

	return 0;
}

void cImage::Clear(PIXEL color)
{
	for( int y = 0; y < height; y++ )
		for( int x = 0; x < width; x++ )
			pixels[y*width+x] = color;
}

