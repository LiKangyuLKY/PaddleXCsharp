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
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace PaddleXCsharp
{
    public partial class DoubleCamera : Form
    {
        private delegate void UpdateUI();  // 声明委托
        int cameraUsingNum;
       
        ///* ================================= 海康相机 ================================= */
        HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();
        MyCamera[] cameraArr1 = new MyCamera[2];
        MyCamera.cbOutputExdelegate cbImage1;
        MyCamera.cbOutputExdelegate cbImage2;
        bool hikCanGrab = false;  // 控制相机是否Grab
        bool chooseHIK = false;           // 海康相机打开标志

        // 用于从驱动获取图像的缓存
        UInt32 m_nBufSizeForDriver = 0;
        //IntPtr m_BufForDriver;

        /* ================================= basler相机 ================================= */
        BaslerCamera baslerCamera = new BaslerCamera();
        Camera[] cameraArr2 = new Camera[2];
        List<ICameraInfo> allCameraInfos;
        private PixelDataConverter converter = new PixelDataConverter(); // basler里用于将相机采集的图像转换成位图
        bool chooseBasler = false;     // Basler相机打开标志


        ///* ================================= inference ================================= */
        #region 接口定义及参数
        int modelType = 1;  // 模型的类型  0：分类模型；1：检测模型；2：分割模型
        string modelPath = ""; // 模型目录路径
        bool useGPU = false;  // 是否使用GPU
        bool useTrt = false;  // 是否使用TensorRT
        bool useMkl = true;  // 是否使用MKLDNN加速模型在CPU上的预测性能
        int mklThreadNum = 16; // 使用MKLDNN时，线程数量
        int gpuID = 0; // 使用GPU的ID号
        string key = ""; //模型解密密钥，此参数用于加载加密的PaddleX模型时使用
        bool useIrOptim = false; // 是否加速模型后进行图优化
        bool isInference1 = false;  // 是否进行推理   
        bool isInference2 = false;
        IntPtr model1; // 模型
        IntPtr model2;

        #region 模型

        // 定义CreatePaddlexModel接口
        [DllImport("paddlex_inference.dll", EntryPoint = "CreatePaddlexModel", CharSet = CharSet.Ansi)]
        static extern IntPtr CreatePaddlexModel(ref int modelType,
                                                string modelPath,
                                                bool useGPU,
                                                bool useTrt,
                                                bool useMkl,
                                                int mklThreadNum,
                                                int gpuID,
                                                string key,
                                                bool useIrOptim);

        // 定义分类接口
        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexClsPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern bool PaddlexClsPredict(IntPtr model, byte[] image, int height, int width, int channels, out int categoryID, out float score);

        // 定义检测接口
        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexDetPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern bool PaddlexDetPredict(IntPtr model, byte[] image, int height, int width, int channels, int max_box, float[] result, bool visualize);
        #endregion

        //// 定义语义分割接口
        //[DllImport("paddlex_inference.dll", EntryPoint = "PaddlexSegPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //static extern bool PaddlexSegPredict(IntPtr model, byte[] image, int height, int width, int channels, );
        #endregion

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
                try
                {
                    // 相机数量
                    uint cameraNum = hIKVisionCamera.CameraNum();
                    _ = hIKVisionCamera.EnumDevices();
                    tbOnlineNum.Text = cameraNum.ToString();
                }
                catch
                {
                    MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
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
            //bnStopDetection.Enabled = false;
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
            bool initFinished = false;
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
            if (cameraUsingNum < 1)
            {
                cameraUsingNum = 1;
                tbUseNum.Text = "1";
            }
            else if (cameraUsingNum > 2)
            {
                cameraUsingNum = 2;
                tbUseNum.Text = "2";
            }
            else
            {
                if (cameraUsingNum == 1)
                    rbCamera2.Enabled = false;
            }
            if ((chooseHIK) && (!chooseBasler))
            {
                // 相机初始化
                //cbImage1 = new MyCamera.cbOutputExdelegate(ImageCallBack);
                //cbImage2 = new MyCamera.cbOutputExdelegate(ImageCallBack);
                cameraArr1 = hIKVisionCamera.MultiCameraInit(cameraUsingNum);
                initFinished = true;
                
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 相机初始化
                cameraArr2 = baslerCamera.MultiCameraInit(cameraUsingNum);
                initFinished = true;
            }
            if(initFinished)
            {
                // 注册事件
                RegisterGrabEvent();
                // label颜色提示
                for (int i = 0; i < cameraUsingNum; i++)
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
        private void RegisterGrabEvent()
        {
            if ((chooseHIK) && (!chooseBasler))
            {

                if (cameraArr1[0] != null)
                {
                    cbImage1 = new MyCamera.cbOutputExdelegate(ImageCallBack_1);
                    cameraArr1[0].MV_CC_RegisterImageCallBackEx_NET(cbImage1, (IntPtr)0);
                }

                if (cameraArr1[1] != null)
                {
                    cbImage2 = new MyCamera.cbOutputExdelegate(ImageCallBack_2);
                    cameraArr1[1].MV_CC_RegisterImageCallBackEx_NET(cbImage2, (IntPtr)0);
                }
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                if (cameraArr2[0] != null && true == cameraArr2[0].IsOpen)
                {
                    cameraArr2[0].StreamGrabber.ImageGrabbed += OnImageGrabbed_1;
                }

                if (cameraArr2[1] != null && true == cameraArr2[1].IsOpen)
                {
                    cameraArr2[1].StreamGrabber.ImageGrabbed += OnImageGrabbed_2;
                }
            }
        }
        //关闭设备
        private void BnClose_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                hIKVisionCamera.MultiDestroyCamera();
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 释放相机
                baslerCamera.MultiDestroyCamera();             
            }
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

        private void BnStartGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                // 开始Grab
                if (rbCamera1.Checked)
                {
                    cameraArr1[0].MV_CC_StartGrabbing_NET();
                    lblCam1.BackColor = Color.Green;
                }
                else if (rbCamera2.Checked && cameraUsingNum > 1)
                {
                    cameraArr1[1].MV_CC_StartGrabbing_NET();
                    lblCam2.BackColor = Color.Green;
                }
                // 控件操作
                SetCtrlWhenStartGrab();

            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 开始Grab
                if (rbCamera1.Checked)
                {
                    // 连续采集模式
                    if(false == cameraArr2[0].StreamGrabber.IsGrabbing)
                    {
                        cameraArr2[0].Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                        cameraArr2[0].StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                        lblCam1.BackColor = Color.Green;
                    } 
                }

                else if (rbCamera2.Checked && cameraUsingNum > 1)
                {
                    if(false == cameraArr2[1].StreamGrabber.IsGrabbing)
                    {
                        cameraArr2[1].Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                        cameraArr2[1].StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                        lblCam2.BackColor = Color.Green;
                    }
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
                // 1号相机被选中
                if (rbCamera1.Checked)
                {
                    cameraArr1[0].MV_CC_StopGrabbing_NET();
                    lblCam1.BackColor = Color.Yellow;
                }
                // 2号相机被选中
                else if (rbCamera2.Checked && cameraUsingNum > 1)
                {
                    cameraArr1[1].MV_CC_StopGrabbing_NET();
                    lblCam2.BackColor = Color.Yellow;
                }
                SetCtrlWhenStopGrab();
            }
            else if ((chooseBasler) && (!chooseHIK))
            { 
                // 1号相机被选中
                if (rbCamera1.Checked)
                {
                    cameraArr2[0].StreamGrabber.Stop();
                    lblCam1.BackColor = Color.Yellow;
                }
                // 2号相机被选中
                else if (rbCamera2.Checked && cameraUsingNum > 1)
                {
                    cameraArr2[1].StreamGrabber.Stop();
                    lblCam2.BackColor = Color.Yellow;
                }
                SetCtrlWhenStopGrab();
            }
        }

        private void OnImageGrabbed_1(Object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                IGrabResult grabResult = e.GrabResult;
                if (grabResult.IsValid)
                {
                    // 四通道RGBA
                    Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                    // 锁定位图的位
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    // 将指针放置到位图的缓冲区
                    converter.OutputPixelFormat = PixelType.BGRA8packed;
                    IntPtr ptrBmp = bmpData.Scan0;
                    converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                    bitmap.UnlockBits(bmpData);
                    DeepLearning deepLearning = new DeepLearning();
                    if (isInference1) { bitmap = deepLearning.Inference(model1,modelType, bitmap); }
                    if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                    {
                        UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                        pictureBox1.BeginInvoke(update);
                    }
                    else { pictureBox1.Image = bitmap; }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("CAM:{0}！\n{1}", "CAM1", ex.Message));
            }
            finally
            {
                e.DisposeGrabResultIfClone();
            }
        }

        private void OnImageGrabbed_2(Object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                IGrabResult grabResult = e.GrabResult;
                if (grabResult.IsValid)
                {
                    // 四通道RGBA
                    Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                    // 锁定位图的位
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    // 将指针放置到位图的缓冲区
                    converter.OutputPixelFormat = PixelType.BGRA8packed;
                    IntPtr ptrBmp = bmpData.Scan0;
                    converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                    bitmap.UnlockBits(bmpData);
                    DeepLearning deepLearning = new DeepLearning();
                    if (isInference2) { bitmap = deepLearning.Inference(model2, modelType, bitmap); }
                    if (pictureBox2.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                    {
                        UpdateUI update = delegate { pictureBox2.Image = bitmap; };
                        pictureBox2.BeginInvoke(update);
                    }
                    else { pictureBox2.Image = bitmap; }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("CAM:{0}！\n{1}", "CAM2", ex.Message));
            }
            finally
            {
                e.DisposeGrabResultIfClone();
            }
        }

        private void ImageCallBack_1(IntPtr m_BufForDriver, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int nRet = cameraArr1[0].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("Get PayloadSize failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            UInt32 nPayloadSize = stParam.nCurValue;
            if (nPayloadSize > m_nBufSizeForDriver)
            {
                if (m_BufForDriver != IntPtr.Zero)
                {
                    Marshal.Release(m_BufForDriver);
                }
                m_nBufSizeForDriver = nPayloadSize;
                m_BufForDriver = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver);
            }
            if (m_BufForDriver == IntPtr.Zero)
            {
                MessageBox.Show("采集失败，请重新连接设备", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 将海康数据类型转为Mat
            nRet = cameraArr1[0].MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref pFrameInfo, 1000); // m_BufForDriver为图像数据接收指针
            //pTemp = m_BufForDriver;
            byte[] byteImage = new byte[pFrameInfo.nHeight * pFrameInfo.nWidth];
            Marshal.Copy(m_BufForDriver, byteImage, 0, pFrameInfo.nHeight * pFrameInfo.nWidth);
            Mat matImage = new Mat(pFrameInfo.nHeight, pFrameInfo.nWidth, MatType.CV_8UC1, byteImage);
            // 单通道图像转为三通道
            Mat matImageNew = new Mat();
            Cv2.CvtColor(matImage, matImageNew, ColorConversionCodes.GRAY2RGB);
            Bitmap bitmap = matImageNew.ToBitmap();  // Mat转为Bitmap                             
            DeepLearning deepLearning = new DeepLearning();
            if (isInference1) { bitmap = deepLearning.Inference(model1, modelType, bitmap); }// 是否进行推理
            if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
            {
                UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                pictureBox1.BeginInvoke(update);
            }
            else { pictureBox1.Image = bitmap; }

        }

        private void ImageCallBack_2(IntPtr m_BufForDriver, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int nRet = cameraArr1[0].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("Get PayloadSize failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            UInt32 nPayloadSize = stParam.nCurValue;
            if (nPayloadSize > m_nBufSizeForDriver)
            {
                if (m_BufForDriver != IntPtr.Zero)
                {
                    Marshal.Release(m_BufForDriver);
                }
                m_nBufSizeForDriver = nPayloadSize;
                m_BufForDriver = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver);
            }
            if (m_BufForDriver == IntPtr.Zero)
            {
                MessageBox.Show("采集失败，请重新连接设备", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 将海康数据类型转为Mat
            nRet = cameraArr1[0].MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref pFrameInfo, 1000); // m_BufForDriver为图像数据接收指针
            //pTemp = m_BufForDriver;
            byte[] byteImage = new byte[pFrameInfo.nHeight * pFrameInfo.nWidth];
            Marshal.Copy(m_BufForDriver, byteImage, 0, pFrameInfo.nHeight * pFrameInfo.nWidth);
            Mat matImage = new Mat(pFrameInfo.nHeight, pFrameInfo.nWidth, MatType.CV_8UC1, byteImage);
            // 单通道图像转为三通道
            Mat matImageNew = new Mat();
            Cv2.CvtColor(matImage, matImageNew, ColorConversionCodes.GRAY2RGB);
            Bitmap bitmap = matImageNew.ToBitmap();  // Mat转为Bitmap
                                                     // 是否进行推理
            DeepLearning deepLearning = new DeepLearning();
            if (isInference2) { bitmap = deepLearning.Inference(model2, modelType, bitmap); }
            if (pictureBox2.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
            {
                UpdateUI update = delegate { pictureBox2.Image = bitmap; };
                pictureBox2.BeginInvoke(update);
            }
            else { pictureBox2.Image = bitmap; }

        }
        // 获取参数
        private void BnGetParam_Click(object sender, EventArgs e)
        {
            // 参数
            string gain = null;
            string exposure = null;
            if ((chooseHIK) && (!chooseBasler))
            {
                if (rbCamera1.Checked)
                    // 获取参数
                    hIKVisionCamera.GetParam(ref gain, ref exposure, cameraArr1[0]);
                else if (rbCamera2.Checked && cameraUsingNum > 1)
                    // 获取参数
                    hIKVisionCamera.GetParam(ref gain, ref exposure, cameraArr1[1]);
                // 显示
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                if(rbCamera1.Checked)
                    // 获取参数
                    baslerCamera.GetParam(ref gain, ref exposure, cameraArr2[0]);
                else if(rbCamera2.Checked && cameraUsingNum > 1)
                    // 获取参数
                    baslerCamera.GetParam(ref gain, ref exposure, cameraArr2[1]);
                // 显示
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
                float exposure = float.Parse(tbExposure.Text);
                float gain = float.Parse(tbGain.Text);
                if (rbCamera1.Checked)
                    hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, cameraArr1[0]);
                else if (rbCamera2.Checked && cameraUsingNum > 1)
                    hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, cameraArr1[1]);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    long exposure = long.Parse(tbExposure.Text);
                    long gain = long.Parse(tbGain.Text);
                    if (rbCamera1.Checked)
                        baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, cameraArr2[0]);
                    else if (rbCamera2.Checked && cameraUsingNum > 1)
                        baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, cameraArr2[1]);
                }
                catch
                {
                    MessageBox.Show("请检查输入的参数值或所选相机序号是否正确！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            }
        }

        // 加载模型
        private void BnLoadModel_Click(object sender, EventArgs e)
        {

            FolderBrowserDialog fileDialog = new FolderBrowserDialog();
            fileDialog.Description = "请选择模型路径";
            fileDialog.ShowNewFolderButton = false;
            if (modelPath != "")
            {
                fileDialog.SelectedPath = modelPath;
            }
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                modelPath = fileDialog.SelectedPath;
                MessageBox.Show("已选择模型路径:" + modelPath, "选择文件提示", MessageBoxButtons.OK, MessageBoxIcon.Information);


                IntPtr a = new IntPtr(modelType);
                if (rbCamera1.Checked)
                { 
                    model1 = CreatePaddlexModel(ref modelType, modelPath, useGPU, useTrt, useMkl, mklThreadNum, gpuID, key, useIrOptim);
                    switch (modelType)
                    {
                        case 0: tbModeltype.Text = "0：图像分类"; break;
                        case 1: tbModeltype.Text = "1：目标检测"; break;
                        case 2: tbModeltype.Text = "2：语义分割"; break;
                    }
                }
                else if (rbCamera2.Checked && cameraUsingNum > 1)
                {
                    model2 = CreatePaddlexModel(ref modelType, modelPath, useGPU, useTrt, useMkl, mklThreadNum, gpuID, key, useIrOptim);
                    switch (modelType)
                    {
                        case 0: tbModeltype.Text = "0：图像分类"; break;
                        case 1: tbModeltype.Text = "1：目标检测"; break;
                        case 2: tbModeltype.Text = "2：语义分割"; break;
                    }
                }
                bnStartDetection.Enabled = true;
                bnStopDetection.Enabled = true;
                bnThreshold.Enabled = true;
            }
        }

        // 将Btimap类转换为byte[]类函数
        public static byte[] GetbyteData(Bitmap bmp)
        {
            BitmapData bmpData = null;
            bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int numbytes = bmpData.Stride * bmpData.Height;
            byte[] byteData = new byte[numbytes];
            IntPtr ptr = bmpData.Scan0;

            Marshal.Copy(ptr, byteData, 0, numbytes);

            return byteData;
        }

        private void BnStartDetection_Click(object sender, EventArgs e)
        {  
            if (rbCamera1.Checked)
            {
                isInference1 = true;
                lblCam1.BackColor = Color.Blue;  // 蓝色表示实施推理
            }
            else if (rbCamera2.Checked)
            {
                isInference2 = true;
                lblCam2.BackColor = Color.Blue;
            }         
        }

        private void BnStopDetection_Click(object sender, EventArgs e)
        {

            if (rbCamera1.Checked)
            {
                isInference1 = false;
                lblCam1.BackColor = Color.Green;  // 蓝色表示实施推理
            }
            else if (rbCamera2.Checked)
            {
                isInference2 = false;
                lblCam2.BackColor = Color.Green;
            }
        }

        private void BnThreshold_Click(object sender, EventArgs e)
        {
            if (rbCamera1.Checked)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Title = "请选择文件";
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = fileDialog.FileName;
                    System.Diagnostics.Process.Start(path);
                    MessageBox.Show("调整阈值后，请重新加载模型！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (rbCamera2.Checked)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Title = "请选择文件";
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = fileDialog.FileName;
                    System.Diagnostics.Process.Start(path);
                    MessageBox.Show("调整阈值后，请重新加载模型！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BnSaveImage_Click(object sender, EventArgs e)
        {

        }

        private void SingleCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            BnClose_Click(sender, e);
            System.Environment.Exit(0);
        }

        class DeepLearning
        {
            bool visualize = false;  
            // 目标物种类，需根据实际情况修改！
            string[] category = { "bocai", "changqiezi", "hongxiancai", "huluobo", "xihongshi", "xilanhua" };
            // 推理
            public Bitmap Inference(IntPtr model, int modelType, Bitmap bmp)
            {
                Bitmap bmpNew = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), bmp.PixelFormat);
                Console.WriteLine(bmpNew.PixelFormat);
                Bitmap resultShow;
                Mat img = BitmapConverter.ToMat(bmpNew);

                int channel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                byte[] source = GetbyteData(bmp);

                if (modelType == 0)
                {
                    bool res = PaddlexClsPredict(model, source, bmp.Height, bmp.Width, channel, out int categoryID, out float score);
                    if (res)
                    {
                        Scalar color = new Scalar(0, 0, 255);
                        string text = category[categoryID] + ": " + score.ToString("f2");
                        OpenCvSharp.Size labelSize = Cv2.GetTextSize(text, HersheyFonts.HersheySimplex, 1, 1, out int baseline);
                        Cv2.Rectangle(img, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(labelSize.Width + 60, labelSize.Height + 20), color, -1, LineTypes.AntiAlias, 0);
                        Cv2.PutText(img, text, new OpenCvSharp.Point(30, 30), HersheyFonts.HersheySimplex, 1, Scalar.White);
                    }
                }

                else if (modelType == 1)
                {
                    int max_box = 10;
                    float[] result = new float[max_box * 6 + 1];
                    bool res = PaddlexDetPredict(model, source, bmp.Height, bmp.Width, channel, max_box, result, visualize);
                    if (res)
                    {
                        Scalar color = new Scalar(255, 0, 0);
                        for (int i = 0; i < result[0]; i++)
                        {
                            Rect rect = new Rect((int)result[6 * i + 3], (int)result[6 * i + 4], (int)result[6 * i + 5], (int)result[6 * i + 6]);
                            Cv2.Rectangle(img, rect, color, 2, LineTypes.AntiAlias);
                            string text = category[(int)result[6 * i + 1]] + ": " + result[6 * i + 2].ToString("f2");
                            Cv2.PutText(img, text, new OpenCvSharp.Point((int)result[6 * i + 3], (int)result[6 * i + 4] + 25), HersheyFonts.HersheyPlain, 2, Scalar.White);
                        }
                    }
                }

                resultShow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format24bppRgb, img.Data);
                System.GC.Collect();
                return resultShow;
            }
        }
    }
}
