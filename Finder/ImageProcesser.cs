using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;

namespace Finder
{
    class ImageProcesser
    {
        Bitmap bm;
        public Image<Rgb, Byte> img;
        public UMat uimage,cannyEdges;
        public List<RotatedRect> boxList;
        public double cannyThreshold = 180.0;//180
        public double cannyThresholdLinking = 120.0;//120

        public ImageProcesser(Bitmap bitm)
        {
            bm = bitm;
            uimage = new UMat();
            img = new Image<Rgb, Byte>(bitm);
        }

        public static void Convert2Graycale(Image<Rgb, Byte> image, UMat output)
        {
            CvInvoke.CvtColor(image, output, ColorConversion.Rgb2Gray);
        }

        public void Convert2Grayscale()
        {
            CvInvoke.CvtColor(img, uimage, ColorConversion.Rgb2Gray);
        }

        public void RemoveNoiseByPyr()
        {
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uimage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimage);
        }

        public void CannyEdgeDetection()
        {
            #region Canny and edge detection
            cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               20, //threshold
               30, //min Line width
               20); //gap between lines
            #endregion
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
        }
    }
}
