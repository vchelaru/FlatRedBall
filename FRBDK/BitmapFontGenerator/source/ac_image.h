#ifndef AC_IMAGE_H
#define AC_IMAGE_H

#define EIMG_FAILED_TO_OPEN_FILE -1
#define EIMG_OUT_OF_MEMORY       -2
#define EIMG_UNSUPPORTED_FORMAT  -3

#include <windows.h>	// HDC

typedef unsigned long PIXEL;

class cImage
{
public:
	cImage();
	cImage(int width, int height);
	virtual ~cImage();

	int CopyToDC(HDC dc, int x, int y, int w, int h);

	int Create(int w, int h);
	void GetBitmapInfoHeader(BITMAPINFOHEADER *bmih);
	void Clear(PIXEL color);

	PIXEL *pixels;
	int    width;
	int    height;
	bool   isTopDown;
};


#endif