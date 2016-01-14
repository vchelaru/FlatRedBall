/*
   AngelCode Tool Box Library
   Copyright (c) 2007 Andreas Jönsson
  
   This software is provided 'as-is', without any express or implied 
   warranty. In no event will the authors be held liable for any 
   damages arising from the use of this software.

   Permission is granted to anyone to use this software for any 
   purpose, including commercial applications, and to alter it and 
   redistribute it freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you 
      must not claim that you wrote the original software. If you use
      this software in a product, an acknowledgment in the product 
      documentation would be appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and 
      must not be misrepresented as being the original software.

   3. This notice may not be removed or altered from any source 
      distribution.
  
   Andreas Jönsson
   andreas@angelcode.com
*/

#ifndef ACIMG_H
#define ACIMG_H

namespace acImage
{
typedef unsigned int   UINT;
typedef unsigned char  BYTE;
typedef unsigned short WORD;
typedef unsigned int   DWORD;

const int E_SUCCESS              = 0;
const int E_ERROR                = -1;
const int E_FORMAT_NOT_SUPPORTED = -2;
const int E_FILE_ERROR           = -3;
const int E_OUT_OF_MEMORY        = -4;
const int E_INVALID_ARG          = -5;

enum PixelFormat
{
	PF_COLORMAP,
	PF_A8,
	PF_R8G8B8,
	PF_A8R8G8B8,
};

struct Image
{
	Image() {palette = 0; data = 0;}
	~Image() {if( palette ) delete[] palette; if( data ) delete[] data; }

	UINT         width;      // width in pixels
	UINT         height;     // width in pixels
	UINT         pitch;      // number of bytes between the start of each line
	PixelFormat  format;     // pixel format
	UINT         numColours; // number of colours in palette
	DWORD       *palette;    // palette
	BYTE        *data;
};

// TGA
const DWORD TGA_RLE = 1;

int SaveTga(const char *filename, Image &image, DWORD flags = 0);
int LoadTga(const char *filename, Image &image);

// BMP
int SaveBmp(const char *filename, Image &image);
int LoadBmp(const char *filename, Image &image);

// PNG
int SavePng(const char *filename, Image &image);
int LoadPng(const char *filename, Image &image);

// DDS
const DWORD DDS_DXT1 = 1;
const DWORD DDS_DXT3 = 2;
const DWORD DDS_DXT5 = 3;

int SaveDds(const char *filename, Image &image, DWORD flags = 0);
int LoadDds(const char *filename, Image &image);

// JPG
// Flags is the quality, from 0 to 100
int SaveJpg(const char *filename, Image &image, DWORD flags = 50);
int LoadJpg(const char *filename, Image &image);

// Helpers
int LoadImageFile(const char *filename, Image &image);
int ConvertToARGB(Image &dst, const Image &src);
int ConvertAToARGB(Image &dst, const Image &src);
int ConvertRGBToARGB(Image &dst, const Image &src);
int ConvertColormapToARGB(Image &dst, const Image &src);
}

#endif
