using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finder.command
{
    public class TaskModelGenerator
    {
        static Database1Entities db = new Database1Entities();
        public static void CreateTaskModel(object obj)
        {
            // mktaskmodel -n 7 1(classId) 等待到達量 掛文號 印兩份一份存查 等繳費證明 掃描繳費證明 拿黏貼憑證 給承辦人
            object[] args = (object[])obj;
            short stepCount = Convert.ToInt16(args[0]);
            int classId = Convert.ToInt32(args[1]);

            TaskClass tc = (from t in db.TaskClass where t.TCID == classId select t).FirstOrDefault();

            TaskModel tm = new TaskModel();
            tm.StepCount = stepCount;
            tm.TCID = tc.TCID;
            db.TaskModel.Add(tm);

            for (short i = 0; i < stepCount; i++)
            {
                TaskModelDetail tmd = new TaskModelDetail();
                tmd.TMID = tm.TMID;
                tmd.Step = i;
                tmd.Description = (string)args[i + 2];
                db.TaskModelDetail.Add(tmd);
            }

            db.SaveChanges();
        }

        public static void CreateTaskClass(object obj)
        {
            object[] args = (object[])obj;
            string className = (string)args[0];

            // Create model class
            TaskClass tc = new TaskClass();
            tc.Name = className;
            db.TaskClass.Add(tc);

            db.SaveChanges();
        }

        public static void ShowTaskClass(object obj)
        {
            object[] args = (object[])obj;
            RecognizeForm form = (RecognizeForm)args[0];

            List<TaskClass> tcs = (from t in db.TaskClass select t).ToList();
            string list = "";
            foreach (TaskClass tc in tcs)
            {
                list += "\n" + tc.TCID + "\t:\t" + tc.Name;
            }
            form.UpdateLog(list);
        }
    }
}
