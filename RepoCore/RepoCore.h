#pragma once
/////////////////////////////////////////////////////////////////////
// RepoCore.h - Handles all repo functionality                     //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* Handles core repo operation
* -Checkin files
* -Checkoutout files
* -Browsing a file
* -querying on database

* Required Files:
* ---------------
* Checkin.h,Checkin.cpp
* Checkout.cpp,Checkout.h"
* PayLoad.cpp,PayLoad.h"
* Browse.cpp,Browse.h"
* RepoCore.cpp

* Maintenance History:
* --------------------
* Ver 1.0 : 08 Mar 2018
*  - first release
*/


#include "../Checkin/Checkin.h"
#include "../Checkout/Checkout.h"
#include "../PayLoad/PayLoad.h"
#include "../Browse/Browse.h"



namespace NoSqlDb {

	template<typename T>
	class Repocore
	{
		
	public:
		using RegExp = const std::string&;
		Repocore(DbCore<T>& db) :dbcore(db),query(db),browse(db)
		{
			/*if (!FileSystem::Directory::exists("../Repository"))
			{
				FileSystem::Directory::create("../Repository");
			}*/
			keys_ = query.keys();
		}
		bool Checkin(Path_& srcpath_, Namespace_& namespace_, Keys& keys, Keys& categories, std::string Descrip);
		bool CloseStatus(Namespace_& namespace_, Filename_& filename_);
		bool HoldStatus(Namespace_& namespace_, Filename_& filename_);
		bool Checkout(Namespace_ namespace_,Filename_ filename_,const VersionNo&);
		bool dependCheck(DbElement<T> dbelem);
		void showDbFile(Namespace_ namespace_, Filename_ filename_, const VersionNo&);

		
		Repocore& select_cond(Conditions<T>& cond);
		Repocore& from_(const Keys& keys);
		Keys& keys() { return keys_; }
		void showDependency(Key& key_);
		static void identify(std::ostream& out = std::cout);
		void getQueryFiles(RegExp regExp);
		void getCatQueryFiles(RegExp regExp);

	private:
		bool checkCyclic(Filename_& filename);
		bool addToDb(Key& key, Filename_& filename, Path_& path, Keys& cat, Keys& childs, std::string Descrip);
		bool dependExist(Keys& keys_);
		NoSqlDb::Checkin<T> checkin;
		NoSqlDb::Checkout checkout;
		Version<T> version;
		DbCore<T>& dbcore;
		Browse<T> browse;
		Query<T> query;
		Keys keys_;
		
	};

	//--------------------------------<Displays content of file >------------------------------------
	template<typename T>
	inline void Repocore<T>::showDbFile(Namespace_ namespace_, Filename_ filename_, const VersionNo& version)
	{
		browse.showFile(namespace_,filename_,version);
	}

	//-----------------------------------<To checkin file to repository>------------------------------------

	template<typename T>
	inline bool Repocore<T>::Checkin(Path_& srcpath_, Namespace_& namespace_, Keys& keys,Keys& categories,std::string Descrip)
	{
		if (!FileSystem::Directory::exists("../Storage/"+namespace_))
		{
			FileSystem::Directory::create("../Storage/"+namespace_);
		}
		Filename_ filename = srcpath_.substr(srcpath_.find_last_of("\\") + 1);
		if (version.existVersion(namespace_, filename, dbcore))
		{
			int nextVer =  version.assignVersion(namespace_, filename, dbcore);
			std::string finalname = namespace_ + "/" + filename + "." + std::to_string(nextVer);
			Key keyname= namespace_ + "::" + filename + "." + std::to_string(nextVer);
			filename=filename+ "." + std::to_string(nextVer);//
			if ( checkin.checkin_file(srcpath_,finalname))
			{
				if (dependExist(keys))
				{
					addToDb(keyname, filename, srcpath_, categories,keys, Descrip);
					return true;
				}
				else
				{
					std::cout << std::endl << "Dependencies are not present checkin failed";
					return false;
				}
			}
		}
		else
		{
			Key keyname = namespace_ + "::" + filename + ".1";
			Filename_ finalname = namespace_ + "/" + filename + ".1"  ;
			filename = filename + ".1";//
			if (checkin.checkin_file(srcpath_, finalname))
			{
				if (dependExist(keys))
				{
					addToDb(keyname, filename, srcpath_, categories, keys, Descrip);
					return true;
				}
				else
				{
					std::cout << std::endl << "Dependencies are not present checkin failed";
					return false;
				}
			}
		}
		return false;
	}

	//-----------------------------------<To Close the Checkin status of a file in repo>------------------------------------

	template<typename T>
	inline bool Repocore<T>::CloseStatus(Namespace_ & namespace_, Filename_ & filename_)
	{
		if (version.existVersion(namespace_, filename_, dbcore))
		{
			int nextVer = version.getlastestVersion(namespace_, filename_, dbcore);
			Filename_ finalname = namespace_ + "::" + filename_ + "." + std::to_string(nextVer);
			DbElement<T> elem;
			elem = dbcore[finalname];
			if (dependCheck(elem))
			{
				std::cout << std::endl << "file checkin closed";
				
				elem.CheckinStat(DbElement<PayLoad>::Close);
				dbcore[finalname] = elem;
				return true;
			}
			else
			{
				std::cout << std::endl << "dependency are open";
				if (checkCyclic(finalname))
				{
					CloseStatus(namespace_, filename_);

				}
				return false;
			}
		}
		else
		{
			std::cout << std::endl << "file doesnt exist";
			return false;
		}
		return false;
	}

	//---------------------------------<To put the Checkin status of a file in repo on pending close>------------------------------------

	template<typename T>
	inline bool Repocore<T>::HoldStatus(Namespace_ & namespace_, Filename_ & filename_)
	{
		if (version.existVersion(namespace_, filename_, dbcore))
		{
			int nextVer = version.getlastestVersion(namespace_, filename_, dbcore);
			Filename_ finalname = namespace_ + "::" + filename_ + "." + std::to_string(nextVer);
			DbElement<T> elem;
			elem = dbcore[finalname];
			if (dependCheck(elem))
			{
				std::cout << std::endl << "file checkin is pending";
				elem.CheckinStat(DbElement<PayLoad>::Pending);
				dbcore[finalname] = elem;
				return true;
			}
			else
			{
				std::cout << std::endl << "dependency are open";
				return false;
			}
		}
		else
		{
			std::cout << std::endl << "file doesnt exist";
			return false;
		}
		return false;
	}

	//-----------------------------------<To Checkout a file from repo>------------------------------------

	template<typename T>
	inline bool Repocore<T>::Checkout(Namespace_ namespace_, Filename_ filename_,const VersionNo& version_)
	{
		Path_ filesrc = "../Storage/" + namespace_ + "/" + filename_ + "." + std::to_string(version_);
		filename_=filename_+"."+ std::to_string(version_);
		if (version.existVersion(namespace_, filename_, dbcore))
		{
			if (checkout.file_Checkout(filesrc,namespace_)) 
			{
				std::string keyname=namespace_+"::"+filename_;
				DbElement<PayLoad> elem = dbcore[keyname];
				for (auto key:elem.children())
				{
					DbElement<PayLoad> elem1 = dbcore[key];
					filesrc = "../Storage/" + namespace_ + "/" + elem1.name();
					checkout.file_Checkout(filesrc, namespace_);
					for (auto value:elem1.children())
					{
						DbElement<PayLoad> elem2 = dbcore[value];
						filesrc = "../Storage/" + namespace_ + "/" + elem2.name();
						checkout.file_Checkout(filesrc, namespace_);
					}
				}
				std::cout << std::endl << "file checked out";
				return true;
			}
		}
		else
		{
			std::cout << std::endl << "file not found";
			return false;
		}
	
		return false;
	}

	//-----------------------------------<Checks the file depndencies if closed>------------------------------------
	template<typename T>
	inline bool Repocore<T>::dependCheck(DbElement<T> dbelem)
	{
		DbElement<T> elem;
		if (!dbelem.children().empty())
		{
			for (auto key : dbelem.children())
			{
				elem = dbcore[key];
				if (elem.CheckinStat()== DbElement<PayLoad>::Open)
				{
					std::cout << std::endl << key << "file is open";
					return false;
				}
			}
		}
		else
		{
			return true;
		}
		return true;
	}

	//------------------------------------------< Checking cyclic dependencies>---------------------------------
	template<typename T>
	inline bool Repocore<T>::checkCyclic(Filename_& filename)
	{
		DbElement<PayLoad> elem = dbcore[filename];
		for (auto key:elem.children())
		{
			DbElement<PayLoad> elem1 = dbcore[key];
			if  (std::find(elem1.children().begin(), elem1.children().end(), filename) != elem1.children().end())
			{
				elem1 = dbcore[key];
				elem1.CheckinStat( DbElement<PayLoad>::Pending);
				dbcore[key] = elem1;
				return true;
			}
		}
		return false;
	}

	//-----------------------------------<Add the checkin info to database>------------------------------------
	template<typename T>
	inline bool Repocore<T>::addToDb(Key& key,Filename_& filename,Path_& path,Keys& cat,Keys& childs, std::string Descrip)
	{
		PayLoad payload;
		payload.value(path);
		payload.categories(cat);
		DbElement<T> elem;
		elem.name( filename);
		elem.descrip(Descrip);
		elem.payLoad(payload);
		elem.children(childs);
		dbcore[key] = elem;
		return true;
	}

	//-----------------------------------<Checks id dependencies exist>------------------------------------
	template<typename T>
	inline bool Repocore<T>::dependExist(Keys& keys_)
	{
		if (keys_.empty())
		{
			return true;
		}
		else
		{
			for (auto key:keys_)
			{
				if (!dbcore.contains(key))
				{
					return false;
				}
			}
			return true;
		}
	}

	

	//-----------------------------------< query on condition>------------------------------------
	template<typename T>
	inline Repocore<T>& Repocore<T>::select_cond(Conditions<T>& cond)
	{
		query.select(cond);
		keys_ = query.keys();
		return *this;
	}

	//-----------------------------------<get keys from certain function>------------------------------------
	template<typename T>
	inline Repocore<T>& Repocore<T>::from_(const Keys & keys)
	{
		query.from(keys);
		keys_ = query.keys();
		return *this;
	}

	//-----------------------------------<Show dependecies of file>------------------------------------
	template<typename T>
	inline void Repocore<T>::showDependency(Key& key_)
	{
		DbCore<T> db_;
		DbElement<PayLoad> elem;
		elem = dbcore[key_];
		for (auto value : elem.children())
		{
			db_[value] = dbcore[value];
		}
		showDb(db_);
	}
	// --------------------- show file name--------------------------
	template<typename T>
	inline void Repocore<T>::identify(std::ostream & out)
	{
		out << "\n  \"" << __FILE__ << "\"";
	}

	//------------------ Query on keys----------------------
	template<typename T>
	inline void Repocore<T>::getQueryFiles(RegExp regExp)
	{
		browse.getFiles(regExp);
	}

	//------------------ Query on Category----------------------
	template<typename T>
	inline void Repocore<T>::getCatQueryFiles(RegExp regExp)
	{
		browse.getCatFiles(regExp);
	}

}