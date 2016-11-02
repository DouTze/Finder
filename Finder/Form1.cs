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

namespace Finder
{
    public partial class Form1 : Form
    {
        public delegate void GrayScale(Image<Rgb, Byte> input);
        public delegate void UpdateImage();
        UMat uimage, cannyEdges;
        public List<RotatedRect> boxList;
        int count = 0;

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

        private void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = 6;
            progressBar1.Value = 0;
            
            label1.Text = "Loading image...";
            ImageProcesser ip = new ImageProcesser((Bitmap)(pictureBox1.Image));
            progressBar1.Increment(1);

            label1.Text = "Convert the image to grayscale and filter out the noise";
            //Convert the image to grayscale and filter out the noise
            Thread c2g = new Thread(ip.Convert2Grayscale);
            c2g.Start();
            c2g.Join();
            progressBar1.Increment(1);

            label1.Text = "use image pyr to remove noise";
            //use image pyr to remove noise
            Thread rmnoise = new Thread(ip.RemoveNoiseByPyr);
            rmnoise.Start();
            rmnoise.Join();
            progressBar1.Increment(1);

            label1.Text = "Canny and edge detection";
            Thread canny = new Thread(ip.CannyEdgeDetection);
            canny.Start();
            canny.Join();
            progressBar1.Increment(1);
            pictureBox2.Image = ip.cannyEdges.Bitmap;

            label1.Text = "Find rectangles";
            Thread findRect = new Thread(ip.FindRectangle);
            findRect.Start();
            findRect.Join();
            progressBar1.Increment(1);

            label1.Text = "Draw rectangles";
            #region draw rectangles
            List<RotatedRect> boxList = ip.boxList;
            Image<Rgb, Byte> img = ip.img;
            foreach (RotatedRect box in boxList)
                img.Draw(box, new Rgb(Color.DarkOrange), 2);
            pictureBox1.Image = img.Bitmap;
            #endregion
            progressBar1.Increment(1);
            label1.Text = "Finished!";
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            //pictureBox1.Height = this.Height;
            //pictureBox1.Width = this.Width;
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = 3;
            progressBar1.Value = 0;

            label1.Text = "Loading image...";
            ImageProcesser ip = new ImageProcesser((Bitmap)(pictureBox1.Image));
            ip.cannyThreshold = Convert.ToInt32(textBox2.Text);
            ip.cannyThresholdLinking = Convert.ToInt32(textBox3.Text);
            progressBar1.Increment(1);

            label1.Text = "Convert the image to grayscale and filter out the noise";
            //Convert the image to grayscale and filter out the noise
            Thread c2g = new Thread(ip.Convert2Grayscale);
            c2g.Start();
            c2g.Join();
            pictureBox2.Image = ip.uimage.Bitmap;
            progressBar1.Increment(1);

            /*
            label1.Text = "use image pyr to remove noise";
            //use image pyr to remove noise
            Thread rmnoise = new Thread(ip.RemoveNoiseByPyr);
            rmnoise.Start();
            rmnoise.Join();
            progressBar1.Increment(1);
            */

            label1.Text = "Canny and edge detection";
            Thread canny = new Thread(ip.CannyEdgeDetection);
            canny.Start();
            canny.Join();
            progressBar1.Increment(1);
            //pictureBox2.Image = ip.cannyEdges.Bitmap;
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            label1.Text = "Convert the image to grayscale and filter out the noise";
            //Convert the image to grayscale and filter out the noise
            Thread c2g = new Thread(DrawGrayScale);
            c2g.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            label1.Text = "use image pyr to remove noise";
            //Convert the image to grayscale and filter out the noise
            Thread pyr = new Thread(DrawPyr);
            pyr.Start();
        }

        private void DrawGrayScale()
        {
            GrayScale gs = new GrayScale(Convert2Gray);
            Image img = pictureBox1.Image;
            this.BeginInvoke(gs, new Object[] { new Image<Rgb, Byte>(new Bitmap(img, Convert.ToInt32(img.Width /4),Convert.ToInt32(img.Height / 4))) });
        }

        private void Convert2Gray(Image<Rgb, Byte> input)
        {
            uimage = new UMat();
            CvInvoke.CvtColor(input, uimage, ColorConversion.Rgb2Gray);
            pictureBox2.Image = uimage.Bitmap;
        }

        private void DrawPyr()
        {
            UpdateImage pyr = new UpdateImage(RemoveNoiseByPyr);
            this.BeginInvoke(pyr, new Object[] { });
        }

        private void RemoveNoiseByPyr()
        {
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uimage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimage);
            pictureBox2.Image = uimage.Bitmap;
        }

        private void DrawCanny()
        {
            UpdateImage canny = new UpdateImage(CannyEdgeDetection);
            this.BeginInvoke(canny, new Object[] { });
        }

        public void CannyEdgeDetection()
        {
            #region Canny and edge detection
            cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, Convert.ToDouble(textBox2.Text), Convert.ToDouble(textBox3.Text));
            
            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 180.0, //Angle resolution measured in radians.
               20, //threshold
               180, //min Line width
               10); //gap between lines
            
            #endregion
            pictureBox2.Image = cannyEdges.Bitmap;
            //draw lines on image
            Image<Rgb, Byte> img = new Image<Rgb, byte>((Bitmap)pictureBox2.Image);
            foreach (var line in lines)
            {
                img.Draw(line, new Rgb(Color.DarkOrange), 1);
            }
            pictureBox2.Image = img.Bitmap;
            cannyEdges.Bitmap.Save("flo_"+count+".png");
            count++;

        }

        private void button6_Click(object sender, EventArgs e)
        {
            label1.Text = "Canny and edge detection";
            Thread canny = new Thread(DrawCanny);
            canny.Start();
        }

        private void DrawRect()
        {
            UpdateImage rect = new UpdateImage(FindRectangle);
            this.BeginInvoke(rect, new Object[] { });
        }

        public void FindRectangle()
        {
            #region Find rectangles
            boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        if (CvInvoke.ContourArea(approxContour, false) > 250) //only consider contours with area greater than 250
                        {
                            if (approxContour.Size == 4) //The contour has 4 vertices.
                            {
                                #region determine if all the angles in the contour are within [80, 100] degree
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(
                                       edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                    if (angle < 80 || angle > 100)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                #endregion

                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                            }
                        }
                    }
                }
            }
            #endregion
            #region draw rectangles
            Image<Rgb, Byte> img = new Image<Rgb,byte>((Bitmap)pictureBox1.Image);
            foreach (RotatedRect box in boxList)
                img.Draw(box, new Rgb(Color.DarkOrange), 2);
            pictureBox2.Image = img.Bitmap;
            #endregion
        }

        private void button7_Click(object sender, EventArgs e)
        {
            label1.Text = "Draw rectangles";
            Thread rect = new Thread(DrawRect);
            rect.Start();
        }
    }
}
