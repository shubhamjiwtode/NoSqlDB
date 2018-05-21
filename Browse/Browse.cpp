/////////////////////////////////////////////////////////////////////
// Browse.cpp-implements information display using Process & query //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////

#include "Browse.h"
#include "../PayLoad/PayLoad.h"
using namespace NoSqlDb;


#ifdef test_Browse


int main() {
	DbCore<PayLoad> db;
	DbElement<PayLoad> elem;
	elem.name("File1");
	elem.descrip("trail file");
	db["FF1"] = elem;
	Browse<PayLoad> browser(db);
	browser.getFiles("File");

	return 0;
}


#endif // test_Browse
