using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MvCamCtrl.NET;  // 海康相机
using Basler.Pylon; // basler 相机
using BaslerDeviceSource; //Basler设备的操作

using System.Runtime.InteropServices;
using System.Threading;


namespace PaddleXCsharp
{
    public partial class SingleCamera : Form
    {
        /* ================================= 海康相机 ================================= */
        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private MyCamera h_MyCamera = new MyCamera();
        bool m_bGrabbing = false;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        Thread m_hReceiveThread = null;
        // 用于从驱动获取图像的缓存
        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver;
        private static Object BufForDriverLock = new Object();

        /* ================================= basler相机 ================================= */
        //Camera b_Mycamera = null;
        //private static Version sfnc2_0_0 = new Version(2, 0, 0);
        //PixelDataConverter pxConvert = new PixelDataConverter();
        //Bitmap image = null;
        //Thread b_hReceiveThread = null;
        //Stopwatch stopWatch = new Stopwatch();

        baslerOperator baslerCamera = new baslerOperator();

        /* ================================= 公共 ================================= */
        // 标志位
        bool m_HIKCamera = false;           // 海康相机打开标志
        bool m_BaslerCamera = false;        // Basler相机打开标志

        // 用于保存图像的缓存
        UInt32 BufSizeForSaveImage = 0;
        IntPtr BufForSaveImage;
        /* ================================= inference ================================= */
        //
        string modelPath = "";
        bool use_gpu = true;
        string run_mode = "fluid";
        int gpu_id = 0;
        double threshold = 0.5;
        bool run_benchmark = false;
        string outPath = "E:/PaddleX_C#/PaddleCsharp/out";
        string imagePath = "E:/PaddleX_C#/PaddleCsharp/Image/detection.jpg";

        //[DllImport("paddlex_inference.dll", EntryPoint = "paddlex_init", CharSet = CharSet.Ansi)]
        //static extern int paddlex_init(string model_dir, bool use_gpu = false, bool use_mkl = true,
        //                               int mkl_thread_num = 4, int gpu_id = 0, string key = "",
        //                               bool use_ir_optim = true); 
        //[DllImport("segmenter.dll", EntryPoint = "LoadModel", SetLastError = true, CharSet = CharSet.Ansi)]
        //static extern IntPtr LoadModel(byte[] input, int height, int width);  //out IntPtr seg_res


        public SingleCamera()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        private void bnEnum_Click(object sender, EventArgs e)
        {
            string type = cameraType.Text;
            if (type == "")
            {
                MessageBox.Show("请在初始化界面中选定相机类型", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (type == "海康相机")
            {
                m_HIKCamera = true;
                m_BaslerCamera = false;
                DeviceListAcq();
            }
            else if (type == "Basler相机")
            {
                m_HIKCamera = false;
                m_BaslerCamera = true;
                DeviceListAcq();
            }
        }

        private void DeviceListAcq()
        {
            cbDeviceList.Items.Clear();
            System.GC.Collect();
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                // 创建设备列表
                m_stDeviceList.nDeviceNum = 0;
                int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_stDeviceList);
                if (0 != nRet)
                {
                    MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 在窗体列表中显示设备名
                for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
                {
                    MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                    {
                        MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                        if (gigeInfo.chUserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("GEV: " + gigeInfo.chUserDefinedName);
                        }
                        else
                        {
                            cbDeviceList.Items.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName);
                        }
                    }
                    else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                    {
                        MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                        if (usbInfo.chUserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("U3V: " + usbInfo.chUserDefinedName);
                        }
                        else
                        {
                            cbDeviceList.Items.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName);
                        }
                    }
                }
                // 选择第一项
                if (m_stDeviceList.nDeviceNum != 0)
                {
                    cbDeviceList.SelectedIndex = 0;
                }
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                baslerCamera.CameraImageEvent += Camera_CameraImageEvent;
                int cameraNum = baslerCamera.CameraNum();
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

                if (cameraNum != 0)
                {
                    cbDeviceList.SelectedIndex = 0;
                }
                /*
                try
                {
                    b_Mycamera = new Camera();
                    int cameraNumber = CameraFinder.Enumerate().Count;
                    if (cameraNumber > 0)
                    {
                        // 在窗体列表中显示设备名
                        for (int i = 0; i < cameraNumber; i++)
                        {
                            string deviceType = b_Mycamera.CameraInfo[CameraInfoKey.DeviceType];
                            if (deviceType == "BaslerUsb")
                            {
                                cbDeviceList.Items.Add("U3V: Basler " + b_Mycamera.CameraInfo[CameraInfoKey.ModelName]);
                            }
                            else if (deviceType == "BaslerGigE")
                            {
                                cbDeviceList.Items.Add("GEV: Basler " + b_Mycamera.CameraInfo[CameraInfoKey.ModelName]);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // 选择第一项
                    if (cameraNumber != 0)
                    {
                        cbDeviceList.SelectedIndex = 0;
                    }
                }
                catch
                {
                    MessageBox.Show("未检测到Basler相机，请查看是否连接！", "Error",  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                */
            }
        }
        private void Camera_CameraImageEvent(Bitmap bmp)
        {
            pictureBox1.Invoke(new EventHandler(delegate
            {
                Bitmap old = pictureBox1.Image as Bitmap;// as Bitmap;
                pictureBox1.Image = bmp;
                if (old != null)
                    old.Dispose();
            }));
        }

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

            bnChooseModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;

        }

        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;

            bnChooseModel.Enabled = true;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = true;
        }

        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;

            bnChooseModel.Enabled = false;
            bnStartDetection.Enabled = false;
            bnStopDetection.Enabled = false;
            bnSaveImage.Enabled = false;
        }
        //public void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        //{
        //    if (InvokeRequired)
        //    {
        //        BeginInvoke(new EventHandler<ImageGrabbedEventArgs>(OnImageGrabbed), sender, e.Clone());
        //        return;
        //    }
        //    // 获取相片
        //    IGrabResult grabResult = e.GrabResult;

        //    //如获取相片为true
        //    if (grabResult.IsValid)
        //    {
        //        if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 33)
        //        {
        //            stopWatch.Restart();
        //            //把抓取到的图片放在bitmap
        //            image = GrabResult2Bmp(grabResult);

        //            // 临时bitmap用于释放上一个bitmap
        //            Bitmap bitmapOld = pictureBox1.Image as Bitmap;

        //            //把抓取图像放在picturebox显示
        //            pictureBox1.Image = image;
        //            //如果想要保存图片，savepath是保存路径，比如public string savePath = "D:/download/0.jpg";
        //            //bitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);

        //            if (bitmapOld != null)
        //            {
        //                // 释放bitmap
        //                bitmapOld.Dispose();
        //            }
        //        }
        //    }
        //}

        //private void OnGrabStarted(Object sender, EventArgs e)
        //{
        //    if (InvokeRequired)
        //    {
        //        BeginInvoke(new EventHandler<EventArgs>(OnGrabStarted), sender, e);
        //        return;
        //    }

        //    //停止时测量，并将事件设置为0
        //    stopWatch.Reset();

        //}

        private void bnOpen_Click(object sender, EventArgs e)
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
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
                if (null == h_MyCamera)
                {
                    h_MyCamera = new MyCamera();
                    if (null == h_MyCamera)
                    {
                        return;
                    }
                }

                int nRet = h_MyCamera.MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }

                nRet = h_MyCamera.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    h_MyCamera.MV_CC_DestroyDevice_NET();
                    MessageBox.Show("设备打开失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = h_MyCamera.MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        nRet = h_MyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
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
                h_MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
                h_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

                // 获取参数
                BnGetParam_Click(null, null);

                // 控件操作
                SetCtrlWhenOpen();
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                try
                {
                    // 初始化
                    baslerCamera.CameraInit();

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

                //// 采集模式为连续采集
                //b_Mycamera.CameraOpened += Configuration.AcquireContinuous;

                //b_Mycamera.Open();
                //b_Mycamera.StreamGrabber.GrabStarted += OnGrabStarted;
                //b_Mycamera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                ////b_Mycamera.Parameters[PLTransportLayer.HeartbeatTimeout].TrySetValue(1000, IntegerValueCorrection.Nearest);

                //// 分配抓取缓冲区大小
                ////b_Mycamera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(5);              
            }
        }

        private void BnClose_Click(object sender, EventArgs e)
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                // 取流标志位清零
                if (m_bGrabbing == true)
                {
                    m_bGrabbing = false;
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

                // 关闭设备
                h_MyCamera.MV_CC_CloseDevice_NET();
                h_MyCamera.MV_CC_DestroyDevice_NET();

                // 控件操作
                SetCtrlWhenClose();
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                // 释放相机
                baslerCamera.DestroyCamera();

                // 控件操作
                SetCtrlWhenClose();

                //if (m_BufForDriver != IntPtr.Zero)
                //{
                //    Marshal.Release(m_BufForDriver);
                //}
                //if (BufForSaveImage != IntPtr.Zero)
                //{
                //    Marshal.Release(BufForSaveImage);
                //}

                //b_Mycamera.Close();


            }
        }
        //private Bitmap GrabResult2Bmp(IGrabResult grabResult)
        //{
        //    // 图像格式转换
        //    Bitmap img = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
        //    BitmapData bmpData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadWrite, img.PixelFormat);
        //    pxConvert.OutputPixelFormat = PixelType.BGRA8packed;
        //    IntPtr bmpIntpr = bmpData.Scan0;
        //    pxConvert.Convert(bmpIntpr, bmpData.Stride * img.Height, grabResult);
        //    img.UnlockBits(bmpData);
        //    return img;
        //}
        public void GrabThreadProcess()
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                int nRet = h_MyCamera.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Get PayloadSize failed！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    return;
                }

                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
                MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();

                while (m_bGrabbing)
                {
                    lock (BufForDriverLock)
                    {
                        nRet = h_MyCamera.MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref stFrameInfo, 1000);
                        if (nRet == MyCamera.MV_OK)
                        {
                            m_stFrameInfo = stFrameInfo;
                        }
                    }

                    if (nRet == MyCamera.MV_OK)
                    {
                        if (RemoveCustomPixelFormats(stFrameInfo.enPixelType))
                        {
                            continue;
                        }
                        stDisplayInfo.hWnd = pictureBox1.Handle;
                        stDisplayInfo.pData = m_BufForDriver;
                        stDisplayInfo.nDataLen = stFrameInfo.nFrameLen;
                        stDisplayInfo.nWidth = stFrameInfo.nWidth;
                        stDisplayInfo.nHeight = stFrameInfo.nHeight;
                        stDisplayInfo.enPixelType = stFrameInfo.enPixelType;
                        h_MyCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                    }
                }
            }
            //else if((m_BaslerCamera) && (!m_HIKCamera))
            //{
            //    try
            //    {
            //        b_Mycamera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
            //        //b_Mycamera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            //        b_Mycamera.StreamGrabber.Start();
            //        while (b_Mycamera.StreamGrabber.IsGrabbing)
            //        {
            //            IGrabResult grabResult = b_Mycamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            //            if (grabResult.GrabSucceeded)
            //            {
            //                image = GrabResult2Bmp(grabResult);
            //                Bitmap bitmapOld = pictureBox1.Image as Bitmap;
            //                pictureBox1.Image = image;

            //                if (bitmapOld != null)
            //                {
            //                    // 释放bitmap
            //                    bitmapOld.Dispose();
            //                }
            //            }
            //        }
            //    }
            //    catch
            //    {
            //        MessageBox.Show("采集错误！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }

            //}

        }

        private void BnStartGrab_Click(object sender, EventArgs e)
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                // 标志位置位true
                m_bGrabbing = true;
                m_hReceiveThread = new Thread(GrabThreadProcess);
                m_hReceiveThread.Start();

                m_stFrameInfo.nFrameLen = 0; // 取流之前先清除帧长度
                m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
                // 开始采集
                int nRet = h_MyCamera.MV_CC_StartGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    m_bGrabbing = false;
                    m_hReceiveThread.Join();
                    MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 控件操作
                SetCtrlWhenStartGrab();
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                try
                {
                    // 连续拍摄
                    baslerCamera.KeepShot();

                    // 控件操作
                    SetCtrlWhenStartGrab();
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //try
                //{
                //    b_Mycamera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                //    b_Mycamera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                //    stopWatch.Restart();
                //}
                //catch
                //{
                //    MessageBox.Show("采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //}



            }
        }
        private void BnStopGrab_Click(object sender, EventArgs e)
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                // 标志位设为false
                m_bGrabbing = false;
                m_hReceiveThread.Join();
                int nRet = h_MyCamera.MV_CC_StopGrabbing_NET();
                if (nRet != MyCamera.MV_OK)
                {
                    MessageBox.Show("停止采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                SetCtrlWhenStopGrab();
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                // 停止采集
                baslerCamera.Stop();
                SetCtrlWhenStopGrab();
            }
        }

        private void BnGetParam_Click(object sender, EventArgs e)
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
                int nRet = h_MyCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
                if (MyCamera.MV_OK == nRet)
                {
                    tbExposure.Text = stParam.fCurValue.ToString("F1");
                }

                nRet = h_MyCamera.MV_CC_GetFloatValue_NET("Gain", ref stParam);
                if (MyCamera.MV_OK == nRet)
                {
                    tbGain.Text = stParam.fCurValue.ToString("F1");
                }
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                string gain = null;
                string exposure = null;
                baslerCamera.GetParam(ref gain, ref exposure);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
            }

        }

        private void BnSetParam_Click(object sender, EventArgs e)
        {
            if ((m_HIKCamera) && (!m_BaslerCamera))
            {
                try
                {
                    float.Parse(tbExposure.Text);
                    float.Parse(tbGain.Text);
                }
                catch
                {
                    MessageBox.Show("请输入正确的值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                h_MyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                int nRet = h_MyCamera.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(tbExposure.Text));
                if (nRet != MyCamera.MV_OK)
                {
                    MessageBox.Show("设置曝光时间失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                h_MyCamera.MV_CC_SetEnumValue_NET("GainAuto", 0);
                nRet = h_MyCamera.MV_CC_SetFloatValue_NET("Gain", float.Parse(tbGain.Text));
                if (nRet != MyCamera.MV_OK)
                {
                    MessageBox.Show("设置增益失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if ((m_BaslerCamera) && (!m_HIKCamera))
            {
                string gainshow = null;
                string exposureshow = null;

                long exposure = long.Parse(tbExposure.Text);
                long gain = long.Parse(tbGain.Text);
                baslerCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow);
                // 显示真实值
                tbGain.Text = gainshow;
                tbExposure.Text = exposureshow;

            }
        }
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

        private void BnChooseModel_Click(object sender, EventArgs e)
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

                bnStartDetection.Enabled = true;
                bnStopDetection.Enabled = true;
                bnSaveImage.Enabled = true;
            }
        }
        private void Inference()
        {

        }

        private void BnStartDetection_Click(object sender, EventArgs e)
        {
            bnStopDetection.Enabled = true;
            Inference();
        }
        private void BnSaveImage_Click(object sender, EventArgs e)
        {

        }
        
        private void BnStopDetection_Click(object sender, EventArgs e)
        {

        }

        private void SingleCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            h_MyCamera.MV_CC_CloseDevice_NET();
            h_MyCamera.MV_CC_DestroyDevice_NET();
            baslerCamera.DestroyCamera();
            System.Environment.Exit(0);
        }
    }
}
