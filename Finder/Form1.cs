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
            ip = new ImageProcesser((Bitmap)pictureBox1.Image, (Bitmap)pictureBox2.Image);
            UpdateText("Clear Edges");
            ip.EdgeFilter();

            textBox2.AppendText("Clear Edges\n");
            UpdateText("Execute Sobel Filter");
            ip.SobelFilter();

            textBox2.AppendText("Execute Sobel Filter\n");
            UpdateText("Find target Rectangles");
            ip.FindRectangle();

            textBox2.AppendText("Find target Rectangles\n");
            UpdateText("Cut image");
            ip.CutImage();

            textBox2.AppendText("Cut image\n");
            UpdateText("Recognize address");
            TesseractEngine ocr = new TesseractEngine(@"C:\Program Files (x86)\Tesseract-OCR\tessdata", "chi_tra+eng", EngineMode.Default);
            Pix img = PixConverter.ToPix(ip.targets[1]);
            Page addpage = ocr.Process(img);
            string[] address = addpage.GetText().Trim().Split(new char[] { '：', ':', '︰' });
            label11.Text = address[1].Trim().Replace(" ", String.Empty);
            label11.Update();
            ocr.Dispose();

            textBox2.AppendText("Recognize address\n");
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
            label3.Text = eid;
            label3.Update();
            label5.Text = date;
            label5.Update();
            label7.Text = price;
            label7.Update();

            textBox2.AppendText("Recognize eid, date, price\n");
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
            label9.Text = kwh + "度";
            label9.Update();
            ocre.Dispose();

            textBox2.AppendText("Recognize kWh\n");
            UpdateText("Finished");
            textBox2.AppendText("Finished\n");
        }

        private void UpdateText(String text)
        {
            label1.Text = "";
            for (int i = 0; i < text.Length; i++)
            {
                label1.Text += text[i];
                label1.Update();
                Thread.Sleep(40);
            }
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
