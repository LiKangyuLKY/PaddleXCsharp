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
using System.IO;
namespace PaddleXCsharp
{
    public partial class SingleCamera : Form
    {
        /* ================================= 海康相机 ================================= */

        HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();
        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private MyCamera h_MyCamera = new MyCamera();
        bool m_bGrabbing = false;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        Thread m_hReceiveThread = null;
        // 用于从驱动获取图像的缓存
        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver;
        private static Object BufForDriverLock = new Object();

        private delegate void UpdateUI();

        int stride;
        Bitmap A = null;

        /* ================================= basler相机 ================================= */
        baslerCamera baslerCamera = new baslerCamera();
        Camera camera = null;
        Thread baslerGrabThread = null;
        private PixelDataConverter converter = new PixelDataConverter();

        // 控制相机Grab
        bool balserCanGrab = false;

        /* ================================= 公共 ================================= */
        // 标志位
        bool chooseHIK = false;           // 海康相机打开标志
        bool chooseBasler = false;        // Basler相机打开标志


        IntPtr BufForSaveImage;
        /* ================================= inference ================================= */

        #region 推理参数
        string modelPath = "";
        bool use_gpu = false;
        bool use_trt = false;
        bool use_mkl = true;
        int mkl_thread_num = 16;
        int gpu_id = 0;
        string key = "";

        bool use_ir_optim = false;
        bool is_inference = false;

        IntPtr model;
        string outPath = "E:/PaddleX_C#/PaddleCsharp/out";
        string imagePath = "E:/PaddleX_C#/PaddleCsharp/Image/detection.jpg";

        [DllImport("paddlex_inference.dll", EntryPoint = "CreatePaddlexModel", CharSet = CharSet.Ansi)]
        static extern IntPtr CreatePaddlexModel(ref int model_type,
                                    string model_dir,
                                    bool use_gpu = false,
                                    bool use_trt = false,
                                    bool use_mkl = true,
                                    int mkl_thread_num = 4,
                                    int gpu_id = 0,
                                    string key = "",
                                    bool use_ir_optim = true);

        [DllImport("paddlex_inference.dll", EntryPoint = "PaddlexDetPredict", CharSet = CharSet.Ansi)]
        static extern IntPtr PaddlexDetPredict(IntPtr model, byte[] image, int height, int width, int channels,IntPtr[] result);
        #endregion

        public SingleCamera()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        // 选择所使用相机的类型
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
                //uint cameraNum = hIKVisionCamera.CameraNum();
                //List<string> items = hIKVisionCamera.EnumDevices();
                //for (int i = 0; i < cameraNum; i++)
                //{
                //    cbDeviceList.Items.Add(items[i]);
                //}

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
                //if (cameraNum != 0)
                if (m_stDeviceList.nDeviceNum != 0)
                {
                    cbDeviceList.SelectedIndex = 0;
                }
            }

            // 枚举basler相机
            else if ((chooseBasler) && (!chooseHIK))
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
        }

        /*
        private void Camera_CameraImageEvent(Bitmap bmp)
        {
            pictureBox1.Invoke(new EventHandler(delegate
            {
                Bitmap old = pictureBox1.Image as Bitmap;// as Bitmap;
                if (is_inference)
                {

                    int stride;
                    byte[] source = GetBGRValues(bmp, out stride);

                    IntPtr[] result = new IntPtr[100];
                    IntPtr resultImage = PaddlexDetPredict(model, source, bmp.Height, bmp.Width, 3, result);

                    
                    Mat img = new Mat(resultImage);
                    Bitmap resultshow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), System.Drawing.Imaging.PixelFormat.Format24bppRgb, img.Data);

                    //Bitmap result = Inference(bmp);
                    bmp = resultshow;
                    //Inference(bmp);
                }
                pictureBox1.Image = bmp;
                if (old != null)
                    old.Dispose();             
            }));
        }
        */

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
        private void bnOpen_Click(object sender, EventArgs e)
        {
            // 启动海康相机
            if ((chooseHIK) && (!chooseBasler))
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

                // 相机初始化
                //hIKVisionCamera.CameraInit(cbDeviceList.SelectedIndex);

                // 获取参数
                BnGetParam_Click(null, null);

                // 控件操作
                SetCtrlWhenOpen();
            }
            // 启动basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 初始化所选相机
                    camera = baslerCamera.CameraInit(cbDeviceList.SelectedIndex);
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 获取参数
                BnGetParam_Click(null, null);

                // 控件操作
                SetCtrlWhenOpen();

            }
        }

        // 关闭设备
        private void BnClose_Click(object sender, EventArgs e)
        {
            // 关闭海康相机
            if ((chooseHIK) && (!chooseBasler))
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
            // 关闭basler相机
            else if ((chooseBasler) && (!chooseHIK))
            {
                if(balserCanGrab == true)
                {
                    balserCanGrab = false;
                    baslerGrabThread.Join();
                }
                // 释放相机
                baslerCamera.DestroyCamera();

                // 控件操作
                SetCtrlWhenClose();
            }
        }

        // 采集进程
        public void GrabThreadProcess()
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
                MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();

                while (m_bGrabbing)
                {
                    int nRet = h_MyCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        stDisplayInfo.hWnd = pictureBox1.Handle;
                        stDisplayInfo.pData = stFrameInfo.pBufAddr;
                        stDisplayInfo.nDataLen = stFrameInfo.stFrameInfo.nFrameLen;
                        stDisplayInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                        stDisplayInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                        stDisplayInfo.enPixelType = stFrameInfo.stFrameInfo.enPixelType;
                        if (RemoveCustomPixelFormats(stFrameInfo.stFrameInfo.enPixelType))
                        {
                            h_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                            continue;
                        }
                        else
                        {
                            h_MyCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                            h_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                        }
                    }
                }
            
        }
            else if ((chooseBasler) && (!chooseHIK))
            {
                // Check if the image can be displayed.
                while (balserCanGrab)
                {
                    IGrabResult grabResult;
                    using (grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException))
                    {
                        if (grabResult.GrabSucceeded)
                        {
                            // Reduce the number of displayed images to a reasonable amount if the camera is acquiring images very fast.
                            Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                            //Console.WriteLine("1");
                            // Lock the bits of the bitmap.
                            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                            // Place the pointer to the buffer of the bitmap.
                            converter.OutputPixelFormat = PixelType.BGRA8packed;
                            IntPtr ptrBmp = bmpData.Scan0;
                            //Console.WriteLine("1");
                            converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                            bitmap.UnlockBits(bmpData);

                            if (is_inference)
                            {
                                bitmap = Inference(bitmap);
                            }
                            if (pictureBox1.InvokeRequired)
                            {
                                UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                                pictureBox1.BeginInvoke(update);
                            }
                            else
                            {
                                pictureBox1.Image = bitmap;
                            }

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

                // 开始采集
                //hIKVisionCamera.StartGrabbing();


                //if (MyCamera.MV_OK != nRet)
                //{
                //    m_bGrabbing = false;
                //    m_hReceiveThread.Join();
                //    MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}

                // 控件操作
                SetCtrlWhenStartGrab();
            }
            else if ((chooseBasler) && (!chooseHIK))
            {
                try
                {
                    // 标志符号
                    balserCanGrab = true;
                    //camera.StreamGrabber.Start();
                    baslerCamera.StartGrabbing();
                    baslerGrabThread = new Thread(GrabThreadProcess);

                    baslerGrabThread.Start();

                    // 控件操作
                    SetCtrlWhenStartGrab();
                }
                catch
                {
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
            else if ((chooseBasler) && (!chooseHIK))
            {
                balserCanGrab = false;
                baslerGrabThread.Join();
                // 停止采集
                baslerCamera.StopGrabbing();
                // 控件操作
                SetCtrlWhenStopGrab();
            }
        }

        #region 参数设置
        private void BnGetParam_Click(object sender, EventArgs e)
        {
            if ((chooseHIK) && (!chooseBasler))
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
            else if ((chooseBasler) && (!chooseHIK))
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
            if ((chooseHIK) && (!chooseBasler))
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
            else if ((chooseBasler) && (!chooseHIK))
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

                int model_type = 1;
                IntPtr a = new IntPtr(model_type);
                model = CreatePaddlexModel(ref model_type, modelPath, use_gpu, use_trt, use_mkl, mkl_thread_num, gpu_id, key, use_ir_optim);

                bnStartDetection.Enabled = true;
                bnStopDetection.Enabled = true;
                bnSaveImage.Enabled = true;
            }
        }

        // 将Btimap类转换为byte[]类函数
        public static byte[] GetBGRValues(Bitmap bmp, out int stride)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            stride = bmpData.Stride;
            var rowBytes = bmpData.Width * Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var imgBytes = bmp.Height * rowBytes;
            byte[] rgbValues = new byte[imgBytes];

            var ptr = bmpData.Scan0;
            for (var i = 0; i < bmp.Height; i++)
            {
                Marshal.Copy(ptr, rgbValues, i * rowBytes, rowBytes);
                ptr += bmpData.Stride; // next row
            }

            bmp.UnlockBits(bmpData);

            return rgbValues;
        }

        public static IntPtr getBytesPtrInt(int[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        // 推理
        private Bitmap Inference(Bitmap bmp)
        {

            byte[] source = GetBGRValues(bmp, out stride);
            int channel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            IntPtr[] result = new IntPtr[100];
            IntPtr resultImage = PaddlexDetPredict(model, source, bmp.Height, bmp.Width, channel, result);

            Bitmap resultshow;
            Mat img = new Mat(resultImage);

            switch(channel)
            {
                case 1:
                    resultshow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format8bppIndexed, img.Data);
                    break;
                case 2:
                    resultshow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format16bppGrayScale, img.Data);
                    break;
                case 3:
                    resultshow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format24bppRgb, img.Data);
                    break;
                default:
                    resultshow = new Bitmap(img.Cols, img.Rows, (int)img.Step(), PixelFormat.Format32bppArgb, img.Data);
                    break;
            }
            System.GC.Collect();
            return resultshow;
            
        }

        private void BnStartDetection_Click(object sender, EventArgs e)
        {
            bnStopDetection.Enabled = true;
            bnStartDetection.Enabled = false;
            is_inference = true;
        }

        // 停止检测
        private void BnStopDetection_Click(object sender, EventArgs e)
        {
            bnStopDetection.Enabled = false;
            bnStartDetection.Enabled = true;
            is_inference = false;
        }

        // 保存图片
        private void BnSaveImage_Click(object sender, EventArgs e)
        {

        }

        // 窗口关闭
        private void SingleCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            h_MyCamera.MV_CC_CloseDevice_NET();
            h_MyCamera.MV_CC_DestroyDevice_NET();
            if(balserCanGrab)
            {
                baslerGrabThread.Abort();
            }           
            baslerCamera.DestroyCamera();
            System.Environment.Exit(0);
        }

    }
}
