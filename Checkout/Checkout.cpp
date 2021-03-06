/////////////////////////////////////////////////////////////////////
// Checkout.h - Moving the checked out files                       //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//                                                                 //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////


#include "Checkout.h"

using namespace NoSqlDb;

//--------------------------------<Checks out file from repository to client folder>------------------------------------
bool Checkout::file_Checkout(Path_ srcpath_,Namespace_ namespace_)
{
	if (!FileSystem::Directory::exists("../ServerPrototype/SendFiles/"))
	{
		FileSystem::Directory::create("../ServerPrototype/SendFiles/");
	}
	std::string finalname;
	 finalname = srcpath_.substr(0, srcpath_.find_last_of("."));
	if (copy_file(srcpath_, finalname))
	{
		std::cout << std::endl << finalname << "  file copied";
		return true;
	}
	else
	{
		std::cout << std::endl << finalname << "  file not copied";
	}
	return false;
}

//--------------------------------<Copies file from repo to client folder after checkout>------------------------------------
bool Checkout::copy_file(Path_ & srcpath, std::string finalname)
{
	std::string finalName = finalname.substr(finalname.find_last_of("/"),finalname.length());
	std::string destination = "../ServerPrototype/SendFiles" + finalName;
	if (FileSystem::File::copy(srcpath, destination))
	{
		return true;
	}
	return false;
}

//----< show file name >---------------------------------------------
void NoSqlDb::Checkout::identify(std::ostream & out)
{
	out << "\n  \"" << __FILE__ << "\"";
}




#ifdef Test_Checkout
void test() {
	Checkout checkout;
	std::string x = "../Repository/NoSqlDb/PayLoad.h.1";
		checkout.file_Checkout(x, "NoSqlDb");
}

int main() 
{
	test();
	getchar();
}



#endif // Test_Checkout
