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


namespace Finder
{
    public partial class RecognizeForm : Form
    {
        delegate void UpdateMain();
        delegate void UpdateMainText(String text);
        delegate void UpdateMainLabel(Label label, String content);
        string[] loading = new string[] { "一", "\\", "|", "/" };

        public RecognizeForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            ExecuteCommand(textBox1.Text);
        }


        private void FindTarget()
        {
            textBox1.Text = "";

            Thread recogBill = new Thread(new ParameterizedThreadStart(ImageProcesser.RecognizeBill));
            recogBill.IsBackground = true;
            recogBill.Start(new object[] {this,  pictureBox1.Image, pictureBox2.Image});

            Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
            loadingAnime.IsBackground = true;
            loadingAnime.Start(recogBill);
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
            if ((Keys)Enum.Parse(typeof(Keys), ((int)e.KeyChar).ToString()) == Keys.Enter)
            {
                textBox1.ReadOnly = true;
                ExecuteCommand(textBox1.Text);
            }
        }

        private void ExecuteCommand(String command)
        {
            String[] comopt = command.Trim().Split(' ');
            switch (comopt[0])
            {
                case "findtarget":
                    UpdateLog(command);
                    FindTarget();
                    break;
                case "a2b":
                    UpdateLog(command);
                    A2B(comopt);
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
                    UpdateLog("Command \""+command+"\" not found");
                    ReleaseCommand();
                    break;
            }
        }

    }
}
