﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using System.IO;

namespace MakeSVMFile
{
    /// <summary>
    /// 特量量を算出するクラス
    /// </summary>
    class FaceFeature
    {
        enum NORMALIZE_TYPE
        {
            NORMAL,
            EXTRA,
        };

        const int SMALL_IMAGE_LIMIT = 100;  //顔画像を拡大対象にする
        const int IMAGE_RESIZE_RATE = 4;    //拡大率

        NORMALIZE_TYPE TYPE = NORMALIZE_TYPE.NORMAL;       //0で0.0～1.0　０意外で平均と標準偏差を使う

        //各パーツの矩形
        struct PartsRectInfo
        {
            public CvRect RightEye;
            public CvRect LeftEye;
            public CvRect Nose;
            public CvRect Mouth;
        };

        //特徴量
        public struct FeatureValue
        {
            public int ID;              //種類分け用のID
            public CvPoint basepoint;   //両目の間の基点
            public double BothEyeDistance; //目と目の間の距離
            public double LeftEyeValueL;
            public double LeftEyeValueR;
            public double RightEyeValueL;
            public double RightEyeValueR;
            public double NoseLValueL;
            public double NoseLValueR;
            public double MouthLValueL;
            public double MouthLValueR;
        };
        //コンストラクタ
        public FaceFeature()
        {
            this.FaceList = new List<string>();
            this.IDList = new List<int>();//特徴量とセットで使うID
        }

        //特徴量の算出
        public void DetectFacePoint()
        {
            this.ReadCount = 0;
            while (this.ReadCount < this.FaceList.Count())
            {
                string input_file_path = this.FaceList[this.ReadCount];
                int face_id = this.IDList[this.ReadCount];
                MakeFeatureFromFile(input_file_path,face_id);

                this.ReadCount++;
            }
        }

        /// <summary>
        /// 導き出した特徴量を0から１に正規化する
        /// 正規化計算ファイルを出力する
        /// </summary>
        public void NormalizeFeature()
        {
            const int DATA_NUM = 8;

            if(TYPE == NORMALIZE_TYPE.NORMAL)
            {
                //最大値と最小値を記録
                double[] max = new double[DATA_NUM];
                double[] min = new double[DATA_NUM];

                //１つの特徴量毎に正規化する
                for (int i = 0; i < DATA_NUM; i++)
                {
                    max[i] = double.MinValue;
                    min[i] = double.MaxValue;
                }
                foreach (FeatureValue feature_data in FeatuerValueList)
                {
                    //最大と最小をとる
                    if (feature_data.LeftEyeValueL > max[0]) { max[0] = feature_data.LeftEyeValueL; }
                    if (feature_data.LeftEyeValueL < min[0]) { min[0] = feature_data.LeftEyeValueL; }
                    if (feature_data.LeftEyeValueR > max[1]) { max[1] = feature_data.LeftEyeValueR; }
                    if (feature_data.LeftEyeValueR < min[1]) { min[1] = feature_data.LeftEyeValueR; }

                    if (feature_data.RightEyeValueL > max[2]) { max[2] = feature_data.RightEyeValueL; }
                    if (feature_data.RightEyeValueL < min[2]) { min[2] = feature_data.RightEyeValueL; }
                    if (feature_data.RightEyeValueR > max[3]) { max[3] = feature_data.RightEyeValueR; }
                    if (feature_data.RightEyeValueR < min[3]) { min[3] = feature_data.RightEyeValueR; }

                    if (feature_data.NoseLValueL > max[4]) { max[4] = feature_data.NoseLValueL; }
                    if (feature_data.NoseLValueL < min[4]) { min[4] = feature_data.NoseLValueL; }
                    if (feature_data.NoseLValueR > max[5]) { max[5] = feature_data.NoseLValueR; }
                    if (feature_data.NoseLValueR < min[5]) { min[5] = feature_data.NoseLValueR; }

                    if (feature_data.MouthLValueL > max[6]) { max[6] = feature_data.MouthLValueL; }
                    if (feature_data.MouthLValueL < min[6]) { min[6] = feature_data.MouthLValueL; }
                    if (feature_data.MouthLValueR > max[7]) { max[7] = feature_data.MouthLValueR; }
                    if (feature_data.MouthLValueR < min[7]) { min[7] = feature_data.MouthLValueR; }
                }

                //正規化の実行
                for (int i = 0; i < this.FeatuerValueList.Count(); i++)
                {
                    FeatureValue tmp = new FeatureValue();
                    tmp.LeftEyeValueL = (FeatuerValueList[i].LeftEyeValueL - min[0]) / (max[0] - min[0]);
                    tmp.LeftEyeValueR = (FeatuerValueList[i].LeftEyeValueR - min[1]) / (max[1] - min[1]);
                    tmp.RightEyeValueL = (FeatuerValueList[i].RightEyeValueL - min[2]) / (max[2] - min[2]);
                    tmp.RightEyeValueR = (FeatuerValueList[i].RightEyeValueR - min[3]) / (max[3] - min[3]);
                    tmp.NoseLValueL = (FeatuerValueList[i].NoseLValueL - min[4]) / (max[4] - min[4]);
                    tmp.NoseLValueR = (FeatuerValueList[i].NoseLValueR - min[5]) / (max[5] - min[5]);
                    tmp.MouthLValueL = (FeatuerValueList[i].MouthLValueL - min[6]) / (max[6] - min[6]);
                    tmp.MouthLValueR = (FeatuerValueList[i].MouthLValueR - min[7]) / (max[7] - min[7]);

                    tmp.ID = FeatuerValueList[i].ID;
                    ScaleFeatuerValueList.Add(tmp);
                }
                //正規化の倍率を出力する
                using (StreamWriter w = new StreamWriter(@"out/normalize_scale.csv"))
                {
                    for (int i = 0; i < DATA_NUM; i++)
                    {
                        w.Write(max[i] + ",");
                    }
                    w.Write("\n");
                    for (int i = 0; i < DATA_NUM; i++)
                    {
                        w.Write(min[i] + ",");
                    }
                    w.Write("\n");
                }

            }
            else
            {
                double[] average = new double[DATA_NUM];  //平均値
                double[] SD = new double[DATA_NUM];       //標準偏差

                //正規化の方法を変えてみる
                //(x-平均値)/標準偏差

                //平均値をとる
                int data_num = 0;
                foreach (FeatureValue feature_data in FeatuerValueList)
                {
                    data_num++;
                    average[0] += feature_data.LeftEyeValueL;
                    average[1] += feature_data.LeftEyeValueR;

                    average[2] += feature_data.RightEyeValueL;
                    average[3] += feature_data.RightEyeValueR;
                    average[4] += feature_data.NoseLValueL;
                    average[5] += feature_data.NoseLValueR;
                    average[6] += feature_data.MouthLValueL;
                    average[7] += feature_data.MouthLValueR;
                }
                for (int i = 0; i < DATA_NUM; i++)
                {
                    average[i] /= FeatuerValueList.Count;
                }

                //標準偏差
                double[] tmp = new double[DATA_NUM];
                foreach (FeatureValue feature_data in FeatuerValueList)
                {
                    tmp[0] += Math.Pow(feature_data.LeftEyeValueL - average[0], 2);
                    tmp[1] += Math.Pow(feature_data.LeftEyeValueR - average[1], 2);
                    tmp[2] += Math.Pow(feature_data.RightEyeValueL - average[2], 2);
                    tmp[3] += Math.Pow(feature_data.RightEyeValueR - average[3], 2);
                    tmp[4] += Math.Pow(feature_data.NoseLValueL - average[4], 2);
                    tmp[5] += Math.Pow(feature_data.NoseLValueR - average[5], 2);
                    tmp[6] += Math.Pow(feature_data.MouthLValueL - average[6], 2);
                    tmp[7] += Math.Pow(feature_data.MouthLValueR - average[7], 2);
                }
                for (int i = 0; i < DATA_NUM; i++)
                {
                    SD[i] = Math.Sqrt(tmp[i] / FeatuerValueList.Count);
                }

                //正規化の実行
                for (int i = 0; i < this.FeatuerValueList.Count(); i++)
                {
                    FeatureValue value = new FeatureValue();
                    value.LeftEyeValueL = (FeatuerValueList[i].LeftEyeValueL - average[0]) / SD[0];
                    value.LeftEyeValueR = (FeatuerValueList[i].LeftEyeValueR - average[1]) / SD[1];
                    value.RightEyeValueL = (FeatuerValueList[i].RightEyeValueL - average[2]) / SD[2];
                    value.RightEyeValueR = (FeatuerValueList[i].RightEyeValueR - average[3]) / SD[3];
                    value.NoseLValueL = (FeatuerValueList[i].NoseLValueL - average[4]) / SD[4];
                    value.NoseLValueR = (FeatuerValueList[i].NoseLValueR - average[5]) / SD[5];
                    value.MouthLValueL = (FeatuerValueList[i].MouthLValueL - average[6]) / SD[6];
                    value.MouthLValueR = (FeatuerValueList[i].MouthLValueR - average[7]) / SD[7];

                    //ID登録
                    value.ID = FeatuerValueList[i].ID;
                    ScaleFeatuerValueList.Add(value);
                }
                //正規化の倍率を出力する
                using (StreamWriter w = new StreamWriter(@"out/normalize_scale.csv"))
                {
                    for (int i = 0; i < DATA_NUM; i++)
                    {
                        w.Write(average[i] + ",");
                    }
                    w.Write("\n");
                    for (int i = 0; i < DATA_NUM; i++)
                    {
                        w.Write(SD[i] + ",");
                    }
                    w.Write("\n");
                }
            }

        }

        //===================================================================
        // private 
        //===================================================================
        /// <summary>
        /// ファイル名から特徴量を出す処理
        /// </summary>
        /// <param name="file_name"></param>
        private void MakeFeatureFromFile(String file_name, int face_id)
        {
            //画像読み込み処理
            using (IplImage img = new IplImage(file_name))
            {
                MakeFeatureFromIpl(img, face_id);
            }
        }
        /// <summary>
        /// ファイル名から特徴量を出す処理
        /// </summary>
        /// <param name="file_name"></param>
        public  void MakeFeatureFromIpl(IplImage ipl_image, int face_id)
        {
            string eye_cascade_xml = @"C:\opencv2.4.10\sources\data\haarcascades\haarcascade_eye.xml";
            string nose_cascade_xml = @"C:\opencv2.4.10\sources\data\haarcascades\haarcascade_mcs_nose.xml";
            string mouth_cascade_xml = @"C:\opencv2.4.10\sources\data\haarcascades\haarcascade_mcs_mouth.xml";

            CvMemStorage strage = new CvMemStorage(0);   // メモリを確保
            CvHaarClassifierCascade eye_cascade = CvHaarClassifierCascade.FromFile(eye_cascade_xml);
            CvHaarClassifierCascade nose_cascade = CvHaarClassifierCascade.FromFile(nose_cascade_xml);
            CvHaarClassifierCascade mouth_cascade = CvHaarClassifierCascade.FromFile(mouth_cascade_xml);

            //リストにあるファイルを一枚づつデータにする
            {
                IplImage tmp_image;
                //サイズが小さければ拡大して使う
                if (ipl_image.Size.Width < SMALL_IMAGE_LIMIT)
                {
                    tmp_image = Cv.CreateImage(new CvSize(ipl_image.Width * IMAGE_RESIZE_RATE, ipl_image.Height * IMAGE_RESIZE_RATE), BitDepth.U8, 3);
                    Cv.Resize(ipl_image, tmp_image);
                }
                else
                {
                    tmp_image = Cv.CreateImage(new CvSize(ipl_image.Width, ipl_image.Height), BitDepth.U8, 3);
                    Cv.Resize(ipl_image, tmp_image);
                }

                //グレースケールに変換
                IplImage gray_image = Cv.CreateImage(new CvSize(tmp_image.Width, tmp_image.Height), BitDepth.U8, 1);
                Cv.CvtColor(tmp_image, gray_image, ColorConversion.BgrToGray);

                //発見した矩形
                this.EyeResult = Cv.HaarDetectObjects(gray_image, eye_cascade, strage);

                //鼻は画像の真ん中の方だけ
                {
                    IplImage gray_nose_image = Cv.CreateImage(new CvSize(tmp_image.Width, tmp_image.Height), BitDepth.U8, 1);
                    Cv.CvtColor(tmp_image, gray_nose_image, ColorConversion.BgrToGray);
                    CvRect rect = new CvRect(0, (int)(tmp_image.Height*0.25), tmp_image.Width, tmp_image.Height / 2);
                    gray_nose_image.ROI = rect;
//                  new CvWindow(gray_nose_image);
//                  Cv.WaitKey();

                    this.NoseResult = Cv.HaarDetectObjects(gray_nose_image, nose_cascade, strage);
                }

                //口は画像の下半分だけを調べる
                {
                    IplImage gray_mouth_image = Cv.CreateImage(new CvSize(tmp_image.Width, tmp_image.Height), BitDepth.U8, 1);
                    Cv.CvtColor(tmp_image, gray_mouth_image, ColorConversion.BgrToGray);
                    CvRect rect = new CvRect(0, (int)(tmp_image.Height *0.66), tmp_image.Width, tmp_image.Height / 3);
                    gray_mouth_image.ROI = rect;
//                    new CvWindow(gray_mouth_image);
//                     Cv.WaitKey();
                    this.MouthResult = Cv.HaarDetectObjects(gray_mouth_image, mouth_cascade, strage);
                }
                //初期化
                DataInit();
                //デバッグ用の表示
//                DebugPrint(tmp_image, this.ReadCount);

                //左眼、右目、鼻、口の矩形を確定させる。
                DecidePartsRect(gray_image);

                //パーツ確定後
//                DebugPrint2(gray_image, this.ReadCount);

                PartsRectInfo parts_info;
                parts_info.RightEye = this.RightEyeRect;
                parts_info.LeftEye = this.LeftEyeRect;
                parts_info.Nose = this.NoseRect;
                parts_info.Mouth = this.MouthRect;

                //特徴量を作る
                FeatureValue feature_value = new FeatureValue();
                bool ret = MakeFeatureValue(gray_image, ref parts_info, out feature_value);

                //正しいデータを登録
                if (ret)
                {
                    feature_value.ID = face_id;
                    this.FeatuerValueList.Add(feature_value);
                }
            }

            //メモリ解放
            eye_cascade.Dispose();
            nose_cascade.Dispose();
            mouth_cascade.Dispose();
            strage.Dispose();
            return;
        }


        /// <summary>
        /// 特徴量をだす
        /// </summary>
        private bool MakeFeatureValue(IplImage img, ref PartsRectInfo input_info, out FeatureValue output_info)
        {
            //仮に代入  
            output_info.basepoint = new CvPoint(0, 0);
            output_info.BothEyeDistance = 0;
            output_info.LeftEyeValueL = 0;
            output_info.LeftEyeValueR = 0;
            output_info.RightEyeValueL = 0;
            output_info.RightEyeValueR = 0;
            output_info.NoseLValueL = 0;
            output_info.NoseLValueR = 0;
            output_info.MouthLValueL = 0;
            output_info.MouthLValueR = 0;
            output_info.ID = 0;

            //パーツがすべてそろっているかの確認
            if (input_info.RightEye.X == 0)
            {
                return false;
            }
            if (input_info.LeftEye.X == 0)
            {
                return false;
            }
            if (input_info.Nose.X == 0)
            {
                return false;
            }
            if (input_info.Mouth.X == 0)
            {
                return false;
            }

            //瞳の間の場所を基点として各パーツとの比率をとる
            //（パーツ座標と基点との距離）/瞳の間の距離を学習データとする
            int LeftEyeCenterX = input_info.LeftEye.X + input_info.LeftEye.Width / 2;
            int LeftEyeCenterY = input_info.LeftEye.Y + input_info.LeftEye.Height / 2;
            int RightEyeCenterX = input_info.RightEye.X + input_info.RightEye.Width / 2;
            int RightEyeCenterY = input_info.RightEye.Y + input_info.RightEye.Height / 2;

            //右目の中心と左目の中心を結んだ線の中点が基準点。
            output_info.basepoint.X = (LeftEyeCenterX + RightEyeCenterX) / 2;
            output_info.basepoint.Y = (LeftEyeCenterY + RightEyeCenterY) / 2;

            //目と目の距離をとる
            output_info.BothEyeDistance = makeTwoPointDistance(LeftEyeCenterX, RightEyeCenterX, LeftEyeCenterY, RightEyeCenterY);
            //基準点から各パーツの右端、左端までの距離をとる
            output_info.LeftEyeValueL = makeTwoPointDistance(input_info.LeftEye.X,
                                                              output_info.basepoint.X,
                                                              input_info.LeftEye.Y,
                                                              output_info.basepoint.Y);
            output_info.LeftEyeValueR = makeTwoPointDistance(input_info.LeftEye.X + input_info.LeftEye.Width,
                                                              output_info.basepoint.X,
                                                              input_info.LeftEye.Y,
                                                              output_info.basepoint.Y);

            output_info.RightEyeValueL = makeTwoPointDistance(input_info.RightEye.X,
                                                              output_info.basepoint.X,
                                                              input_info.RightEye.Y,
                                                              output_info.basepoint.Y);
            output_info.RightEyeValueR = makeTwoPointDistance(input_info.RightEye.X + input_info.RightEye.Width,
                                                              output_info.basepoint.X,
                                                              input_info.RightEye.Y,
                                                              output_info.basepoint.Y);

            output_info.NoseLValueL = makeTwoPointDistance(input_info.Nose.X,
                                                  output_info.basepoint.X,
                                                  input_info.Nose.Y,
                                                  output_info.basepoint.Y);
            output_info.NoseLValueR = makeTwoPointDistance(input_info.Nose.X + input_info.Nose.Width,
                                                              output_info.basepoint.X,
                                                              input_info.Nose.Y,
                                                              output_info.basepoint.Y);

            output_info.MouthLValueL = makeTwoPointDistance(input_info.Mouth.X,
                                                  output_info.basepoint.X,
                                                  input_info.Mouth.Y,
                                                  output_info.basepoint.Y);
            output_info.MouthLValueR = makeTwoPointDistance(input_info.Mouth.X + input_info.Mouth.Width,
                                                              output_info.basepoint.X,
                                                              input_info.Mouth.Y,
                                                              output_info.basepoint.Y);


            //基準点からパーツまでの距離と瞳間距離の比率を特徴量とする
            output_info.LeftEyeValueL /= output_info.BothEyeDistance;
            output_info.LeftEyeValueR /= output_info.BothEyeDistance;
            output_info.RightEyeValueL /= output_info.BothEyeDistance;
            output_info.RightEyeValueR /= output_info.BothEyeDistance;
            output_info.NoseLValueL /= output_info.BothEyeDistance;
            output_info.NoseLValueR /= output_info.BothEyeDistance;
            output_info.MouthLValueL /= output_info.BothEyeDistance;
            output_info.MouthLValueR /= output_info.BothEyeDistance;

            return true;
        }
        /// <summary>
        /// ２点間の距離を出す
        /// </summary>
        /// <returns></returns>
        private double makeTwoPointDistance(int x1, int y1, int x2, int y2)
        {
            double answer = 0;

            answer = Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2);
            answer = Math.Sqrt(answer);

            return answer;
        }
        /// <summary>
        /// パーツの矩形を確定させる
        /// </summary>
        private void DecidePartsRect(IplImage img)
        {
            //両目の矩形を探す　左眼は画像の半分より左で逆は右
            int image_half_x = img.Width / 2;
            int image_half_y = img.Height / 2;
            for (int i = 0; i < this.EyeResult.Total; i++)
            {
                CvRect rect = this.EyeResult[i].Value.Rect;
                int rect_size = rect.Height * rect.Width;

                //右目
                if (rect.X < image_half_x)
                {
                    //サイズの大きい矩形を採用
                    if (this.RightEyeRect.Width * this.RightEyeRect.Height <= rect_size)
                    {
                        this.RightEyeRect = rect;
                    }
                }
            }

            for (int i = 0; i < this.EyeResult.Total; i++)
            {
                CvRect rect = this.EyeResult[i].Value.Rect;
                int rect_size = rect.Height * rect.Width;

                //左目
                if (rect.X >= image_half_x)
                {
                    //サイズの大きい矩形を採用
                    if (this.LeftEyeRect.Width * this.LeftEyeRect.Height <= rect_size)
                    {
                        this.LeftEyeRect = rect;
                    }
                }
            }

            //鼻の矩形を確定させる。
            for (int i = 0; i < this.NoseResult.Total; i++)
            {
                CvRect rect = this.NoseResult[i].Value.Rect;
                //中央だけ検索しているので座標を加える
                rect.Y += (int)(img.Height * 0.25);
                int rect_size = rect.Height * rect.Width;

                //画像の中央に位置するはず
                if (rect.X < image_half_x && image_half_x < rect.X + rect.Width)
                {
///                    if (rect.Y < image_half_y && image_half_y < rect.Y + rect.Height)
                    {
                        //サイズの大きい矩形を採用
                        if (this.NoseRect.Width * this.NoseRect.Height <= rect_size)
                        {
                            this.NoseRect = rect;
                        }
                    }
                }
            }

            //口の矩形を確定させる。
            for (int i = 0; i < this.MouthResult.Total; i++)
            {
                CvRect rect = this.MouthResult[i].Value.Rect;
                //下半分だけ検索しているのでその座標が出ているから加える
                rect.Y += (int)(img.Height *0.66);
                int rect_size = rect.Height * rect.Width;

                //画像の下半分にあるはず
                if (image_half_y < rect.Y)
                {
                    //サイズの大きい矩形を採用
                    if (this.MouthRect.Width * this.MouthRect.Height <= rect_size)
                    {
                        this.MouthRect = rect;
                    }
                }
            }
        }

        /// <summary>
        /// パラメータの初期化
        /// </summary>
        private void DataInit()
        {
            this.LeftEyeRect = new CvRect(0, 0, 0, 0);
            this.RightEyeRect = new CvRect(0, 0, 0, 0);
            this.MouthRect = new CvRect(0, 0, 0, 0);
            this.NoseRect = new CvRect(0, 0, 0, 0);
        }

        /// <summary>
        /// 矩形選択後の画像を表示
        /// </summary>
        /// <param name="img"></param>
        /// <param name="count"></param>
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
                //パスからファイル名を取る
                string path = this.FaceList[this.ReadCount];
                String file_name = Path.GetFileNameWithoutExtension(path);

                string out_name = this.OutPutFolda + @"\decide_parts_" + file_name + @".jpeg";
                Cv.SaveImage(out_name, img);
                Cv.WaitKey();
            }

        }

        /// <summary>
        /// デバッグ用の表示
        /// </summary>
        /// <param name="img"></param>
        /// <param name="count"></param>
        private void DebugPrint(IplImage img, int count)
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
                //中央だけ検索しているので座標を加える
                rect.Y += (int)(img.Height * 0.25);
                Cv.Rectangle(img, rect, new CvColor(0, 255, 0));
            }

            //口の検出
            for (int i = 0; i < MouthResult.Total; i++)
            {
                //矩形の大きさに書き出す
                CvRect rect = MouthResult[i].Value.Rect;
                //下半分だけ検索しているのでその座標が出ているから加える
                rect.Y += (int)(img.Height * 0.66);
                Cv.Rectangle(img, rect, new CvColor(0, 0, 255));
            }

            using (new CvWindow(img))
            {
                //パスからファイル名を取る
                string path = this.FaceList[this.ReadCount];
                String file_name = Path.GetFileNameWithoutExtension(path);

                string out_name = this.OutPutFolda + @"\find_" + file_name + @".jpeg";
                Cv.SaveImage(out_name, img);
                Cv.WaitKey();
            }
        }


        //=========================================================
        //  メンバ変数
        //=========================================================
        public List<string> FaceList { get; set; }
        public List<int> IDList { get; set; }

        private NORMALIZE_TYPE TYPE1
        {
            get
            {
                return TYPE;
            }

            set
            {
                TYPE = value;
            }
        }

        public String OutPutFolda = @"";

        public List<FeatureValue> FeatuerValueList = new List<FeatureValue>(); //特徴量のLIST
        public List<FeatureValue> ScaleFeatuerValueList = new List<FeatureValue>(); //特徴量を正規化したリスト

        CvRect RightEyeRect, LeftEyeRect, NoseRect, MouthRect;        //パーツの座標
        CvSeq<CvAvgComp> EyeResult, NoseResult, MouthResult;          //パーツ検出結果

        private int ReadCount; //リストからの読み込み番号
    }
}
