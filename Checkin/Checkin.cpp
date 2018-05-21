#ifdef TEST_Checkin
/////////////////////////////////////////////////////////////////////
// Checkin.h - Moving the Checked in files                         //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////


#include "Checkin.h"
#include "../PayLoad/PayLoad.h"

using namespace NoSqlDb;

void test() {
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
	int y = v.assignVersion("NoSqlDb", "Edit.h", db);
	std::cout << std::endl << y;
	if (v.existVersion("NoSqlDb", "Edit.h.2", db))
	{
		std::cout << "yes";
	}
	else
	{
		std::cout << "no";
	}

	Checkin<PayLoad> checkin;
	std::string xx= "../Edit/Edit.h";
	std::string yy = "NoSqlDb/Edit.h.1";
	checkin.checkin_file(xx,yy);

}

int main() {
	test();
	
	getchar();
}

#endif