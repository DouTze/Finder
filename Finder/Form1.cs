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

namespace Finder
{
    public partial class Form1 : Form
    {
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
                Image<Rgb, Byte> img = new Image<Rgb, Byte>((Bitmap)(pictureBox1.Image));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Image<Rgb, Byte> img = new Image<Rgb, Byte>((Bitmap)(pictureBox1.Image));

            //Convert the image to grayscale and filter out the noise
            UMat uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);

            //use image pyr to remove noise
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uimage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimage);

            #region Canny and edge detection
            double cannyThreshold = 180.0;
            double cannyThresholdLinking = 120.0;
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               20, //threshold
               30, //min Line width
               2000); //gap between lines

            #endregion

            #region Find triangles and rectangles
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle
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
                        if (CvInvoke.ContourArea(approxContour, false) > 2000) //only consider contours with area greater than 250
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

            #region draw triangles and rectangles

            foreach (RotatedRect box in boxList)
                img.Draw(box, new Rgb(Color.DarkOrange), 2);
            pictureBox1.Image = img.Bitmap;
            #endregion
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            //pictureBox1.Height = this.Height;
            //pictureBox1.Width = this.Width;
        }
    }
}
