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
#include <vector>
#include <setjmp.h>
#include <jpeglib.h>
#include "acimg.h"

namespace acImage
{

struct ErrorMgr 
{
	jpeg_error_mgr pub;
	jmp_buf setjmp_buffer;
};

METHODDEF(void) OnErrorExit(j_common_ptr cinfo)
{
	ErrorMgr *err = (ErrorMgr*)cinfo->err;
	longjmp(err->setjmp_buffer, 1);
}

int SaveJpg(const char *filename, Image &image, DWORD flags)
{
	// Validate the image
	if( image.format != PF_R8G8B8 &&
		image.format != PF_A8 )
	{
		return E_FORMAT_NOT_SUPPORTED;
	}

	// The quality is from 0 to 100
	if( (flags & 0xFF) > 100 )
	{
		return E_INVALID_ARG;
	}

	FILE *f = fopen(filename, "wb");
	if( f == 0 )
		return E_FILE_ERROR;

	// Initialize structures
	jpeg_compress_struct cinfo;

	// Use our error handler
	ErrorMgr jerr;
	cinfo.err = jpeg_std_error(&jerr.pub);
	jerr.pub.error_exit = OnErrorExit;

	// Define our return point in case of error
	if( setjmp(jerr.setjmp_buffer) ) 
	{
		// Something went wrong
		jpeg_destroy_compress(&cinfo);
		fclose(f);
		return E_FILE_ERROR;
	}

	jpeg_create_compress(&cinfo);

	// Define destination
	jpeg_stdio_dest(&cinfo, f);

	// Set image attributes
	cinfo.image_width      = image.width;
	cinfo.image_height     = image.height;
	if( image.format == PF_R8G8B8 )
	{
		cinfo.input_components = 3; // RGB pixels
		cinfo.in_color_space   = JCS_RGB;
	}
	else if( image.format == PF_A8 )
	{
		cinfo.input_components = 1; // Grayscale
		cinfo.in_color_space   = JCS_GRAYSCALE;
	}

	jpeg_set_defaults(&cinfo);
	jpeg_set_quality(&cinfo, flags & 0xFF, TRUE);

	// Save the file
	jpeg_start_compress(&cinfo, TRUE);
	if( image.format == PF_R8G8B8 )
	{
		JSAMPROW row_pointer[1] = {new BYTE[image.pitch]};
		while( cinfo.next_scanline < cinfo.image_height ) 
		{
			// Swap the Red and Blue channel of the image
			BYTE *inrow = &image.data[cinfo.next_scanline * image.pitch];
			BYTE *outrow = row_pointer[0];
			for( UINT x = 0; x < image.width*3; x += 3 )
			{
				outrow[0] = inrow[2];
				outrow[1] = inrow[1];
				outrow[2] = inrow[0];
				inrow  += 3;
				outrow += 3;
			}
			jpeg_write_scanlines(&cinfo, row_pointer, 1);
		}
		delete[] row_pointer[0];
	}
	else if( image.format == PF_A8 )
	{
		while( cinfo.next_scanline < cinfo.image_height ) 
		{
			JSAMPROW row_pointer[1] = {&image.data[cinfo.next_scanline * image.pitch]};
			jpeg_write_scanlines(&cinfo, row_pointer, 1);
		}
	}
	jpeg_finish_compress(&cinfo);

	// Clean up
	fclose(f);
	jpeg_destroy_compress(&cinfo);

	return E_SUCCESS;
}

int LoadJpg(const char *filename, Image &image)
{
	image.data = 0;

	// Open the file
	FILE *f = fopen(filename, "rb");
	if( f == 0 ) 
		return E_FILE_ERROR;

	jpeg_decompress_struct cinfo;

	// Use our error handler
	ErrorMgr jerr;
	cinfo.err = jpeg_std_error(&jerr.pub);
	jerr.pub.error_exit = OnErrorExit;

	// Define our return point in case of error
	if( setjmp(jerr.setjmp_buffer) ) 
	{
		// Something went wrong
		jpeg_destroy_decompress(&cinfo);
		fclose(f);
		return E_FILE_ERROR;
	}

	// Create the structure
    jpeg_create_decompress(&cinfo);

	// Inform that we'll load from the file
	jpeg_stdio_src(&cinfo, f);

	// Read the file header and perform the necessary post processing
	jpeg_read_header(&cinfo, TRUE);
	jpeg_start_decompress(&cinfo);

	// Validate the format
	if( cinfo.output_components == 3 && cinfo.out_color_space == JCS_RGB )
	{
		image.format = PF_R8G8B8;
		image.pitch  = cinfo.output_width * cinfo.output_components;
	}
	else if( cinfo.output_components == 1 && cinfo.out_color_space == JCS_GRAYSCALE )
	{
		image.format = PF_A8;
		image.pitch = cinfo.output_width;
	}
	else
	{
		fclose(f);
		jpeg_destroy_decompress(&cinfo);
		return E_FORMAT_NOT_SUPPORTED;
	}

	image.width  = cinfo.output_width;
	image.height = cinfo.output_height;
	image.data   = new BYTE[image.pitch*image.height];

	// Allocate a row, that will be freed by libjpeg
	if( image.format == PF_R8G8B8 )
	{
		JSAMPARRAY buffer = (*cinfo.mem->alloc_sarray)((j_common_ptr)&cinfo, JPOOL_IMAGE, image.pitch, 1);
		while( cinfo.output_scanline < cinfo.output_height ) 
		{
			jpeg_read_scanlines(&cinfo, buffer, 1);

			// Swap the Red and Blue channel of the image
			BYTE *outrow = &image.data[(cinfo.output_scanline-1) * image.pitch];
			BYTE *inrow = buffer[0];
			for( UINT x = 0; x < image.width*3; x += 3 )
			{
				outrow[0] = inrow[2];
				outrow[1] = inrow[1];
				outrow[2] = inrow[0];
				inrow  += 3;
				outrow += 3;
			}
		}
	}
	else if( image.format == PF_A8 )
	{
		while( cinfo.output_scanline < cinfo.output_height ) 
		{
			JSAMPROW row_pointer[1] = {&image.data[cinfo.output_scanline * image.pitch]};
			jpeg_read_scanlines(&cinfo, row_pointer, 1);
		}
	}
	jpeg_finish_decompress(&cinfo);

	// Clean up
	jpeg_destroy_decompress(&cinfo);
	fclose(f);

	return E_SUCCESS;
}

} // namespace acImage
