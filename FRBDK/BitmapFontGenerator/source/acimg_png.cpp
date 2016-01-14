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

#include <png.h>
#include <vector>
#include <stdio.h>

#include "acimg.h"

namespace acImage
{

int SavePng(const char *filename, Image &image)
{
	// Validate the image
	if( image.format != PF_A8R8G8B8 &&
		image.format != PF_R8G8B8 &&
		image.format != PF_A8 )
	{
		return E_FORMAT_NOT_SUPPORTED;
	}


	png_structp png;
	png_infop   info;
	
	FILE *f = fopen(filename, "wb");
	if( f == 0 )
		return E_FILE_ERROR;

	// Initialize the png structure with no special error handling
	png = png_create_write_struct(PNG_LIBPNG_VER_STRING, 0, 0, 0);
	if( png == 0 )
	{
		fclose(f);
		return E_ERROR;
	}
	
	// Allocate and initialize the image information data
	info = png_create_info_struct(png);
	if( info == 0 )
	{
		fclose(f);
		png_destroy_write_struct(&png, png_infopp_NULL);
		return E_ERROR;
	}

	// Set error handling.  
	if( setjmp(png_jmpbuf(png)) )
	{
		// If we get here, we had a problem
		fclose(f);
		png_destroy_write_struct(&png, &info);
		return E_ERROR;
	}

	// Set the file stream for writing
	png_init_io(png, f);

	// Set the image information
	int color_type;
	if(      image.format == PF_A8R8G8B8 ) color_type = PNG_COLOR_TYPE_RGB_ALPHA;
	else if( image.format == PF_R8G8B8   ) color_type = PNG_COLOR_TYPE_RGB;
	else if( image.format == PF_A8       ) color_type = PNG_COLOR_TYPE_GRAY;

	// TODO: Allow this to be defined in parameters
	bool saveInterlaced = false;
	int interlace_type = saveInterlaced ? PNG_INTERLACE_ADAM7 : PNG_INTERLACE_NONE;

	png_set_IHDR(png, info, image.width, image.height, 8, color_type, 
		interlace_type, PNG_COMPRESSION_TYPE_BASE, PNG_FILTER_TYPE_BASE);

	// Write the file header information
	png_write_info(png, info);

	// We need to swap the order of the color channels (not alpha)
	if( color_type != PNG_COLOR_TYPE_GRAY )
		png_set_bgr(png);

/* TODO: We can allow the application to pass in a 32bit image and only save the rgb channels
	// Remove alpha channel as specified
	if( color_type == PNG_COLOR_TYPE_RGB ) 
		png_set_filler(png, 0, PNG_FILLER_AFTER);
*/
	// Set up an array of pointer to the pixel rows
	png_bytep *rows = new png_bytep[image.height];
	if( rows == 0 )
	{
		fclose(f);
		png_destroy_write_struct(&png, &info);
		return E_ERROR;
	}

	// Get the address of the start of each pixel row
	for( UINT n = 0; n < image.height; n++ )
		rows[n] = png_bytep(image.data + n*image.pitch);

	// Write the image
	png_write_image(png, rows);

	// Complete the file writing
	png_write_end(png, info);

	// Clean up
	delete[] rows;

	png_destroy_write_struct(&png, &info);

	fclose(f);

	return E_SUCCESS;
}

int LoadPng(const char *filename, Image &image)
{
	image.data = 0;

	// Open the file
	FILE *f = fopen(filename, "rb");
	if( f == 0 ) 
		return E_FILE_ERROR;

	png_structp png;
	png_infop   info;
	png_infop   endinfo;

	// Initialize the png structure with no special error handling
	png = png_create_read_struct(PNG_LIBPNG_VER_STRING, 0, 0, 0);
	if( png == 0 )
	{
		fclose(f);
		return E_ERROR;
	}

	// Allocate and initialize the image information data
	info = png_create_info_struct(png);
	if( info == 0 )
	{
		fclose(f);
		png_destroy_read_struct(&png, (png_infopp)NULL, (png_infopp)NULL);
		return E_ERROR;
	}

    endinfo = png_create_info_struct(png);
    if( endinfo == 0 )
    {
		fclose(f);
        png_destroy_read_struct(&png, &info, (png_infopp)NULL);
        return E_ERROR;
    }

	// Set error handling.  
	if( setjmp(png_jmpbuf(png)) )
	{
		// If we get here, we had a problem
		fclose(f);
        png_destroy_read_struct(&png, &info, &endinfo);
		return E_ERROR;
	}

	// Give our file pointer to libpng
	png_init_io(png, f);

	DWORD transforms = PNG_TRANSFORM_STRIP_16   | // Convert 16bit samples to 8bit
		               PNG_TRANSFORM_PACKING    | // Convert 1,2,4bit samples to 8bit
					   PNG_TRANSFORM_PACKSWAP   | // Least significant byte first
					   PNG_TRANSFORM_BGR;         // Transform BGR to RGB
	// TODO: Have a flag to expand 24bit to 32bit
	//				   PNG_TRANSFORM_EXPAND;      // Expand RGB to ARGB
	png_read_png(png, info, transforms, NULL);

	// We can close the file now
	fclose(f);

	// Now copy the image data to our Image structure
	UINT bitDepth  = png_get_bit_depth(png, info);
	UINT colorType = png_get_color_type(png, info);
	image.width    = png_get_image_width(png, info);
	image.height   = png_get_image_height(png, info);
	image.pitch    = png_get_rowbytes(png, info);

	// Validate image format
	if( bitDepth != 8 )
	{
		png_destroy_read_struct(&png, &info, &endinfo);
		return E_FORMAT_NOT_SUPPORTED;
	}

	if( colorType == PNG_COLOR_TYPE_RGB_ALPHA )
		image.format = PF_A8R8G8B8;
	else if( colorType == PNG_COLOR_TYPE_RGB )
		image.format = PF_R8G8B8;
	else if( colorType == PNG_COLOR_TYPE_GRAY )
		image.format = PF_A8;
	else
	{
		png_destroy_read_struct(&png, &info, &endinfo);
		return E_FORMAT_NOT_SUPPORTED;
	}

	image.data = new BYTE[image.pitch * image.height];

	png_bytep *rows = png_get_rows(png, info);

	for( UINT y = 0; y < image.height; y++ )
		memcpy(&image.data[y * image.pitch], rows[y], image.pitch);

	// Clean up
	png_destroy_read_struct(&png, &info, &endinfo);

	return E_SUCCESS;
}

} // namespace acImage
