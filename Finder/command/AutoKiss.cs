using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Finder.command
{
    public class AutoKiss
    {
        public static void KissMe(object obj)
        {
            object[] objs = (object[])obj;
            MainForm mform = (MainForm)objs[0];
            Kiss("gt_allenc%40mail.taipei.gov.tw", "000000cb%40", mform);
        }

        public static void Kiss(object obj)
        {
            object[] objs = (object[])obj;
            string userid = (string)objs[0];
            string pwd = (string)objs[1];
            MainForm mform = (MainForm)objs[2];
            Kiss(userid, pwd, mform);
        }

        public static void Kiss(string user, string pwd, MainForm mform)
        {
            mform.UpdateText("Start Kissing");
            mform.UpdateLog("Start Kissing");
            mform.UpdateText("Start Login " + user);
            
            // Get first session id
            mform.UpdateLog("[LOGIN] Get first session id");
            HttpWebRequest request = WebRequest.CreateHttp("http://kiss.taipei.gov.tw/");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            CookieCollection sessionid = response.Cookies;

            // Get redirect url
            mform.UpdateLog("[LOGIN] Login");
            request = WebRequest.CreateHttp("http://kiss.taipei.gov.tw/Login/Login_Check.asp");
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            CookieContainer sid = new CookieContainer();
            sid.Add(sessionid);
            request.CookieContainer = sid;
            string param = "Login_Name=" + user + "&Login_PassWord=" + pwd;
            byte[] bs = Encoding.ASCII.GetBytes(param);

            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
            }
            response = (HttpWebResponse)request.GetResponse();
            StreamReader input = new StreamReader(response.GetResponseStream());
            string content = "";
            while (!input.EndOfStream)
            {
                content += input.ReadLine();
            }
            Regex regex = new Regex("location.href=\"(?<uri>[^\"]+)\";");
            Match match = regex.Match(content);
            string uri = match.Groups["uri"].Value;

            // Get Userid & .net session id
            mform.UpdateLog("[LOGIN] Get user id & .NET session id");
            request = WebRequest.CreateHttp("http://kiss.taipei.gov.tw/Login/" + uri);
            sid = new CookieContainer();
            sid.Add(response.Cookies);
            request.CookieContainer = sid;
            response = (HttpWebResponse)request.GetResponse();
            input = new StreamReader(response.GetResponseStream());
            while (!input.EndOfStream)
            {
                input.ReadLine();
            }
            mform.UpdateText("Login " + user+" successful!");
            mform.UpdateLog ("Login " + user + " successful!");

            // Get index content
            mform.UpdateText("Start auto kissing");
            request = WebRequest.CreateHttp("http://kiss.taipei.gov.tw/Calendar/index.aspx");
            CookieContainer cl = new CookieContainer();
            cl.Add(response.Cookies);
            request.CookieContainer = cl;
            content = "";
            using (response = (HttpWebResponse)request.GetResponse())
            {
                sid = new CookieContainer();
                sid.Add(response.Cookies);
                input = new StreamReader(response.GetResponseStream());
                while (!input.EndOfStream)
                {
                    content += input.ReadLine();
                }
            }

            // Analysis content
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);

            string countx = "//*[@id=\"ctl00_ContentPlaceHolder2_WebPartManager1_gwpPanel4_dlmsg_ctl00_lblimsg\"]/li[1]";
            string linkx = "//*[@id=\"ctl00_ContentPlaceHolder2_WebPartManager1_gwpPanel4_dlmsg_ctl00_lblimsg\"]/li/a";
            HtmlNodeCollection c = doc.DocumentNode.SelectNodes(countx);

            // Get total unread message count
            string title = "";
            foreach (HtmlNode node in c)
            {
                title = node.InnerText;
            }
            regex = new Regex("[0-9]+");
            match = regex.Match(title);
            int count = Convert.ToInt32(match.Value);
            mform.UpdateLog("[MESSAGE] " + count + " LINKS FOUND!");

            // Get <=10 unread message url
            List<string> urls = new List<string>();
            HtmlNodeCollection links = doc.DocumentNode.SelectNodes(linkx);
            regex = new Regex("href='(?<url>[^']+)'");
            foreach (HtmlNode link in links)
            {
                urls.Add("http://kiss.taipei.gov.tw" + regex.Match(link.OuterHtml).Groups["url"].Value);
            }

            // Read to end
            while (count > 0)
            {
                count -= links.Count;
                foreach (string url in urls)
                {
                    mform.UpdateLog("[OPEN URL] : " + url);

                    request = WebRequest.CreateHttp(url);
                    request.CookieContainer = sid;
                    response = (HttpWebResponse)request.GetResponse();
                    input = new StreamReader(response.GetResponseStream());
                    while (!input.EndOfStream)
                    {
                        input.ReadLine();
                    }

                    Thread.Sleep(20);
                }
            }
            mform.UpdateText("FINISHED!");
            mform.UpdateLog("FINISHED!");
        }
    }
    
}



