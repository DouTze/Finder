using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Finder.command
{
    class Email
    {
        public static void GetContact(object obj)
        {
        }

        private static Application outlook = new Application();
        private static List<ContactItem> oldData, newData;
        private static List<Person> webContact;

        static void sMain(string[] args)
        {
            // write web contact to local outlook pst
            SyncWebContact();
        }

        private static void PlayText(string msg)
        {
            for (int i = 0; i < msg.Length; i++)
            {
                Console.Write(msg[i]);
                Thread.Sleep(20);
            }
            Console.WriteLine();
        }

        private static void GetCurrentContact()
        {
            CookieCollection cookies = new CookieCollection();
            Cookie _ga = new Cookie("_ga", "GA1.3.1706935120.1480477007");
            _ga.Domain = "a";
            cookies.Add(_ga);

            //mode=ajax&command=list&queryfield=&querystring=&pagesize=25&pageno=2&workingabid=PA.001@mail.taipei.gov.tw&workingdirid=D6144%403%3A40&workinggroupid=&tofield=&ssnid=&m=910695101&m=75807568
            string user = "gt_tcc";
            string pwd = "%40bc000000";
            string param = "keep_days=7.5&CaptCode=&USERID=" + user + "&PASSWD=" + pwd + "&CaptAns=";
            byte[] bs = Encoding.ASCII.GetBytes(param);


            PlayText("[PROCESS] Login");
            //https://mail.taipei.gov.tw/cgi-bin/adb2main
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mail.taipei.gov.tw/cgi-bin/login");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies);

            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
            }

            using (WebResponse response = request.GetResponse())
            {
                StreamReader input = new StreamReader(response.GetResponseStream());
                while (!input.EndOfStream)
                {
                    input.ReadLine();
                }
            }

            PlayText("[PROCESS] Getting GT's URL");
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://mail.taipei.gov.tw/cgi-bin/adb2tree2?command=getsub&workingabid=PA.001@mail.taipei.gov.tw&m=413");
            req.Method = "GET";
            req.CookieContainer = request.CookieContainer;
            string a = "";
            using (WebResponse response = req.GetResponse())
            {
                StreamReader input = new StreamReader(response.GetResponseStream());
                while (!input.EndOfStream)
                {
                    a += input.ReadLine();
                }
            }
            Regex reg = new Regex(@"CDATA\[(?<json>[^\>]*)\]\>");
            Match r = reg.Match(a);
            string json = r.Groups["json"].Value;
            json = json.Remove(json.Length - 1).Replace("\t", "");
            BlockObject block = JsonConvert.DeserializeObject<BlockObject>(json);
            List<RgRec> allblock = block.rg_Rec;
            string gt = (from g in allblock where g.szTitle.Equals("臺北市公共運輸處") select g).FirstOrDefault().szID;


            PlayText("[PROCESS] Getting each class");
            string pa = "mode=ajax&pageno=1&command=list&workingabid=PA.001@mail.taipei.gov.tw&workingdirid=" + gt;
            bs = Encoding.ASCII.GetBytes(pa);
            req = (HttpWebRequest)WebRequest.Create("https://mail.taipei.gov.tw/cgi-bin/adb2main");
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = request.CookieContainer;

            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
            }

            a = "";
            using (WebResponse response = req.GetResponse())
            {
                StreamReader input = new StreamReader(response.GetResponseStream());
                while (!input.EndOfStream)
                {
                    a += input.ReadLine();
                }
            }
            r = reg.Match(a);
            json = r.Groups["json"].Value;
            json = json.Remove(json.Length - 1).Replace("\t", "");

            RootObject model = JsonConvert.DeserializeObject<RootObject>(json);
            List<List<object>> loc = model.rgEntries;
            List<KeyValuePair<string, string>> locs = new List<KeyValuePair<string, string>>();
            foreach (List<object> ent in loc)
            {
                if (ent != null)
                    locs.Add(new KeyValuePair<string, string>(Convert.ToString(ent[1]), Convert.ToString(ent[2])));
            }

            webContact = new List<Person>();
            foreach (KeyValuePair<string, string> root in locs)
            {
                PlayText("Loading " + root.Value);
                bool stop = false;
                int page = 1;
                do
                {
                    string par = "mode=ajax&command=list&pageno=" + page + "&pagesize=25&workingabid=PA.001@mail.taipei.gov.tw&workingdirid=" + root.Key + "&tofield=&ssnid=";
                    bs = Encoding.ASCII.GetBytes(par);
                    req = (HttpWebRequest)WebRequest.Create("https://mail.taipei.gov.tw/cgi-bin/adb2main");
                    req.Method = "POST";
                    req.ContentType = "application/x-www-form-urlencoded";
                    req.CookieContainer = request.CookieContainer;

                    using (Stream reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(bs, 0, bs.Length);
                    }

                    a = "";
                    using (WebResponse response = req.GetResponse())
                    {
                        StreamReader input = new StreamReader(response.GetResponseStream());
                        while (!input.EndOfStream)
                        {
                            a += input.ReadLine();
                        }
                    }
                    r = reg.Match(a);
                    json = r.Groups["json"].Value;
                    json = json.Remove(json.Length - 1).Replace("\t", "");
                    ChildObject child = JsonConvert.DeserializeObject<ChildObject>(json);
                    List<List<object>> cloc = child.rgEntries;

                    foreach (List<object> ent in cloc)
                    {
                        if (ent != null)
                        {
                            //Console.WriteLine(Convert.ToString(ent[2])+"\t"+ Convert.ToString(ent[3]));
                            Person p = new Person();
                            p.name = Convert.ToString(ent[2]);
                            p.email = Convert.ToString(ent[3]);
                            p.belongClass = root.Value;
                            webContact.Add(p);
                        }
                    }

                    if (child.nPageNo == child.nTtlPage)
                    {
                        stop = true;
                    }
                    page++;
                } while (!stop);
            }
        }

        private static void SyncPSTContact()
        {
            string UncPath = @"\\172.28.98.6\public";
            string Domain = "local";
            string User = "gt_david" + new Random().Next(1000);
            string Passowrd = "1234";

            PlayText("確認與 " + UncPath + " 的連線");
            Unc unc = new Unc();
            unc.Connect(UncPath, Domain, User, Passowrd);
            if (unc.IsConnect || unc.LastError == 1219)
            {
                PlayText("已連線到 " + UncPath);

                oldData = new List<ContactItem>();
                newData = new List<ContactItem>();
                try
                {
                    PlayText("Start loading new contacts");
                    ReadPSTContact(@"\\172.28.98.6\public\outlookContact\people.pst");
                    Console.WriteLine("Finished");

                    PlayText("Start loading local contacts");
                    ReadCurrentContact();
                    Console.WriteLine("Finished");

                    PlayText("Write a new contact");
                    AddUnExistContactFromPST();
                    PlayText("Finished");
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            else
            {
                PlayText(UncPath + " 連線失敗");
                PlayText("錯誤代碼 " + unc.LastError.ToString());
            }
            Console.WriteLine("請按任意鍵關閉視窗");
            Console.ReadKey(true);
        }

        private static void SyncWebContact()
        {
            oldData = new List<ContactItem>();
            try
            {
                PlayText("Start loading new contacts");
                GetCurrentContact();
                Console.WriteLine("Finished");

                PlayText("Start loading local contacts");
                ReadCurrentContact();
                Console.WriteLine("Finished");

                PlayText("Write a new contact");
                AddUnExistContactFromWeb();
                PlayText("Finished");
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Console.WriteLine("請按任意鍵關閉視窗");
            Console.ReadKey(true);
        }

        private static void ReadPSTContact(string path)
        {
            outlook.GetNamespace("MAPI").AddStore(path);
            MAPIFolder exFolder = outlook.GetNamespace("MAPI").Folders.GetLast();
            foreach (Folder f in exFolder.Folders)
            {
                Items items = f.Items;
                foreach (object item in items)
                {
                    if (item is ContactItem)
                    {
                        ContactItem contact = item as ContactItem;
                        newData.Add(contact);
                        Console.WriteLine(contact.Department + ",\t" + contact.FullName + ",\t" + contact.Email1Address);
                    }
                }
            }
            outlook.GetNamespace("MAPI").RemoveStore(exFolder);

        }

        private static void ReadCurrentContact()
        {
            MAPIFolder folder = outlook.GetNamespace("MAPI").GetDefaultFolder(OlDefaultFolders.olFolderContacts);
            IEnumerable<ContactItem> contacts = folder.Items.OfType<ContactItem>();
            foreach (ContactItem contact in contacts)
            {
                oldData.Add(contact);
                //Console.WriteLine(contact.Department + ",\t" + contact.FullName + ",\t" + contact.Email1Address);
            }
        }

        private static void AddUnExistContactFromPST()
        {
            MAPIFolder folder = outlook.GetNamespace("MAPI").GetDefaultFolder(OlDefaultFolders.olFolderContacts);
            foreach (ContactItem nc in newData)
            {
                bool isExist = false;
                foreach (ContactItem oc in oldData)
                {
                    if (oc.Email1Address.Equals(nc.Email1Address))
                    {
                        isExist = true;
                        break;
                    }
                }
                if (!isExist)
                {
                    ContactItem tc = nc.Copy();
                    tc.Move(folder);
                    tc.Save();
                }
            }
        }

        private static void AddUnExistContactFromWeb()
        {
            int ex = 0;
            MAPIFolder folder = outlook.GetNamespace("MAPI").GetDefaultFolder(OlDefaultFolders.olFolderContacts);
            foreach (Person nc in webContact)
            {
                bool isExist = false;
                foreach (ContactItem oc in oldData)
                {
                    if (oc.Email1Address.Equals(nc.email))
                    {
                        isExist = true;
                        break;
                    }
                }
                if (!isExist)
                {
                    Console.WriteLine("[NEW EMPLOYEE] " + nc.belongClass + ",\t" + nc.name + ",\t" + nc.email);
                    ContactItem person = (ContactItem)outlook.CreateItem(OlItemType.olContactItem);
                    person.Email1Address = nc.email;
                    person.FullName = nc.name;
                    person.Department = nc.belongClass;
                    person.Save();
                    ex++;
                }
            }
            if (ex == 0) PlayText("[MESSAGE] No more employee");
        }

        private static void MakeUnExistContactCSV()
        {
            StreamWriter output = new StreamWriter(@"Contact.csv", false, Encoding.Default);
            GetCurrentContact();
            for (int i = 0; i < webContact.Count; i++)
            {
                Person p = webContact[i];
                output.WriteLine(p.belongClass + "," + p.name + "," + p.email);
            }
            output.Flush();
            output.Close();
        }
    }

    //建立Unc類別，並使用NetUseAdd與NetUseDel
    class Unc : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct USE_INFO_2
        {
            internal string ui2_local;
            internal string ui2_remote;
            internal string ui2_password;
            internal UInt32 ui2_status;
            internal UInt32 ui2_asg_type;
            internal UInt32 ui2_refcount;
            internal UInt32 ui2_usecount;
            internal string ui2_username;
            internal string ui2_domainname;
        }

        [DllImport("NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern UInt32 NetUseAdd(string UncServerName, UInt32 Level, ref USE_INFO_2 Buf, out UInt32 ParmError);

        [DllImport("NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern UInt32 NetUseDel(string UncServerName, string UseName, UInt32 ForceCond);

        private string _UncPath;
        private int _LastError;
        private bool _IsConnect;

        public bool Connect(string UncPath, string Domain, string User, string Password)
        {
            try
            {
                var useinfo = new USE_INFO_2
                {
                    ui2_remote = UncPath,
                    ui2_domainname = Domain,
                    ui2_username = User,
                    ui2_password = Password,
                    ui2_asg_type = 0,
                    ui2_usecount = 1
                };
                _UncPath = UncPath;
                uint parmError;
                uint Code = NetUseAdd(null, 2, ref useinfo, out parmError);
                _LastError = (int)Code;
                _IsConnect = (Code == 0);
                return _IsConnect;
            }
            catch
            {
                _LastError = Marshal.GetLastWin32Error();
                return false;
            }
        }

        public bool Disconnect()
        {
            try
            {
                uint Code = NetUseDel(null, _UncPath, 2);
                _LastError = (int)Code;
                return (Code == 0);
            }
            catch
            {
                _LastError = Marshal.GetLastWin32Error();
                return false;
            }
        }

        public bool IsConnect
        {
            get { return _IsConnect; }
        }

        public int LastError
        {
            get { return _LastError; }
        }

        public void Dispose()
        {
            if (_IsConnect)
                Disconnect();
            GC.SuppressFinalize(this);
        }
    }


    public class RootObject
    {
        public string magic { get; set; }
        public string crumb { get; set; }
        public int nTtlPage { get; set; }
        public int nPageSize { get; set; }
        public int nPageNo { get; set; }
        public int nBgnIdx { get; set; }
        public int nEndIdx { get; set; }
        public string szLastCmd { get; set; }
        public string szPageType { get; set; }
        public int nInitOption { get; set; }
        public string szWorkingDirID { get; set; }
        public string szWorkingABID { get; set; }
        public int nWorkingABType { get; set; }
        public string szWorkingABName { get; set; }
        public string szRootDirID { get; set; }
        public List<List<string>> rgPathes { get; set; }
        public int nErrorMsg { get; set; }
        public int nErrorNum { get; set; }
        public int nActionNum { get; set; }
        public int nTotalCount { get; set; }
        public int nErrorCount { get; set; }
        public bool bIfToField { get; set; }
        public string szToField { get; set; }
        public bool bIfPermW { get; set; }
        public int nSortBy { get; set; }
        public int nEntryCount { get; set; }
        public bool bIfPubADB { get; set; }
        public string szSSNID { get; set; }
        public string szSortField { get; set; }
        public bool bIfSortBy { get; set; }
        public List<List<string>> rgAdbooks { get; set; }
        public List<List<string>> rgTitle { get; set; }
        public bool bIfShowSerial { get; set; }
        public List<List<object>> rgEntries { get; set; }
        public bool bIfNoEntry { get; set; }
        public bool bIfHasRECV { get; set; }
        public List<object> rgTo { get; set; }
        public bool bIfNoTo { get; set; }
        public List<object> rgCc { get; set; }
        public bool bIfNoCc { get; set; }
        public List<object> rgBcc { get; set; }
        public bool bIfNoBcc { get; set; }
        public bool bIfRefreshFrm { get; set; }
    }

    public class ChildObject
    {
        public string magic { get; set; }
        public string crumb { get; set; }
        public int nTtlPage { get; set; }
        public int nPageSize { get; set; }
        public int nPageNo { get; set; }
        public int nBgnIdx { get; set; }
        public int nEndIdx { get; set; }
        public string szLastCmd { get; set; }
        public string szPageType { get; set; }
        public int nInitOption { get; set; }
        public string szWorkingDirID { get; set; }
        public string szWorkingABID { get; set; }
        public int nWorkingABType { get; set; }
        public string szWorkingABName { get; set; }
        public string szRootDirID { get; set; }
        public List<List<string>> rgPathes { get; set; }
        public int nErrorMsg { get; set; }
        public int nErrorNum { get; set; }
        public int nActionNum { get; set; }
        public int nTotalCount { get; set; }
        public int nErrorCount { get; set; }
        public bool bIfToField { get; set; }
        public string szToField { get; set; }
        public bool bIfPermW { get; set; }
        public int nSortBy { get; set; }
        public int nEntryCount { get; set; }
        public bool bIfPubADB { get; set; }
        public string szSSNID { get; set; }
        public string szSortField { get; set; }
        public bool bIfSortBy { get; set; }
        public List<List<string>> rgAdbooks { get; set; }
        public List<List<string>> rgTitle { get; set; }
        public bool bIfShowSerial { get; set; }
        public List<List<object>> rgEntries { get; set; }
        public bool bIfNoEntry { get; set; }
        public bool bIfHasRECV { get; set; }
        public List<object> rgTo { get; set; }
        public bool bIfNoTo { get; set; }
        public List<object> rgCc { get; set; }
        public bool bIfNoCc { get; set; }
        public List<object> rgBcc { get; set; }
        public bool bIfNoBcc { get; set; }
        public bool bIfRefreshFrm { get; set; }
    }

    public class RgRec
    {
        public string szID { get; set; }
        public string szTitle { get; set; }
        public int nEntryType { get; set; }
        public bool bHasChild { get; set; }
        public string nVDType { get; set; }
    }

    public class BlockObject
    {
        public string workingabid { get; set; }
        public int nType { get; set; }
        public List<RgRec> rg_Rec { get; set; }
    }

    public class Person
    {
        public string name { get; set; }
        public string email { get; set; }
        public string belongClass { get; set; }
    }

}
