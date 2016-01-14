/*
   AngelCode Tool Box Library
   Copyright (c) 2008-2009 Andreas Jonsson
  
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
  
   Andreas Jonsson
   andreas@angelcode.com
*/

// 2009-06-18 Added const correctness
// 2009-03-11 Changed index to unsigned to avoid crash on negative values
// 2008-12-07 Added GetAttrAsBool
// 2008-11-13 Added support for multiple attributes of the same name

#include "acutil_config.h"
#include "acutil_log.h"

using namespace std;

namespace acUtility
{

CConfig::CConfig()
{
}

CConfig::~CConfig()
{
}

int CConfig::LoadConfigFile(const char *file)
{
	FILE *f = fopen(file, "rb");
	if( f )
	{
		char buffer[512];

		// Parse the material file for attributes
		while( fgets(buffer, 512, f) )
		{
			if( buffer[0] == '#' || buffer[0] == ' ' || buffer[0] == '\n' || buffer[0] == '\r' || buffer[0] == '\t' )
				continue;

			char *token = strchr(buffer, '=');
			if( token ) 
			{
				*token++ = 0;
				int len = (int)strlen(token);
				if( token[len-1] == '\n' || token[len-1] == '\r' )
					token[--len] = 0;
				if( token[len-1] == '\n' || token[len-1] == '\r' )
					token[--len] = 0;

				string attr = buffer;
				string value = token;

				map<string,vector<string> >::iterator it = attrMap.find(attr);
				if( it == attrMap.end() )
					attrMap.insert(map<string,vector<string> >::value_type(attr, vector<string>(1, value)));
				else
					it->second.push_back(value);
			}
		}

		fclose(f);
	}
	else
	{
		LOG(("Failed to load file '%s' (%d)", file, errno));
	}

	return 0;
}

int CConfig::GetAttrCount(const char *name) const
{
	map<string, vector<string> >::const_iterator it = attrMap.find(name);
	if( it != attrMap.end() )
		return (int)it->second.size();

	return 0;
}

bool CConfig::GetAttrAsInt(const char *name, int &value, unsigned int index, int defaultValue) const
{
	map<string,vector<string> >::const_iterator it = attrMap.find(name);
	if( it != attrMap.end() && index < (int)it->second.size() )
	{
		value = atoi(it->second[index].c_str());
		return true;
	}

	value = defaultValue;
	return false;
}

bool CConfig::GetAttrAsFloat(const char *name, float &value, unsigned int index, float defaultValue) const
{
	map<string,vector<string> >::const_iterator it = attrMap.find(name);
	if( it != attrMap.end() && index < (int)it->second.size() )
	{
		value = (float)atof(it->second[index].c_str());
		return true;
	}

	value = defaultValue;
	return false;
}

bool CConfig::GetAttrAsDouble(const char *name, double &value, unsigned int index, double defaultValue) const
{
	map<string,vector<string> >::const_iterator it = attrMap.find(name);
	if( it != attrMap.end() && index < (int)it->second.size() )
	{
		value = atof(it->second[index].c_str());
		return true;
	}

	value = defaultValue;
	return false;
}

bool CConfig::GetAttrAsBool(const char *name, bool &value, unsigned int index, bool defaultValue) const
{
	map<string,vector<string> >::const_iterator it = attrMap.find(name);
	if( it != attrMap.end() && index < (int)it->second.size() )
	{
		int i = atoi(it->second[index].c_str());
		value = i ? true : false;
		return true;
	}

	value = defaultValue;
	return false;
}

bool CConfig::GetAttrAsString(const char *name, string &value, unsigned int index, const string &defaultValue) const
{
	map<string,vector<string> >::const_iterator it = attrMap.find(name);
	if( it != attrMap.end() && index < (int)it->second.size() )
	{
		value = it->second[index];
		return true;
	}

	value = defaultValue;
	return false;
}

}
