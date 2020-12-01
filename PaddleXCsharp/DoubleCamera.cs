using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MvCamCtrl.NET;  // 海康相机
using HIKDeviceSource; // 海康
using Basler.Pylon; // Basler 相机
using BaslerDeviceSource; // Basler
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace PaddleXCsharp
{
    public partial class DoubleCamera : Form
    {
        //private delegate void UpdateUI();  // 声明委托
        int cameraUsingNum;
        
        /* ================================= basler相机 ================================= */
        BaslerCamera baslerCamera = new BaslerCamera();
        Camera[] cameraArr1 = new Camera[2];
        List<ICameraInfo> allCameraInfos;
        //Thread baslerGrabThread1 = null;
        //private PixelDataConverter converter = new PixelDataConverter(); // basler里用于将相机采集的图像转换成位图

        //bool baslerCanGrab = false;    // 控制相机是否Grab
        //private PixelDataConverter converter = new PixelDataConverter(); // basler里用于将相机采集的图像转换成位图
        bool chooseBasler = false;     // Basler相机打开标志

        ///* ================================= 海康相机 ================================= */
        //HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();
        //MyCamera camera2 = null;
        //Thread hikGrabThread = null;

        //bool hikCanGrab = false;  // 控制相机是否Grab
        bool chooseHIK = false;           // 海康相机打开标志

        //// 用于从驱动获取图像的缓存
        //UInt32 m_nBufSizeForDriver = 0;
        //IntPtr m_BufForDriver;

        ///* ================================= inference ================================= */
        //#region 接口定义及参数
        //int modelType = 1;  // 模型的类型  0：分类模型；1：检测模型；2：分割模型
        //string modelPath = ""; // 模型目录路径
        //bool useGPU = false;  // 是否使用GPU
        //bool useTrt = false;  // 是否使用TensorRT
        //bool useMkl = true;  // 是否使用MKLDNN加速模型在CPU上的预测性能
        //int mklThreadNum = 16; // 使用MKLDNN时，线程数量
        //int gpuID = 0; // 使用GPU的ID号
        //string key = ""; //模型解密密钥，此参数用于加载加密的PaddleX模型时使用
        //bool useIrOptim = false; // 是否加速模型后进行图优化

        //bool isInference = false;  // 是否进行推理   
        //IntPtr model; // 模型

        //// 定义CreatePaddlexModel接口
        //[DllImport("paddlex_inference.dll", EntryPoint = "CreatePaddlexModel", CharSet = CharSet.Ansi)]
        //static extern IntPtr CreatePaddlexModel(ref int modelType, string modelPath, bool useGPU, bool useTrt, bool useMkl, int mklThreadNum, int gpuID, string key, bool useIrOptim);

        //// 定义PaddlexDetPredict接口
        //[DllImport("paddlex_inference.dll", EntryPoint = "PaddlexDetPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //static extern IntPtr PaddlexDetPredict(IntPtr model, byte[] image, int height, int width, int channels, IntPtr[] result);
        //#endregion

        public DoubleCamera()
        {
            InitializeComponent();
            // 子线程安全访问窗体控件
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        // 选择所使用相机的类型
        private void BnEnum_Click(object sender, EventArgs e)
        {
            string type = cameraType.Text;
            if (type == "")
            {
                MessageBox.Show("请在初始化界面中选定相机类型", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (type == "海康相机")
            {
                chooseHIK = true;
                chooseBasler = false;
                DeviceListAcq();
            }
            else if (type == "Basler相机")
            {
                chooseHIK = false;
                chooseBasler = true;
                DeviceListAcq();
            }
        }
        
        // 枚举设备
        private void DeviceListAcq()
        {
            System.GC.Collect();

            // 枚举海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                //try
                //{
                //    // 相机数量
                //    uint cameraNum = hIKVisionCamera.CameraNum();
                //    tbOnlineNum.Text = cameraNum.ToString();
                //}
                //catch
                //{
                //    MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}
            }

            //枚举basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 返回相机数量
                    int cameraNum = baslerCamera.CameraNum();
                    allCameraInfos = baslerCamera.CameraEnum();
                    tbOnlineNum.Text = cameraNum.ToString();
                }
                catch
                {
                    MessageBox.Show("枚举设备失败，请检查连接状态！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        #region 控件使能
        private void SetCtrlWhenOpen()
        {
            bnOpen.Enabled = false;
            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = true;
            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            bnGetParam.Enabled = true;
            bnSetParam.Enabled = true;
        }
        private void SetCtrlWhenClose()
        {
            bnOpen.Enabled = true;
            bnClose.Enabled = false;
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            bnGetParam.Enabled = false;
            bnSetParam.Enabled = false;
            bnLoadModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;

        }
        private void SetCtrlWhenStartGrab()
        {
            //bnStartGrab.Enabled = true;
            //bnStopGrab.Enabled = true;
            bnLoadModel.Enabled = true;
            bnStartDetection.Enabled = true;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = true;
        }
        private void SetCtrlWhenStopGrab()
        {
            //bnStartGrab.Enabled = true;
            //bnStopGrab.Enabled = false;
            bnLoadModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;
        }
        #endregion

        // 打开设备
        private void BnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                int.Parse(tbUseNum.Text);
            }
            catch
            {
                MessageBox.Show("请输入正确的值!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 获取使用设备的数量
            cameraUsingNum = int.Parse(tbUseNum.Text);
            // 参数检测
            if (cameraUsingNum <= 0)
            {
                cameraUsingNum = 1;
                tbUseNum.Text = "1";
            }
            if (cameraUsingNum > 2)
            {
                cameraUsingNum = 2;
                tbUseNum.Text = "2";
            }
            if ((chooseHIK) && (!chooseBasler))
            {
                MessageBox.Show("test");
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 相机初始化
                cameraArr1 = baslerCamera.MultiCameraInit(cameraUsingNum);               
                // 注册事件
                RegisterGrabEvent();
                // label颜色提示
                for(int i = 0; i <cameraUsingNum; i++)
                {
                    Control[] ctrlArr = this.Controls.Find("lblCam" + (i + 1), true);
                    if (ctrlArr.Length > 0 && ctrlArr[0] is Label)
                    {
                        Label lbl = (Label)ctrlArr[0];
                        lbl.BackColor = Color.Yellow;
                    }
                }
                // 控件操作
                SetCtrlWhenOpen();
            }  
        }

        //关闭设备
        private void BnClose_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                MessageBox.Show("test");
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 释放相机
                baslerCamera.MultiDestroyCamera();
                for (int i = 0; i < cameraUsingNum; i++)
                {
                    Control[] ctrlArr = this.Controls.Find("lblCam" + (i + 1), true);
                    if (ctrlArr.Length > 0 && ctrlArr[0] is Label)
                    {
                        Label lbl = (Label)ctrlArr[0];
                        lbl.BackColor = Color.Red;
                    }
                }
                // 控件操作
                SetCtrlWhenClose();
            }
        }

        // 采集进程1
        //public void GrabThreadProcess()
        //{
        //    if ((chooseHIK) && (!chooseBasler))
        //    {
        //        MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
        //        camera2.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
        //        UInt32 nPayloadSize = stParam.nCurValue;
        //        if (nPayloadSize > m_nBufSizeForDriver)
        //        {
        //            if (m_BufForDriver != IntPtr.Zero)
        //            {
        //                Marshal.Release(m_BufForDriver);
        //            }
        //            m_nBufSizeForDriver = nPayloadSize;
        //            m_BufForDriver = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver);
        //        }
        //        if (m_BufForDriver == IntPtr.Zero)
        //        {
        //            return;
        //        }

        //        MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体
        //        IntPtr pTemp = IntPtr.Zero;

        //        while (hikCanGrab)
        //        {
        //            // 将海康数据类型转为Mat
        //            int nRet = camera2.MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref stFrameInfo, 1000); // m_BufForDriver为图像数据接收指针
        //            pTemp = m_BufForDriver;
        //            byte[] byteImage = new byte[stFrameInfo.nHeight * stFrameInfo.nWidth];
        //            Marshal.Copy(m_BufForDriver, byteImage, 0, stFrameInfo.nHeight * stFrameInfo.nWidth);
        //            Mat matImage = new Mat(stFrameInfo.nHeight, stFrameInfo.nWidth, MatType.CV_8UC1, byteImage);
        //            // 单通道图像转为三通道
        //            Mat matImageNew = new Mat();
        //            Cv2.CvtColor(matImage, matImageNew, ColorConversionCodes.GRAY2RGB);
        //            Bitmap bitmap = matImageNew.ToBitmap();  // Mat转为Bitmap
        //            // 是否进行推理
        //            //if (isInference)  bitmap = Inference(bitmap); 
        //            if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
        //            {
        //                UpdateUI update = delegate { pictureBox1.Image = bitmap; };
        //                pictureBox1.BeginInvoke(update);
        //            }
        //            else { pictureBox1.Image = bitmap; }
        //        }
        //    }
        //    else if ((chooseBasler) && (!chooseHIK))
        //    {  
        //        while (baslerCanGrab)
        //        {
        //            IGrabResult grabResult1;
        //            //IGrabResult grabResult2 = cameraArr1[1].StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
        //            using (grabResult1 = cameraArr1[0].StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException))
        //            {
        //                if (grabResult1.GrabSucceeded)
        //                {
        //                    // 四通道RGBA
        //                    Bitmap bitmap = new Bitmap(grabResult1.Width, grabResult1.Height, PixelFormat.Format32bppRgb);
        //                    // 锁定位图的位
        //                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
        //                    // 将指针放置到位图的缓冲区
        //                    converter.OutputPixelFormat = PixelType.BGRA8packed;
        //                    IntPtr ptrBmp = bmpData.Scan0;
        //                    converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult1);
        //                    bitmap.UnlockBits(bmpData);
        //                    // 是否进行推理
        //                    //if (isInference) bitmap = Inference(bitmap);
        //                    // 禁止跨线程直接访问控件，故invoke到主线程中
        //                    // 参考：https://bbs.csdn.net/topics/350050105
        //                    //       https://www.cnblogs.com/lky-learning/p/14025280.html
        //                    if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
        //                    {
        //                        UpdateUI update = delegate { pictureBox1.Image = bitmap; };
        //                        pictureBox1.BeginInvoke(update);
        //                    }
        //                    else { pictureBox1.Image = bitmap; }
        //                }
        //            }
        //        }
        //    }
        //}

        // 开始采集
        private void BnStartGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                //try
                //{
                //    // 标志位置位true
                //    hikCanGrab = true;
                //    // 开始采集
                //    hIKVisionCamera.StartGrabbing();
                //    // 用线程更新显示
                //    hikGrabThread = new Thread(GrabThreadProcess);
                //    hikGrabThread.Start();
                //    // 控件操作
                //    SetCtrlWhenStartGrab();
                //}
                //catch
                //{
                //    hikCanGrab = false;
                //    hikGrabThread.Join();
                //    MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}

            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 开始Grab
                if (rbCamera1.Checked)
                {
                    // 连续采集模式
                    cameraArr1[0].Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                    cameraArr1[0].StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                    lblCam1.BackColor = Color.Green;
                }

                else if (rbCamera2.Checked)
                {
                    cameraArr1[1].Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                    cameraArr1[1].StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                    lblCam2.BackColor = Color.Green;
                }
                // 控件操作
                SetCtrlWhenStartGrab();
            }
        }

        // 停止采集
        private void BnStopGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {

            }
            else if ((chooseBasler) && (!chooseHIK))
            { 
                // 1号相机被选中
                if (rbCamera1.Checked)
                {
                    cameraArr1[0].StreamGrabber.Stop();
                    lblCam1.BackColor = Color.Yellow;
                }
                // 2号相机被选中
                else if (rbCamera2.Checked)
                {
                    cameraArr1[1].StreamGrabber.Stop();
                    lblCam2.BackColor = Color.Yellow;
                }
            }
        }

        private void RegisterGrabEvent()
        {
            if (cameraArr1[0] != null && true == cameraArr1[0].IsOpen)
            {
                cameraArr1[0].StreamGrabber.ImageGrabbed += OnImageGrabbed_1;
            }

            if (cameraArr1[1] != null && true == cameraArr1[1].IsOpen)
            {
                cameraArr1[1].StreamGrabber.ImageGrabbed += OnImageGrabbed_2;
            }
        }

        private void OnImageGrabbed(string camName, ImageGrabbedEventArgs e, PictureBox pic)
        {
            try
            {
                IGrabResult grabResult = e.GrabResult;
                if (grabResult.IsValid)
                {
                    Bitmap bm = (Bitmap)pic.Image;
                    if (false == BitmapFactory.IsCompatible(bm, grabResult.Width, grabResult.Height, false))
                    {
                        BitmapFactory.CreateBitmap(out bm, grabResult.Width, grabResult.Height, false);
                        pic.Image = bm;
                    }
                    BitmapFactory.UpdateBitmap(bm, (byte[])grabResult.PixelData, grabResult.Width, grabResult.Height, false);

                    pic.Refresh();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("CAM:{0}！\n{1}", camName, ex.Message));
            }
            finally
            {
                e.DisposeGrabResultIfClone();
            }
        }

        private void OnImageGrabbed_1(Object sender, ImageGrabbedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<ImageGrabbedEventArgs>(OnImageGrabbed_1), sender, e.Clone());
                return;
            }
            OnImageGrabbed("CAM1", e, pictureBox1);
        }
        private void OnImageGrabbed_2(Object sender, ImageGrabbedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<ImageGrabbedEventArgs>(OnImageGrabbed_2), sender, e.Clone());
                return;
            }
            OnImageGrabbed("CAM2", e, pictureBox2);
        }

        // 获取参数
        private void BnGetParam_Click(object sender, EventArgs e)
        {
            // 参数
            string gain = null;
            string exposure = null;
            if ((chooseHIK) && (!chooseBasler))
            {
                //// 获取参数
                //hIKVisionCamera.GetParam(ref gain, ref exposure);
                //tbGain.Text = gain;
                //tbExposure.Text = exposure;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                if(rbCamera1.Checked)
                    // 获取参数
                    baslerCamera.GetParam(ref gain, ref exposure, cameraArr1[0]);
                else if(rbCamera2.Checked)
                    // 获取参数
                    baslerCamera.GetParam(ref gain, ref exposure, cameraArr1[1]);
                    
                Console.WriteLine(cameraArr1[0].IsOpen);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }
        }

        // 设置参数
        private void BnSetParam_Click(object sender, EventArgs e)
        {
            string gainshow = null;
            string exposureshow = null;
            if ((chooseHIK) && (!chooseBasler))
            {
                //float exposure = float.Parse(tbExposure.Text);
                //float gain = float.Parse(tbGain.Text);
                //hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow);
                //// 显示真实值
                //tbGain.Text = gainshow;
                //tbExposure.Text = exposureshow;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                long exposure = long.Parse(tbExposure.Text);
                long gain = long.Parse(tbGain.Text);
                if (rbCamera1.Checked)
                    baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, cameraArr1[0]);
                else if (rbCamera2.Checked)
                    baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, cameraArr1[1]);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            }
        }

        private void SingleCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            BnClose_Click(sender, e);
            System.Environment.Exit(0);
        }
    }
}
