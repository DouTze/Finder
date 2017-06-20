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
using Tesseract;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace Finder
{
    class ImageProcesser
    {
        static UMat sobelEdges;
        public static List<RotatedRect> boxList;
        public static int count = 0;
        public static List<Rectangle> rects = new List<Rectangle>();
        static Bitmap ori_img, pre_img, show_img;
        public static Bitmap[] targets = new Bitmap[3];
        public delegate void ShowForm(MainForm parent, RecognizeForm rform);
        public static RecognizeForm rform;
        public static int savet = 0;
        private static Matrix<byte> _Cross, _Diamond, _Square, _XSharp;

        public ImageProcesser(Bitmap ori)
        {
            ori_img = ori;
        }

        public static void ShowRecognizeForm(MainForm parent, RecognizeForm rform)
        {
            if (parent.InvokeRequired)
            {
                ShowForm sf = new ShowForm(ShowRecognizeForm);
                parent.Invoke(sf, new object[] { parent, rform });
            }
            else
            {
                rform.Show(parent);
            }
        }

        public static void Execute(object obj)
        {
            object[] objs = (object[])obj;

            MainForm mform = (MainForm)objs[0];
            // eb -execute {cmd [args]}
            string[] cmd = (string[])objs[1];

            if (rform == null)
            {
                rform = new RecognizeForm();
                ShowRecognizeForm(mform, rform);
            }

            string errorMsg = "none";
            try
            {
                switch (cmd[0])
                {
                    case "showimg":
                        for (int i = 0; i < cmd.Length; i++)
                        {
                            if (cmd[i].Equals("-input"))
                            {
                                ori_img = (Bitmap)Bitmap.FromFile(cmd[i + 1]);
                                break;
                            }
                        }
                        if (ori_img == null)
                        {
                            errorMsg = "please enter the -input path";
                        }
                        else
                        {
                            show_img = ori_img;
                            rform.UpdateImage(show_img);
                        }
                        break;
                    case "back":
                        Bitmap tmp = show_img;
                        show_img = pre_img;
                        pre_img = tmp;
                        rform.UpdateImage(show_img);
                        break;
                    case "avggray":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            Image<Gray, byte> grayimg = new Image<Gray, byte>(show_img);
                            grayimg._ThresholdBinary(grayimg.GetAverage(), new Gray(255));
                            pre_img = show_img;
                            show_img = grayimg.Bitmap;
                            rform.UpdateImage(show_img);
                        }
                        break;
                    case "sobel":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            SobelFilter();
                        }
                        break;
                    case "median":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            Image<Bgr, byte> img = new Image<Bgr, byte>(show_img).SmoothMedian(3);
                            pre_img = show_img;
                            show_img = img.Bitmap;
                            rform.UpdateImage(show_img);
                        }
                        break;
                    case "erode":
                        Image<Gray, byte> eimg = new Image<Gray, byte>(show_img);
                        Size kSize = new System.Drawing.Size(Convert.ToInt32(cmd[1]), Convert.ToInt32(cmd[2]));
                        Point anchor = new Point(Convert.ToInt32(cmd[3]), Convert.ToInt32(cmd[4]));
                        ElementShape shape = ElementShape.Rectangle;
                        switch (cmd[5])
                        {
                            case "ellipse":
                                shape = ElementShape.Ellipse;
                                break;
                            case "rectangle":
                                shape = ElementShape.Rectangle;
                                break;
                            case "cross":
                                shape = ElementShape.Cross;
                                break;
                        }
                        Mat element = CvInvoke.GetStructuringElement(shape, kSize, anchor);
                        Mat dstImg = new Mat();
                        CvInvoke.Erode(eimg, dstImg, element, anchor, Convert.ToInt32(cmd[6]), BorderType.Default, new MCvScalar(0, 0, 0));
                        pre_img = show_img;
                        show_img = dstImg.Bitmap;
                        rform.UpdateImage(show_img);
                        break;
                    case "dilate":
                        Image<Gray, byte> dimg = new Image<Gray, byte>(show_img);
                        Size dSize = new System.Drawing.Size(Convert.ToInt32(cmd[1]), Convert.ToInt32(cmd[2]));
                        Point danchor = new Point(Convert.ToInt32(cmd[3]), Convert.ToInt32(cmd[4]));
                        ElementShape dshape = ElementShape.Rectangle;
                        switch (cmd[5])
                        {
                            case "ellipse":
                                dshape = ElementShape.Ellipse;
                                break;
                            case "rectangle":
                                dshape = ElementShape.Rectangle;
                                break;
                            case "cross":
                                dshape = ElementShape.Cross;
                                break;
                        }
                        Mat delement = CvInvoke.GetStructuringElement(dshape, dSize, danchor);
                        Mat ddstImg = new Mat();
                        CvInvoke.Dilate(dimg, ddstImg, delement, danchor, Convert.ToInt32(cmd[6]), BorderType.Default, new MCvScalar(0, 0, 0));
                        pre_img = show_img;
                        show_img = ddstImg.Bitmap;
                        rform.UpdateImage(show_img);
                        break;
                    case "clearedge":
                        EdgeFilter();
                        break;
                    case "canny":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            Image<Gray, byte> cannyimg = new Image<Gray, byte>(show_img).Canny(Convert.ToDouble(cmd[1]), Convert.ToDouble(cmd[2]));
                            pre_img = show_img;
                            show_img = cannyimg.Bitmap;
                            rform.UpdateImage(show_img);
                        }
                        break;
                    case "corners":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            GetCornersByHarris();
                            //GetCorners();
                        }
                        break;
                    case "findrec":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            FindRectangle(Convert.ToInt32(cmd[1]), Convert.ToInt32(cmd[2]));
                        }
                        break;
                    case "houghlines":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            Image<Gray, byte> img = new Image<Gray, byte>(show_img);
                            LineSegment2D[][] lines = img.HoughLines(
                                Convert.ToDouble(cmd[1]),   // canny low threshold
                                Convert.ToDouble(cmd[2]),   // canny high threshold
                                1,                          // rho
                                Math.PI / 180.0,            // theta
                                Convert.ToInt32(cmd[3]),    // threshold(cross point)
                                Convert.ToDouble(cmd[4]),   // min lenght for line
                                Convert.ToDouble(cmd[5])    // max allow gap between lines
                                );
                            foreach (LineSegment2D line in lines[0])
                            {
                                img.Draw(line, new Gray(0), 1);
                            }
                            pre_img = show_img;
                            show_img = img.Bitmap;
                            rform.UpdateImage(show_img);
                        }
                        break;
                    case "iterrec":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            DrawAllRectangles();
                        }
                        break;
                    case "refresh":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            pre_img = show_img;
                            show_img = ori_img;
                            rform.UpdateImage(show_img);
                        }
                        break;
                    case "saveimg":
                        if (ori_img == null)
                        {
                            errorMsg = "please run showimg command first";
                        }
                        else
                        {
                            show_img.Save("save_img_" + savet++ + ".png");
                        }
                        break;
                    case "exit":
                        rform.ExitForm();
                        rform.Dispose();
                        rform = null;
                        break;
                    case "help":
                        StreamReader input = new StreamReader(@"document.txt");
                        string doc;
                        bool tag = false;
                        while (!input.EndOfStream)
                        {
                            doc = input.ReadLine();
                            if (doc.Equals("<img>"))
                            {
                                tag = true;
                                continue;
                            }
                            else if (doc.Equals("</img>"))
                            {
                                break;
                            }
                            if (tag) mform.UpdateLog(doc);
                        }
                        input.Close();
                        input.Dispose();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                mform.UpdateLog(e.Message);
            }


            if (errorMsg.Equals("none"))
                mform.UpdateText("finished");
            else
                mform.UpdateLog(errorMsg);

        }

        public static void RecognizeBill(object obj)
        {
            object[] objs = (object[])obj;
            MainForm mform = (MainForm)objs[0];
            ori_img = (Bitmap)Bitmap.FromFile((string)objs[1]);

            RecognizeForm rform = new RecognizeForm();
            ShowRecognizeForm(mform, rform);

            mform.UpdateText("Clear Edges");
            EdgeFilter();
            rform.UpdateImage(show_img);

            mform.UpdateLog("Clear Edges");
            mform.UpdateText("Execute Sobel Filter");
            SobelFilter();
            rform.UpdateImage(show_img);

            mform.UpdateLog("Execute Sobel Filter");
            mform.UpdateText("Find target Rectangles");
            FindRectangle(10, 20);
            rform.UpdateImage(show_img);

            mform.UpdateLog("Find target Rectangles");
            mform.UpdateText("Cut image");
            CutImage();

            mform.UpdateLog("Cut image");
            mform.UpdateText("Recognize address");
            TesseractEngine ocr = new TesseractEngine(@"C:\Users\Allen Chou\Documents\Visual Studio 2013\Projects\Finder\packages\Tesseract.3.0.2.0\tessdata", "chi_tra+eng", EngineMode.Default);
            Pix img = PixConverter.ToPix(targets[1]);
            Page addpage = ocr.Process(img);
            string address = addpage.GetText().Trim().Replace(" ", String.Empty);
            ocr.Dispose();

            mform.UpdateLog("Recognize address");
            mform.UpdateText("Recognize eid, date, price");
            Pix idpimg = PixConverter.ToPix(targets[2]);
            TesseractEngine ocre = new TesseractEngine(@"C:\Users\Allen Chou\Documents\Visual Studio 2013\Projects\Finder\packages\Tesseract.3.0.2.0\tessdata", "eng", EngineMode.Default);
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

            mform.UpdateLog("Recognize eid, date, price\n");
            mform.UpdateText("Recognize kWh");
            Pix kwhimg = PixConverter.ToPix(targets[0]);
            ocre = new TesseractEngine(@"C:\Users\Allen Chou\Documents\Visual Studio 2013\Projects\Finder\packages\Tesseract.3.0.2.0\tessdata", "eng", EngineMode.Default);
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
            ocre.Dispose();

            mform.UpdateLog("Recognize kWh");
            mform.UpdateLog("Result [ " + eid + " , " + date + " , " + price + " , " + kwh + " , " + address + " ]");
            mform.UpdateText("Finished");
            mform.UpdateLog("Finished");
        }

        private static void FindRectangle(int min, int max)
        {
            Image<Rgb, Byte> img = new Image<Rgb, byte>(ori_img);
            Image<Gray, Byte> edgeImage = new Image<Gray, byte>(show_img);
            #region Find rectangles
            boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            double rate = img.Width * img.Height / 3864714.0;
            bool isIDPExist = false, isUseExist = false, isAddressExist = false;
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(edgeImage, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        double area = CvInvoke.ContourArea(contour, false);
                        if (area > 100000 * rate && area < 142000 * rate && !isIDPExist)
                        {
                            // eid, date, price
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[pts.Length];
                            for (int pi = 0; pi < pts.Length; pi++)
                                ptfs[pi] = new PointF(pts[pi].X, pts[pi].Y);
                            Rectangle rec = PointCollection.BoundingRectangle(ptfs);
                            img.Draw(rec, new Rgb(Color.DarkOrange), 2);
                            rects.Add(rec);
                            isIDPExist = true;
                        }
                        else if (area > 350000 * rate && area < 450000 * rate)
                        {
                            // use
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[4];
                            int minx = int.MaxValue, miny = int.MaxValue, maxx = 0, maxy = 0;
                            for (int pi = 0; pi < pts.Length; pi++)
                            {
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
                            ptfs[0] = new PointF(minx, miny);
                            ptfs[1] = new PointF(minx, (miny + maxy) / 2);
                            ptfs[2] = new PointF(maxx, miny);
                            ptfs[3] = new PointF(maxx, (miny + maxy) / 2);
                            Rectangle rec = PointCollection.BoundingRectangle(ptfs);
                            img.Draw(PointCollection.BoundingRectangle(ptfs), new Rgb(Color.DarkOrange), 2);
                            if (!isUseExist)
                            {
                                rects.Add(rec);
                                isUseExist = true;
                            }


                            PointF[] ptfks = new PointF[4];
                            ptfks[0] = new PointF(maxx * 1.5f, maxy);
                            ptfks[1] = new PointF(maxx * 1.5f, (float)(maxy + 32 * rate));
                            ptfks[2] = new PointF(minx, maxy);
                            ptfks[3] = new PointF(minx, (float)(maxy + 32 * rate));
                            Rectangle rec2 = PointCollection.BoundingRectangle(ptfks);
                            img.Draw(PointCollection.BoundingRectangle(ptfks), new Rgb(Color.DarkOrange), 2);
                            if (!isAddressExist)
                            {
                                rects.Add(rec2);
                                isAddressExist = true;
                            }
                        }
                        else if (area > min * rate && area < max * rate)
                        {
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[pts.Length];
                            for (int pi = 0; pi < pts.Length; pi++)
                                ptfs[pi] = new PointF(pts[pi].X, pts[pi].Y);
                            Rectangle rec = PointCollection.BoundingRectangle(ptfs);
                            img.Draw(rec, new Rgb(Color.DarkOrange), 2);
                            rects.Add(rec);
                        }
                    }
                }
            }
            #endregion
            #region draw rectangles

            foreach (RotatedRect box in boxList)
                img.Draw(box, new Rgb(Color.DarkOrange), 2);
            pre_img = show_img;
            show_img = img.Bitmap;
            rform.UpdateImage(show_img);
            //img.Save("rect_" + this.count + ".png");
            #endregion
        }

        private static void DrawAllRectangles()
        {

            Image<Gray, Byte> edgeImage = new Image<Gray, byte>(show_img);
            #region Find rectangles
            boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(edgeImage, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                for (int i = 0; i < contours.Size; i++)
                {
                    Image<Rgb, Byte> img = new Image<Rgb, byte>(ori_img);
                    double rate = img.Width * img.Height / 3864714.0;

                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        double area = CvInvoke.ContourArea(contour, false);

                        if (area > 40000)
                        {
                            Point[] pts = contour.ToArray();
                            PointF[] ptfs = new PointF[pts.Length];
                            for (int pi = 0; pi < pts.Length; pi++)
                                ptfs[pi] = new PointF(pts[pi].X, pts[pi].Y);

                            Rectangle rec = PointCollection.BoundingRectangle(ptfs);
                            img.Draw(rec, new Rgb(Color.DarkOrange), 2);

                            // Create string to draw.
                            String drawString = "Area : " + area;

                            // Draw string to image.
                            img.Draw(drawString, rec.Location, FontFace.HersheyTriplex, 1, new Rgb(Color.Black), 2);

                            #region draw rectangle
                            pre_img = show_img;
                            show_img = img.Bitmap;
                            rform.UpdateImage(show_img);
                            img.Save("rect_" + i + ".png");
                            #endregion
                        }
                    }
                }
            }
            #endregion
        }


        private static void EdgeFilter()
        {
            Image<Rgb, Byte> img = new Image<Rgb, byte>(ori_img);
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
            //img.Save("edge_" + count + ".png");
            //ori_img = img.Bitmap;
            pre_img = show_img;
            show_img = img.Bitmap;
            rform.UpdateImage(show_img);
        }

        private static void SobelFilter()
        {
            Image<Gray, Byte> img = new Image<Gray, byte>(show_img);
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

            sobelEdges = new UMat();
            CvInvoke.Threshold(sobelImage, sobelEdges, 70, 255, ThresholdType.Binary);
            //sobelEdges.Save("sobel_" + count + ".png");
            pre_img = show_img;
            show_img = sobelImage.Bitmap;
            rform.UpdateImage(show_img);
        }

        public static void GetCornersByHarris()
        {
            Image<Gray, Byte> image = new Image<Gray, Byte>(show_img);

            HarrisDetector harris = new HarrisDetector();
            harris.Detect(image);
            List<Point> featurePoints = new List<Point>();
            harris.GetCorners(featurePoints, 0.01);
            harris.DrawFeaturePoints(image, featurePoints);

            pre_img = show_img;
            show_img = image.Bitmap;
            rform.UpdateImage(show_img);
        }

        public static void GetCorners()
        {
            byte[,] cross = new byte[5, 5] { 
            {0,0,1,0,0},
            {0,0,1,0,0},
            {1,1,1,1,1},
            {0,0,1,0,0},
            {0,0,1,0,0}};
            _Cross = new Matrix<byte>(cross); 
            
            byte[,] diamond = new byte[5, 5] { 
            {0,0,1,0,0},
            {0,1,1,1,0},
            {1,1,1,1,1},
            {0,1,1,1,0},
            {0,0,1,0,0}};
            _Diamond = new Matrix<byte>(diamond);

            byte[,] square = new byte[5, 5] { 
            {0,0,0,0,0},
            {0,1,1,1,0},
            {0,1,1,1,0},
            {0,1,1,1,0},
            {0,0,0,0,0}};
            _Square = new Matrix<byte>(square);

            byte[,] xshape = new byte[5, 5] { 
            {1,0,0,0,1},
            {0,1,0,1,0},
            {0,0,1,0,0},
            {0,1,0,1,0},
            {1,0,0,0,1}};
            _XSharp = new Matrix<byte>(xshape);

            Image<Gray, byte> simg = new Image<Gray, byte>(show_img);

            Image<Gray, byte> dimg = new Image<Gray, byte>(show_img);
            CvInvoke.Dilate(dimg, dimg, _Cross, new Point(0, 0), 1, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(dimg, dimg, _Diamond, new Point(0, 0), 1, BorderType.Default, new MCvScalar(0, 0, 0));

            Image<Gray, byte> dimg2 = new Image<Gray, byte>(show_img);
            CvInvoke.Dilate(dimg2, dimg2, _XSharp, new Point(0, 0), 1, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(dimg2, dimg2, CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(2, 2)), new Point(0, 0), 1, BorderType.Default, new MCvScalar(0, 0, 0));

            dimg = dimg.AbsDiff(dimg2);
            dimg._ThresholdBinary(new Gray(10), new Gray(255));

            for (int h = 0; h < dimg.Height; h++)
            {
                for (int w = 0; w < dimg.Width; w++)
                {
                    if (dimg[h, w].Intensity != 0)
                    {
                        CircleF circle = new CircleF(new PointF(w, h), 5);
                        simg.Draw(circle, new Gray(0), 1);
                    }
                }
            }

            pre_img = show_img;
            show_img = simg.Bitmap;
            rform.UpdateImage(show_img);
        }

        private static void CutImage()
        {
            #region 影像裁切
            Image img = show_img as Image;
            //設定裁切範圍
            foreach (Rectangle rect in rects)
            {
                //建立新的影像
                Image cropImage = new Bitmap(rect.Width, rect.Height) as Image;
                //準備繪製新的影像
                Graphics graphics2 = Graphics.FromImage(cropImage);
                //開始繪製裁切影像
                graphics2.DrawImage(img, 0, 0, rect, GraphicsUnit.Pixel);
                graphics2.Dispose();
                //儲存新的影像
                targets[count % 3] = (Bitmap)cropImage;
                count++;
            }
            #endregion
        }
    }

    class HarrisDetector
    {
        //32-bit float image of corner strength
        private Image<Gray, float> _CornerStrength;
        //32-bit float image of thresholded corners
        private Image<Gray, float> _CornerTh;
        //size of neighborhood for derivatives smoothing
        int _Neighborhood;
        //aperture for gradient computation
        int _Aperture;
        //Harris parameter
        double _K;
        //maximum strength for threshold computation
        double _MaxStrength;
        //calculated threshold (internal)
        double _Threshold;

        public HarrisDetector()
        {
            this._Neighborhood = 3;
            this._Aperture = 3;
            this._K = 0.01;
            this._MaxStrength = 0.0;
            this._Threshold = 0.01;
        }

        /// <summary>
        /// Compute Harris corners
        /// </summary>
        /// <param name="image">source image</param>
        public void Detect(Image<Gray, Byte> image)
        {
            this._CornerStrength = new Image<Gray, float>(image.Size);
            //Harris computation
            CvInvoke.CornerHarris(
                image,                //source image
                this._CornerStrength, //result image
                this._Neighborhood,   //neighborhood size
                this._Aperture,       //aperture size
                this._K);             //Harris parameter

            //internal threshold computation
            double[] maxStrength;
            double[] minStrength; //not used
            Point[] minPoints;    //not used
            Point[] maxPoints;    //not used
            this._CornerStrength.MinMax(out minStrength, out maxStrength, out minPoints, out maxPoints);
            this._MaxStrength = maxStrength[0];
        }

        /// <summary>
        /// Get the corner map from computed Harris values
        /// </summary>
        /// <param name="qualityLevel">Harris values</param>
        /// <returns>corner map</returns>
        public Image<Gray, Byte> GetCornerMap(double qualityLevel)
        {
            Image<Gray, Byte> cornerMap;
            //thresholding the corner strength
            this._Threshold = qualityLevel * this._MaxStrength;
            this._CornerTh = this._CornerStrength.ThresholdBinary(
                new Gray(this._Threshold),
                new Gray(255));
            //convert to 8-bit image
            cornerMap = this._CornerTh.Convert<Gray, Byte>();
            return cornerMap;
        }

        /// <summary>
        /// Get the feature points from the computed Harris value
        /// </summary>
        /// <param name="cornerPoints">feature points</param>
        /// <param name="qualityLevel">Harris value</param>
        public void GetCorners(List<Point> cornerPoints, double qualityLevel)
        {
            Image<Gray, Byte> cornerMap = GetCornerMap(qualityLevel);
            GetCorners(cornerPoints, cornerMap);
        }

        //Get the feature points from the computed corner map
        void GetCorners(List<Point> cornerPoints, Image<Gray, Byte> cornerMap)
        {
            //Interate over the pixels to obtain all features
            for (int h = 0; h < cornerMap.Height; h++)
            {
                for (int w = 0; w < cornerMap.Width; w++)
                {
                    //if it is a feature point
                    if (cornerMap[h, w].Intensity > 0)
                    {
                        cornerPoints.Add(new Point(w, h));
                    }
                }
            }
        }

        /// <summary>
        /// Draw circles at feature point locations on an image
        /// </summary>
        /// <param name="image">image</param>
        /// <param name="points">feature points</param>
        public void DrawFeaturePoints(Image<Gray, Byte> image, List<Point> points)
        {
            //for all corner
            foreach (Point point in points)
            {
                //draw a circle at each corner location
                CircleF circle = new CircleF(new PointF(point.X, point.Y), 3);
                image.Draw(circle, new Gray(255), 1);
            }
        }

    }
}
