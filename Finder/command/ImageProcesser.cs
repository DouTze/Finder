﻿using System;
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

namespace Finder
{
    class ImageProcesser
    {
        static UMat sobelEdges;
        public static List<RotatedRect> boxList;
        public static int count = 0;
        public static List<Rectangle> rects = new List<Rectangle>();
        static Bitmap ori_img, pro_img, show_img;
        public static Bitmap[] targets = new Bitmap[3];
        public delegate void ShowForm(MainForm parent, RecognizeForm rform);
        public static RecognizeForm rform;

        public ImageProcesser(Bitmap ori, Bitmap pro)
        {
            ori_img = ori;
            pro_img = pro;
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
            switch (cmd[0])
            {
                case "showimg":
                    for (int i = 0; i < cmd.Length; i++)
                    {
                        if (cmd[i].Equals("-input"))
                        {
                            ori_img = (Bitmap)Bitmap.FromFile(cmd[i + 1]);
                            pro_img = (Bitmap)Bitmap.FromFile(cmd[i + 1]);
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
                case "canny":
                    if (ori_img == null)
                    {
                        errorMsg = "please run showimg command first";
                    }
                    else
                    {
                        Image<Gray, byte> cannyimg = new Image<Gray, byte>(show_img).Canny(Convert.ToDouble(cmd[1]), Convert.ToDouble(cmd[2]));
                        show_img = cannyimg.Bitmap;
                        rform.UpdateImage(show_img);
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
                            Math.PI/180.0,              // theta
                            Convert.ToInt32(cmd[3]),    // threshold(cross point)
                            Convert.ToDouble(cmd[4]),   // min lenght for line
                            Convert.ToDouble(cmd[5])    // max allow gap between lines
                            );
                        foreach (LineSegment2D line in lines[0])
                        {
                            img.Draw(line, new Gray(0), 1);
                        }
                        show_img = img.Bitmap;
                        rform.UpdateImage(show_img);
                    }
                    break;
                case "refresh":
                    if (ori_img == null)
                    {
                        errorMsg = "please run showimg command first";
                    }
                    else
                    {
                        show_img = ori_img;
                        rform.UpdateImage(show_img);
                    }
                    break;
                case "exit":
                    rform.ExitForm();
                    rform.Dispose();
                    rform = null;
                    break;
                default:
                    break;
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
            pro_img = (Bitmap)Bitmap.FromFile((string)objs[1]);

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
            FindRectangle();
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

        private static void FindRectangle()
        {
            Image<Rgb, Byte> img = new Image<Rgb, byte>(ori_img);
            #region Find rectangles
            boxList = new List<RotatedRect>(); //a box is a rotated rectangle
            double rate = img.Width * img.Height / 3864714.0;
            bool isIDPExist = false, isUseExist = false, isAddressExist = false;
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(sobelEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
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
                    }
                }
            }
            #endregion
            #region draw rectangles

            foreach (RotatedRect box in boxList)
                img.Draw(box, new Rgb(Color.DarkOrange), 2);
            show_img = img.Bitmap;
            //img.Save("rect_" + this.count + ".png");
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
            show_img = img.Bitmap;
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
            show_img = sobelImage.Bitmap;
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
}
