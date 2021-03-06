
/////////////////////////////////////////////////////////////////////
// TextExecutive.cpp - executes all test requirement               //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////

#include "../RepoCore/RepoCore.h"
#include "../PayLoad/PayLoad.h"
#include "../Utilities/TestUtilities/TestUtilities.h"

using namespace NoSqlDb;

class test {
public:
	test(DbCore<PayLoad>& db_) :db(db_){}
	void Test1();
	void test2();
	void test3();
	void test4();
	void test5();
	void test6();
private:
	DbCore<PayLoad> db;
};

void test::Test1() 
{
	std::cout << "\n                    ++++++++++++++ Demontrating Requirement #3 ++++++++++++++++++ \n";
	Repocore<PayLoad>::identify();
	Checkin<PayLoad>::identify();
	Checkout::identify();
	Version<PayLoad>::identify();
	Browse<PayLoad>::identify();


}

void  test::test2() 
{
	std::cout << "\n                    ++++++++++++++ Demontrating Requirement #4 (Checkin) ++++++++++++++++++ \n";
	Repocore<PayLoad> repocore(db);
	Path_ srcpath = "../DateTime/DateTime.h";
	Namespace_ nSpace = "NoSqlDb";
	Keys vec;
	Keys children;
	vec.push_back("cat1");
	vec.push_back("cat3");
	Filename_ filen = "DateTime.h";
	std::cout <<"\n\n---------------------------------------------- Checking in first file DateTime.h \n";
	repocore.Checkin(srcpath,nSpace,children,vec);

	showDb(db);
	std::cout << "\n\n--------------------------------------------- Closing the DateTime.h.1 checkinstatus in first file DateTime.h\n";
	repocore.CloseStatus(nSpace, filen);
	showDb(db);
	std::cout << "\n\n--------------------------------------------- Checking in new version of DateTime.h\n";
	repocore.Checkin(srcpath, nSpace, children, vec);
	showDb(db);
	std::cout << "\n\n--------------------------------------------- Checking new DateTime.h, while keeping previous checkin open\n";
	repocore.Checkin(srcpath, nSpace, children, vec);
	showDb(db);
	std::cout << "\n\n--------------------------------------------- Checking Query.h, but unable to close as dependency DateTime.h.2 is open \n";
	children.push_back("NoSqlDb::DateTime.h.2");
	srcpath = "../Query/Query.h";
	filen = "Query.h";
	repocore.Checkin(srcpath, nSpace, children, vec);
	showDb(db);
	std::cout << "\n\n                    ++++++++++++++++++++++++++ CheCking In More Files in NoSqlDb namespace ++++++++++++++++++++++++++++ \n";
	repocore.CloseStatus(nSpace,filen);
	vec.clear();
	vec.push_back("cat2");
	filen = "DateTime.cpp";
	srcpath = "../DateTime/DateTime.cpp";
	repocore.Checkin(srcpath, nSpace, children, vec);
	repocore.CloseStatus(nSpace, filen);
	srcpath = "../Query/Query.cpp";
	children.clear();
	children.push_back("NoSqlDb::Query.h.1");
	filen = "Query.cpp";
	repocore.Checkin(srcpath, nSpace, children, vec);
	repocore.CloseStatus(nSpace, filen);
	showDb(db);
}

void test::test3()
{
	std::cout << "\n\n                    ++++++++++++++++++++++++++ CheCking In More Files in File namespace ++++++++++++++++++++++++++++ \n";
	Repocore<PayLoad> repocore(db);
	Path_ srcpath = "../FileSystemDemo/FileSystem.h";
	Namespace_ nSpace = "File";
	Keys vec;
	Keys children;
	vec.push_back("cat4");
	Filename_ filename = "FileSystem.h";
	repocore.Checkin(srcpath, nSpace, children, vec);
	repocore.CloseStatus(nSpace, filename);
	repocore.Checkin(srcpath, nSpace, children, vec);

	children.push_back("File::FileSystem.h.1");
	
	srcpath = "../FileSystemDemo/FileSystem.cpp";
	vec.push_back("cat3");
	repocore.Checkin(srcpath, nSpace, children, vec);
	
	srcpath = "../Checkin/Checkin.h";
	repocore.Checkin(srcpath, nSpace, children, vec);

	srcpath = "../Checkout/Checkout.cpp";

	repocore.Checkin(srcpath, nSpace, children, vec);
	showDb(db);

	repocore.getQueryFiles("cpp");
}

void test::test4()
{
	Repocore<PayLoad> repocore(db);
	std::cout << "\n                    ++++++++++++++ Demontrating Requirement #4 (Checkout) ++++++++++++++++++ \n";
	Namespace_ namespace_ = "NoSqlDb";
	Filename_ filename = "Query.cpp";
	std::cout << "\n\n--------------------------------checked out file Query.cpp with dependencies------------- ";
	repocore.Checkout(namespace_,filename,1);
}

void test::test5()
{
	Repocore<PayLoad> repocore(db);
	std::cout << "\n                    ++++++++++++++ Demontrating Requirement #5 (Query) ++++++++++++++++++ \n\n";
	std::cout << "\n\n                    [][]-------------------- Performing query on Namespace----NoSqlDb files of this namespace are--------------------[][]\n";
	repocore.getQueryFiles("NoSqlDb");
	std::cout << "\n\n                    [][]-------------------- Performing query on file----DateTime.h files of this namespace are--------------------[][]\n";
	repocore.getQueryFiles("DateTime.h");
	std::cout << "\n\n                    [][]-------------------- Performing query for all cpp files result is--------------------[][]\n";
	repocore.getQueryFiles(".cpp");
	std::cout << "\n\n                    [][]-------------------- Performing category query for all cat2 files reuslt is--------------------[][]\n";
	repocore.getCatQueryFiles("cat2");
	std::cout << "\n\n                    [][]-------------------- Shows dependecies of Query.h --------------------[][]\n";
	std::string file = "NoSqlDb::Query.h.1";
	repocore.showDependency(file);
	Namespace_ namespace_ = "File";
	Filename_ filename = "FileSystem.cpp";
	std::cout << "\n\n                    ++++++++++++++ Displaying FileSystem.cpp file ++++++++++++++++++ \n";
	repocore.showDbFile(namespace_,filename,1);
}

void test::test6()
{
	std::cout << "\n\n                        ++++++++++++++ Demontrating Circular dependency  ++++++++++++++++++ \n";
	Repocore<PayLoad> repocore(db);
	Path_ srcpath = "../Persist/Persist.h";
	Namespace_ nSpace = "Circular";
	Keys vec;
	Keys children;
	Key key_ = "Circular::Persist.h.1";
	Key key2_ = "Circular::Process.h.1";
	Key filename = "Process.h";
	std::cout << "\n\n---------------------------------------------- Checking  Persist.h \n";
	repocore.Checkin(srcpath, nSpace, children, vec);
	srcpath = "../Process/Process.h";
	std::cout << "\n\n---------------------------------------------- Checking  Process.h \n";
	children.push_back(key_);
	repocore.Checkin(srcpath, nSpace, children, vec);
	children.clear();
	srcpath = "../Persist/Persist.h";
	children.push_back(key2_);
	repocore.Checkin(srcpath, nSpace, children, vec);
	showDb(db);
	filename = "Process.h";
	std::cout << "\n\n---------------------------------------------- Attempting to close Process having cicular dependency with Persist \n";
	repocore.CloseStatus(nSpace,filename);
	std::cout << "\n\n---------------------------------------------- Cyclic dependency handled for Process and Persist \n";
	showDb(db);
	srcpath = "../Persist/Persist.h";
	std::cout << "\n\n       ++++++++++++++++++++++++++++++++++++ Checked in Persist file on Persist.h.1(Pending) ++++++++++++++++++++++++++++ \n";
	repocore.Checkin(srcpath, nSpace, children, vec);
	showDb(db);

}



int main()
{
	DbCore<PayLoad> db;
	test t(db);
	t.Test1();
	t.test2();
	t.test3();
	t.test4();
	t.test5();
	t.test6();
	getchar();
    return 0;
}

