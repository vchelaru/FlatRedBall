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
#include <string.h>
#include <squish.h>
#include "acimg.h"

namespace acImage
{

// Reference: http://msdn.microsoft.com/archive/default.asp?url=/archive/en-us/directx9_c/directx/graphics/reference/ddsfilereference/ddsfileformat.asp

struct DdsHeader
{
	DWORD dwMagic;
	DWORD dwSize;
	DWORD dwFlags;
	DWORD dwHeight;
	DWORD dwWidth;
	DWORD dwPitchOrLinearSize;
	DWORD dwDepth;
	DWORD dwMipMapCount;
	DWORD dwReserved1[11];
	struct 
	{
		DWORD dwSize;
		DWORD dwFlags;
		DWORD dwFourCC;
		DWORD dwRGBBitCount;
		DWORD dwRBitMask;
		DWORD dwGBitMask;
		DWORD dwBBitMask;
		DWORD dwRGBAlphaBitMask;
	} ddpfPixelFormat;
	struct
	{
		DWORD dwCaps1;
		DWORD dwCaps2;
		DWORD Reserved[2];
	} ddsCaps;
	DWORD dwReserved2;
};

// DDS flags
const int DDSD_CAPS        = 0x00000001;
const int DDSD_HEIGHT      = 0x00000002;
const int DDSD_WIDTH       = 0x00000004;
const int DDSD_PITCH       = 0x00000008;
const int DDSD_PIXELFORMAT = 0x00001000;
const int DDSD_MIPMAPCOUNT = 0x00020000;
const int DDSD_LINEARSIZE  = 0x00080000;
const int DDSD_DEPTH       = 0x00800000;

// DDS pixel format flags
const int DDPF_ALPHAPIXELS = 0x00000001;
const int DDPF_FOURCC      = 0x00000004;
const int DDPF_RGB         = 0x00000040;

// DDS caps 1 flags
const int DDSCAPS_COMPLEX = 0x00000008;
const int DDSCAPS_TEXTURE = 0x00001000;
const int DDSCAPS_MIPMAP  = 0x00400000;

// DDS caps 2 flags
const int DDSCAPS2_CUBEMAP           = 0x00000200;
const int DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
const int DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
const int DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
const int DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
const int DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
const int DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
const int DDSCAPS2_VOLUME            = 0x00200000;



int SaveDds(const char *filename, Image &image, DWORD flags)
{
	// Validate the image
	if( image.format != PF_A8R8G8B8 &&
		image.format != PF_R8G8B8 &&
		image.format != PF_A8 )
	{
		return E_FORMAT_NOT_SUPPORTED;
	}

	if( (flags == DDS_DXT1 ||
		 flags == DDS_DXT3 ||
		 flags == DDS_DXT5) &&
	    image.format != PF_A8R8G8B8 )
	{
		return E_FORMAT_NOT_SUPPORTED;
	}

	FILE *f = fopen(filename, "wb");
	if( f == 0 )
		return E_FILE_ERROR;

	// Save the image description
	DdsHeader dds;
	memset(&dds, 0, sizeof(dds));
	
	dds.dwMagic  = *(DWORD*)"DDS ";
	dds.dwSize   = 124;
	dds.dwFlags  = DDSD_CAPS|DDSD_PIXELFORMAT|DDSD_WIDTH|DDSD_HEIGHT;
	dds.dwHeight = image.height;
	dds.dwWidth  = image.width;

	dds.ddsCaps.dwCaps1 = DDSCAPS_TEXTURE;
	dds.ddpfPixelFormat.dwSize = 32;

	if( flags == 0 )
	{
		dds.dwFlags |= DDSD_PITCH;

		// Fill in the pixel format
		if( image.format == PF_R8G8B8 ||
			image.format == PF_A8R8G8B8 )
			dds.ddpfPixelFormat.dwFlags += DDPF_RGB;
		if( image.format == PF_A8 ||
			image.format == PF_A8R8G8B8 )
			dds.ddpfPixelFormat.dwFlags += DDPF_ALPHAPIXELS;

		if( image.format == PF_A8 )
		{
			dds.dwPitchOrLinearSize = image.width;
			if( dds.dwPitchOrLinearSize % 4 ) 
				dds.dwPitchOrLinearSize += 4 - (dds.dwPitchOrLinearSize % 4);

			dds.ddpfPixelFormat.dwRGBBitCount     = 8;
			dds.ddpfPixelFormat.dwRGBAlphaBitMask = 0xFF;
		}
		else if( image.format == PF_R8G8B8 )
		{
			dds.dwPitchOrLinearSize = image.width*3;
			if( dds.dwPitchOrLinearSize % 4 ) 
				dds.dwPitchOrLinearSize += 4 - (dds.dwPitchOrLinearSize % 4);

			dds.ddpfPixelFormat.dwRGBBitCount     = 24;
			dds.ddpfPixelFormat.dwRBitMask        = 0x00FF0000;
			dds.ddpfPixelFormat.dwGBitMask        = 0x0000FF00;
			dds.ddpfPixelFormat.dwBBitMask        = 0x000000FF;
		}
		else if( image.format == PF_A8R8G8B8 )
		{
			dds.dwPitchOrLinearSize = image.width*4;

			dds.ddpfPixelFormat.dwRGBBitCount     = 32;
			dds.ddpfPixelFormat.dwRBitMask        = 0x00FF0000;
			dds.ddpfPixelFormat.dwGBitMask        = 0x0000FF00;
			dds.ddpfPixelFormat.dwBBitMask        = 0x000000FF;
			dds.ddpfPixelFormat.dwRGBAlphaBitMask = 0xFF000000;
		}

		fwrite(&dds, sizeof(dds), 1, f);

		// Save image data
		DWORD pixelSize;
		if( image.format == PF_A8       ) pixelSize = 1;
		if( image.format == PF_R8G8B8   ) pixelSize = 3;
		if( image.format == PF_A8R8G8B8 ) pixelSize = 4;
		for( UINT y = 0; y < image.height; y++ )
			fwrite(&image.data[y*image.pitch], image.width*pixelSize, 1, f);
	}
	else
	{
		dds.dwFlags |= DDSD_LINEARSIZE;
		dds.ddpfPixelFormat.dwFlags |= DDPF_FOURCC;

		// Determine dimension aligned to 4 pixels
		UINT width = image.width;
		if( width % 4 ) width += 4 - (width % 4);

		UINT height = image.height;
		if( height % 4 ) height += 4 - (height % 4);

		int method;
		int blockSize;
		if( flags == DDS_DXT1 )
		{
			dds.ddpfPixelFormat.dwFourCC = *(DWORD*)"DXT1";
			blockSize = 8;
			method = squish::kDxt1;
		}
		else if( flags == DDS_DXT3 )
		{
			dds.ddpfPixelFormat.dwFourCC = *(DWORD*)"DXT3";
			blockSize = 16;
			method = squish::kDxt3;
		}
		else if( flags == DDS_DXT5 )
		{
			dds.ddpfPixelFormat.dwFourCC = *(DWORD*)"DXT5";
			blockSize = 16;
			method = squish::kDxt5;
		}

		// Determine linear size
		dds.dwPitchOrLinearSize = width/4 * height/4 * blockSize;

		fwrite(&dds, sizeof(dds), 1, f);

		// Save the image in blocks of 4x4 pixels
		BYTE block[16];
		DWORD source[16];

		for( UINT y = 0; y < image.height; y+= 4 )
		{
			for( UINT x = 0; x < image.width; x+= 4 )
			{
				DWORD *pixels = source;

				for( UINT py = 0; py < 4; py++ )
				{
					if( y+py < image.height )
					{
						for( UINT px = 0; px < 4; px++ )
						{
							if( x+px < image.width )
							{
								DWORD pixel = ((DWORD*)image.data)[(y+py)*image.width+x+px];

								// Swap red and blue channels
								pixel ^= ((pixel&0xFF)<<16);
								pixel ^= ((pixel>>16)&0xFF);
								pixel ^= ((pixel&0xFF)<<16);

								*pixels++ = pixel;
							}
							else
								*pixels++ = *(pixels-1);
						}
					}
					else
					{
						*pixels++ = *(pixels-4);
						*pixels++ = *(pixels-4);
						*pixels++ = *(pixels-4);
						*pixels++ = *(pixels-4);
					}
				}

				squish::Compress((BYTE*)source, block, method);
				fwrite(block, blockSize, 1, f);
			}
		}
	}

	fclose(f);

	return E_SUCCESS;
}

int LoadDds(const char *filename, Image &image)
{
	image.data = 0;

	// Open the file
	FILE *f = fopen(filename, "rb");
	if( f == 0 ) 
		return E_FILE_ERROR;

	// Read in the header
	DdsHeader dds;
	fread(&dds, sizeof(dds), 1, f);

	// Verify static bytes
	if( dds.dwMagic                != *(DWORD*)"DDS " || 
		dds.dwSize                 != 124             ||
		dds.ddpfPixelFormat.dwSize != 32              ||
		!(dds.ddsCaps.dwCaps1 & DDSCAPS_TEXTURE) )
	{
		fclose(f);
		return E_FORMAT_NOT_SUPPORTED;
	}

	if( (dds.ddpfPixelFormat.dwFlags & DDPF_FOURCC) == 0 )
	{
		if( dds.ddpfPixelFormat.dwRGBBitCount == 32 )
			image.format = PF_A8R8G8B8;
		else if( dds.ddpfPixelFormat.dwRGBBitCount == 24 )
			image.format = PF_R8G8B8;
		else if( dds.ddpfPixelFormat.dwRGBBitCount == 8 )
			image.format = PF_A8;
		else
		{
			fclose(f);
			return E_FORMAT_NOT_SUPPORTED;
		}

		// TODO: Should check the channel bit masks to 
		// determine if some bits must be swapped

		DWORD pixelSize;
		if( image.format == PF_A8       ) pixelSize = 1;
		if( image.format == PF_R8G8B8   ) pixelSize = 3;
		if( image.format == PF_A8R8G8B8 ) pixelSize = 4;

		image.width  = dds.dwWidth;
		image.height = dds.dwHeight;
		if( dds.dwFlags & DDSD_PITCH )
			image.pitch  = dds.dwPitchOrLinearSize;
		else
			image.pitch = image.width*pixelSize;
		image.data   = new BYTE[image.pitch * image.height];

		// Read image data
		// DDS images don't store the pad bytes, so only read the actual 
		// pixel data even though the image has a larger pitch 
		for( UINT y = 0; y < image.height; y++ )
			fread(&image.data[y*image.pitch], image.width*pixelSize, 1, f);
	}
	else if( dds.ddpfPixelFormat.dwFlags & DDPF_FOURCC )
	{
		// Verify compression format
		UINT blockSize;
		UINT method;
		if( dds.ddpfPixelFormat.dwFourCC == *(DWORD*)"DXT1" )
		{
			blockSize = 8;
			method = squish::kDxt1;
		}
		else if( dds.ddpfPixelFormat.dwFourCC == *(DWORD*)"DXT3" )
		{
			blockSize = 16;
			method = squish::kDxt3;
		}
		else if( dds.ddpfPixelFormat.dwFourCC == *(DWORD*)"DXT5" )
		{
			blockSize = 16;
			method = squish::kDxt5;
		}
		else
		{
			fclose(f);
			return E_FORMAT_NOT_SUPPORTED;
		}

		image.format = PF_A8R8G8B8;
		image.width  = dds.dwWidth;
		image.pitch  = dds.dwWidth*4;
		image.height = dds.dwHeight;
		image.data   = new BYTE[image.height*image.pitch];

		// Read the image in blocks of 4x4 pixels
		BYTE block[16];
		DWORD target[16];

		for( UINT y = 0; y < image.height; y += 4 )
		{
			for( UINT x = 0; x < image.width; x += 4 )
			{
				fread(block, blockSize, 1, f);
				squish::Decompress((BYTE*)target, block, method);

				DWORD *pixels = target;

				for( UINT py = 0; py < 4; py++ )
				{
					if( y+py < image.height )
					{
						for( UINT px = 0; px < 4; px++ )
						{
							if( x+px < image.width )
							{
								DWORD pixel = *pixels++;

								// Swap red and blue channels
								pixel ^= ((pixel&0xFF)<<16);
								pixel ^= ((pixel>>16)&0xFF);
								pixel ^= ((pixel&0xFF)<<16);

								((DWORD*)image.data)[(y+py)*image.width+x+px] = pixel;
							}
							else
								pixels++;
						}
					}
				}
			}
		}
	}
	else
	{
		fclose(f);
		return E_FORMAT_NOT_SUPPORTED;
	}

	fclose(f);

	return E_SUCCESS;
}

} // namespace acImage
