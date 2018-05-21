#pragma once
/////////////////////////////////////////////////////////////////////
// Verson.h - handles versioning of files                          //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* Handles versioning of file
* getting latest version
* check if version exist

* Required Files:
* ---------------
* Defination.h
* DbCore.h,DbCore.cpp
* Version.cpp

* Maintenance History:
* --------------------
* Ver 1.0 : 08 Mar 2018
*  - first release
*/

#include <string>
#include <regex>
#include "../DbCore/Definitions.h"
#include "../DbCore/DbCore.h"

namespace NoSqlDb 
{
	template<typename T>
	class Version
	{
	public:
		int getlastestVersion(Namespace_ namespace_, Filename_ filename,DbCore<T>&);
		int assignVersion(Namespace_ namespace_, Filename_ filename, DbCore<T>&);
		bool existVersion(Namespace_ namespace_, Filename_ filename, DbCore<T>&);
		static void identify(std::ostream& out = std::cout);
	private:
		int extractVersion(Filename_& filename);
	};


	//--------------------------------<Extract latest verion of file in repository>------------------------------------	
	template<typename T>
	inline int Version<T>::getlastestVersion(Namespace_ namespace_, Filename_ filename, DbCore<T>& dbCore)
	{
		Keys keyVec;
		keyVec = dbCore.keys();
		std::regex e(namespace_ + "::" + filename);
		int maxVer = 0;
		std::smatch s;
		for (auto it : keyVec)
		{
			int max = 0;
			if (std::regex_search(it, e))
			{
				max = extractVersion(it);
				if (max>maxVer)
				{
					maxVer = max;
				}
			}
		}
		return maxVer;
	}

	//--------------------------------<Extract current+1 version of file in repository>------------------------------------
	template<typename T>
	inline int Version<T>::assignVersion(Namespace_ namespace_, Filename_ filename, DbCore<T>& dbCore)
	{
		DbElement<T> elem;
		Keys keyVec;
		keyVec = dbCore.keys();
		std::regex e(namespace_ + "::" + filename);
		int maxVer=0;
		std::smatch s;
		for(auto it:keyVec)
		{
			int max=0;
			if (std::regex_search(it, e))
			{
				max = extractVersion(it);
				if (max>maxVer)
				{
					maxVer = max;
				}
			}
		} 
		Filename_ name = namespace_ + "::" + filename + "." + std::to_string(maxVer);
		elem = dbCore[name];
		if (elem.CheckinStat()==elem.Open)
		{
			return maxVer;
		}
		else
		{
			return maxVer + 1;
		}
		return 0;
	}

	//--------------------------------<Checks whether version of specific file exist of not>------------------------------------
	template<typename T>
	inline bool Version<T>::existVersion(Namespace_ namespace_, Filename_ filename, DbCore<T>& dbCore)
	{
		Keys keyVec;
		keyVec = dbCore.keys();
		std::regex e(namespace_ + "::" + filename);
		std::smatch s;
		for (auto it : keyVec)
		{
			int max = 0;
			if (std::regex_search(it, e))
			{
				return true;
			}
		}
		return false;
	}

	//----< show file name >---------------------------------------------
	template<typename T>
	void Version<T>::identify(std::ostream& out)
	{
		out << "\n  \"" << __FILE__ << "\"";
	}

	//--------------------------------<Gives the version number of certain file>------------------------------------
	template<typename T>
	inline int Version<T>::extractVersion(Filename_ & filename)
	{
		std::string rawname;
		rawname = filename.substr(filename.find_last_not_of("."), filename.length());
		return std::stoi(rawname);
	}

	


}