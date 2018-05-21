#pragma once
/////////////////////////////////////////////////////////////////////
// Browse.h - Handles information display using Process            //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* Handles query on database
* Display particular file content when requested

* Required Files:
* ---------------
* Defination.h
* Process.h,Process.cpp
* FileSystem.h,FileSystem.cpp
* Query.h,Query.cpp
* Browse.cpp

* Maintenance History:
* --------------------
*Ver 1.0 : 1 May 2018
*  - first release
*/

#include "../DbCore/Definitions.h"
#include "../Process/Process.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../Query/Query.h"
using namespace FileSystem;



namespace NoSqlDb 
{
	template <typename T>
	class Browse
	{
	public:
		using RegExp = const std::string&;
		Browse(DbCore<T>& db):dbcore(db) {}
		void showFile(Namespace_& namespace_,Filename_& filename_,const VersionNo& version);
		void showAllDirec();
		static void identify(std::ostream& out = std::cout);
		void getFiles(RegExp regExp);
		void getCatFiles(RegExp regExp);


	private:
		Keys keys_;
		Directory direct;
		DbCore<T>& dbcore;
	};

	//--------------------------------<Uses process to display file in notepad>------------------------------------
	
	template<typename T>
	inline void Browse<T>::showFile(Namespace_ & namespace_, Filename_ & filename_,const VersionNo & version)
	{
		
		Process p;
		p.title("Browsing File");
		std::string appPath = "c:/windows/system32/notepad.exe";
		p.application(appPath);
		std::string cmdLine = "/A ../Storage/"+namespace_+"/"+filename_+"."+std::to_string(version);
		p.commandLine(cmdLine);
		std::cout << "\n  starting process: \"" << appPath << "\"";
		std::cout << "\n  with this cmdlne: \"" << cmdLine << "\"";
		p.create();
		CBP callback = []() { std::cout << "\n  --- child process exited with this message ---"; };
		p.setCallBackProcessing(callback);
		p.registerCallback();
	}

	//--------------------------------<Gets all Diectories in Repo>------------------------------------
	template<typename T>
	inline void Browse<T>::showAllDirec()
	{
		direct.getDirectories();
	}
	//------------- shows filename-------------------------
	template<typename T>
	inline void Browse<T>::identify(std::ostream & out)
	{
		out << "\n  \"" << __FILE__ << "\"";
	}

	//------------- Does query on keys -------------------------
	template<typename T>
	inline void Browse<T>::getFiles(RegExp regExp)
	{
		DbCore<T> db_;
		Keys veckeys;
		for (auto key:dbcore.keys())
		{
			std::regex re(regExp);
			if (std::regex_search(key, re))
			{
				db_[key]= dbcore[key];
			}
		}
		showDb(db_);
	}

	//------------- Does query on category-------------------------
	template<typename T>
	inline void Browse<T>::getCatFiles(RegExp regExp)
	{
		DbCore<T> db_;
		Keys veckeys;
		DbElement<PayLoad> elem;
		for (auto key : dbcore.keys())
		{
			elem = dbcore[key];
			for (auto value : elem.payLoad().categories())
			{
				std::regex re(regExp);
				if (std::regex_search(value, re))
				{
					db_[key] = dbcore[key];
				}
			}
		}
		showDb(db_);
	}


}