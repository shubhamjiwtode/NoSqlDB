/////////////////////////////////////////////////////////////////////
// RepoCore.h - Handles all repo functionality                     //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////


#include "../RepoCore/RepoCore.h"

using namespace NoSqlDb;


#ifdef Test_REPOCORE

void testrepo() {
	DbCore<PayLoad> db;
	/*
	DbElement<PayLoad> elem = makeElement<PayLoad>("a name", "a description");
	PayLoad pl;
	pl.value("..//..//Repository//NoSqlDb//Edit.h.1");
	pl.categories().push_back("some category");
	elem.payLoad(pl);
	db["NoSqlDb::Edit.h.1"] = elem;
	db["NoSqlDb::Edit.h.2"] = elem;
	showDb(db);*/
	
	Path_ srcpath = "../Edit/Edit.h";
	Namespace_ namespace_ = "NoSqlDb";
	Repocore<PayLoad> repocore(db);
	repocore.Checkin(srcpath,namespace_);
	std::string x = "xxxx";
	repocore.Checkout(namespace_,x,1);

	showDb(db);
}

int main() 
{
	testrepo();
	getchar();
}

#endif // Test_DBCORE


