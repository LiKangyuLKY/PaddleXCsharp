import sys
import grpc
from client_core import Predict_det
import argparse
import cv2
import matplotlib.pylab as plt
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('--model_dir',type=str,default='../model_humanseg_mobile/')
parser.add_argument('--channel',type=str,default='localhost:54888')
parser.add_argument('--use_gpu',type=bool,default=False)
parser.add_argument('--gpu_id',type=str,default='0')

args = parser.parse_args()

def run():
    print('starting creative')

    #写入视频
    fps = 25          # 视频帧率
    size = (360, 640) # 需要转为视频的图片的尺寸
    video = cv2.VideoWriter("../output.mp4", cv2.VideoWriter_fourcc(*'mp4v'), fps, size)
    FPS_NUM = 0

    #创建预测器
    Predict_deter = Predict_det(
        channel=args.channel,
        model_dir = args.model_dir,
        use_gpu=args.use_gpu,
        gpu_id=args.gpu_id
    )
    load_model = Predict_deter.load_model()
    print(load_model)


    #读取视频流
    total_NUM = 0
    cap = cv2.VideoCapture("../man.mp4")
    while cap.isOpened():
        ret, frame = cap.read()

        # cv2.imshow('frame',frame)
        # cv2.waitKey(delay=40)

        #目标检测
        # result = Predict_deter.pdx_predict_det(frame)

        #实例分割
        # result = Predict_deter.pdx_predict_det_seg(frame)

        #图像分类
        # result = Predict_deter.pdx_predict_cls(frame)

        #语义分割
        result = Predict_deter.pdx_predict_seg(frame)
        # print(result.shape)
        total_NUM += 1
        if total_NUM%100==0:
            print(total_NUM)

        # cv2.imshow('result',result)
        # cv2.waitKey(delay=40)

        # 坑坑坑--写入视频
        result = result.astype(np.uint8)
        video.write(result)



 
    cap.release()
    cv2.destroyAllWindows()
    print('测试完成')

if __name__ == '__main__':
    run()