/*
   AngelCode Tool Box Library
   Copyright (c) 2008 Andreas Jönsson
  
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

// 2009-07-25 - Changed all buffers from char* to unsigned char*

#ifndef ACUTIL_UNICODE_H
#define ACUTIL_UNICODE_H

namespace acUtility
{

enum EUnicodeByteOrder
{
	LITTLE_ENDIAN,
	BIG_ENDIAN,
};

// This function will attempt to decode a UTF-8 encoded character in the buffer.
// If the encoding is invalid, the function returns -1.
int DecodeUTF8(const unsigned char *encodedBuffer, unsigned int *outCharLength);

// This function will encode the value into the buffer.
// If the value is invalid, the function returns -1, else the encoded length.
int EncodeUTF8(unsigned int value, unsigned char *outEncodedBuffer, unsigned int *outCharLength);

// This function will attempt to decode a UTF-16 encoded character in the buffer.
// If the encoding is invalid, the function returns -1.
int DecodeUTF16(const unsigned char *encodedBuffer, unsigned int *outCharLength, EUnicodeByteOrder byteOrder = LITTLE_ENDIAN);

// This function will encode the value into the buffer.
// If the value is invalid, the function returns -1, else the encoded length.
int EncodeUTF16(unsigned int value, unsigned char *outEncodedBuffer, unsigned int *outCharLength, EUnicodeByteOrder byteOrder = LITTLE_ENDIAN);

}

#endif