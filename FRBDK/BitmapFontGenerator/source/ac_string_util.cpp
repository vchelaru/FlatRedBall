/*
   ac_string_util.cpp - version 1.0, June 25h, 2003

   Some string utility functions, that doesn't necessitate
   use of acCString.

   Copyright (c) 2003 Andreas Jönsson

   This software is provided 'as-is', without any form of
   warranty. In no case will the author be held responsible
   for any damage caused by its use.

   Permission is granted to anyone to use the software for
   for any purpose, including commercial. It is also allowed
   to modify the software and redistribute it free of charge.
   The permission is granted with the following restrictions:

   1. The origin of the software may not be misrepresented.
      It must be plainly understandable who is the author of
      the original software.
   2. Altered versions must not be misrepresented as the
      original software, i.e. must be plainly marked as
      altered.
   3. This notice may not be removed or altered from any
      distribution of the software, altered or not.

   Andreas Jönsson
   andreas@angelcode.com
*/

#include <stdarg.h>		// va_list, va_start(), etc
#include <stdlib.h>     // strtod(), strtol()
#include <assert.h>     // assert()
#include <stdio.h>      // _vsnprintf()
#include "ac_string_util.h"

string acStringFormat(const char *format, ...)
{
	string ret;

	va_list args;
	va_start(args, format);

	char tmp[256];
	int r = _vsnprintf(tmp, 255, format, args);

	if( r > 0 )
	{
		ret = tmp;
	}
	else
	{
		int n = 512;
		string str; 
		str.resize(n);

		while( (r = _vsnprintf(&str[0], n, format, args)) < 0 )
		{
			n *= 2;
			str.resize(n);
		}

		ret = str.c_str();
	}

	va_end(args);

	return ret;
}

double acStringScanDouble(const char *string, int *numScanned)
{
	char *end;

	double res = ::strtod(string, &end);

	if( numScanned )
		*numScanned = end - string;

	return res;
}

int acStringScanInt(const char *string, int base, int *numScanned)
{
	assert(base > 0);

	char *end;

	int res = ::strtol(string, &end, base);

	if( numScanned )
		*numScanned = end - string;

	return res;
}

acUINT acStringScanUInt(const char *string, int base, int *numScanned)
{
	assert(base > 0);

	char *end;

	acUINT res = ::strtoul(string, &end, base);

	if( numScanned )
		*numScanned = end - string;

	return res;
}

// Algorithm presented by Dan Berstein in comp.lang.c
acUINT acStringHash(const char *string)
{
	acUINT hash = 5381;
	acUINT c;

	while(c = (unsigned)*string++)
		hash = ((hash << 5) + hash) + c; // hash * 33 + c

	return hash;
}