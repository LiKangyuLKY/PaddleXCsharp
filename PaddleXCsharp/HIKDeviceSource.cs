using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;


namespace HIKDeviceSource
{
    using ImageCallBack = MyCamera.cbOutputdelegate;
    using ExceptionCallBack = MyCamera.cbExceptiondelegate;
    public class HIKVisionCamera : MyCamera
    {
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        public const int CO_FAIL = -1;
        public const int CO_OK = 0;
        int cameraNum = 0;

        //放出一个Camera
        MyCamera camera = null;
        MyCamera[] cameraArr = new MyCamera[2];

        // 列表
        MyCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();

        public HIKVisionCamera()
        {
            camera = new MyCamera();
        }
        //相机个数
        public uint CameraNum()
        {
            MyCamera.MV_CC_EnumDevicesEx_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList, "Hikvision");
            return deviceList.nDeviceNum;
        }

        // 枚举相机
        public List<string> EnumDevices()
        {
            uint num = CameraNum();
            List<string> allCameraInfos = new List<string>();
            for (int i = 0; i < num; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        allCameraInfos.Add("GEV: " + gigeInfo.chUserDefinedName);
                    }
                    else
                    {
                        allCameraInfos.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName);
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));

                    if (usbInfo.chUserDefinedName != "")
                    {
                        allCameraInfos.Add("U3V: " + usbInfo.chUserDefinedName);
                    }
                    else
                    {
                        allCameraInfos.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName);
                    }
                }

            }
            return allCameraInfos;


        }

        // 相机初始化
        public MyCamera CameraInit(int index)
        {

            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[index],
                                                                typeof(MyCamera.MV_CC_DEVICE_INFO));
            if (null == camera)
            {
                camera = new MyCamera();
            }
            // 创建设备
            camera.MV_CC_CreateDevice_NET(ref device);
            // 打开设备
            camera.MV_CC_OpenDevice_NET();
            // 探测网络最佳包大小(只对GigE相机有效)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = camera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    camera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                }
            }
            // 设置连续采集模式
            camera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            camera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            return camera;
        }

        public MyCamera[] MultiCameraInit(int Num)
        {
            cameraNum = Num;
            for (int i = 0; i < cameraNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i],
                                                                    typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (null == cameraArr[i])
                {
                    cameraArr[i] = new MyCamera();
                }
                // 创建设备
                cameraArr[i].MV_CC_CreateDevice_NET(ref device);
                // 打开设备
                cameraArr[i].MV_CC_OpenDevice_NET();
                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = cameraArr[i].MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        cameraArr[i].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    }
                }
                // 设置连续采集模式
                cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
                cameraArr[i].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            }
            return cameraArr;
        }

        // 关闭相机对象
        public void DestroyCamera()
        {
            if(camera != null)
            {
                // 停止采集
                camera.MV_CC_StopGrabbing_NET();
                // 关闭设备
                camera.MV_CC_CloseDevice_NET();
                camera.MV_CC_DestroyDevice_NET();
                camera = null;
            }  
        }

        public void MultiDestroyCamera()
        {
            for (int i = 0; i < cameraNum; i++)
            {
                if (cameraArr[i] != null)
                {
                    // 停止采集
                    cameraArr[i].MV_CC_StopGrabbing_NET();
                    // 关闭设备
                    cameraArr[i].MV_CC_CloseDevice_NET();
                    cameraArr[i].MV_CC_DestroyDevice_NET();
                    cameraArr[i] = null;
                }
            }
        }

        // 获取参数
        public void GetParam(ref string gain, ref string exposure, MyCamera cam)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            // 获取曝光值
            int nRet = cam.MV_CC_GetFloatValue_NET("Gain", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                gain = stParam.fCurValue.ToString("F0");
            }
            // 获取增益
            nRet = cam.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                exposure = stParam.fCurValue.ToString("F0");
            }
        }

        // 设置参数
        public void SetParam(float gain, float exposure, ref string gainString, ref string exposureString, MyCamera cam)
        {
            // 增益
            // 获取当前Gain、最大Gain、最小Gain
            MyCamera.MVCC_FLOATVALUE gainValue = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetGain_NET(ref gainValue);
            //float gain = gainValue.fCurValue;
            float maxGain = gainValue.fMax;
            float minGain = gainValue.fMin;
            // 判断是否处于最小到最大值之间
            if (gain < minGain)
            {
                MessageBox.Show("小于最小值，已修改为最小增益值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                gain = minGain;
            }
            else if (gain > maxGain)
            {
                MessageBox.Show("大于最大值，已修改为最大增益值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                gain = maxGain;
            }
            // 关闭自动设置
            cam.MV_CC_SetEnumValue_NET("GainAuto", 0);
            // 设置Gain
            cam.MV_CC_SetFloatValue_NET("Gain", gain);
            gainString = gain.ToString("F0");

            // 曝光时间
            // 获取当前Exposure、最大Exposure、最小Exposure
            MyCamera.MVCC_FLOATVALUE exposureValue = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetExposureTime_NET(ref exposureValue);
            //float exposure = exposureValue.fCurValue;
            float maxExposure = exposureValue.fMax;
            float minExposure = exposureValue.fMin;
            // 判断是否处于最小到最大值之间
            if (exposure < minExposure)
            {
                MessageBox.Show("小于最小值，已修改为最小曝光时间值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                exposure = minExposure;
            }
            else if (exposure > maxExposure)
            {
                MessageBox.Show("大于最大值，已修改为最大曝光时间值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                exposure = maxExposure;
            }

            // 关闭自动设置
            cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            // 设置ExposureTime
            cam.MV_CC_SetFloatValue_NET("ExposureTime", exposure);
            exposureString = exposure.ToString("F0");
        }

        // 开始采集
        public void StartGrabbing()
        {
            m_stFrameInfo.nFrameLen = 0; // 取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            camera.MV_CC_StartGrabbing_NET(); // 开始Grab
        }

        // 停止采集
        public void StopGrabbing()
        {
            if (camera != null)
            {
                camera.MV_CC_StopGrabbing_NET();
            }
        }

        // 类型转换
        public IntPtr HikConvert()
        {
            IntPtr pTemp = IntPtr.Zero;
            //MyCamera.MvGvspPixelType enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            //if (m_stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8 || m_stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed)
            //{
            //    IntPtr m_BufForDriver;
            //    pTemp = m_BufForDriver;
            //    enDstPixelType = m_stFrameInfo.enPixelType;
            //}
            return pTemp;
        }
    }
}
