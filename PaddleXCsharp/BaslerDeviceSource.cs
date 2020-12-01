using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace BaslerDeviceSource
{
    public class BaslerCamera
    {
        // 版本
        private static Version sfnc2_0_0 = new Version(2, 0, 0);

        //放出一个Camera
        Camera camera = null;
        Camera[] cameraArr = new Camera[2];
        List<ICameraInfo> allCameraInfos;
        int cameraNum = 0;

        //basler里用于将相机采集的图像转换成位图
        PixelDataConverter pxConvert = new PixelDataConverter();

        // 相机个数
        public int CameraNum()
        {
            int cameraNumber = CameraFinder.Enumerate().Count;
            return cameraNumber;
        }

        // 枚举相机
        public List<ICameraInfo> CameraEnum()
        {
            // 相机个数
            allCameraInfos = CameraFinder.Enumerate();
            return allCameraInfos;
        }

        // 相机初始化
        public Camera CameraInit(int index)
        {
            if (null == camera)
            {
                // 获取所选相机
                ICameraInfo selectedCamera = allCameraInfos[index];
                camera = new Camera(selectedCamera);
            }
            //打开相机
            if (true == camera.IsOpen)
            {
                camera.Close();
            }
            camera.Open();
            return camera;
        }

        // 多相机初始化
        public Camera[] MultiCameraInit(int Num)
        {
            cameraNum = Num;
            for (int i = 0; i < cameraNum; i++)
            { 
                cameraArr[i] = new Camera(allCameraInfos[i]);
                if (true == cameraArr[i].IsOpen)
                {
                    cameraArr[i].Close();
                }
                //打开相机
                cameraArr[i].Open();
            }
            return cameraArr;
        }

        // 获取参数
        public void GetParam(ref string gain ,ref string exposure, Camera cam)
        {
            if (cam.GetSfncVersion() < sfnc2_0_0)
            {
                gain = cam.Parameters[PLCamera.GainRaw].GetValue().ToString();
                exposure = cam.Parameters[PLCamera.ExposureTimeAbs].GetValue().ToString();
            }
            else
            {
                gain = cam.Parameters[PLCamera.Gain].GetValue().ToString();
                exposure = cam.Parameters[PLCamera.ExposureTime].GetValue().ToString();
            }
        }

        // 设置参数
        public void SetParam(long gain, long exposure, ref string gainString, ref string exposureString, Camera cam)
        {
            // 增益
            cam.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Off); // 关闭自动设置
            if (cam.GetSfncVersion() < sfnc2_0_0) // 旧版本设备
            {
                long maxGain = cam.Parameters[PLCamera.GainRaw].GetMaximum();
                long minGain = cam.Parameters[PLCamera.GainRaw].GetMinimum();
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
                cam.Parameters[PLCamera.GainRaw].SetValue(gain);

                gainString = gain.ToString();
            }
            else // 新版本设备（USB3)
            {
                double gainNew = gain;
                double maxGain = cam.Parameters[PLCamera.Gain].GetMaximum();
                double minGain = cam.Parameters[PLCamera.Gain].GetMinimum();
                if (gainNew < minGain)
                {
                    MessageBox.Show("小于最小值，已修改为最小增益值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    gainNew = minGain;
                }
                else if (gainNew > maxGain)
                {
                    MessageBox.Show("大于最大值，已修改为最大增益值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    gainNew = maxGain;
                }
                cam.Parameters[PLCamera.Gain].SetValue(gainNew);

                gainString = gainNew.ToString();
            }

            // 曝光时间
            cam.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Off); // 关闭自动设置
            if (cam.GetSfncVersion() < sfnc2_0_0) // 旧版本设备
            {
                long maxExposure = cam.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
                long minExposure = cam.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
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
                cam.Parameters[PLCamera.ExposureTimeRaw].SetValue(exposure);

                exposureString = exposure.ToString();
            }
            else // 新版本设备（USB3)
            {
                double exposureNew = exposure;
                double maxExposure = cam.Parameters[PLCamera.ExposureTime].GetMaximum();
                double minExposure = cam.Parameters[PLCamera.ExposureTime].GetMinimum();
                if (exposureNew < minExposure)
                {
                    MessageBox.Show("小于最小值，已修改为最小曝光时间值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    exposureNew = minExposure;
                }
                else if (exposureNew > maxExposure)
                {
                    MessageBox.Show("大于最大值，已修改为最大曝光时间值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    exposureNew = maxExposure;
                }
                cam.Parameters[PLCamera.ExposureTime].SetValue(exposureNew);

                exposureString = exposureNew.ToString();
            } 
        }

        // 开始采集
        public void StartGrabbing()
        {
            if (camera != null)
            {
                camera.StreamGrabber.Start();
            }
        }

        // 停止采集
        public void StopGrabbing()
        {
            if (camera != null)
            {
                camera.StreamGrabber.Stop();
            }
        }

        // 关闭相机对象
        public void DestroyCamera()
        {
            if (camera != null)
            {
                // 停止采集
                camera.StreamGrabber.Stop();
                // 关闭设备
                camera.Close();
                camera.Dispose();
                camera = null;
            }
        }

        // 多相机关闭相机对象
        public void MultiDestroyCamera()
        {
            for (int i = 0; i < cameraNum; i++)
            {
                if (cameraArr[i] != null && true == cameraArr[i].IsOpen)
                {
                    if(true == cameraArr[i].StreamGrabber.IsGrabbing)
                        cameraArr[i].StreamGrabber.Stop();  // 停止采集
                    // 关闭设备
                    cameraArr[i].Close();
                    cameraArr[i].Dispose();
                    cameraArr[i] = null;
                }
            }              
        }
    }
}
