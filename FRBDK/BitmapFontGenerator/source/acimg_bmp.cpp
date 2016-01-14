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

#include <stdio.h>
#include "acimg.h"

namespace acImage
{

// Reference: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/gdi/bitmaps_4v1h.asp

#pragma pack(push)
#pragma pack(1)
struct BitmapFileHeader
{
    WORD    bfType;
    DWORD   bfSize;
    WORD    bfReserved1;
    WORD    bfReserved2;
    DWORD   bfOffBits;
};

struct BitmapInfoHeader
{
    DWORD   biSize;
    long    biWidth;
    long    biHeight;
    WORD    biPlanes;
    WORD    biBitCount;
    DWORD   biCompression;
    DWORD   biSizeImage;
    long    biXPelsPerMeter;
    long    biYPelsPerMeter;
    DWORD   biClrUsed;
    DWORD   biClrImportant;
};
#pragma pack(pop)

// Values for ht biCompression field
const DWORD BI_RGB       = 0;
const DWORD BI_RLE8      = 1;
const DWORD BI_RLE4      = 2;
const DWORD BI_BITFIELDS = 3;
const DWORD BI_JPEG      = 4;
const DWORD BI_PNG       = 5;


// TODO: Add support for RLE
// TODO: Add support for 16 bit 

int SaveBmp(const char *filename, Image &image)
{
	// Validate the image
	if( image.format != PF_A8R8G8B8 &&
		image.format != PF_R8G8B8 &&
		image.format != PF_A8 )
	{
		return E_FORMAT_NOT_SUPPORTED;
	}

	FILE *f = fopen(filename, "wb");
	if( f == 0 )
		return E_FILE_ERROR;

	// Fill in the bitmap file header
	BitmapFileHeader bmfh;
	bmfh.bfType      = 'MB';
	bmfh.bfReserved1 = 0;
	bmfh.bfReserved2 = 0;
	bmfh.bfOffBits   = sizeof(BitmapFileHeader) + sizeof(BitmapInfoHeader); 
	bmfh.bfSize      = bmfh.bfOffBits; // Size of the entire file

    // Fill in the bitmap info header
	BitmapInfoHeader bmih;
	bmih.biSize          = sizeof(BitmapInfoHeader);
	bmih.biHeight        = image.height; // bottom-up
	bmih.biWidth         = image.width;
	bmih.biPlanes        = 1;
	bmih.biCompression   = BI_RGB;
	bmih.biSizeImage     = 0;
	bmih.biXPelsPerMeter = 0;
	bmih.biYPelsPerMeter = 0;
	bmih.biClrUsed       = 0;
	bmih.biClrImportant  = 0;
	if( image.format == PF_A8 )
	{
		bmfh.bfOffBits += sizeof(DWORD)*256;
		bmfh.bfSize += sizeof(DWORD)*256;
		bmih.biBitCount = 8;
	}
	else if( image.format == PF_R8G8B8 )
		bmih.biBitCount = 24;
	else if( image.format == PF_A8R8G8B8 )
		bmih.biBitCount = 32;

	// Determine the pitch
	UINT pitch = image.width*bmih.biBitCount/8;
	pitch = (image.pitch & 0x3) ? (image.pitch & (~0x3))+4 : image.pitch;

	// Update the file size
	bmfh.bfSize += pitch * image.height;

	// Save the image file
	fwrite(&bmfh, sizeof(bmfh), 1, f);
	fwrite(&bmih, sizeof(bmih), 1, f);
	
	// Write the palette
	if( bmih.biBitCount == 8 )
	{
		for( int n = 0; n < 256; n++ )
		{
			DWORD color = n | (n<<8) | (n<<16) | (n<<24);
			fwrite(&color, 4, 1, f);
		}
	}

	// Write the pixels bottom up
	UINT padding = pitch - image.width*bmih.biBitCount/8;
	for( UINT y = 0; y < image.height; y++ )
	{
		BYTE *row = &image.data[(image.height-1-y)*image.pitch];
		fwrite(row, image.width*bmih.biBitCount/8, 1, f);

		DWORD zero = 0;
		fwrite(&zero, padding, 1, f);
	}
	fclose(f);

	return E_SUCCESS;
}

int LoadBmp(const char *filename, Image &image)
{
	image.data       = 0;
	image.palette    = 0;
	image.numColours = 0;

	// Open the file
	FILE *f = fopen(filename, "rb");
	if( f == 0 ) 
		return E_FILE_ERROR;

	// Read the bitmap file header
	BitmapFileHeader bmfh;
	fread(&bmfh, sizeof(BitmapFileHeader), 1, f);

	// Is the type 'MB'?
	if( bmfh.bfType != 'MB' )
	{
		fclose(f);
		return E_FORMAT_NOT_SUPPORTED;
	}

	// Read the bitmap info header
	BitmapInfoHeader bmih;
	fread(&bmih, sizeof(BitmapInfoHeader), 1, f);

	if( bmih.biSize        != sizeof(BitmapInfoHeader) ||
		bmih.biCompression != BI_RGB ||
		bmih.biPlanes      != 1 )
	{
		fclose(f);
		return E_FORMAT_NOT_SUPPORTED;
	}

	// Determine format
	if( bmih.biBitCount <= 8 )
		image.format = PF_COLORMAP;
	else if( bmih.biBitCount == 24 )
		image.format = PF_R8G8B8;
	else if( bmih.biBitCount == 32 )
		image.format = PF_A8R8G8B8;
	else
	{
		fclose(f);
		return E_FORMAT_NOT_SUPPORTED;
	}

	// Read the palette
	int numPaletteEntries = 1 << bmih.biBitCount;
	if( bmih.biBitCount <= 8 )
	{
		if( bmih.biClrUsed )
			numPaletteEntries = bmih.biClrUsed;

		image.numColours = numPaletteEntries;
		image.palette = new DWORD[image.numColours];
		if( image.palette == 0 )
		{
			fclose(f);
			return E_OUT_OF_MEMORY;
		}
	}
	else
		numPaletteEntries = 0;

	fread(image.palette, sizeof(DWORD)*numPaletteEntries, 1, f);

	// Read the image data
	image.pitch = bmih.biWidth*bmih.biBitCount/8;
	image.pitch = (image.pitch & 0x3) ? (image.pitch & (~0x3))+4 : image.pitch;

	image.width  = bmih.biWidth;
	image.height = bmih.biHeight < 0 ? -bmih.biHeight : bmih.biHeight;

	image.data = new BYTE[image.pitch * image.height];
	if( image.data == 0 ) 
	{
		fclose(f);
		return E_OUT_OF_MEMORY;
	}

	if( bmih.biHeight > 0 )
	{
		// Read the image from the bottom and up
		for( UINT y = 0; y < image.height; y++ )
		{
			BYTE *row = &image.data[(image.height-1-y)*image.pitch];
			fread(row, image.pitch, 1, f);
		}
	}
	else
		fread(image.data, image.pitch*bmih.biHeight, 1, f);
	fclose(f);

	// Unpack pixels with bitdepth less than 8
	if( bmih.biBitCount < 8 )
	{
		BYTE *data = image.data;
		UINT pitch = image.pitch;

		image.pitch  = bmih.biWidth;
		image.pitch  = (image.pitch & 0x3) ? (image.pitch & (~0x3))+4 : image.pitch;

		image.data = new BYTE[image.pitch * image.height];

		UINT parts = 8/bmih.biBitCount;
		UINT mask  = 0xFF >> (8-bmih.biBitCount);

		for( UINT y = 0; y < image.height; y++ )
		{
			BYTE *oldrow = &data[pitch*y];
			BYTE *newrow = &image.data[image.pitch*y];

			for( UINT x = 0; x < image.width; x++ )
			{
				BYTE pixel = oldrow[x/parts];
				pixel = (pixel>>((parts-1-x%parts)*bmih.biBitCount))&mask;
				newrow[x] = pixel;
			}
		}

		delete[] data;
	}

	return E_SUCCESS;
}

}