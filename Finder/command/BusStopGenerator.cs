using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finder.command
{
    class BusStopGenerator
    {
        static Database1Entities db = new Database1Entities();
        public static void ImportBusStop(object obj)
        {
            object[] args = (object[])obj;
            string path = (string)args[0];
            RecognizeForm form = (RecognizeForm)args[1];

            form.UpdateText("Getting data from database");
            List<BusStop> dbStop = (from s in db.BusStop select s).ToList();
            List<BusStopClass> dbStopClass = (from c in db.BusStopClass select c).ToList();
            form.UpdateLog("Getting data from database");
            List<BusStop> temp = new List<BusStop>();
            List<BusStopClass> tempC = new List<BusStopClass>();
            form.UpdateText("Start reading file");
            StreamReader input = new StreamReader(path);
            while (!input.EndOfStream)
            {
                string[] data = input.ReadLine().Split(',');
                BusStopClass bsc = (from bc in dbStopClass where bc.Name.Equals(data[5]) select bc).FirstOrDefault();
                if (bsc == null)
                {
                    bsc = (from b in tempC where b.Name.Equals(data[5]) select b).FirstOrDefault();
                    if (bsc == null)
                    {
                        bsc = new BusStopClass();
                        bsc.Name = data[5];
                        db.BusStopClass.Add(bsc);

                        tempC.Add(bsc);
                        db.SaveChanges();
                    }
                }

                BusStop stop = (from st in dbStop where 
                                    st.District.Equals(data[1].Trim()) 
                                    && st.Name.Equals(data[2].Trim()) 
                                    && st.Address.Equals(data[3].Trim()) 
                                    && st.Direction.Equals(data[4].Trim()) 
                                    && st.BSCID == bsc.BSCID 
                                select st).FirstOrDefault();
                if (stop == null)
                {
                    stop = (from tt in temp
                            where
                                tt.District.Equals(data[1].Trim())
                                && tt.Name.Equals(data[2].Trim())
                                && tt.Address.Equals(data[3].Trim())
                                && tt.Direction.Equals(data[4].Trim())
                                && tt.BSCID == bsc.BSCID
                            select tt).FirstOrDefault();
                    if (stop == null)
                    {
                        stop = new BusStop();
                        stop.District = data[1].Trim();
                        stop.Name = data[2].Trim().Replace(" ",String.Empty).Replace("　",String.Empty);
                        if (stop.Name.Length > 20)
                        {
                            form.UpdateLog("STOP [ "+stop.Name+" ]\t:\t NAME LENGTH > 20");
                        }
                        stop.Address = data[3].Trim();
                        if (stop.Name.Length > 50)
                        {
                            form.UpdateLog("STOP [ " + stop.Name + " ]\t:\t ADDRESS LENGTH > 50");
                        }
                        if (stop.Address.Equals(""))
                        {
                            stop.Address = "unknown";
                        }
                        stop.Direction = data[4].Trim().Replace("往", String.Empty).Replace("向", String.Empty);
                        if (stop.Name.Length > 20)
                        {
                            form.UpdateLog("STOP [ " + stop.Name + " ]\t:\t NAME DIRECTION > 1");
                        }
                        if (stop.Direction.Equals(""))
                        {
                            stop.Direction = "N";
                        }
                        stop.BSCID = bsc.BSCID;

                        temp.Add(stop);
                        db.BusStop.Add(stop);
                    }
                }
            }
            form.UpdateLog("Start reading file");
            form.UpdateText("Read finished");
            form.UpdateLog("Read finished");
            form.UpdateText("Incert data into database");
            db.SaveChanges();
            form.UpdateLog("Incert finished");
        }

    }
}
