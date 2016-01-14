/*
   ac_string_util.h - version 1.0, June 25h, 2003

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


#ifndef AC_STRING_UTIL_H
#define AC_STRING_UTIL_H

#include <string>
using std::string;

typedef unsigned int acUINT;

double acStringScanDouble(const char *string, int *numScanned);
int    acStringScanInt(const char *string, int base, int *numScanned);
acUINT acStringScanUInt(const char *string, int base, int *numScanned);
acUINT acStringHash(const char *string);

string acStringFormat(const char *format, ...);

#endif
