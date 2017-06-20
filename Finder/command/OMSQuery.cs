using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Office.Interop.Excel;

namespace Finder.command
{
    class OMSQuery
    {
        private static string default_acc = "chouyi";
        private static string default_pas = "ebus@5284";
        private static string oms_index = "http://oms.5284.com.tw/OMS/";
        private static CookieCollection cookies;
        public OMSQuery()
        {



        }

        public static bool IsLogin()
        {
            return (cookies == null) ? false : true;
        }

        public static void Login()
        {
            HttpWebRequest req = WebRequest.CreateHttp(oms_index + "login.html");
            HttpWebResponse response = (HttpWebResponse)req.GetResponse();

            StreamReader input = new StreamReader(response.GetResponseStream());
            string context = "";
            while (!input.EndOfStream)
            {
                context += input.ReadLine();
            }
            req = WebRequest.CreateHttp(oms_index + "j_spring_security_check");
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            req.Method = "POST";

            string param = "j_username=" + default_acc + "&j_password=" + default_pas;
            byte[] bs = Encoding.ASCII.GetBytes(param);

            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
            }
            response = (HttpWebResponse)req.GetResponse();

            input = new StreamReader(response.GetResponseStream());
            context = "";
            while (!input.EndOfStream)
            {
                context += input.ReadLine();
            }
            cookies = new CookieCollection();
            string[] session = response.Headers.GetValues(2)[0].Split(';');
            Cookie cookie = new Cookie(session[0].Split('=')[0], session[0].Split('=')[1]);
            cookie.Path = session[1].Split('=')[1];
            cookie.Domain = "oms.5284.com.tw";
            cookies.Add(cookie);
        }

        public static void Execute(object obj)
        {
            int[] dates = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            object[] objs = (object[])obj;

            Application excel = new Application();
            string strPath = @"C:\Users\Allen Chou\Documents\013-U5發車準點稽核.xlsx";
            Workbook Wbook = excel.Workbooks.Open(strPath);
            System.IO.FileInfo xlsAttribute = new FileInfo(strPath);
            xlsAttribute.Attributes = FileAttributes.Normal;

            MainForm mform = (MainForm)objs[0];
            // oms {cmd [args]}
            //oms 2017 02 02 04 01 1000 013-U5
            string[] cmd = (string[])objs[1];
            string year = cmd[0];
            int smonth = Convert.ToInt32(cmd[1]);
            int sdate = Convert.ToInt32(cmd[2]);
            int emonth = Convert.ToInt32(cmd[3]);
            int edate = Convert.ToInt32(cmd[4]);
            string limit = cmd[5];
            string carNum = cmd[6];
            int month = smonth;
            int date = sdate;
            int sheetcount = 1;

            mform.UpdateText("Start Login");
            if (!IsLogin()) Login();
            mform.UpdateLog("Start Login");

            mform.UpdateText("Query car number[ "+carNum+" ]'s data from "+smonth+"/"+sdate+" to "+emonth+"/"+edate);
            mform.UpdateLog("Query car number[ " + carNum + " ]'s data from " + smonth + "/" + sdate + " to " + emonth + "/" + edate);
            while (month != emonth || date != edate)
            {
                mform.UpdateLog("Start query [ " + month + "/" + date + " ]");
                string cmonth = (Convert.ToString(month).Length < 2) ? "0" + Convert.ToString(month) : Convert.ToString(month);
                string cdate = (Convert.ToString(date).Length < 2) ? "0" + Convert.ToString(date) : Convert.ToString(date);

                string query = "http://oms.5284.com.tw/OMS/action/auditRecordTP/findA1_1_1?companyId=200&companyName=%E9%A6%96%E9%83%BD%E5%AE%A2%E9%81%8B&stationId=16225&stationName=%E6%B0%91%E7%94%9F%E7%AB%99&pathId=16111&pathName=307&recordDate=" + year + "%2F" + cmonth + "%2F" + cdate + "&page=1&start=0&limit=" + limit + "&sort=%5B%7B%22property%22%3A%22sysMemo1%22%2C%22direction%22%3A%22ASC%22%7D%5D";
                HttpWebRequest req = WebRequest.CreateHttp(query);
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cookies);
                req.Referer = oms_index + "jsp/index.jsp";
                req.Accept = "*/*";
                req.KeepAlive = true;
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                StreamReader input = new StreamReader(response.GetResponseStream());
                string json = "";
                while (!input.EndOfStream)
                {
                    json += input.ReadLine();
                }
                OMSObject model = JsonConvert.DeserializeObject<OMSObject>(json);

                Wbook.Worksheets.Add(Type.Missing, Wbook.Worksheets[sheetcount], 1, Type.Missing);
                Worksheet Wsheet = excel.Worksheets[sheetcount + 1];
                Wsheet.Name = carNum + "發車準點稽核" + cmonth + cdate;

                int rowcount = 1;

                List<string> list = new List<string>();
                foreach (Datum d in model.data)
                {
                    if (d.carNum2 != null && d.carNum2.Equals(carNum))
                    {
                        bool empexist = false;
                        foreach (string name in list)
                        {
                            if (name != null && name.Equals(d.empName))
                            {
                                empexist = true;
                                break;
                            }
                        }
                        if (!empexist && d.empName != null) list.Add(d.empName);
                        Wsheet.get_Range("A" + rowcount + 1, "D" + rowcount + 1).NumberFormatLocal = "@";
                        Wsheet.Cells[rowcount + 1, 1] = (d.sysDepartureTime.Length < 6) ? "0" + d.sysDepartureTime.Substring(0, 3) : d.sysDepartureTime.Substring(0, 4);
                        Wsheet.Cells[rowcount + 1, 2] = (d.sysArrivalTime.Length < 6) ? "0" + d.sysArrivalTime.Substring(0, 3) : d.sysArrivalTime.Substring(0, 4);
                        Wsheet.Cells[rowcount + 1, 3] = d.stopSeq;
                        Wsheet.Cells[rowcount + 1, 4] = d.stopInfo;
                        rowcount++;
                    }

                }
                if (rowcount == 1)
                {
                    Wsheet.Cells[1, 1] = "無資料";
                }
                else
                {
                    Wsheet.Cells[1, 1] = "發車時間";
                    Wsheet.Cells[1, 2] = "返站時間";
                    Wsheet.Cells[1, 3] = "觸發站序";
                    Wsheet.Cells[1, 4] = "觸發站位";
                }

                for (int i = 0; i < list.Count; i++)
                    Wsheet.Cells[rowcount + 1, i + 1] = list[i];

                sheetcount++;
                date++;
                if (date > dates[month-1])
                {
                    month++;
                    date = 1;
                }
            }
            mform.UpdateText("Saveing......");
            Wbook.Save();
            Wbook.Close();
            mform.UpdateText("Finished");
            mform.UpdateLog("Finished");
        }

        private static void Query()
        {

        }
    }
    public class Datum
    {
        public int id { get; set; }
        public int? refbatchId { get; set; }
        public int? refrecId { get; set; }
        public string departureDate { get; set; }
        public string scheTime { get; set; }
        public int schePathAttrId { get; set; }
        public int pathId { get; set; }
        public string pathName { get; set; }
        public string empId { get; set; }
        public string empName { get; set; }
        public string carNum1 { get; set; }
        public int? carId1 { get; set; }
        public string realDepartureTime { get; set; }
        public string realArrivalTime { get; set; }
        public string abnormal1 { get; set; }
        public string range1 { get; set; }
        public int? worktime1 { get; set; }
        public string memo { get; set; }
        public int? setPathId { get; set; }
        public string setPathName { get; set; }
        public int? setSysPathAttrId { get; set; }
        public string carNum2 { get; set; }
        public int? carId2 { get; set; }
        public string sysDepartureTime { get; set; }
        public string sysArrivalTime { get; set; }
        public string stopSeq { get; set; }
        public string stopInfo { get; set; }
        public string abnormal2 { get; set; }
        public string range2 { get; set; }
        public int? worktime2 { get; set; }
        public string sysMemo1 { get; set; }
        public string sysMemo2 { get; set; }
        public int stationId { get; set; }
        public object stationName { get; set; }
        public int companyId { get; set; }
        public string companyName { get; set; }
        public object successRate { get; set; }
        public string estSysDepartureTime { get; set; }
        public object repairStatus { get; set; }
        public object muser { get; set; }
        public object mTime { get; set; }
    }

    public class OMSObject
    {
        public string message { get; set; }
        public int total { get; set; }
        public List<Datum> data { get; set; }
        public bool success { get; set; }
    }
}
