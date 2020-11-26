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
using System.IO;
namespace PaddleXCsharp
{
    public partial class SingleCamera : Form
    {
        private delegate void UpdateUI();  // 声明委托

        /* ================================= basler相机 ================================= */
        BaslerCamera baslerCamera = new BaslerCamera();
        Camera camera1 = null;
        Thread baslerGrabThread = null;
        private PixelDataConverter converter = new PixelDataConverter(); // basler里用于将相机采集的图像转换成位图

        bool balserCanGrab = false;    // 控制相机是否Grab
        bool chooseBasler = false;     // Basler相机打开标志

        /* ================================= 海康相机 ================================= */
        HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();
        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        MyCamera camera2 = null;
        bool hikCanGrab = false;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        Thread m_hReceiveThread = null;
        // 用于从驱动获取图像的缓存
        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver;

        // ch:用于保存图像的缓存 | en:Buffer for saving image
        UInt32 m_nBufSizeForSaveImage = 0;
        IntPtr m_BufForSaveImage;


        private static Object BufForDriverLock = new Object();

        //int stride;

        bool chooseHIK = false;           // 海康相机打开标志



        IntPtr BufForSaveImage;


        /* ================================= inference ================================= */
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

        bool isInference = false;  // 是否进行推理   
        IntPtr model; // 模型

        // 定义CreatePaddlexModel接口
        [DllImport("paddlex_inference.dll", EntryPoint = "CreatePaddlexModel", CharSet = CharSet.Ansi)]
        static extern IntPtr CreatePaddlexModel(ref int modelType, string modelPath, bool useGPU, bool useTrt, bool useMkl, int mklThreadNum, int gpuID, string key, bool useIrOptim);

        // 定义PaddlexDetPredict接口
        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexDetPredict", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr PaddlexDetPredict(IntPtr model, byte[] image, int height, int width, int channels,IntPtr[] result);
        #endregion

        public SingleCamera()
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

        // 枚举相机
        private void DeviceListAcq()
        {
            // 清空列表
            cbDeviceList.Items.Clear();
            System.GC.Collect();

            // 枚举海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 相机数量
                    uint cameraNum = hIKVisionCamera.CameraNum();
                    // 枚举相机
                    List<string> items = hIKVisionCamera.EnumDevices();
                    for (int i = 0; i < cameraNum; i++)
                    {
                        cbDeviceList.Items.Add(items[i]);
                    }
                    // 选择第一项
                    if (cameraNum != 0)
                    {
                        cbDeviceList.SelectedIndex = 0;
                    }
                }
                catch
                {
                    MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }             
            }

            // 枚举basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 返回相机数量
                    int cameraNum = baslerCamera.CameraNum();
                    // 枚举相机
                    List<ICameraInfo> items = baslerCamera.CameraEnum();
                    for (int i = 0; i < cameraNum; i++)
                    {
                        if (items[i][CameraInfoKey.DeviceType] == "BaslerGigE")
                        {
                            cbDeviceList.Items.Add("GEV: Basler " + items[i][CameraInfoKey.ModelName]);
                        }
                        else if (items[i][CameraInfoKey.DeviceType] == "BaslerUsb")
                        {
                            cbDeviceList.Items.Add("U3V: Basler " + items[i][CameraInfoKey.ModelName]);
                        }
                    }
                    // 选择第一项
                    if (cameraNum != 0)
                    {
                        cbDeviceList.SelectedIndex = 0;
                    }
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
            bnStopGrab.Enabled = false;
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
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;
            bnLoadModel.Enabled = true;
            bnStartDetection.Enabled = true;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = true;
        }
        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            bnLoadModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;
        }
        #endregion

        // 启动设备
        private void BnOpen_Click(object sender, EventArgs e)
        {
            // 启动海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    camera2 = hIKVisionCamera.CameraInit(cbDeviceList.SelectedIndex);

                    // 获取参数
                    BnGetParam_Click(null, null);

                    // 控件操作
                    SetCtrlWhenOpen();
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                /*
                if (m_stDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
                {
                    MessageBox.Show("未找到设备，请检查！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 获取选择的设备信息
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                                  typeof(MyCamera.MV_CC_DEVICE_INFO));
                // 打开设备
                if (null == camera2)
                {
                    camera2 = new MyCamera();
                    if (null == camera2)
                    {
                        return;
                    }
                }

                int nRet = camera2.MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }

                nRet = camera2.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    camera2.MV_CC_DestroyDevice_NET();
                    MessageBox.Show("设备打开失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = camera2.MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        nRet = camera2.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                        if (nRet != MyCamera.MV_OK)
                        {
                            MessageBox.Show("Set Packet Size failed！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Get Packet Size failed！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                // 设置连续采集模式
                camera2.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
                camera2.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

                // 相机初始化
                //hIKVisionCamera.CameraInit(cbDeviceList.SelectedIndex);

                */
            }
            
            // 启动basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 初始化所选相机
                    camera1 = baslerCamera.CameraInit(cbDeviceList.SelectedIndex);
                    // 获取参数
                    BnGetParam_Click(null, null);
                    // 控件操作
                    SetCtrlWhenOpen();
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                } 
            }
        }

        // 关闭设备
        private void BnClose_Click(object sender, EventArgs e)
        {
            // 关闭海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 取流标志位清零
                    if (hikCanGrab == true)
                    {
                        hikCanGrab = false;
                        m_hReceiveThread.Join();
                    }
                    if (m_BufForDriver != IntPtr.Zero)
                    {
                        Marshal.Release(m_BufForDriver);
                    }
                    if (BufForSaveImage != IntPtr.Zero)
                    {
                        Marshal.Release(BufForSaveImage);
                    }
                    // 释放相机
                    hIKVisionCamera.DestroyCamera();
                    // 控件操作
                    SetCtrlWhenClose();
                }     
                catch
                {
                    MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            // 关闭basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    if (balserCanGrab == true)
                    {
                        balserCanGrab = false;
                        baslerGrabThread.Join();
                    }
                    // 释放相机
                    baslerCamera.DestroyCamera();

                    // 控件操作
                    SetCtrlWhenClose();
                }
                catch
                {
                    MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        // 采集进程
        public void GrabThreadProcess()
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                camera2.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
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
                    return;
                }

                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体
                IntPtr pTemp = IntPtr.Zero;

                while (hikCanGrab)
                {
                    // 将海康数据类型转为Mat
                    int nRet = camera2.MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref stFrameInfo, 1000); // m_BufForDriver为图像数据接收指针
                    pTemp = m_BufForDriver;
                    byte[] byteImage = new byte[stFrameInfo.nHeight * stFrameInfo.nWidth];
                    Marshal.Copy(m_BufForDriver, byteImage, 0, stFrameInfo.nHeight * stFrameInfo.nWidth);
                    Mat matImage = new Mat(stFrameInfo.nHeight, stFrameInfo.nWidth, MatType.CV_8UC1, byteImage);
                    // 单通道图像转为三通道
                    Mat matImageNew = new Mat();
                    Cv2.CvtColor(matImage, matImageNew, ColorConversionCodes.GRAY2RGB);
                    Bitmap bitmap = matImageNew.ToBitmap();  // Mat转为Bitmap
                    // 是否进行推理
                    if (isInference) { bitmap = Inference(bitmap); }
                    if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                    {
                        UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                        pictureBox1.BeginInvoke(update);
                    }
                    else { pictureBox1.Image = bitmap; }
                }            
        }
            else if ((chooseBasler) && (!chooseHIK))
            {
                while (balserCanGrab)
                {
                    IGrabResult grabResult;
                    using (grabResult = camera1.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException))
                    {
                        if (grabResult.GrabSucceeded)
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
                            // 是否进行推理
                            if (isInference) { bitmap = Inference(bitmap); }
                            // 禁止跨线程直接访问控件，故invoke到主线程中
                            // 参考：https://bbs.csdn.net/topics/350050105
                            //       https://www.cnblogs.com/lky-learning/p/14025280.html
                            if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                            {
                                UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                                pictureBox1.BeginInvoke(update);
                            }
                            else { pictureBox1.Image = bitmap; }
                        }
                    }
                }
            }
        }
        
        // 开始采集
        private void BnStartGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 标志位置位true
                    hikCanGrab = true;
                    // 开始采集
                    hIKVisionCamera.StartGrabbing();
                    // 用线程更新显示
                    m_hReceiveThread = new Thread(GrabThreadProcess);
                    m_hReceiveThread.Start();
                    // 控件操作
                    SetCtrlWhenStartGrab();
                }
                catch
                {
                    hikCanGrab = false;
                    m_hReceiveThread.Join();
                    MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 标志符号
                    balserCanGrab = true;
                    // 开始Grab
                    baslerCamera.StartGrabbing();
                    // 用线程更新显示
                    baslerGrabThread = new Thread(GrabThreadProcess);
                    baslerGrabThread.Start();
                    // 控件操作
                    SetCtrlWhenStartGrab();
                }
                catch
                {
                    balserCanGrab = false;
                    baslerGrabThread.Join();
                    MessageBox.Show("开始采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
        }

        // 停止采集
        private void BnStopGrab_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {  
                    hikCanGrab = false;   // 标志位设为false
                    m_hReceiveThread.Join();  // 主线程阻塞，等待线程结束
                    hIKVisionCamera.StopGrabbing();  // 停止采集
                    SetCtrlWhenStopGrab();  // 控件操作
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                { 
                    balserCanGrab = false;  // 标志位设为false
                    baslerGrabThread.Join();  // 主线程阻塞，等待线程结束
                    baslerCamera.StopGrabbing();  // 停止采集
                    SetCtrlWhenStopGrab();  // 控件操作
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        #region 参数设置
        private void BnGetParam_Click(object sender, EventArgs e)
        {
            // 参数
            string gain = null;
            string exposure = null;
            if ((chooseHIK) && (!chooseBasler))
            {
                // 获取参数
                hIKVisionCamera.GetParam(ref gain, ref exposure);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // 获取参数
                baslerCamera.GetParam(ref gain, ref exposure);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }
        }
        private void BnSetParam_Click(object sender, EventArgs e)
        {
            string gainshow = null;
            string exposureshow = null;
            if ((chooseHIK) && (!chooseBasler))
            {
                float exposure = float.Parse(tbExposure.Text);
                float gain = float.Parse(tbGain.Text);
                hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                long exposure = long.Parse(tbExposure.Text);
                long gain = long.Parse(tbGain.Text);
                baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;
            }
        }
        #endregion

        // 去除自定义的像素格式
        private bool RemoveCustomPixelFormats(MyCamera.MvGvspPixelType enPixelFormat)
        {
            Int32 nResult = ((int)enPixelFormat) & (unchecked((Int32)0x80000000));
            if (0x80000000 == nResult)
            {
                return true;
            }
            else
            {
                return false;
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
                model = CreatePaddlexModel(ref modelType, modelPath, useGPU, useTrt, useMkl, mklThreadNum, gpuID, key, useIrOptim);

                bnStartDetection.Enabled = true;
                bnStopDetection.Enabled = true;
                bnSaveImage.Enabled = true;
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

        public static IntPtr getBytesPtrInt(int[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        // 推理
        private Bitmap Inference(Bitmap bmp)
        {           
            int channel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

            byte[] source = GetbyteData(bmp);
            IntPtr[] result = new IntPtr[100];
            //IntPtr result = Marshal.AllocHGlobal(24); // 结构体在使用时一定要分配空间(6*sizeof(float))

            IntPtr resultImage = PaddlexDetPredict(model, source, bmp.Height, bmp.Width, channel, result);

            //Marshal.WriteInt32(result, 24); // 向内存块里写入数值
            //Console.WriteLine("--category_id:{0}", Marshal.ReadInt32(result, 0));
            //Console.WriteLine("--score:{0}", Marshal.ReadInt32(result, 4)); //移动4个字节
            //Console.WriteLine("--coordinate1: " + Marshal.ReadInt32(result, 8));
            //Console.WriteLine("--coordinate2: " + Marshal.ReadInt32(result, 12));
            //Console.WriteLine("--coordinate3: " + Marshal.ReadInt32(result, 16));
            //Console.WriteLine("--coordinate4: " + Marshal.ReadInt32(result, 20));

            Bitmap resultShow;
            Mat img = new Mat(resultImage);
            switch (channel)
            {
                case 1:
                    resultShow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format8bppIndexed, img.Data);
                    break;
                case 2:
                    resultShow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format16bppGrayScale, img.Data);
                    break;
                case 3:
                    resultShow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format24bppRgb, img.Data);
                    break;
                default:
                    resultShow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format32bppArgb, img.Data);
                    break;
            }
            System.GC.Collect();
            return resultShow;
        }

        private void BnStartDetection_Click(object sender, EventArgs e)
        {
            bnStopDetection.Enabled = true;
            bnStartDetection.Enabled = false;
            isInference = true;
        }

        // 停止检测
        private void BnStopDetection_Click(object sender, EventArgs e)
        {
            bnStopDetection.Enabled = false;
            bnStartDetection.Enabled = true;
            isInference = false;
        }

        // 保存图片
        private void BnSaveImage_Click(object sender, EventArgs e)
        {

        }

        // 窗口关闭
        private void SingleCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            //BnClose_Click(sender, e);
            hIKVisionCamera.DestroyCamera();
            baslerCamera.DestroyCamera();
            System.Environment.Exit(0);
        }
    }
}
