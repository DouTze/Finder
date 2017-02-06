using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using Finder.command;


namespace Finder
{
    public partial class MainForm : Form
    {
        delegate void UpdateMain();
        delegate void UpdateMainText(String text);
        delegate void UpdateMainLabel(Label label, String content);
        private string[] loading = new string[] { "一", "\\", "|", "/" };
        private const string showpwd = "Taipei City Public Transportation Office............................................................Design By DouTze, 2016. ";
        private int showpwd_i = 0;
        private string spd = "";
        private bool login_a = false;
        private bool login_p = false;
        private string userAccount = "";
        private string password = "";
        private List<String> commandHistory = new List<string>();
        private int historyIndex = 0;
        public User user;
        private int curr_x, curr_y;
        private bool isWndMove;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            ExecuteCommand(textBox1.Text);
        }


        private void ElectricityBill(string[] args)
        {
            textBox1.Text = "";
            Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
            Thread recogBill = new Thread(new ParameterizedThreadStart(ImageProcesser.RecognizeBill));
            recogBill.IsBackground = true;
            string option = args[1];
            try
            {
                switch (option)
                {
                    case "-e":
                        recogBill.Start(new object[] { this, @"C:\Users\Allen Chou\Documents\Visual Studio 2013\Projects\Finder\Finder\tess.png" });
                        loadingAnime.IsBackground = true;
                        loadingAnime.Start(recogBill);
                        break;
                    case "-input":
                        recogBill.Start(new object[] { this, args[2] });
                        loadingAnime.IsBackground = true;
                        loadingAnime.Start(recogBill);
                        break;
                    case "-p":
                        Thread executeIP = new Thread(new ParameterizedThreadStart(ImageProcesser.Execute));
                        executeIP.IsBackground = true;
                        string[] cmd = new string[args.Length - 2];
                        Array.Copy(args, 2, cmd, 0, cmd.Length);
                        executeIP.Start(new object[] { this, cmd });
                        loadingAnime.IsBackground = true;
                        loadingAnime.Start(executeIP);
                        break;
                    default:
                        Exception e = new Exception("Unknown command "+option);
                        throw e;
                }
            }
            catch (Exception e)
            {
                UpdateLog(e.Message);
                ReleaseCommand();
            }


        }

        private void Mail(string[] args)
        {
            textBox1.Text = "";
            Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
            Thread mail = new Thread(new ParameterizedThreadStart(Email.GetContact));
            mail.IsBackground = true;
            string option = args[1];
            try
            {
                switch (option)
                {
                    case "-ls":
                        mail.Start(new object[] { this });
                        loadingAnime.IsBackground = true;
                        loadingAnime.Start(mail);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                UpdateLog(e.Message);
                ReleaseCommand();
            }
        }

        private void A2B(string[] args)
        {
            textBox1.Text = "";

            if (args.Length < 3)
            {
                UpdateLog("Command \"a2b\" need 2 args");
            }
            else
            {
                Thread a2b = new Thread(new ParameterizedThreadStart(Address2Block.Run));
                a2b.IsBackground = true;
                a2b.Start(new object[] { this, args[1], args[2] });

                Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
                loadingAnime.IsBackground = true;
                loadingAnime.Start(a2b);
            }
        }

        private void Kiss(string[] args)
        {
            textBox1.Text = "";
            Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
            Thread kiss = new Thread(new ParameterizedThreadStart(AutoKiss.Kiss));
            kiss.IsBackground = true;
            if (args.Length > 1)
            {
                string userid = args[1];
                string pwd = args[2];
                kiss.Start(new object[] { userid, pwd, this });
            }
            else
            {
                kiss = new Thread(new ParameterizedThreadStart(AutoKiss.KissMe));
                kiss.IsBackground = true;
                kiss.Start(new object[] { this });
            }
            loadingAnime.IsBackground = true;
            loadingAnime.Start(kiss);
        }

        private void MakeTaskModel(string[] args)
        {
            textBox1.Text = "";

            if (args.Length < 3)
            {
                UpdateLog("Command \"mktm\" need 2 args");
            }
            else
            {
                string option = args[1];
                try
                {
                    Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
                    switch (option)
                    {
                        case "-ntm":
                            break;
                        case "-ntc":
                            textBox1.Text = "";
                            Thread ntc = new Thread(new ParameterizedThreadStart(TaskModelGenerator.CreateTaskClass));
                            ntc.IsBackground = true;
                            ntc.Start(new object[] { args[2] });

                            loadingAnime.IsBackground = true;
                            loadingAnime.Start(ntc);
                            break;
                        case "-lstc":
                            textBox1.Text = "";
                            Thread lstc = new Thread(new ParameterizedThreadStart(TaskModelGenerator.ShowTaskClass));
                            lstc.IsBackground = true;
                            lstc.Start(new object[] { this });

                            loadingAnime.IsBackground = true;
                            loadingAnime.Start(lstc);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    UpdateLog(e.Message);
                    ReleaseCommand();
                }
            }
        }

        private void BusStopFunction(string[] args)
        {
            textBox1.Text = "";

            string option = args[1];
            try
            {
                Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
                switch (option)
                {
                    case "-import":
                        string[] filename = args[2].Split('.');
                        if (!filename[filename.Length - 1].Equals("csv"))
                        {
                            loadingAnime = null;
                            UpdateLog("Only support CSV file, please check file format and try again");
                            ReleaseCommand();
                            break;
                        }
                        textBox1.Text = "";
                        Thread import = new Thread(new ParameterizedThreadStart(BusStopGenerator.ImportBusStop));
                        import.IsBackground = true;
                        import.Start(new object[] { args[2], this });

                        loadingAnime.IsBackground = true;
                        loadingAnime.Start(import);
                        break;
                    case "-ls":
                        textBox1.Text = "";
                        Thread list = new Thread(new ParameterizedThreadStart(BusStopGenerator.ShowBusStop));
                        list.IsBackground = true;
                        list.Start(new object[] { this });

                        loadingAnime.IsBackground = true;
                        loadingAnime.Start(list);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                UpdateLog(e.Message);
                ReleaseCommand();

            }
        }

        private void WhoAmI()
        {
            textBox1.Text = "";
            UpdateLog((user != null) ? user.Name : "unknown user");
        }

        private void RunLogin(string[] args)
        {
            if (args.Length == 1)
            {
                textBox1.Text = "";

                UpdateText("Enter User Account");
                login_a = true;
                ReleaseCommand();
            }
            else
            {
                string option = args[1];
                try
                {
                    switch (option.Substring(0, 2))
                    {
                        case "-r":
                            textBox1.Text = "";
                            Thread ul = new Thread(new ParameterizedThreadStart(Login.CreateUser));
                            ul.IsBackground = true;
                            ul.Start(new object[] { args[2], args[3], args[4], args[5], args[6], this });

                            Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
                            loadingAnime.IsBackground = true;
                            loadingAnime.Start(ul);
                            break;
                        case "-q":
                            textBox1.Text = "";
                            user = null;
                            UpdateLog("Logout successful");
                            ReleaseCommand();
                            break;
                        default:
                            UpdateLog("Option [ " + option + " ] not found");
                            ReleaseCommand();
                            break;
                    }
                }
                catch (Exception e)
                {
                    UpdateLog(e.Message);
                    ReleaseCommand();
                }
            }

        }

        public void UpdateText(String text)
        {
            if (this.InvokeRequired)
            {
                UpdateMainText umt = new UpdateMainText(UpdateText);
                this.Invoke(umt, new object[] { text });
            }
            else
            {
                label1.Text = "";
                for (int i = 0; i < text.Length; i++)
                {
                    label1.Text += text[i];
                    label1.Update();
                    Thread.Sleep(20);
                }
            }
        }

        public void UpdateLog(String log)
        {
            if (this.InvokeRequired)
            {
                UpdateMainText umt = new UpdateMainText(UpdateLog);
                this.Invoke(umt, new object[] { log });
            }
            else
            {
                textBox2.AppendText("[" + DateTime.Now + "]\t" + log + "\n");
                textBox2.Update();
            }
        }

        public void UpdateLogMessage(String msg)
        {
            if (this.InvokeRequired)
            {
                UpdateMainText umt = new UpdateMainText(UpdateLogMessage);
                this.Invoke(umt, new object[] { msg });
            }
            else
            {
                textBox2.AppendText(msg + "\n");
                textBox2.Update();
            }
        }

        public void UpdateLabel(Label label, String content)
        {
            if (this.InvokeRequired)
            {
                UpdateMainLabel uml = new UpdateMainLabel(UpdateLabel);
                this.Invoke(uml, new object[] { label, content });
            }
            else
            {
                label.Text = content;
                label.Update();
            }
        }

        public void UpdateLoadingIcon(object bgThread)
        {
            int b = 0;
            while (((Thread)bgThread).ThreadState != ThreadState.Stopped)
            {
                UpdateLabel(label12, loading[b]);
                b = (b + 1) % loading.Length;
                Thread.Sleep(50);
            }
            ReleaseCommand();
        }


        public void ReleaseCommand()
        {
            if (this.InvokeRequired)
            {
                UpdateMain rc = new UpdateMain(ReleaseCommand);
                this.Invoke(rc, new object[] { });
            }
            else
            {
                textBox1.ReadOnly = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (login_p
                && !((Keys)Enum.Parse(typeof(Keys), ((int)e.KeyChar).ToString()) == Keys.Enter))
            {
                if ((Keys)Enum.Parse(typeof(Keys), ((int)e.KeyChar).ToString()) == Keys.Back && spd.Length > 0)
                {
                    showpwd_i = (showpwd_i - 1) % showpwd.Length;
                    spd = spd.Remove(spd.Length - 1);
                    password = password.Remove(password.Length - 1);
                }
                else
                {
                    password += e.KeyChar;
                    e.KeyChar = showpwd[showpwd_i];
                    textBox1.Text = spd;
                    spd += showpwd[showpwd_i++];
                    showpwd_i = showpwd_i % showpwd.Length;
                    textBox1.SelectionStart = textBox1.Text.Length;
                }
            }
            if ((Keys)Enum.Parse(typeof(Keys), ((int)e.KeyChar).ToString()) == Keys.Enter)
            {
                if (login_a)
                {
                    userAccount = textBox1.Text;
                    UpdateLog("User:\t" + userAccount);
                    textBox1.Text = "";
                    textBox1.ReadOnly = true;
                    UpdateText("Enter Password");

                    login_a = false;
                    login_p = true;
                    ReleaseCommand();
                }
                else if (login_p)
                {
                    UpdateLog("[----PASSWORD----]");
                    textBox1.Text = "";
                    textBox1.ReadOnly = true;
                    login_p = false;
                    //user = login.GetUser(userAccount, password);
                    textBox1.Text = "";

                    Thread li = new Thread(new ParameterizedThreadStart(Login.GetUser));
                    li.IsBackground = true;
                    li.Start(new object[] { userAccount, password, this });

                    Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
                    loadingAnime.IsBackground = true;
                    loadingAnime.Start(li);

                    password = "";
                    showpwd_i = 0;
                    spd = "";
                }
                else
                {
                    textBox1.ReadOnly = true;
                    commandHistory.Add(textBox1.Text);
                    historyIndex = commandHistory.Count;
                    ExecuteCommand(textBox1.Text);
                }
            }
        }


        private void ExecuteCommand(String command)
        {
            String[] comopt = command.Trim().Split(' ');
            switch (comopt[0])
            {
                case "img":
                    UpdateLog(command);
                    ElectricityBill(comopt);
                    break;
                case "a2b":
                    UpdateLog(command);
                    A2B(comopt);
                    break;
                case "login":
                    UpdateLog(command);
                    RunLogin(comopt);
                    break;
                case "kiss":
                    UpdateLog(command);
                    Kiss(comopt);
                    break;
                case "mail":
                    UpdateLog(command);
                    Mail(comopt);
                    break;
                case "mktm":
                    UpdateLog(command);
                    MakeTaskModel(comopt);
                    break;
                case "bs":
                    UpdateLog(command);
                    BusStopFunction(comopt);
                    break;
                case "whoami":
                    UpdateLog(command);
                    WhoAmI();
                    ReleaseCommand();
                    break;
                case "":
                    UpdateLog("");
                    ReleaseCommand();
                    break;
                case "clear":
                    textBox1.Text = "";
                    textBox2.Clear();
                    ReleaseCommand();
                    break;
                case "exit":
                    Application.Exit();
                    break;
                default:
                    textBox1.Text = "";
                    UpdateLog("Command \"" + command + "\" not found");
                    ReleaseCommand();
                    break;
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            int width = this.Width - 1;
            int height = this.Height - 1;
            Pen greenPen = new Pen(Color.FromArgb(255, 0, 120, 0), 1);
            e.Graphics.DrawRectangle(greenPen, 0, 0, width, height);
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.curr_x = e.X;
                this.curr_y = e.Y;
                this.isWndMove = true;
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isWndMove)
                this.Location = new Point(this.Left + e.X - this.curr_x, this.Top + e.Y - this.curr_y);
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            this.isWndMove = false;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            int width = panel1.Width - 1;
            int height = panel1.Height - 1;
            Pen greenPen = new Pen(Color.FromArgb(255, 0, 120, 0), 1);
            e.Graphics.DrawRectangle(greenPen, 0, 0, width, height);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyValue)
            {
                case 38:
                    if (historyIndex != 0)
                    {
                        textBox1.Text = commandHistory[--historyIndex];
                    }
                    break;
                case 40:
                    if (historyIndex < commandHistory.Count - 1 && commandHistory.Count != 0)
                    {
                        textBox1.Text = commandHistory[++historyIndex];
                    }
                    else
                    {
                        textBox1.Text = "";
                        historyIndex = commandHistory.Count;
                    }
                    break;
            }
        }

    }
}
