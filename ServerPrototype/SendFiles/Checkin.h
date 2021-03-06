#pragma once
/////////////////////////////////////////////////////////////////////
// Checkin.h - Moving the Checked in files                         //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* Checks in file and copies them from 
  their location to respository

* Required Files:
* ---------------
* FileSystem.h,FileSystem.cpp
* Dbcore.h,Dbcore.cpp
 checkin.cpp

* Maintenance History:
* --------------------
*Ver 1.0 : 1 May 2018
*  - first release
*/




#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../DbCore/DbCore.h"
using namespace FileSystem;

namespace NoSqlDb {


	template <typename T>
	class Checkin 
	{
		
	public:
		Checkin() {}
		bool checkin_file(Path_& srcpath_, Filename_& finalname);
		bool copy_file(Path_& srcpath,std::string);
		static void identify(std::ostream& out = std::cout);
	private:
		
		
	};

	//--------------------------------<Checks in file from certain path to repository>------------------------------------
	template <typename T>
	inline bool Checkin<T>::checkin_file(Path_& srcpath_,Filename_& finalname)
	{
		if (copy_file(srcpath_, finalname))
		{
			std::cout << std::endl<< "  file copied to Repository"<<finalname;
			return true;
		}
		else
		{
			std::cout << std::endl <<  finalname<< "  file not copied";
			return false;
		}	
	    return false;
	}

	//--------------------------------<Copies checkin files to repository>------------------------------------
	template<typename T>
	inline bool Checkin<T>::copy_file(Path_& srcpath_, std::string finalname)
	{
		std::string destination = "../Storage/"+ finalname;
		std::string FILENAME= srcpath_.substr(srcpath_.find_last_of("\\") + 1);
		std::string source = "../ServerPrototype/SaveFiles/"+FILENAME;
		std::string srname = FileSystem::Path::getFullFileSpec(source);
		std::string dname = FileSystem::Path::getFullFileSpec(destination);
		if (FileSystem::File::copy(srname,dname))
		{
			return true;
		}
		return false;
	}

	//---------------< show filename>-----------------
	template<typename T>
	inline void Checkin<T>::identify(std::ostream & out)
	{
		out << "\n  \"" << __FILE__ << "\"";
	}
}
