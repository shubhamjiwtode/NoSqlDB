#pragma once
/////////////////////////////////////////////////////////////////////
// Checkout.h - Moving the checked out files                       //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* Checkout file from repo when asked
* and copies to Client

* Required Files:
* ---------------
* Version.h,Version.cpp
* FileSystem.h,FileSystem.cpp
* Defination.h
* Checkout.cpp
*
* Maintenance History:
* --------------------
* Ver 1.0 : 1 May 2018
*  - first release
*/

#include "../Versioning/Version.h"
#include "../DbCore/Definitions.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"

using namespace FileSystem;

namespace NoSqlDb 
{
	class Checkout
	{
	public:
		bool file_Checkout(Path_ srcpath_,Namespace_ namespace_);
		bool copy_file(Path_ & srcpath, std::string finalname);
		static void identify(std::ostream& out = std::cout);
		Checkout() 
		{
			if (!FileSystem::Directory::exists("../ServerPrototype/SendFiles"))
			{
				FileSystem::Directory::create("../ServerPrototype/SendFiles");

			}
		}
	private:
	};


}