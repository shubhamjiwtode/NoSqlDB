/////////////////////////////////////////////////////////////////////////
// ServerPrototype.cpp - Console App that processes incoming messages  //
// ver 1.1                                                             //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018           //
/////////////////////////////////////////////////////////////////////////

#include "ServerPrototype.h"
#include"../Persist/Persist.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../Process/Process.h"
#include <chrono>
#include <regex>

namespace MsgPassComm = MsgPassingCommunication;

using namespace Repository;
using namespace FileSystem;
using Msg = MsgPassingCommunication::Message;

//----< return name of every file on path >----------------------------

Files Server::getFiles(const Repository::SearchPath& path)
{
  return Directory::getFiles(path);
}
//----< return name of every subdirectory on path >--------------------

Dirs Server::getDirs(const Repository::SearchPath& path)
{
  return Directory::getDirectories(path);
}



namespace MsgPassingCommunication
{
  // These paths, global to MsgPassingCommunication, are needed by 
  // several of the ServerProcs, below.
  // - should make them const and make copies for ServerProc usage

  std::string sendFilePath;
  std::string saveFilePath;
  
  //----< show message contents >--------------------------------------

  template<typename T>
  void show(const T& t, const std::string& msg)
  {
    std::cout << "\n  " << msg.c_str();
    for (auto item : t)
    {
      std::cout << "\n    " << item.c_str();
    }
  }
  //----< test ServerProc simply echos message back to sender >--------

  std::function<Msg(Msg)> echo = [](Msg msg) {
    Msg reply = msg;
    reply.to(msg.from());
    reply.from(msg.to());
    return reply;
  };
  //----< getFiles ServerProc returns list of files on path >----------

  std::function<Msg(Msg)> getFiles = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command("getFiles");
	reply.attribute("path", msg.value("path"));
    std::string path = msg.value("path");
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != ".")
        searchPath = searchPath + "\\" + path;
      Files files = Server::getFiles(searchPath);
      size_t count = 0;
      for (auto item : files)
      {
        std::string countStr = Utilities::Converter<size_t>::toString(++count);
        reply.attribute("file" + countStr, item);
      }
    }
    else
    {
      std::cout << "\n  getFiles message did not define a path attribute";
    }
    return reply;
  };
  //----< getDirs ServerProc returns list of directories on path >-----

  std::function<Msg(Msg)> getDirs = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command("getDirs");
    std::string path = msg.value("path");
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != ".")
        searchPath = searchPath + "\\" + path;
      Files dirs = Server::getDirs(searchPath);
      size_t count = 0;
      for (auto item : dirs)
      {
        if (item != ".." && item != ".")
        {
          std::string countStr = Utilities::Converter<size_t>::toString(++count);
          reply.attribute("dir" + countStr, item);
        }
      }
    }
    else
    {
      std::cout << "\n  getDirs message did not define a path attribute";
    }
    return reply;
  };
  //----< To close the Checkin >-----
  bool closecheckin(std::string namespace_, std::string filename_)
  {
	  Repocore<PayLoad> repocore(Repository::db_);
	  if (repocore.CloseStatus(namespace_, filename_)) {
		  return true;
	  }
	  return false;
  }

  //----< Handles checkin command >-----
  bool makeCheckin(std::string path_, std::string namespace_, std::string depend_, std::string category_, std::string descrip_)
  {
	  
	  Repocore<PayLoad> repocore(Repository::db_);
	  std::vector<std::string> dependVect;
	  std::stringstream ss(depend_);
	  if (depend_ == "na")
	  {
		  dependVect.clear();
	  }
	  else
	  {
		  while (ss.good())
		  {
			  std::string substr;
			  getline(ss, substr, '|');
			  dependVect.push_back(substr);
		  }
	  }
	  std::vector<std::string> catVect;
	  std::stringstream sscat(category_);
	  while (sscat.good())
	  {
		  std::string substr;
		  getline(sscat, substr, '|');
		  catVect.push_back(substr);
	  }
	  if (repocore.Checkin(path_, namespace_, dependVect, catVect, descrip_))
	  {
		  return true;
	  }

	  return false;
  }

  //----< Handles Checkin msg >-----
  std::function<Msg(Msg)> Checkinfile = [](Msg msg)
  {
	  
	  Msg reply;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.command("ackcheckin");
	  
	  if (makeCheckin(msg.value("path"), msg.value("namespace"), msg.value("dependency"), msg.value("category"), msg.value("description"))) {
		  if (msg.value("status") == "Close")
		  {
			  closecheckin( msg.value("namespace"), msg.value("filename"));
		  }
	  }
	  reply.attribute("message", "Checkindone");
	  NoSqlDb::Persist<PayLoad> persist(db_);
	  
	  std::ofstream file;
	  file.open("database.xml");
	  file << persist.toXml();
	  file.close();
	  return reply;
  };
  //----< handles checkout command >-----
  bool makeCheckout(std::string namespace_,std::string filename,int version_) {
	  Repocore<PayLoad> repocore(Repository::db_);
	  if (repocore.Checkout(namespace_, filename, version_)) {
		  return true;
	  }
	  return false;
  }
  //----< Convertes vector to string seperated with | >-----
  std::string vectoDstring(std::vector<std::string> vec)
  {
	  if (vec.size() == 0)
	  {
		  return "";
	  }
	  if (vec.size() == 1)
	  {
		  return vec[0];
	  }
	  std::string vecstring;
	  vecstring = vec[0];
	  for (int i = 1; i < int(vec.size()); i++)
	  {
		  vecstring = vecstring + "|" + vec[i];
	  }
	  return vecstring;
  }
  //----< handles checkout msg >-----
  std::function<Msg(Msg)> Checkoutfile = [](Msg msg)
  {
	  std::string filename;
	  Msg reply = msg;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.command("ackcheckout");
	  reply.attribute("message", "Checkoutdone");
	  if (msg.value("version")=="0")
	  {
		  Version<PayLoad> version;
		  int ver = version.getlastestVersion(msg.value("namespace"), msg.value("filename"), Repository::db_);
		  makeCheckout(msg.value("namespace"), msg.value("filename"), ver);
		  filename = msg.value("namespace") + "::" + msg.value("filename") + "." + std::to_string(ver);
	  }
	  else
	  {
		  makeCheckout(msg.value("namespace"), msg.value("filename"), stoi(msg.value("version")));
		  filename = msg.value("namespace")+"::" + msg.value("filename")+"."+ (msg.value("version"));
	  }
	  reply.attribute("sendingFile", msg.value("filename"));
	  DbElement<PayLoad> elem;
	  if (db_.contains(filename))
	  {
		  elem = db_[filename];

	  }
	  std::vector<std::string> vec = elem.children();
	  for (size_t i = 0; i < vec.size(); i++)
	  {
		  std::string depend = "depend" + std::to_string(i);
		  reply.attribute(depend, vec[0]);
	  }
	  reply.attribute("count", std::to_string(vec.size()));
	  return reply;
  };

  
  //----< string to vector >-----
  std::vector<std::string> dStringtoVec(std::string key) 
  {
	  std::vector<std::string> keys;
	  std::stringstream ss(key);
	  while (ss.good())
	  {
		  std::string substr;
		  getline(ss, substr, '|');
		  keys.push_back(substr);
	  }
	  return keys ;
  }
  //----< handle query command >-----
  std::vector<std::string> makeQuery(Msg msg) {
	  Query<PayLoad> query(db_); 
	  Conditions<PayLoad> cond;
	  if (msg.value("name")!="na"&& msg.value("name") != "")
	  {
		  if (msg.value("version")!="na" && msg.value("version") != "")
		  {
			  std::string filename = msg.value("name") + "." + msg.value("version");
			  cond.name(filename);
		  }
		  else
		  {
			  cond.name(msg.value("name"));
		  }
		  
	  }
	  else {
		  if (msg.value("version") != "na"&& msg.value("version") != "") {
			  std::string filename = "." + msg.value("version");
			  cond.name(filename);
		  }
	  }
	  
	  if (msg.value("depend") != "na"&& msg.value("depend") != "")
	  {
		  std::vector<std::string> x = dStringtoVec(msg.value("depend"));
		  cond.children(x);
	  } 
	  if(msg.value("descrip")!="na" && msg.value("descrip") != "")cond.description(msg.value("descrip"));
	 
	  if (msg.value("category")!="na"&& msg.value("category") != "")
	  {
		  cond.category(dStringtoVec(msg.value("category")));
	  }
	  query.select(cond);
	  std::vector<std::string> keys;
	  keys = query.keys();
	 
	  return keys;
  }
  //----< gets status of checkin >-----
  std::string getStat(DbElement<PayLoad>::CheckinStatus stat)
  {
	  if (stat == DbElement<PayLoad>::Close)
	  {
		  return "Close";
	  }
	  if (stat == DbElement<PayLoad>::Open)
	  {
		  return "Open";
	  }
	  if (stat == DbElement<PayLoad>::Pending)
	  {
		  return "Pending";
	  }
	  return "xxx";
  }

  //----< Handles get DB command >-----
  std::function<Msg(Msg)> getDB = [](Msg msg)
  {
	  Msg msg_;
	  msg_.to(msg.from());
	  msg_.from(msg.to());
	  Msg reply = msg_;
	  PayLoad payload;
	  reply.command("ackgetDB");
	  reply.attribute("message", "sending db");
	 
	  int i = 0;
	  for (auto item : db_)
	  {

		  if (db_.size() == 0)
		  {
			  reply.attribute("message", "no data to show");
			  return reply;
		  }
		  else
		  {
			  reply.attribute("key" + std::to_string(i), item.first);
			  reply.attribute("fileName" + std::to_string(i), item.second.name());
			  reply.attribute("dateTime" + std::to_string(i), item.second.dateTime());
			  reply.attribute("description" + std::to_string(i), item.second.descrip());
			  reply.attribute("depend" + std::to_string(i), vectoDstring(item.second.children()));
			  reply.attribute("status" + std::to_string(i), getStat(item.second.CheckinStat()));
			  payload = item.second.payLoad();
			  reply.attribute("path" + std::to_string(i), payload.value());
			  reply.attribute("category" + std::to_string(i), vectoDstring(payload.categories()));
			  i++;
		  }
		  
	  }
	  reply.attribute("count", std::to_string(i));
	  return reply;
  };

  //----< handles Query Command >-----
  std::function<Msg(Msg)> query = [](Msg msg)
  {

	  Msg msg_;
	  msg_.to(msg.from());
	  msg_.from(msg.to());
	  Msg reply = msg_;
	  PayLoad payload;
	  reply.command("ackQuery");
	  reply.attribute("message", "query done");
	  std::vector<std::string> keys = makeQuery(msg);
	  int i = 0;
	  for (auto item:db_)
	  {
		  
		  if (keys.size()==0)
		  {
			  reply.attribute("message", "no data to show");
			  return reply;
		  }
		  if (std::find(keys.begin(), keys.end(), item.first) != keys.end())
		  {
			  reply.attribute("key" + std::to_string(i), item.first);
			  reply.attribute("fileName" + std::to_string(i), item.second.name());
			  reply.attribute("dateTime" + std::to_string(i), item.second.dateTime());
			  reply.attribute("description" + std::to_string(i), item.second.descrip());
			  reply.attribute("depend" + std::to_string(i), vectoDstring(item.second.children()));
			  reply.attribute("status" + std::to_string(i), getStat(item.second.CheckinStat()));
			  payload = item.second.payLoad();
			  reply.attribute("path" + std::to_string(i), payload.value());
			  reply.attribute("category" + std::to_string(i), vectoDstring(payload.categories()));
			  i++;
		  }
		  
	  }
	  reply.attribute("count", std::to_string(i));
	  
	  return reply;
	  
  };
 


  
  //----< Handles view metadata Command>-----
  std::function<Msg(Msg)> viewmeta = [](Msg msg)
  {
	  Msg reply = msg;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.command("ackviewmeta");
	  std::string keyname = msg.value("namespace") + "::" + msg.value("filename") + "." + msg.value("version");
	  DbElement<PayLoad> elem;
	  PayLoad payload;
	  
	  int hfis = db_.size();
	  if (db_.contains(keyname))
	  {
		  elem = db_[keyname];
		  payload = elem.payLoad();
		  reply.attribute("message", "metasent");
		  reply.attribute("description", elem.descrip());
		  reply.attribute("date", elem.dateTime());
		  reply.attribute("path", payload.value());
		  if (elem.CheckinStat()==elem.Close)
		  {
			  reply.attribute("status", "Open");
		  }
		  else
		  {
			  reply.attribute("status", "Open");
		  }
		  
		  reply.attribute("depend",vectoDstring(elem.children()));
		  reply.attribute("cat", vectoDstring(payload.categories()));
	  }
	  reply.attribute("message", "metadata not found");
	  return reply;
  };

  //----< handles get version command >-----
  std::function<Msg(Msg)> getversion = [](Msg msg)
  {
	  Msg reply = msg;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.command("ackgetVersion");
	  Version<PayLoad> version;
	  int ver = version.getlastestVersion(msg.value("namespace"), msg.value("filename"), Repository::db_);
	  reply.attribute("version",std::to_string(ver) );
	  return reply;

  };
  //----< handles get without parent >-----
  std::function<Msg(Msg)> getWOP = [](Msg msg)
  {
	  Msg reply = msg;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.command("ackgetWOP");
	  
	  Query<PayLoad> query(db_);
	  std::string keys;
	  query.getWOParent();
	  keys=vectoDstring(query.wopkeys());
	  reply.attribute("keys",keys);
	  return reply;

  };
  

  //----< sendFile ServerProc sends file to requester >----------------
  /*
  *  - Comm sends bodies of messages with sendingFile attribute >------
  */
  std::function<Msg(Msg)> sendFile = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command("sendFile");
	std::string path = msg.value("path");
    reply.attribute("sendingFile", msg.value("fileName"));
    reply.attribute("fileName", msg.value("fileName"));
    reply.attribute("verbose", "blah blah");
	
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != "." && path != searchPath)
        searchPath = searchPath + "/" + path;
     if (!FileSystem::Directory::exists(searchPath))
      {
        std::cout << "\n  file source path does not exist";
        return reply;
      }
	  std::string filePath = searchPath +"/" + msg.value("fileName");
      std::string fullSrcPath = FileSystem::Path::getFullFileSpec(filePath);
      std::string fullDstPath = sendFilePath;
      if (!FileSystem::Directory::exists(fullDstPath))
      {
        std::cout << "\n  file destination path does not exist";
        return reply;
      }
      fullDstPath += "/" + msg.value("fileName");
      FileSystem::File::copy(fullSrcPath, fullDstPath);
    }
    else
    {
      std::cout << "\n  getDirs message did not define a path attribute";
    }
    return reply;
  };


  std::function<Msg(Msg)> connection = [](Msg msg) {
	  Msg reply = msg;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.attribute("message", "Connected");
	  return reply;
  };
  //----< analyze code on current server path >--------------------------
  /*
  *  - Creates process to run CodeAnalyzer on specified path
  *  - Won't return until analysis is done and logfile.txt
  *    is copied to sendFiles directory
  */
  std::function<Msg(Msg)> codeAnalyze = [](Msg msg) {
	  Msg reply;
	  reply.to(msg.from());
	  reply.from(msg.to());
	  reply.command("sendFile");
	  reply.attribute("sendingFile", "logfile.txt");
	  reply.attribute("fileName", "logfile.txt");
	  reply.attribute("verbose", "blah blah");
	  std::string path = msg.value("path");
	  if (path != "")
	  {
		  std::string searchPath = storageRoot;
		  if (path != "." && path != searchPath)
			  searchPath = searchPath + "\\" + path;
		  if (!FileSystem::Directory::exists(searchPath))
		  {
			  std::cout << "\n  file source path does not exist";
			  return reply;
		  }
		  // run Analyzer using Process class

		  Process p;
		  p.title("test application");
		  //std::string appPath = "c:/su/temp/project4sample/debug/CodeAnalyzer.exe";
		  std::string appPath = "CodeAnalyzer.exe";
		  p.application(appPath);

		  //std::string cmdLine = "c:/su/temp/project4Sample/debug/CodeAnalyzer.exe ";
		  std::string cmdLine = "CodeAnalyzer.exe ";
		  cmdLine += searchPath + " ";
		  cmdLine += "*.h *.cpp /m /r /f";
		  //std::string cmdLine = "c:/su/temp/project4sample/debug/CodeAnalyzer.exe ../Storage/path *.h *.cpp /m /r /f";
		  p.commandLine(cmdLine);

		  std::cout << "\n  starting process: \"" << appPath << "\"";
		  std::cout << "\n  with this cmdlne: \"" << cmdLine << "\"";

		  CBP callback = []() { std::cout << "\n  --- child process exited ---"; };
		  p.setCallBackProcessing(callback);

		  if (!p.create())
		  {
			  std::cout << "\n  can't start process";
		  }
		  p.registerCallback();

		  std::string filePath = searchPath + "\\" + /*msg.value("codeAnalysis")*/ "logfile.txt";
		  std::string fullSrcPath = FileSystem::Path::getFullFileSpec(filePath);
		  std::string fullDstPath = sendFilePath;
		  if (!FileSystem::Directory::exists(fullDstPath))
		  {
			  std::cout << "\n  file destination path does not exist";
			  return reply;
		  }
		  fullDstPath += std::string("\\") + /*msg.value("codeAnalysis")*/ "logfile.txt";
		  FileSystem::File::copy(fullSrcPath, fullDstPath);
	  }
	  else
	  {
		  std::cout << "\n  getDirs message did not define a path attribute";
	  }
	  return reply;
  };
}


using namespace MsgPassingCommunication;

int main()
{
  SetConsoleTitleA("Project4Sample Server Console");
  std::cout << "\n  Testing Server Prototype";
  std::cout << "\n ==========================\n";
  sendFilePath = FileSystem::Directory::createOnPath("../ServerPrototype/SendFiles");
  saveFilePath = FileSystem::Directory::createOnPath("../ServerPrototype/SaveFiles");
  DbCore<PayLoad> db;
  Server server(serverEndPoint, "ServerPrototype");
  server.initializedb(db);
  MsgPassingCommunication::Context* pCtx = server.getContext();
  pCtx->saveFilePath = saveFilePath;
  pCtx->sendFilePath = sendFilePath;
  server.start();
  std::cout << "\n  testing getFiles and getDirs methods";
  std::cout << "\n --------------------------------------";
  Files files = server.getFiles();
  show(files, "Files:");
  Dirs dirs = server.getDirs();
  show(dirs, "Dirs:");
  std::cout << "\n \n testing message ing";
  std::cout << "\n ----------------------------";
  server.addMsgProc("echo", echo);
  server.addMsgProc("getFiles", getFiles);
  server.addMsgProc("getDirs", getDirs);
  server.addMsgProc("sendFile", sendFile);
  server.addMsgProc("codeAnalyze", codeAnalyze);
  server.addMsgProc("serverQuit", echo);
  server.addMsgProc("Checkoutfile", Checkoutfile);
  server.addMsgProc("viewmeta", viewmeta);
  server.addMsgProc("getversion", getversion);
  server.addMsgProc("Checkinfile", Checkinfile);
  server.addMsgProc("connection", connection);
  server.addMsgProc("query", query);
  server.addMsgProc("getDB", getDB);
  server.addMsgProc("getWOP", getWOP);
  server.processMessages();
  Msg msg(serverEndPoint, serverEndPoint);  // send to self
  msg.name("msgToSelf");
  std::cout << "\n  press enter to exit\n";
  std::cin.get();
  std::cout << "\n";
  msg.command("serverQuit");
  server.postMessage(msg);
  server.stop();
  return 0;
}


