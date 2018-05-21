#ifdef TEST_VERSION
/////////////////////////////////////////////////////////////////////
// Verson.h - handles versioning of files                          //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////



#include <chrono>
#include "Version.h"
#include "../PayLoad/PayLoad.h"
#include "../Utilities/StringUtilities/StringUtilities.h"

using namespace NoSqlDb;
void test() 
{
	DbCore<PayLoad> db;

	DbElement<PayLoad> elem = makeElement<PayLoad>("a name", "a description");
	PayLoad pl;
	pl.value("..//..//Repository//NoSqlDb//Edit.h.1");
	pl.categories().push_back("some category");
	elem.payLoad(pl);
	db["NoSqlDb::Edit.h.1"] = elem;
	db["NoSqlDb::Edit.h.2"] = elem;
	showDb(db);
	Version<PayLoad> v;
	int y=v.assignVersion("NoSqlDb", "Edit.h", db);
	std::cout << std::endl << y;
	if (v.existVersion("NoSqlDb", "Edit.h.2", db))
	{
		std::cout << "yes";
	}
	else
	{
		std::cout << "no";
	}
}
int main() {
	test();
	std::string x = "../Client/Client.cpp";
	std::string rawname;
	rawname = x.substr(x.find_last_of("//")+1);
	std::cout << std::endl << rawname;
	getchar();
}

#endif