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

        bool mTragger = true;
        public static MyCamera.cbOutputExdelegate ImageCallback;

        //放出一个Camera
        MyCamera camera = null;

        // 列表
        MyCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();

        public HIKVisionCamera()
        {
            camera = new MyCamera();
        }
        //相机个数
        public uint CameraNum()
        {
            string pManufacturerName = "Hikvision";
            MyCamera.MV_CC_EnumDevicesEx_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList, pManufacturerName);
            //Console.WriteLine(deviceList.ToString());
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
        public void CameraInit(int index)
        {
            if (deviceList.nDeviceNum == 0 || index == -1)
            {
                MessageBox.Show("未找到设备，请检查！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 获取选择的设备信息
            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[index],
                                                                typeof(MyCamera.MV_CC_DEVICE_INFO));
            // 打开设备
            if (null == camera)
            {
                camera = new MyCamera();
            }
            // 创建设备
            int nRet = camera.MV_CC_CreateDevice_NET(ref device);
            Console.WriteLine(nRet);
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("创建设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            nRet = camera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                camera.MV_CC_DestroyDevice_NET();
                MessageBox.Show("设备打开失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 探测网络最佳包大小(只对GigE相机有效)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = camera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = camera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        MessageBox.Show("Set Packet Size failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Get Packet Size failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // 设置连续采集模式
            camera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            camera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

        }

        public int StartGrabbing()
        {
            m_stFrameInfo.nFrameLen = 0; // 取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            int nRet = camera.MV_CC_StartGrabbing_NET();
            return nRet;
        }

        public int Grabbing()
        {
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            int nRet = camera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 10000);
            return nRet;
        }

    }
}
