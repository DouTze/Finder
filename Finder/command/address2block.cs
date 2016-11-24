using Google.Maps;
using Google.Maps.Places;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Finder
{
    class Address2Block
    {
        static List<String> busStop = new List<String>();
        static List<String> busStopAddress = new List<String>();
        static List<String> block = new List<String>();
        public static void Run(object obj)
        {
            object[] objs = (object[])obj;
            RecognizeForm form = (RecognizeForm)objs[0];
            String inp = (String)objs[1];
            String outp = (String)objs[2];
            try
            {
                form.UpdateText("Start loading file [ " + inp + " ]");
                StreamReader input = new StreamReader(inp);
                StreamWriter output = new StreamWriter(outp);
                String[] cvsformat;
                form.UpdateLog("Start loading file [ " + inp + " ]");
                while (!input.EndOfStream)
                {
                    cvsformat = input.ReadLine().Split(',');
                    busStop.Add(cvsformat[1]);
                    String addr = cvsformat[2];
                    int h = addr.IndexOf("巷");
                    int i = addr.IndexOf("弄");
                    int j = addr.IndexOf("號");
                    int k = addr.IndexOf("段");

                    if (j != -1)
                    {
                        addr = addr.Substring(0, j + 1);
                    }
                    else if (i !=-1)
                    {
                        addr = addr.Substring(0, i + 1);
                    }
                    else if (h !=-1)
                    {
                        addr = addr.Substring(0, h + 1);
                    }
                    else if (k != -1)
                    {
                        addr = addr.Substring(0, k + 1);
                    }
                    busStopAddress.Add(addr);
                }
                form.UpdateLog("Loading file [ " + inp + " ] finished");

                form.UpdateText("Start search address by Google Map");
                form.UpdateLog("Start search address by Google Map");
                Regex reg = new Regex("台北市(?<block>.{2})區");
                GoogleSigned.AssignAllServices(new GoogleSigned("AIzaSyCHjm9IB5R9kVBD3j_9dIuyVs_AKtVCA7s"));
                for (int i = 0; i < busStop.Count; i++)
                {
                    String b = "NOT FOUND";
                    form.UpdateText("Search " + busStop[i]);
                    if (busStopAddress[i] == "")
                    {
                        form.UpdateLog(i + " [ " + busStop[i] + " ] : " + b);
                        output.WriteLine((i + 1) + "," + b + "," + busStop[i]);
                        continue;
                    }
                    TextSearchRequest request = new TextSearchRequest();
                    request.Query = busStopAddress[i];
                    if (busStopAddress[i].Length < 5)
                    {
                        request.Query = busStop[i];
                    }
                    request.Language = "zh-TW";
                    request.Sensor = false;
                    PlacesResponse response = new PlacesService().GetResponse(request);

                    PlacesResult[] result = response.Results;

                    if (result.Length==0)
                    {
                        TextSearchRequest stopRequest = new TextSearchRequest();
                        stopRequest.Query = busStop[i];
                        stopRequest.Language = "zh-TW";
                        stopRequest.Sensor = false;
                        PlacesResponse stopResponse = new PlacesService().GetResponse(stopRequest);
                        result = stopResponse.Results;
                    }

                    if (reg.IsMatch(result[0].FormattedAddress))
                    {
                        Match match = reg.Match(result[0].FormattedAddress);
                        b = match.Groups["block"].Value + "區";
                    }
                    
                    form.UpdateLog(i + " [ " + busStop[i] + " ] : " + b);
                    output.WriteLine((i + 1) + "," + b + "," + busStop[i]);
                }
                output.Flush();
                output.Close();
                input.Close();
                form.UpdateText("All records finished");
                form.UpdateLog("All records finished");
            }
            catch (Exception e)
            {
                form.UpdateLog("Occur error "+e.Message+", Stop task");
            }
            
        }
    }
}
