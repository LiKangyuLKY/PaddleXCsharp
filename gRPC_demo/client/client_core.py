import os,sys
# sys.path.append('../Packages')
import grpc

import os
import psutil
import signal


def getAllPid():
    pid_dict = {}
    pids = psutil.pids()
    for pid in pids:
        p = psutil.Process(pid)
        pid_dict[pid] = p.name()
    return pid_dict


def kill(pid):
    try:
        kill_pid = os.kill(pid, signal.SIGABRT)
        print('已杀死pid为{}的进程,　返回值是:{}'.format(pid, kill_pid))
    except Exception as e:
        print('没有如此进程!!!')


def app_kill():
    kill_list = []
    dic = getAllPid()
    for t in dic.keys():
        if dic[t] == "App.exe":
            kill_list.append(t)

    for i in range(len(kill_list)-1):
        kill(kill_list[i])


# sys.path.append(os.path.join(os.getcwd(),'../example'))
from example import PaddleXserver_pb2
from example import PaddleXserver_pb2_grpc
import cv2

app_kill()

import paddlex as pdx

import numpy as np
import base64

def image_to_base64(img):
    img_str = cv2.imencode('.jpg', img)[1].tostring()  # 将图片编码成流数据，放到内存缓存中，然后转化成string格式
    b64_code = base64.b64encode(img_str) # 编码成base64
    img_str=str(b64_code, encoding='utf-8')
    return img_str

def base64_to_image(img):
    img_b64decode = base64.b64decode(img)  # base64解码
    img_array = np.fromstring(img_b64decode,np.uint8) # 转换np序列
    img = cv2.imdecode(img_array,cv2.COLOR_BGR2RGB)  # 转换Opencv格式
    return img

class Predict_det:
    '''
    客户端预测对象
    '''
    def __init__(self,channel,model_dir,use_gpu,gpu_id = '0'):
        # 链接rpc 服务器
        self.channel = grpc.insecure_channel(channel)   #str
        #调用 rpc 服务

        #stub用来调用服务端方法
        self.stub  = PaddleXserver_pb2_grpc.PaddleXserverStub(self.channel)

        self.model_dir = model_dir
        self.use_gpu = use_gpu
        self.gpu_id = gpu_id

    def load_model(self):
        '''
        将参数传给服务端，加载模型返回加载结果
        '''
        # print('use model_dir'+self.model_dir)
        respone =  self.stub.paddlex_init(PaddleXserver_pb2.paddlex_init_cmd(
            model_dir = self.model_dir,
            use_gpu = self.use_gpu,
            gpu_id = self.gpu_id
        ))
        return respone.init_result

    def pdx_predict_det(self,img):
        '''
        目标检测接口
        将帧图片传给服务端预测返回筛选之后的boxes
        将poxes的结果可视化
        '''
        or_img = img
        img = image_to_base64(img)
        respone = self.stub.paddlex_predict_det(PaddleXserver_pb2.image(
            _image = img
        ))
        result = []
        for value in respone.boxes:
            dict_temp = {}
            dict_temp['category_id'] = value.category_id
            dict_temp['bbox'] = []
            dict_temp['bbox'].append(value.bbox.xmin)
            dict_temp['bbox'].append(value.bbox.ymin)
            dict_temp['bbox'].append(value.bbox.width)
            dict_temp['bbox'].append(value.bbox.height)
            dict_temp['score'] = value.score
            dict_temp['category'] = value.category
            result.append(dict_temp)
        visualize_img = pdx.det.visualize(or_img,result,threshold=0,save_dir=None)
        return visualize_img

    def pdx_predict_det_seg(self,img):
        '''
        实例分割接口
        将帧图发送服务端，返回结果
        可视化结果
        '''
        or_img = img
        img = image_to_base64(img)
        respone = self.stub.paddlex_predict_det_seg(PaddleXserver_pb2.image(
            _image = img
        ))
        result = []
        for value in respone.boxes_seg:
            dict_temp = {}
            dict_temp['category_id'] = value.category_id
            dict_temp['bbox'] = []
            dict_temp['bbox'].append(value.bbox.xmin)
            dict_temp['bbox'].append(value.bbox.ymin)
            dict_temp['bbox'].append(value.bbox.width)
            dict_temp['bbox'].append(value.bbox.height)
            dict_temp['score'] = value.score
            dict_temp['mask'] = base64_to_image(value._mask)
            dict_temp['category'] = value.category
            result.append(dict_temp)
        visualize_img = pdx.det.visualize(or_img,result,threshold=0,save_dir=None)
        return visualize_img

    def pdx_predict_cls(self,img):
        '''
        图像分类接口
        将帧图发送服务端，返回结果一个
        '''

        img = image_to_base64(img)
        respone = self.stub.paddlex_predict_cls(PaddleXserver_pb2.image(
            _image = img
        ))
        result = []
        dict_temp = {}
        dict_temp['category_id'] = respone.category_id
        dict_temp['score'] = respone.score
        dict_temp['category'] = respone.category
        result.append(dict_temp)
        return result

    def pdx_predict_seg(self,img):
        '''
        语义分割接口
        将帧图发送服务端，返回一个分割结果
        '''
        or_img = img
        img = image_to_base64(img)
        respone = self.stub.paddlex_predict_seg(PaddleXserver_pb2.image(
            _image = img
        ))
        dict_temp = {}
        dict_temp['score_map'] = []
        for value in respone._score_map:
            temp = base64_to_image(value.value)
            temp = temp[:,:,np.newaxis]/255
            dict_temp['score_map'].append(temp.astype('float32'))
        dict_temp['score_map'] = np.concatenate(dict_temp['score_map'],axis=2)
        dict_temp['label_map'] = base64_to_image(respone.label_map)
        visualize_img = pdx.seg.visualize(or_img, dict_temp, weight=respone.set_threshold, save_dir=None, color=None)
        return visualize_img

