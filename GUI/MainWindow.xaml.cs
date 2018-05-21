
/////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Handles all GUI related Functionality      //
// ver 1.0                                                         //
//Author- SHUBHAM JIWTODE #870427143                               //
//Source- Jim Fawcett                                              //
//CSE687 - Object Oriented Design, Spring 2018                     //
/////////////////////////////////////////////////////////////////////
/*
* File Operations:
* -------------------
* This package has handlers for following objects 
* - Various Button Click (Connect,Checkin,Checkout,Browse etc)
* - Various listBox forDirectory and files
* - Combo Box Selection Handlers
* 
* Public Interface:
* ---------------------
* processMessages() - recieve msg and invoke it     
clearDirs()- Clear Directory listbox
insertParent()- navigate to parent directory
addDir()- Add item to Directory listbox
clearFiles()- clear all item in listbox
addFile()- add item to file listbox
addClientProc()- enq the processes
DispatcherLoadGetDirs()- Loads data from server to directory listbox
DispatcherLoadGetFiles()- loads data from server to files listbox
DispatcherReadFile() - read data from file
DispatcherLoadMakeConnection() - handles the connection function
DispatcherLoadGetFile()- get a single file from server
DispatcherAckCheckin()- Acknowledges checkin
DispatcherAckgetVersion()- gets available version of file from server
Dispatcherackviewmeta()- gets meta data of a particular file
descripenum()- get description of a file
dateenum() get date of a file
pathenum()- gets path of a file
statusenum()- gets status of a file
Dispatcherackcheckout()- acknowledges checkout
showFile(string fileName, string fileContent)- shows file content in new window

DirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)- handles double click on dir list
ConnectButton_Click(object sender, RoutedEventArgs e)- handle connect button 
Checkintabclick(object sender, MouseButtonEventArgs e)- handle checkin tab button
Browse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)- handle browse tab click
ciDirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)- handles Checkin Directory list
CheckinButton_Click(object sender, RoutedEventArgs e)- handles checkin button
CheckoutButton_Click(object sender, RoutedEventArgs e)- handles checkout button
RadioB_browsefile(object sender, RoutedEventArgs e)- radio button to select file to checkin
RadioB_Adddepend(object sender, RoutedEventArgs e)- radio button to add dependencies
RadioB_AddCat(object sender, RoutedEventArgs e)- radio button to add category
WPButton_Click(object sender, RoutedEventArgs e) - to get files without parent
* Required Files:
* ---------------
* MainWindow.xaml
* 

* Maintenance History:
* --------------------
*Ver 1.0 : 1 May 2018
*  - first release
*/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO;
using MsgPassingCommunication;
namespace GUI
{

    ////// Interaction logic for MainWindow.xaml

    public partial class MainWindow : Window
    {
        public class BoolStringClass
        {
            public string LBText { get; set; }
            public bool LBSelected { get; set; }
            public bool DepndList { get; set; }
        }
        public ObservableCollection<BoolStringClass> FileListB { get; set; }
        public MainWindow()
        {
            FileListB = new ObservableCollection<BoolStringClass>();
            InitializeComponent();
            this.DataContext = this;
        }

        private string saveFilesPath = "../ServerPrototype/saveFiles";
        private string sendFilesPath = "../ServerPrototype/sendFiles";

        private List<String> catList = new List<string>();
        private string checkinfilecat;
        private string checkinfiledepend = "na";
        private string checkinStat;
        private FileDisplayWin file_window = null;
        private Stack<string> pathStack_ = new Stack<string>();
        private Translater translater;
        private CsEndPoint endPoint_;
        private Thread rcvThrd = null;
        private Dictionary<string, Action<CsMessage>> dispatcher_
          = new Dictionary<string, Action<CsMessage>>();
        private bool connection_flag = false;
        private int windowcondition = 0;
        private string CheckinfileName;
        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();
        //----< process incoming messages on child thread >----------------

        private void processMessages()
        {
            ThreadStart thrdProc = () => {
                while (true)
                {
                    CsMessage msg = translater.getMessage();
                    string msgId = msg.value("command");
                    if (dispatcher_.ContainsKey(msgId))
                        dispatcher_[msgId].Invoke(msg);
                }
            };
            rcvThrd = new Thread(thrdProc);
            rcvThrd.IsBackground = true;
            rcvThrd.Start();
        }
        //----< function dispatched by child thread to main thread >-------

        private void clearDirs()
        {
            if (windowcondition == 0)
            {
                DirList.Items.Clear();
            }
            else { ciDirListBox.Items.Clear(); }
        }
        //----< function dispatched by child thread to main thread >-------

        private void addDir(string dir)
        {
            if (windowcondition == 0)
            {
                DirList.Items.Add(dir);
            }
            else { ciDirListBox.Items.Add(dir); }
        }
        //----< function dispatched by child thread to main thread >-------

        private void insertParent()
        {
            if (windowcondition == 0)
            {
                DirList.Items.Insert(0, "..");
            }
            else { ciDirListBox.Items.Insert(0, ".."); }
        }
        //----< function dispatched by child thread to main thread >-------

        private void clearFiles()
        {
            if (windowcondition == 0)
            {
                FileList.Items.Clear();
            }
            else { FileListB.Clear(); }
        }
        //----< function dispatched by child thread to main thread >-------

        private void addFile(string file, string path)
        {
            string filename;
            if (System.IO.Path.GetFileName(path) == "Storage") { filename = "root" + "::" + file; }
            else
            {
                filename = System.IO.Path.GetFileName(path) + "::" + file;
            }
            if (windowcondition == 0)
            {
                FileList.Items.Add(file);
            }
            else { FileListB.Add(new BoolStringClass { LBSelected = false, LBText = filename, DepndList = false }); }
        }
        //----< add client processing for message with key >---------------

        private void addClientProc(string key, Action<CsMessage> clientProc)
        {
            dispatcher_[key] = clientProc;
        }
        //----< load getDirs processing into dispatcher dictionary >-------

        private void DispatcherLoadGetDirs()
        {
            Action<CsMessage> getDirs = (CsMessage rcvMsg) =>
            {
                Action clrDirs = () =>
                {
                    clearDirs();
                };
                Dispatcher.Invoke(clrDirs, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("dir"))
                    {
                        Action<string> doDir = (string dir) =>
                        {
                            addDir(dir);
                        };
                        Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                    }
                }
                Action insertUp = () =>
                {
                    insertParent();
                };
                Dispatcher.Invoke(insertUp, new Object[] { });
            };
            Action<CsMessage> getAuthor = (CsMessage rcvMsg) =>
            {
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("Author"))
                    {
                        Action<string> doDir = (string dir) =>
                        {
                            addDir(dir);
                        };
                        Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("getDirs", getDirs);
            addClientProc("getAuthor", getAuthor);
        }
        //----< load getFiles processing into dispatcher dictionary >------

        private void DispatcherLoadGetFiles()
        {
            Action<CsMessage> getFiles = (CsMessage rcvMsg) =>
            {
                Action clrFiles = () =>
                {
                    clearFiles();
                };
                Dispatcher.Invoke(clrFiles, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("file"))
                    {
                        Action<string> doFile = (string file) =>
                        {
                            addFile(file, rcvMsg.value("path"));
                        };
                        Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("getFiles", getFiles);
        }

        //--------------------------<Reads file contains>-------------------------//
        private void DispatcherReadFile()
        {
            Action<CsMessage> readFile = (CsMessage rcvMsg) =>
            {
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("content"))
                    {
                        Action<string> mess = (string value) =>
                        {
                            file_window.FileSpace.Text = enumer.Current.Value;
                        };
                        Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                    }
                    if (key.Contains("name"))
                    {
                        Action<string> mess = (string value) =>
                        {
                            file_window.Title = enumer.Current.Value;
                        };
                        Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("readFile", readFile);
        }
        //------------------------ To make connection with the server -----------------------------

        private void DispatcherLoadMakeConnection()
        {
            Action<CsMessage> connection = (CsMessage rcvMsg) =>
            {
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "Connected")
                            {
                                connection_flag = true;
                                this.statusBarText.Text = "Connected..";
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("connection", connection);
        }

        //--------------------------<load a particular file in dipatcher>-------------------------//
        private void DispatcherLoadGetFile()
        {
            Action<CsMessage> sendFile = (CsMessage rcvMsg) =>
            {
                Action displayFile = () =>
                {
                    string szFileName = rcvMsg.value("fileName");
                    string szFileContent = System.IO.File.ReadAllText("../../SaveFiles" + "/" + szFileName);

                    showFile(szFileName, szFileContent);

                };
                Dispatcher.Invoke(displayFile, new Object[] { });

                Console.WriteLine("File sent message recieved");
            };
            addClientProc("sendFile", sendFile);
        }

        //--------------------------<loads checkin function>-------------------------//
        private void DispatcherAckCheckin() {

            Action<CsMessage> ackcheckin = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "Checkindone")
                            {

                                this.statusBarText.Text = " File Checkin Succesfull";
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }

            };
            addClientProc("ackcheckin", ackcheckin);

        }


        //--------------------------<Loads the get version function>-------------------------//
        private void DispatcherAckgetVersion()
        {

            Action<CsMessage> ackgetVersion = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("version"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            VM_Ver_CB.Items.Clear();
                            for (int i = 0; i < Int32.Parse(status); i++) {

                                VM_Ver_CB.Items.Add(i + 1);
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("ackgetVersion", ackgetVersion);
        }


        //--------------------------<Load view meta data function in dispatcher>-------------------------//
        private void Dispatcherackviewmeta()
        {

            Action<CsMessage> ackviewmeta = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    if (enumer.Current.Key == "description")
                    {
                        descripenum(enumer, rcvMsg);
                    }
                    if (enumer.Current.Key == "date")
                    {
                        dateenum(enumer, rcvMsg);
                    }
                    if (enumer.Current.Key == "status")
                    {
                        statusenum(enumer, rcvMsg);
                    }
                    if (enumer.Current.Key == "path")
                    {
                        pathenum(enumer, rcvMsg);
                    }
                    if (enumer.Current.Key == "depend")
                    {
                        dependenum(enumer, rcvMsg);
                    }
                    if (enumer.Current.Key == "cat")
                    {
                        catenum(enumer, rcvMsg);
                    }
                }
            };
            addClientProc("ackviewmeta", ackviewmeta);
        }

        //--------------------------<displey the description in view meta data screen>-------------------------//
        private void descripenum(Dictionary<string, string>.Enumerator enumer, CsMessage msg) {
            Action<string> connect = (string status) =>
            {
                VM_FD_TB.Text = msg.attributes["description"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });

        }
        //--------------------------<displey the date in view meta data screen>-------------------------//
        private void dateenum(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_date_TB.Text = msg.attributes["date"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }
        //--------------------------<displey the path in view meta data screen>-------------------------//
        private void pathenum(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_FP_TB.Text = msg.attributes["path"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }
        //--------------------------<displey the status in view meta data screen>-------------------------//
        private void statusenum(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_CIS_TB.Text = msg.attributes["status"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }
        //--------------------------<displey the dependency in view meta data screen>-------------------------//
        private void dependenum(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_depend_TB.Text = msg.attributes["depend"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }
        //--------------------------<displey the category in view meta data screen>-------------------------//
        private void catenum(Dictionary<string, string>.Enumerator enumer, CsMessage msg)
        {
            Action<string> connect = (string status) =>
            {
                VM_cat_TB.Text = msg.attributes["cat"];
            };
            Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
        }

        //--------------------------<Checkout dispatcher>-------------------------//
        private void Dispatcherackcheckout()
        {

            Action<CsMessage> ackcheckout = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "Checkoutdone")
                            {
                                this.statusBarText.Text = " File Checkout Succesfull";
                                for (int i = 0; i < Convert.ToInt64(rcvMsg.value("count")); i++)
                                {
                                    CsEndPoint serverEndPoint = new CsEndPoint();
                                    serverEndPoint.machineAddress = "localhost";
                                    serverEndPoint.port = 8080;

                                    CsMessage msg = new CsMessage();
                                    msg.add("to", CsEndPoint.toString(serverEndPoint));
                                    msg.add("from", CsEndPoint.toString(endPoint_));
                                    msg.add("command", "sendFile");
                                    msg.add("nopop", "true");
                                    string depend = "depend" + (i).ToString();

                                    int pos = rcvMsg.value(depend).IndexOf("::");
                                    string name = rcvMsg.value(depend).Substring(pos + 2, rcvMsg.value(depend).Length - pos - 3);
                                    msg.add("fileName", name);
                                    translater.postMessage(msg);
                                }

                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("ackcheckout", ackcheckout);
        }
        //--------------------------<To acknowledge the query results>--------------------------------//


        private void DispatcherackQuery()
        {

            Action<CsMessage> ackQuery = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "query done")
                            {
                                this.statusBarText.Text = " File query Succesfull";
                                showdbTB(rcvMsg);
                                Console.WriteLine("\n==============================================  Query Result ==============================================");
                                showdb(rcvMsg);
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("ackQuery", ackQuery);
        }
        //-------------------------------------<To get the databse from server>----------------------------------------------

        private void DispatcherackgetDB()
        {

            Action<CsMessage> ackgetDB = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> connect = (string status) =>
                        {
                            if (status == "sending db")
                            {
                                this.statusBarText.Text = " sending db Succesfull";
                                showdb(rcvMsg);
                            }
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("ackgetDB", ackgetDB);
        }
        //-------------------------------------<To get files without parent>-------------------------

        private void DispatcherackgetWOP()
        {

            Action<CsMessage> ackgetWOP = (CsMessage rcvMsg) => {

                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("keys"))
                    {
                        Action<string> connect = (string status) =>
                        {

                            QueryDbTB.Clear();
                            QueryDbTB.Text = status;
                        };
                        Dispatcher.Invoke(connect, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("ackgetWOP", ackgetWOP);
        }
        //------------------------------------<To show query databse in textbox>--------------------------

        private void showdbTB(CsMessage msg)
        {
            QueryDbTB.Clear();
            for (int i = 0; i < Convert.ToInt64(msg.value("count")); i++)
            {
                string key = "key" + i.ToString();
                string filename = "fileName" + i.ToString();
                string path = "path" + i.ToString();
                string description = "description" + i.ToString();
                string status = "status" + i.ToString();
                string datetime = "dateTime" + i.ToString();
                string depend = "depend" + i.ToString();
                string category = "category" + i.ToString();
                
                QueryDbTB.AppendText("\n" + "\bKey :-" + msg.value(key).PadRight(5) + "   \bName :-" + msg.value(filename).PadRight(5)
                    + "\n\bPath :-" + msg.value(path).PadRight(5) +
                    "\n\bDescription :-" + msg.value(description).PadRight(5) +
                    "\n\bStatus :-" + msg.value(status).PadRight(5) + "     \bDate Time :-" + msg.value(datetime).PadRight(1));
                QueryDbTB.AppendText("\n\bDependencies:-  " + msg.value(depend));
                QueryDbTB.AppendText("\n\bCategories:-  " + msg.value(category));
                QueryDbTB.AppendText(Environment.NewLine);
            }

        }
        //-------------------------------<Function to show database>-----------------------------------------------
        private void showdb(CsMessage msg)
        {
            Console.WriteLine("[][][][][][][][][][][][][][][][][][][][][][] Showing DB [][][][][][][][][][][][][][][][][][][][");
            Console.WriteLine("\n                            Key                       Name               Path                 Description           Status            Time ");
            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------");
            for (int i = 0; i < Convert.ToInt64(msg.value("count")); i++)
            {
                string key = "key" + i.ToString();
                string filename = "fileName" + i.ToString();
                string path = "path" + i.ToString();
                string description = "description" + i.ToString();
                string status = "status" + i.ToString();
                string datetime = "dateTime" + i.ToString();
                string depend = "depend" + i.ToString();
                string category = "category" + i.ToString();
                Console.WriteLine(msg.value(key) + "     " + msg.value(filename) + "     " + msg.value(path) + "     " + msg.value(description) + "     " + msg.value(status).PadRight(10) + msg.value(datetime).PadRight(1));
                Console.WriteLine("Dependencies:  " + msg.value(depend));
                Console.WriteLine("Categories:  " + msg.value(category));
                Console.WriteLine("\n");
            }
        }

        //--------------------------<Show file in diffwindow>-------------------------//
        private void showFile(string fileName, string fileContent)
        {
            Console.WriteLine(fileName.ToString());
            FileDisplayWin p = new FileDisplayWin();
            p.FileSpace.Text = fileContent;
            p.Show();
        }
        //----< load all dispatcher processing >---------------------------

        private void loadDispatcherBrowse()
        {
            DispatcherLoadGetDirs();
            DispatcherLoadGetFiles();
        }
        private void loadDispatcher()
        {
            DispatcherackQuery();
            Dispatcherackcheckout();
            DispatcherAckgetVersion();
            DispatcherLoadMakeConnection();
            DispatcherAckCheckin();
            DispatcherReadFile();
            Dispatcherackviewmeta();
            loadDispatcherBrowse();
            DispatcherLoadGetFile();
            DispatcherackgetDB();
            DispatcherackgetWOP();
        }

        //----< start Comm, fill window display with dirs and files >------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            endPoint_ = new CsEndPoint();
            endPoint_.machineAddress = "localhost";
            endPoint_.port = 8082;
            translater = new Translater();
            translater.listen(endPoint_);
            // start processing messages
            processMessages();
            // load dispatcher
            loadDispatcher();
            Thread.Sleep(500);
            saveFilesPath = translater.setSaveFilePath("../../SaveFiles");
            sendFilesPath = translater.setSendFilePath("../../SendFiles");
            automate();
        }
        //----< strip off name of first part of path >---------------------

        private string removeFirstDir(string path)
        {
            string modifiedPath = path;
            int pos = path.IndexOf("/");
            modifiedPath = path.Substring(pos + 1, path.Length - pos - 1);
            return modifiedPath;
        }
        //----< respond to mouse double-click on dir name >----------------

        private void DirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (DirList.SelectedItem == null)
                return;
            string selectedDir = (string)DirList.SelectedItem;
            string path;
            if (selectedDir == "..")
            {
                if (pathStack_.Count > 1)  // don't pop off "Storage"
                    pathStack_.Pop();
                else
                    return;
            }
            else
            {
                path = pathStack_.Peek() + "/" + selectedDir;
                pathStack_.Push(path);
            }  // display path in Dir TextBlcok
            PathTextBlock.Text = removeFirstDir(pathStack_.Peek()); // build message to get dirs and post it
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getDirs");
            msg.add("path", pathStack_.Peek());
            translater.postMessage(msg);        // build message to get files and post it
            msg.remove("command");
            msg.add("command", "getFiles");
            translater.postMessage(msg);
        }

        //----< first test not completed >---------------------------------

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("connecting to localhost 8080");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = this.IPAddrName.Text;
            serverEndPoint.port = Int32.Parse(this.PortName.Text);
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "connection");
            translater.postMessage(msg);
            msg.remove("command");
            getDB();
        }

        //--------------------------<Checkin tab handler>-------------------------//
        private void Checkintabclick(object sender, MouseButtonEventArgs e)
        {
            windowcondition = 1;
            dependfinal.Items.Clear();
        }
        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Connect Tab");
        }

        private void TabItem_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Checkout Tab");
        }

        private void TabItem_MouseLeftButtonDown_2(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n View Meta Data Tab");
        }

        //--------------------------<Browse button handler>-------------------------//

        private void Browse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("\n Browse Tab");
            if (!connection_flag)
                return;
            windowcondition = 0;

            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;

            PathTextBlock.Text = "Storage";
            pathStack_.Push("../Storage");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getDirs");
            msg.add("path", pathStack_.Peek());
            translater.postMessage(msg);
            msg.remove("command");
            msg.add("command", "getFiles");
            translater.postMessage(msg);
            msg.remove("command");
        }

        //--------------------------<Directory Listbox Double click handler>-------------------------//
        private void ciDirList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (ciDirListBox.SelectedItem == null)
                return;
            // build path for selected dir
            string selectedDir = (string)ciDirListBox.SelectedItem;
            string path;
            if (selectedDir == "..")
            {
                if (pathStack_.Count > 1)  // don't pop off "Storage"
                    pathStack_.Pop();
                else
                    return;
            }
            else
            {
                path = pathStack_.Peek() + "/" + selectedDir;
                pathStack_.Push(path);
            }


            // build message to get dirs and post it
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getDirs");
            msg.add("path", pathStack_.Peek());
            translater.postMessage(msg);

            // build message to get files and post it
            msg.remove("command");
            msg.add("command", "getFiles");
            translater.postMessage(msg);
        }

        //--------------------------<Checkin button handler>-------------------------//
        private void CheckinButton_Click(object sender, RoutedEventArgs e)
        {
            if (checkin_checklist())
            {
                Console.WriteLine("Checkin file");
                CsEndPoint serverEndPoint = new CsEndPoint();
                serverEndPoint.machineAddress = "localhost";
                serverEndPoint.port = 8080;
                CsMessage msg = new CsMessage();
                msg.add("to", CsEndPoint.toString(serverEndPoint));
                msg.add("from", CsEndPoint.toString(endPoint_));
                msg.add("command", "Checkinfile");
                msg.add("namespace", NamespaceTB.Text);
                msg.add("path", SelectFileTB.Text.ToString());
                msg.add("filename", ExtractFilename(SelectFileTB.Text.ToString()));
                msg.add("description", DescripTB.Text.ToString());
                msg.add("status", checkinStat);
                msg.add("category", checkinfilecat);
                msg.add("dependency", checkinfiledepend);
                string sourceFile = System.IO.Path.GetFullPath(SelectFileTB.Text.ToString());
                string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename(SelectFileTB.Text.ToString()));
                System.IO.File.Copy(sourceFile, destFile, true);
                msg.add("sendingFile", ExtractFilename(SelectFileTB.Text.ToString()));
                translater.postMessage(msg);
                msg.show();
                getDB();
            }

        }

        //--------------------------<Checkout button handler>-------------------------//

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("checkingin file: " + SelectFileCOTB.Text.ToString());
            Console.WriteLine("Checkout file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkoutfile");
            msg.add("namespace", NamespaceCOTB.Text.ToString());
            msg.add("filename", SelectFileCOTB.Text.ToString());
            msg.add("version", version_checklist());

            translater.postMessage(msg);
            msg.show();
        }


        //--------------------------<Browse file radio button handler>-------------------------//

        void RadioB_browsefile(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Browse clicked");
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".cpp";
            dlg.Filter = "*.h|*.cpp";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                SelectFileTB.Text = filename;
            }
        }

        //--------------------------<Add dependency radio button handler>-------------------------//
        void RadioB_Adddepend(object sender, RoutedEventArgs e)
        {
            ciDirListBox.IsEnabled = true;
            ciFilListBox.IsEnabled = true;
            Console.WriteLine("Add Dependcy clicked");
            AddcatButton.IsEnabled = false;
            confirmDependclick.IsEnabled = true;
            dependfinal.Items.Clear();
            dependfinalTB.Text = "Dependencies";
            dependfinal.SelectionMode = SelectionMode.Single;
            if (!connection_flag)
                return;
            windowcondition = 1;
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            PathTextBlock.Text = "Storage";
            pathStack_.Clear();
            pathStack_.Push("../Storage");
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getDirs");
            msg.add("path", pathStack_.Peek());
            translater.postMessage(msg);
            msg.remove("command");
            msg.add("command", "getFiles");
            translater.postMessage(msg);
            msg.remove("command");
        }

        //--------------------------<Add category radio button handler>-------------------------//
        void RadioB_AddCat(object sender, RoutedEventArgs e)
        {
            ciDirListBox.IsEnabled = false;
            ciFilListBox.IsEnabled = false;
            Console.WriteLine("Add Category clicked");
            AddcatButton.IsEnabled = true;
            confirmDependclick.IsEnabled = false;
            dependfinalTB.Text = "Categories";
            catList.Clear();
            dependfinal.Items.Clear();
            dependfinal.SelectionMode = SelectionMode.Multiple;
            dependfinal.Items.Add("A");
            dependfinal.Items.Add("B");
            dependfinal.Items.Add("C");
            dependfinal.Items.Add("D");
            dependfinal.Items.Add("E");
            dependfinal.Items.Add("F");
        }

        //--------------------------<Adding selected files from filelist to dependency listbox>-------------------------//
        private void CISelectedFile_Checked(object sender, RoutedEventArgs e)
        {

            if (ciFilListBox.IsEnabled == true)
            {
                foreach (var item in FileListB)
                {
                    if (item.LBSelected == true && item.DepndList == false)
                    {
                        item.DepndList = true;
                        dependfinal.Items.Add(item.LBText);
                    }
                    else if (item.LBSelected == false && item.DepndList == true)
                    {
                        item.DepndList = false;

                        dependfinal.Items.Remove(item.LBText);
                    }

                    dependfinal.Items.Refresh();
                }
            }

        }


        //--------------------------<to get file list from listbox>-------------------------//
        private void dependfinal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dependfinal.SelectionMode == SelectionMode.Multiple)
            {
                foreach (var item in dependfinal.SelectedItems) {
                    if (!catList.Contains(item.ToString()))
                    {
                        catList.Add(item.ToString());
                    }
                }
            }
        }

        //--------------------------<To get filename from filepath>-------------------------//
        private string ExtractFilename(string filename)
        {
            CheckinfileName = System.IO.Path.GetFileName(filename);
            return CheckinfileName;
        }

        //--------------------------<To Add Category to Checkin meta>-------------------------//
        private void CatAddClick(object sender, RoutedEventArgs e)
        {

            if (!String.IsNullOrEmpty(otherCatTB.Text))
            {
                catList.Add(otherCatTB.Text);
            }
            int i = 0;
            string[] temp = new string[catList.Count];

            foreach (var item in catList)
            {
                temp[i] = item.ToString();
                i++;
            }
            checkinfilecat = string.Join("|", temp);
        }

        //--------------------------<Selection changed in Status ComboBox>-------------------------//
        private void CSComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItem)CScombox.SelectedItem).Content.ToString() == "Open") {
                checkinStat = "Open";
            }
            else if (((ComboBoxItem)CScombox.SelectedItem).Content.ToString() == "Close") {
                checkinStat = "Close";
            }
        }

        //--------------------------<View Meta Button handler>-------------------------//
        private void ViewMetaButton_Click(object sender, RoutedEventArgs e)
        {
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "viewmeta");
            msg.add("namespace", VM_NS_TB.Text.ToString());
            msg.add("filename", VM_FN_TB.Text.ToString());
            msg.add("version", VM_Ver_CB.SelectedItem.ToString());
            translater.postMessage(msg);
        }

        //--------------------------<To get version of file>-------------------------//
        private void getVersion_Click(object sender, RoutedEventArgs e)
        {
            VM_Ver_CB.IsEnabled = true;


            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getversion");
            msg.add("namespace", VM_NS_TB.Text.ToString());
            msg.add("filename", VM_FN_TB.Text.ToString());
            translater.postMessage(msg);
        }

        //--------------------------<To confirm depend selection>-------------------------//
        private void confirmDependclick_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            string[] temp = new string[dependfinal.Items.Count];
            if (dependfinal.Items.Count == 0) {
                checkinfiledepend = "na";
                return;
            }
            foreach (var item in dependfinal.Items) {
                temp[i] = item.ToString();
                i++;
            }

            checkinfiledepend = string.Join("|", temp);
        }

        //--------------------------<To check if textboxs are empty before checkin>-------------------------//
        private bool checkin_checklist() {
            if (!string.IsNullOrEmpty(DescripTB.Text) && !string.IsNullOrEmpty(NamespaceTB.Text) && !string.IsNullOrEmpty(SelectFileTB.Text) && !string.IsNullOrEmpty(checkinStat) && !string.IsNullOrEmpty(checkinfilecat) /*&& !string.IsNullOrEmpty(checkinfiledepend)*/)
            {
                return true;
            }
            return false;
        }

        //--------------------------< TO get resulting Version>-------------------------//
        private string version_checklist() {
            if (string.IsNullOrEmpty(versionTB.Text.ToString())) {
                return "0";
            }
            return versionTB.Text.ToString();
        }

        //--------------------------<Tiggered on selection of Version in ComboBox>-------------------------//
        private void VM_Ver_CB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


        }

        //--------------------------<Browse filelist double click handler to view file>-------------------------//
        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // build path for selected dir
            string selectedFile = pathStack_.Peek() + "/" + (string)DirList.SelectedItem;

            Console.WriteLine("++" + selectedFile);


            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;

            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "sendFile");
            msg.add("path", selectedFile);
            msg.add("nopop", "false");
            string[] temp = new string[2];
            temp = FileList.SelectedItem.ToString().Split(':');

            msg.add("fileName", temp[0]);
            translater.postMessage(msg);
        }

        //--------------------------<Automate demonstration Function>-------------------------//
        private void automate() {

            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][]   Demonstrating functionality 1. Connect ][][][][][][][][][][][][][][][][][][][]");
            ConnectAuto();
            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][] Demonstrating GUI functionality 2. Checkin ][][][][][][][][][][][][][][][][][][][] ");
            CheckinAuto();
            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][] Demonstrating GUI functionality 3. Checkout [][][][][][][][][][][][][][][][]]][][][][][][][][][][]");
            CheckoutAuto();
            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][] Demonstrating GUI functionality 4. Browse 5. View File [][][][][][][][][][][][][][][][]]][][][][][][][][][][]");
            Console.WriteLine("\n \b For Browse requiremnt files are loaded in browse tab \nClick on DateTime.h contents are dsplayed \n file transfer is also shown");
            BrowseAuto();
            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][] Demonstrating GUI functionality 6. View Meta Data [][][][][][][][][][][][][][][][]]][][][][][][][][][][]");
            ViewmetaAuto();
            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][] Demonstrating GUI functionality 7. Without Parent [][][][][][][][][][][][][][][][]]][][][][][][][][][][]");
            autowithoutparent();
            Console.WriteLine("\n[][][][][][][][][][][][][][][][]]][][][][][][][][][][] Demonstrating GUI functionality 8. Query [][][][][][][][][][][][][][][][]]][][][][][][][][][][]");
            autoquery();
            autoquery2();
            Console.WriteLine("\n\n\n\n*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|*|\n Dumping databse in Database.xml in ../Serverprototype \n*****************************************************************************************************");
        }

        //--------------------------<Automating Connection Function>-------------------------//
        private void ConnectAuto() {
            Console.WriteLine("Address: localhost Port:8080");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = this.IPAddrName.Text;
            serverEndPoint.port = Int32.Parse(this.PortName.Text);
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "connection");
            translater.postMessage(msg);
            msg.remove("command");
            msg.show();
        }

        //--------------------------<Automating Checkin Function>-------------------------//
        private void CheckinAuto() {

            checkinT1();
            checkinT2();
            checkinT3();
            checkinT4();
            checkinT5();
            checkinT6();
            checkinT7();
            getDB();
        }

        //--------------------------<Automating Checkout Function>-------------------------//
        private void CheckoutAuto() {
            Console.WriteLine("Checking out file of namespace : NoSqlDb");
            Console.WriteLine("File: Persist.h");
            Console.WriteLine("Version: lastest");
            NamespaceCOTB.Text = "NoSqlDb";
            SelectFileCOTB.Text = "Persist.h";
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkoutfile");
            msg.add("namespace", NamespaceCOTB.Text.ToString());
            msg.add("filename", SelectFileCOTB.Text.ToString());
            msg.add("version", version_checklist());
            translater.postMessage(msg);
            msg.show();
            Console.Write("Checkingout file at GUI/savefiles \n requesting Dependencies too");
        }

        //--------------------------<Automating Browse file Function>-------------------------//

        private void BrowseAuto() {

            Console.WriteLine("\nSelecting Persist.h from filelist \n On double click View Content Are Displyed \n For this demonstration Persist.h is transfered from ../Storage/NoSqlDb/DateTime.h to ../GUI/SaveFiles ");
            string selectedFile = "../Storage/NoSqlDb/";
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "sendFile");
            msg.add("fileName", "Persist.h");
            msg.add("path", selectedFile);
            msg.add("nopop", "false");
            translater.postMessage(msg);
            msg.show();
        }

        //--------------------------<Automating ViewMeta Function>-------------------------//
        private void ViewmetaAuto() {
            Console.WriteLine("Namespace: NoSqlDb");
            Console.WriteLine("File: Message.h");

            VM_NS_TB.Text = "SomeDb";
            VM_FN_TB.Text = "Checkin.h";
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getversion");
            msg.add("namespace", VM_NS_TB.Text.ToString());
            msg.add("filename", VM_FN_TB.Text.ToString());
            translater.postMessage(msg);
            Console.WriteLine("\ngetting Versions of file");
            msg.show();
            Console.WriteLine("Getting Meta Data of 1th version of NoSqlDb::Message.h");
            VM_Ver_CB.SelectedIndex = 1;

            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg1 = new CsMessage();
            msg1.add("to", CsEndPoint.toString(serverEndPoint));
            msg1.add("from", CsEndPoint.toString(endPoint_));
            msg1.add("command", "viewmeta");
            msg1.add("namespace", VM_NS_TB.Text.ToString());
            msg1.add("filename", VM_FN_TB.Text.ToString());
            msg1.add("version", "1");
            translater.postMessage(msg1);
            msg1.show();
            getDB();
        }
        //--------------------------------------<Auto mation for files without parent query>-----------------------------------------
        private void autowithoutparent()
        {
            Console.Write("\ngetting all files without parent");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getWOP");
            translater.postMessage(msg);
            getDB();
        }
        //--------------------------------------<Query Click Handler>-----------------------------------------
        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "query");
            msg.add("name", queryNameTB.Text.ToString());
            msg.add("version", verTB.Text.ToString());
            msg.add("descrip", queryDescripTB.Text.ToString());
            msg.add("datefrom", queryFromTB.Text.ToString());
            msg.add("dateto", queryToTB.Text.ToString());
            msg.add("category", queryCatTB.Text.ToString());
            msg.add("depend", queryDependTB.Text.ToString());
            translater.postMessage(msg);
        }
        //--------------------------------------<Funtion to display Database>-----------------------------------------
        private void getDB()
        {
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getDB");
            translater.postMessage(msg);
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT1()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "NoSqlDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../DateTime/DateTime.h"));
            msg.add("filename", ExtractFilename("../../../DateTime/DateTime.h"));
            msg.add("description", "Header file for datetime project ");
            msg.add("status", "Close");
            msg.add("category", "A|B");
            msg.add("dependency", "na");
            string sourceFile = System.IO.Path.GetFullPath("../../../DateTime/DateTime.h");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../DateTime/DateTime.h"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../DateTime/DateTime.h"));
            translater.postMessage(msg);
            msg.show();
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT2()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "NoSqlDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../Translater/Translater.h"));
            msg.add("filename", ExtractFilename("../../../Translater/Translater.h"));
            msg.add("description", "Header file for Translater project ");
            msg.add("status", "Close");
            msg.add("category", "A|C");
            msg.add("dependency", "na");
            string sourceFile = System.IO.Path.GetFullPath("../../../Translater/Translater.h");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../Translater/Translater.h"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../Translater/Translater.h"));
            translater.postMessage(msg);
            msg.show();
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT3()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "NoSqlDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../Persist/Persist.h"));
            msg.add("filename", ExtractFilename("../../../Persist/Persist.h"));
            msg.add("description", "Header file for Persist project ");
            msg.add("status", "Close");
            msg.add("category", "B|C");
            msg.add("dependency", "NoSqlDb::DateTime.h.1");
            string sourceFile = System.IO.Path.GetFullPath("../../../Persist/Persist.h");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../Persist/Persist.h"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../Persist/Persist.h"));
            translater.postMessage(msg);
            msg.show();
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT4()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "NoSqlDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../Persist/Persist.h"));
            msg.add("filename", ExtractFilename("../../../Persist/Persist.h"));
            msg.add("description", "Header file for Persist project ");
            msg.add("status", "Open");
            msg.add("category", "B|C");
            msg.add("dependency", "NoSqlDb::DateTime.h.1");
            string sourceFile = System.IO.Path.GetFullPath("../../../Persist/Persist.h");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../Persist/Persist.h"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../Persist/Persist.h"));
            translater.postMessage(msg);
            msg.show();
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT5()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "SomeDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../DbCore/DbCore.h"));
            msg.add("filename", ExtractFilename("../../../DbCore/DbCore.h"));
            msg.add("description", "Header file for DbCore project ");
            msg.add("status", "Close");
            msg.add("category", "C|D");
            msg.add("dependency", "na");
            string sourceFile = System.IO.Path.GetFullPath("../../../DbCore/DbCore.h");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../DbCore/DbCore.h"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../DbCore/DbCore.h"));
            translater.postMessage(msg);
            msg.show();
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT6()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "SomeDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../Checkin/Checkin.h"));
            msg.add("filename", ExtractFilename("../../../Checkin/Checkin.h"));
            msg.add("description", "Header file for Persist project ");
            msg.add("status", "Close");
            msg.add("category", "D|E");
            msg.add("dependency", "SomeDb::DbCore.h.1");
            string sourceFile = System.IO.Path.GetFullPath("../../../Checkin/Checkin.h");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../Checkin/Checkin.h"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../Checkin/Checkin.h"));
            translater.postMessage(msg);
            msg.show();
        }
        //--------------------------------------<Checkin file for demo>-----------------------------------------
        private void checkinT7()
        {
            Console.WriteLine("Checkin file");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "Checkinfile");
            msg.add("namespace", "SomeDb");
            msg.add("path", System.IO.Path.GetFullPath("../../../Checkin/Checkin.cpp"));
            msg.add("filename", ExtractFilename("../../../Checkin/Checkin.cpp"));
            msg.add("description", "Header file for Checkin project ");
            msg.add("status", "Close");
            msg.add("category", "E");
            msg.add("dependency", "SomeDb::DbCore.h.1|SomeDb::Checkin.h.1");
            string sourceFile = System.IO.Path.GetFullPath("../../../Checkin/Checkin.cpp");
            string destFile = System.IO.Path.Combine("../../SendFiles/", ExtractFilename("../../../Checkin/Checkin.cpp"));
            System.IO.File.Copy(sourceFile, destFile, true);
            msg.add("sendingFile", ExtractFilename("../../../Checkin/Checkin.cpp"));
            translater.postMessage(msg);
            msg.show();
        }
        //-------------------------------------<Button Click For File Withut Parent>-----------------------------------
        private void WPButton_Click(object sender, RoutedEventArgs e)
        {
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "getWOP");
            translater.postMessage(msg);
        }
        //-------------------------------------<Automation on query>-----------------------------------
        private void autoquery()
        {
            Console.WriteLine("Query on categories ----------  A,B");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "query");
            msg.add("name", queryNameTB.Text.ToString());
            msg.add("version", verTB.Text.ToString());
            msg.add("descrip", queryDescripTB.Text.ToString());
            msg.add("datefrom", queryFromTB.Text.ToString());
            msg.add("dateto", queryToTB.Text.ToString());
            msg.add("category", "B|A");
            msg.add("depend", queryDependTB.Text.ToString());
            translater.postMessage(msg);
            Console.WriteLine("Query on filename ---- DbCore.h");
            CsMessage msg1 = new CsMessage();
            msg1.add("to", CsEndPoint.toString(serverEndPoint));
            msg1.add("from", CsEndPoint.toString(endPoint_));
            msg1.add("command", "query");
            msg1.add("name", "DbCore");
            msg1.add("version", verTB.Text.ToString());
            msg1.add("descrip", queryDescripTB.Text.ToString());
            msg1.add("datefrom", queryFromTB.Text.ToString());
            msg1.add("dateto", queryToTB.Text.ToString());
            msg1.add("category", queryCatTB.Text.ToString());
            msg1.add("depend", queryDependTB.Text.ToString());
            translater.postMessage(msg1);
            
            

        }

        //-------------------------------------<Automaton on rest of query>-----------------------------------
        private void autoquery2() {
            Console.WriteLine("Query on version ------- 2");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg2 = new CsMessage();
            msg2.add("to", CsEndPoint.toString(serverEndPoint));
            msg2.add("from", CsEndPoint.toString(endPoint_));
            msg2.add("command", "query");
            msg2.add("name", queryNameTB.Text.ToString());
            msg2.add("version", "2");
            msg2.add("descrip", queryDescripTB.Text.ToString());
            msg2.add("datefrom", queryFromTB.Text.ToString());
            msg2.add("dateto", queryToTB.Text.ToString());
            msg2.add("category", queryCatTB.Text.ToString());
            msg2.add("depend", queryDependTB.Text.ToString());
            translater.postMessage(msg2);
            Console.WriteLine("Query on dependencies  -----  SomeDb::DbCore.h.1");
            CsMessage msg3 = new CsMessage();
            msg3.add("to", CsEndPoint.toString(serverEndPoint));
            msg3.add("from", CsEndPoint.toString(endPoint_));
            msg3.add("command", "query");
            msg3.add("name", queryNameTB.Text.ToString());
            msg3.add("version", verTB.Text.ToString());
            msg3.add("descrip", queryDescripTB.Text.ToString());
            msg3.add("datefrom", queryFromTB.Text.ToString());
            msg3.add("dateto", queryToTB.Text.ToString());
            msg3.add("category", queryCatTB.Text.ToString());
            msg3.add("depend", "SomeDb::DbCore.h.1");
            translater.postMessage(msg3);
        }
    }
}
