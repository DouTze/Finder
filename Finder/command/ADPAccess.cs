using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finder.command
{
    class ADPAccess
    {
        public static void QueryHistory(object obj)
        {
            WebServer server = new WebServer(
                (reqData) => { return SwitchProcess(reqData); });
            object[] objs = (object[])obj;
            MainForm mform = (MainForm)objs[0];
            string[] cmd = (string[])objs[1];
        }
        public static JSONResponse SwitchProcess(string data)
        {
            JSONResponse resp = new JSONResponse();

            switch (data)
            {

            }

            return resp;
        }
    }
}
