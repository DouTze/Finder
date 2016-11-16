using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using System.Threading;
using Tesseract;
using System.Text.RegularExpressions;

namespace Finder
{
    public partial class Form1 : Form
    {
        ImageProcesser ip;
        delegate void UpdateMainText(String text);
        delegate void UpdateMainLabel(Label label, String content);
        string[] loading = new string[] { "一", "\\", "|", "/" };

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog filename = new OpenFileDialog();
            if (filename.ShowDialog() == DialogResult.OK)
            {
                string name = filename.FileName;
                textBox1.Text = name;
                Bitmap bm = new Bitmap(name);
                pictureBox1.Image = bm;
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            Thread recogBill = new Thread(RecognizeBill);
            recogBill.IsBackground = true;
            recogBill.Start();

            Thread loadingAnime = new Thread(new ParameterizedThreadStart(UpdateLoadingIcon));
            loadingAnime.IsBackground = true;
            loadingAnime.Start(recogBill);
        }

        private void UpdateText(String text)
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
                    Thread.Sleep(40);
                }
            }
        }

        private void UpdateLog(String log)
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

        private void UpdateLabel(Label label, String content)
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

        private void UpdateLoadingIcon(object bgThread)
        {
            int b = 0;
            while (((Thread)bgThread).ThreadState != ThreadState.Stopped)
            {
                UpdateLabel(label12, loading[b]);
                b = (b + 1) % loading.Length;
                Thread.Sleep(125);
            }
        }

        private void RecognizeBill()
        {
            ip = new ImageProcesser((Bitmap)pictureBox1.Image, (Bitmap)pictureBox2.Image);
            UpdateText("Clear Edges");
            ip.EdgeFilter();

            UpdateLog("Clear Edges\n");
            UpdateText("Execute Sobel Filter");
            ip.SobelFilter();

            UpdateLog("Execute Sobel Filter\n");
            UpdateText("Find target Rectangles");
            ip.FindRectangle();

            UpdateLog("Find target Rectangles\n");
            UpdateText("Cut image");
            ip.CutImage();

            UpdateLog("Cut image\n");
            UpdateText("Recognize address");
            TesseractEngine ocr = new TesseractEngine(@"C:\Program Files (x86)\Tesseract-OCR\tessdata", "chi_tra+eng", EngineMode.Default);
            Pix img = PixConverter.ToPix(ip.targets[1]);
            Page addpage = ocr.Process(img);
            string[] address = addpage.GetText().Trim().Split(new char[] { '：', ':', '︰' });
            UpdateLabel(label11, address[1].Trim().Replace(" ", String.Empty));
            ocr.Dispose();

            UpdateLog("Recognize address\n");
            UpdateText("Recognize eid, date, price");
            Pix idpimg = PixConverter.ToPix(ip.targets[2]);
            TesseractEngine ocre = new TesseractEngine(@"C:\Program Files (x86)\Tesseract-OCR\tessdata", "eng", EngineMode.Default);
            Page idppage = ocre.Process(idpimg);
            string[] idpdata = idppage.GetText().Trim().Split(' ');
            int tar = 0;
            string eid = "";
            for (int i = 0; i < idpdata.Length; i++)
            {
                Regex rex = new Regex("\\d{2}-\\d{2}-\\d{4}-\\d{2}-\\d{1}");
                if (rex.IsMatch(idpdata[i]))
                {
                    tar = i;
                    Match match = rex.Match(idpdata[i]);
                    eid = match.Value;
                    break;
                }
            }
            ocre.Dispose();

            string date = idpdata[tar + 1];
            string price = idpdata[tar + 2].Replace("*", String.Empty);
            UpdateLabel(label3, eid);
            UpdateLabel(label5, date);
            UpdateLabel(label7, price);

            UpdateLog("Recognize eid, date, price\n");
            UpdateText("Recognize kWh");
            Pix kwhimg = PixConverter.ToPix(ip.targets[0]);
            ocre = new TesseractEngine(@"C:\Program Files (x86)\Tesseract-OCR\tessdata", "eng", EngineMode.Default);
            Page kwhpage = ocre.Process(kwhimg);
            string[] kwhdata = kwhpage.GetText().Trim().Split(' ');
            string kwh = "";
            for (int i = 0; i < kwhdata.Length; i++)
            {
                Regex rex = new Regex("\\*\\d{1,}");
                if (rex.IsMatch(kwhdata[i]))
                {
                    Match match = rex.Match(kwhdata[i]);
                    kwh = match.Value.Replace("*", String.Empty);
                    break;
                }
            }
            UpdateLabel(label9, kwh + "度");
            ocre.Dispose();

            UpdateLog("Recognize kWh\n");
            UpdateText("Finished");
            UpdateLog("Finished\n");
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

    }
}
