/*
   AngelCode Tool Box Library
   Copyright (c) 2007-2009 Andreas Jonsson
  
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

// 2009-04-12 Fixed compile errors on VC2008

#include <stdio.h>
#include <string.h>
#include "acimg.h"

namespace acImage
{

int ConvertToARGB(Image &dst, const Image &src)
{
	if( src.format == PF_COLORMAP )
		return ConvertColormapToARGB(dst, src);

	if( src.format == PF_A8 )
		return ConvertAToARGB(dst, src);

	if( src.format == PF_R8G8B8 )
		return ConvertRGBToARGB(dst, src);

	if( src.format == PF_A8R8G8B8 )
	{
		dst.data       = 0;
		dst.palette    = 0;
		dst.numColours = 0;
		dst.width      = src.width;
		dst.height     = src.height;
		dst.format     = PF_A8R8G8B8;
		dst.pitch      = src.width*4;

		dst.data = new BYTE[dst.pitch*dst.height];
		
		memcpy(dst.data, src.data, src.height*src.pitch);
	}

	return E_SUCCESS;
}

int ConvertRGBToARGB(Image &dst, const Image &src)
{
	dst.data       = 0;
	dst.palette    = 0;
	dst.numColours = 0;
	dst.width      = src.width;
	dst.height     = src.height;
	dst.format     = PF_A8R8G8B8;
	dst.pitch      = src.width*4;

	if( src.format != PF_R8G8B8 )
		return E_FORMAT_NOT_SUPPORTED;

	// Allocate memory
	dst.data = new BYTE[dst.pitch*dst.height];

	// Convert RGB images to ARGB images
	BYTE *data = src.data;
	UINT pitch = src.pitch;

	for( UINT y = 0; y < dst.height; y++ )
	{
		BYTE *oldrow = &src.data[src.pitch*y];
		DWORD *newrow = (DWORD*)&dst.data[dst.pitch*y];

		for( UINT x = 0; x < dst.width; x++ )
		{
			DWORD color = 0xFF000000;     // alpha
			color += oldrow[x*3];         // blue
			color += oldrow[x*3+1]<<8;    // green
			color += oldrow[x*3+2]<<16;   // red
			newrow[x] = color;
		}
	}

	return E_SUCCESS;
}

int ConvertAToARGB(Image &dst, const Image &src)
{
	dst.data       = 0;
	dst.palette    = 0;
	dst.numColours = 0;
	dst.width      = src.width;
	dst.height     = src.height;
	dst.format     = PF_A8R8G8B8;
	dst.pitch      = src.width*4;

	if( src.format != PF_A8 )
		return E_FORMAT_NOT_SUPPORTED;

	// Allocate memory
	dst.data = new BYTE[dst.pitch*dst.height];

	// Convert paletted images to true color
	BYTE *data = src.data;
	UINT pitch = src.pitch;

	for( UINT y = 0; y < dst.height; y++ )
	{
		BYTE *oldrow = &src.data[src.pitch*y];
		DWORD *newrow = (DWORD*)&dst.data[dst.pitch*y];

		for( UINT x = 0; x < dst.width; x++ )
			newrow[x] = (oldrow[x]<<24) | 0xFFFFFF;
	}

	return E_SUCCESS;
}

int ConvertColormapToARGB(Image &dst, const Image &src)
{
	dst.data       = 0;
	dst.palette    = 0;
	dst.numColours = 0;
	dst.width      = src.width;
	dst.height     = src.height;
	dst.format     = PF_A8R8G8B8;
	dst.pitch      = src.width*4;

	if( src.format != PF_COLORMAP || src.numColours == 0 )
		return E_FORMAT_NOT_SUPPORTED;

	// Allocate memory
	dst.data = new BYTE[dst.pitch*dst.height];

	// Convert paletted images to true color
	BYTE *data = src.data;
	UINT pitch = src.pitch;

	for( UINT y = 0; y < dst.height; y++ )
	{
		BYTE *oldrow = &src.data[src.pitch*y];
		DWORD *newrow = (DWORD*)&dst.data[dst.pitch*y];

		for( UINT x = 0; x < dst.width; x++ )
			newrow[x] = src.palette[oldrow[x]] | 0xFF000000;
	}

	return E_SUCCESS;
}

int LoadImageFile(const char *filename, Image &img)
{
	const char *ext = strrchr(filename, '.');
	if( ext == 0 )
		return E_INVALID_ARG;

	if( _stricmp(ext, ".jpg") == 0 )
		return LoadJpg(filename, img);
	else if( _stricmp(ext, ".bmp") == 0 )
		return LoadBmp(filename, img);
	else if( _stricmp(ext, ".tga") == 0 )
		return LoadTga(filename, img);
	else if( _stricmp(ext, ".dds") == 0 )
		return LoadDds(filename, img);
	else if( _stricmp(ext, ".png") == 0 )
		return LoadPng(filename, img);

	return E_FORMAT_NOT_SUPPORTED;
}

}