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
        UMat uimage, cannyEdges, houghLines;
        public List<RotatedRect> boxList;
        LineSegment2D[] lines;
        int count = 0;
        int fstIndex;

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
            label1.Text = "Draw rectangles";
            Thread edge = new Thread(DrawEdge);
            edge.Start();
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

            label1.Text = "Draw edges";
            Thread edge = new Thread(DrawEdge);
            edge.Start();
            progressBar1.Increment(1);

            label1.Text = "Draw sobel";
            Thread sobel = new Thread(DrawSobel);
            sobel.Start();
            progressBar1.Increment(1);

            label1.Text = "Draw rectangles";
            Thread rect = new Thread(DrawRect);
            rect.Start();
            progressBar1.Increment(1);

            label1.Text = "Finished";
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
            this.BeginInvoke(gs, new Object[] { new Image<Rgb, Byte>(new Bitmap(img, Convert.ToInt32(img.Width / 4), Convert.ToInt32(img.Height / 4))) });
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
            int x = Convert.ToInt32(textBox4.Text);
            int y = Convert.ToInt32(textBox5.Text);
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uimage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimage);
            CvInvoke.AdaptiveThreshold(uimage, uimage, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 25, 5);
            Mat structure = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(x, y), new Point(-1, 1));
            CvInvoke.MorphologyEx(uimage, uimage, MorphOp.Close, structure, new Point(-1, 1), 1, BorderType.Default, new MCvScalar());
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

            houghLines = new UMat();
            CvInvoke.HoughLines(
               uimage,
               houghLines,
               3, //Distance resolution in pixel-related units
               Math.PI / 90.0, //Angle resolution measured in radians.
               5, //threshold
               1, //min Line width
               5); //gap between lines
            lines = CvInvoke.HoughLinesP(
               cannyEdges,
               3, //Distance resolution in pixel-related units
               Math.PI / 90.0, //Angle resolution measured in radians.
               5, //threshold
               1, //min Line width
               5); //gap between lines

            #endregion
            pictureBox2.Image = cannyEdges.Bitmap;
            //draw lines on image
            Image<Rgb, Byte> img = new Image<Rgb, byte>((Bitmap)pictureBox1.Image);
            fstIndex = 0;
            double highest = img.Height;
            for (int i = 0; i < lines.Length; i++)
            {
                LineSegment2D line = lines[i];
                Point a = line.P1;
                Point b = line.P2;

                // recog hline
                double sin = Math.Abs(a.X - b.X) / line.Length;
                if (sin < 0.5)
                {
                    double avgh = (a.Y + b.Y) / 2;
                    if (avgh < highest)
                    {
                        highest = avgh;
                        fstIndex = i;
                    }
                }


                LineSegment2D nline = new LineSegment2D();
                nline.P1 = new Point(a.X * 4, a.Y * 4);
                nline.P2 = new Point(b.X * 4, b.Y * 4);
                //img.Draw(nline, new Rgb(Color.DarkOrange), 2);
            }
            pictureBox1.Image = img.Bitmap;
            cannyEdges.Bitmap.Save("flo_" + count + ".png");
            img.Bitmap.Save("lines_" + count + ".png");
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
            Image<Rgb, Byte> img = new Image<Rgb, byte>((Bitmap)pictureBox1.Image);
            #region Find rectangles
            double min = Convert.ToDouble(textBox2.Text);
            double max = Convert.ToDouble(textBox3.Text);
            boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            double rate = img.Width * img.Height / 3864714.0;
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
                        double area = CvInvoke.ContourArea(contour, false);
                        if (area > 100000 * rate && area < 142000 * rate)
                        {
                            // eid, date, price
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[pts.Length];
                            for (int pi = 0; pi < pts.Length; pi++)
                                ptfs[pi] = new PointF(pts[pi].X, pts[pi].Y);
                            img.Draw(PointCollection.BoundingRectangle(ptfs), new Rgb(Color.DarkOrange), 2);
                        }
                        else if (area > 350000 * rate && area < 450000 * rate)
                        {
                            // use
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[pts.Length];
                            int minx=int.MaxValue, miny=int.MaxValue, maxx=0, maxy=0;
                            for (int pi = 0; pi < pts.Length; pi++)
                            {
                                ptfs[pi] = new PointF(pts[pi].X, pts[pi].Y);
                                if (pts[pi].X < minx)
                                {
                                    minx = pts[pi].X;
                                }
                                if (pts[pi].Y < miny)
                                {
                                    miny = pts[pi].Y;
                                }
                                if (pts[pi].X > maxx)
                                {
                                    maxx = pts[pi].X;
                                }
                                if (pts[pi].Y > maxy)
                                {
                                    maxy = pts[pi].Y;
                                }
                            }

                            img.Draw(PointCollection.BoundingRectangle(ptfs), new Rgb(Color.DarkOrange), 2);

                            PointF[] ptfks = new PointF[4];
                            ptfks[0] = new PointF(maxx * 1.5f, maxy);
                            ptfks[1] = new PointF(maxx * 1.5f, (float)(maxy + 32 * rate));
                            ptfks[2] = new PointF(minx, maxy);
                            ptfks[3] = new PointF(minx, (float)(maxy + 32 * rate));
                            img.Draw(PointCollection.BoundingRectangle(ptfks), new Rgb(Color.DarkOrange), 2);
                        }
                        else if (area > min * rate && area < max * rate)
                        {
                            // use
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[pts.Length];
                            for (int pi = 0; pi < pts.Length; pi++)
                                ptfs[pi] = new PointF(pts[pi].X, pts[pi].Y);
                            img.Draw(PointCollection.BoundingRectangle(ptfs), new Rgb(Color.DarkRed), 2);
                        }
                    }
                }
            }
            #endregion
            #region draw rectangles

            //foreach (RotatedRect box in boxList)
            //img.Draw(box, new Rgb(Color.DarkOrange), 2);
            pictureBox2.Image = img.Bitmap;
            img.Save("rect_" + this.count + ".png");
            #endregion
        }

        private void button7_Click(object sender, EventArgs e)
        {
            label1.Text = "Draw rectangles";
            Thread rect = new Thread(DrawRect);
            rect.Start();
        }

        private void DrawEdge()
        {
            UpdateImage edge = new UpdateImage(EdgeFilter);
            this.BeginInvoke(edge, new Object[] { });
        }

        private void EdgeFilter()
        {
            Image<Rgb, Byte> img = new Image<Rgb, byte>((Bitmap)pictureBox1.Image);
            MIplImage MIpImg = (MIplImage)System.Runtime.InteropServices.Marshal.PtrToStructure(img.Ptr, typeof(MIplImage));
            unsafe
            {
                int height = img.Height;
                int width = img.Width;
                int point;
                byte* npixel = (byte*)MIpImg.ImageData;

                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        point = w * 3;
                        double avg = (npixel[point] + npixel[point + 1] + npixel[point + 2]) / 3;
                        if (npixel[point] > avg - 15 && npixel[point] < avg + 15
                            && npixel[point + 1] > avg - 15 && npixel[point + 1] < avg + 15
                            && npixel[point + 2] > avg - 15 && npixel[point + 2] < avg + 15)
                        {
                            if (avg < 200)
                            {
                                npixel[point] = 0;
                                npixel[point + 1] = 0;
                                npixel[point + 2] = 0;
                            }
                            else
                            {
                                npixel[point] = 255;
                                npixel[point + 1] = 255;
                                npixel[point + 2] = 255;
                            }
                        }
                    }
                    npixel = npixel + MIpImg.WidthStep;
                }

            }
            img.Save("edge_" + count + ".png");
            pictureBox1.Image = img.Bitmap;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            label1.Text = "Draw sobel";
            Thread sobel = new Thread(DrawSobel);
            sobel.Start();
        }

        private void DrawSobel()
        {
            UpdateImage sobel = new UpdateImage(SobelFilter);
            this.BeginInvoke(sobel, new Object[] { });
        }

        private void SobelFilter()
        {
            Image<Gray, Byte> img = new Image<Gray, byte>((Bitmap)pictureBox1.Image);
            Image<Gray, float> shimg = img.Sobel(1, 0, 3);
            Image<Gray, float> svimg = img.Sobel(0, 1, 3);

            //Convert negative values to positive valus
            shimg = shimg.AbsDiff(new Gray(0));
            svimg = svimg.AbsDiff(new Gray(0));

            Image<Gray, float> sobel = shimg + svimg;
            //Find sobel min or max value
            double[] mins, maxs;
            //Find sobel min or max value position
            Point[] minLoc, maxLoc;
            sobel.MinMax(out mins, out maxs, out minLoc, out maxLoc);
            //Conversion to 8-bit image
            Image<Gray, Byte> sobelImage = sobel.ConvertScale<byte>(255 / maxs[0], 0);

            cannyEdges = new UMat();
            CvInvoke.Threshold(sobelImage, cannyEdges, 70, 255, ThresholdType.Binary);
            cannyEdges.Save("sobel_" + count + ".png");
            //pictureBox1.Image = sobelImage.Bitmap;
        }

    }
}
