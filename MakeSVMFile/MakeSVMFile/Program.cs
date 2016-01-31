﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenCvSharp;

namespace MakeSVMFile
{
    class Program
    {
        static void Main(string[] args)
        {
            MakeSvmFile svm_make = new MakeSvmFile();
            svm_make.Exec();
        }
    }

    class MakeSvmFile
    {
        public void Exec()
        {
            //学習するファイルを読み込む
            ReadFileList();

            //特徴点を出す
            DetectFacePoint();

        }

        //画像ファイルから特徴点をだす
        private void DetectFacePoint()
        {
            string eye_cascade_xml = @"C:\opencv2.4.8\sources\data\haarcascades\haarcascade_eye.xml";
            string nose_cascade_xml = @"C:\opencv2.4.8\sources\data\haarcascades\haarcascade_mcs_nose.xml";
            string mouth_cascade_xml = @"C:\opencv2.4.8\sources\data\haarcascades\haarcascade_mcs_mouth.xml";

            CvMemStorage strage = new CvMemStorage(0);   // メモリを確保
            CvHaarClassifierCascade eye_cascade = CvHaarClassifierCascade.FromFile(eye_cascade_xml);
            CvHaarClassifierCascade nose_cascade = CvHaarClassifierCascade.FromFile(nose_cascade_xml);
            CvHaarClassifierCascade mouth_cascade = CvHaarClassifierCascade.FromFile(mouth_cascade_xml);


            int read_count = 0;
            while (read_count < this.KubotaList.Count())
            {
                string input_file_path = this.KubotaList[read_count];

                using (IplImage img = new IplImage(input_file_path))
                {
                    //グレースケールに変換
                    IplImage gray_image = Cv.CreateImage(new CvSize(img.Width,img.Height),BitDepth.U8,1);
                    Cv.CvtColor(img, gray_image, ColorConversion.BgrToGray);

                    //発見した矩形
                    //TODO まゆの位置も有る方がいいかも
                    this.EyeResult = Cv.HaarDetectObjects(gray_image, eye_cascade, strage);
                    this.NoseResult = Cv.HaarDetectObjects(gray_image, nose_cascade, strage);
                    this.MouthResult = Cv.HaarDetectObjects(gray_image, mouth_cascade, strage);

                    //デバッグ用の表示
                    DebugPrint(img, read_count);

                    //左眼、右目、鼻、口の矩形を確定させる。
                    DecidePartsRect(gray_image);

                    //パーツ確定後
//                    DebugPrint2(img, read_count);


                    //基点を作る
                    MakeBasePoint(gray_image);



                    read_count++;
                }
            }
        }


        /// <summary>
        /// パーツの矩形を確定させる
        /// </summary>
        private void DecidePartsRect(IplImage img)
        {
            //両目の矩形を探す　左眼は画像の半分より左で逆は右
            int image_half_x = img.Width / 2; 
            for (int i = 0; i < this.EyeResult.Total; i++)
            {
                CvRect rect = this.EyeResult[i].Value.Rect;
                int rect_size = rect.Width * rect.Height;
                if (rect.X <= image_half_x)
                {
                    //サイズの大きい矩形を採用
                    if (LeftEyeRect.Width * LeftEyeRect.Height <= rect_size)
                    {
                        LeftEyeRect = rect;
                    }
                }
                else
                {
                    //サイズの大きい矩形を採用
                    if (RightEyeRect.Width * RightEyeRect.Height <= rect_size)
                    {
                        RightEyeRect = rect;
                    }
                }
            }

            //鼻の矩形を確定させる。
            for (int i = 0; i < this.NoseResult.Total; i++)
            {
                CvRect rect = this.NoseResult[i].Value.Rect;
                int rect_size = rect.Width * rect.Height;
                //サイズの大きい矩形を採用
                if (NoseRect.Width * NoseRect.Height <= rect_size)
                {
                    NoseRect = rect;
                }
            }

            //鼻の矩形を確定させる。
            for (int i = 0; i < this.MouthResult.Total; i++)
            {
                CvRect rect = this.MouthResult[i].Value.Rect;
                int rect_size = rect.Width * rect.Height;
                //サイズの大きい矩形を採用
                if (MouthRect.Width * MouthRect.Height <= rect_size)
                {
                    MouthRect = rect;
                }
            }
        }

        /// <summary>
        /// 目と目の間の座標。基点を作る
        /// </summary>
        private void MakeBasePoint(IplImage img)
        {
            //瞳の間の場所を基点として各パーツとの比率をとる
            //（パーツ座標と基点との距離）/瞳の間の距離を学習データとする
        }


        private void DebugPrint2(IplImage img, int count)
        {
            //目の結果
            Cv.Rectangle(img, this.LeftEyeRect, new CvColor(255, 0, 0));
            Cv.Rectangle(img, this.RightEyeRect, new CvColor(255, 0, 0));

            //鼻の結果
            Cv.Rectangle(img, this.NoseRect, new CvColor(0, 255, 0));

            //口の結果
            Cv.Rectangle(img, this.MouthRect, new CvColor(0, 0, 255));

            using (new CvWindow(img))
            {
                string out_name = @"out\decide_parts" + count + @".jpeg";
                Cv.SaveImage(out_name, img);
                Cv.WaitKey();
            }                           

        }

        /// <summary>
        /// デバッグ用の表示
        /// </summary>
        /// <param name="img"></param>
        /// <param name="count"></param>
        private void DebugPrint(IplImage img,int count)
        {
            //目の結果
            for (int i = 0; i < this.EyeResult.Total; i++)
            {
                //矩形の大きさに書き出す
                CvRect rect = EyeResult[i].Value.Rect;
                Cv.Rectangle(img, rect, new CvColor(255, 0, 0));
            }

            //鼻の検出
            for (int i = 0; i < NoseResult.Total; i++)
            {
                //矩形の大きさに書き出す
                CvRect rect = NoseResult[i].Value.Rect;
                Cv.Rectangle(img, rect, new CvColor(0, 255, 0));
            }

            //口の検出
            for (int i = 0; i < MouthResult.Total; i++)
            {
                //矩形の大きさに書き出す
                CvRect rect = MouthResult[i].Value.Rect;
                Cv.Rectangle(img, rect, new CvColor(0, 0, 255));
            }

            using (new CvWindow(img))
            {
                string out_name = @"out\out" + count + @".jpeg";
                Cv.SaveImage(out_name, img);
                Cv.WaitKey();
            }                           


        }

        //顔写真リストファイルを読み込み
        private void ReadFileList()
        {
            string read_list = @"";
//            read_list = @"I:\myprog\github\kubokinJudge\data\kubota_face\kubota_face_list.txt";
            read_list = @"I:\myprog\github\kubokinJudge\data\fujikin_face\fujikin_face_list.txt";

            //リストファイルと読みこんでファイル名をとる
            using (StreamReader sr = new StreamReader(read_list))
            {
                //1行づつ読み込む
                while (sr.Peek() > -1)
                {
                    this.KubotaList.Add(sr.ReadLine());
                }
                //閉じる
                sr.Close();
            }
        }

        List<string> KubotaList = new List<string>();

        CvPoint BasePoint;    //目と目の間の座標。基点

        CvSeq<CvAvgComp> EyeResult, NoseResult, MouthResult;
        CvRect RightEyeRect, LeftEyeRect, NoseRect, MouthRect;        //パーツの座標
    }
}
