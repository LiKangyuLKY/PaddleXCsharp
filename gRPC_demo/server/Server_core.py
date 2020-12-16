import os,sys
import grpc
# sys.path.append('../')
# sys.path.append(os.path.join(os.getcwd(),'../example'))


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



from example import PaddleXserver_pb2
from example import PaddleXserver_pb2_grpc
import cv2
import yaml

app_kill()
#import paddlex的时候会出现bug
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


#实现定义的接口服务
class PaddleXserver(PaddleXserver_pb2_grpc.PaddleXserverServicer):

    def paddlex_init(self,request,context):
        '''
        用获得的参数初始化加载模型
        返回加载模型的结果
        '''
        #预测器
        self.server_predict = Server_Predict(request.model_dir,request.use_gpu,request.gpu_id)
        respone = self.server_predict.load_model()
        return PaddleXserver_pb2.paddlex_init_result(init_result = respone)

    def paddlex_predict_det(self,request,context):
        '''
        目标检测接口
        获得上传的帧图片，将预测完毕的图片筛选后返回
        '''
        img = base64_to_image(request._image)

        #写入图片用
        result = self.server_predict.predict(img)
        det_inputs_result = PaddleXserver_pb2.det_inputs_result()
        for value in result:
            #循环读取result字典
            # 返回符合score的box
            if value['score']< self.server_predict.score.default_threshold:
                continue
            #把真值赋给数据对象
            temp = det_inputs_result.boxes.add()
            temp.category_id = value['category_id']
            temp.bbox.xmin = value['bbox'][0]
            temp.bbox.ymin = value['bbox'][1]
            temp.bbox.width = value['bbox'][2]
            temp.bbox.height = value['bbox'][3]
            temp.score = value['score']
            temp.category = value['category']
        return det_inputs_result
    
    def paddlex_predict_det_seg(self,request,context):
        '''
        实例分割接口
        接受帧图预测
        结果筛选后返回
        '''
        img = base64_to_image(request._image)
        #写入图片用
        result = self.server_predict.predict(img)
        det_seg_inputs_result = PaddleXserver_pb2.det_seg_inputs_result() #返回符合score的box
        for value in result:
            if value['score']< self.server_predict.score.default_threshold:
                continue
            #把真值赋给数据对象
            temp = det_seg_inputs_result.boxes_seg.add()
            temp.category_id = value['category_id']
            temp.bbox.xmin = value['bbox'][0]
            temp.bbox.ymin = value['bbox'][1]
            temp.bbox.width = value['bbox'][2]
            temp.bbox.height = value['bbox'][3]
            temp.score = value['score']
            temp._mask = image_to_base64(value['mask'])
            temp.category = value['category']
        return det_seg_inputs_result

    def paddlex_predict_cls(self,request,context):
        '''
        图像分类接口
        返回的结果只有一个不是一组
        '''
        img = base64_to_image(request._image)
        result = self.server_predict.predict(img)
        cls_inputs_result = PaddleXserver_pb2.cls_inputs_result()
        #把真值赋给数据对象,只返回第一个值
        cls_inputs_result.category_id = result[0]['category_id']
        cls_inputs_result.score = result[0]['score']
        cls_inputs_result.category = result[0]['category']
        return cls_inputs_result

    def paddlex_predict_seg(self,request,context):
        '''
        语义分割接口
        解释帧图返回预测结果
        '''
        img = base64_to_image(request._image)
        result = self.server_predict.predict(img)
        seg_inputs_result = PaddleXserver_pb2.seg_inputs_result()
        #把真值赋给数据对象
        score_maps = (result['score_map']*255).astype(np.uint8)
        score_maps = np.array_split(score_maps,score_maps.shape[2],axis=2)
        for mask in score_maps:
            score_map = seg_inputs_result._score_map.add()
            score_map.value = image_to_base64(mask)
        seg_inputs_result.label_map = image_to_base64(result['label_map'])
        seg_inputs_result.set_threshold = self.server_predict.score.default_threshold
        return seg_inputs_result


# 加载score.yaml文件的参数
class Score_Config:
    def __init__(self, conf_file):
        if not os.path.exists(conf_file):
            raise Exception('Config file path [%s] invalid!' % conf_file)
        with open(conf_file) as fp:
            configs = yaml.load(fp, Loader=yaml.FullLoader)
            self.default_threshold = configs['default_threshold']
            self.threshold_category_0 = configs['threshold_category_0']
            self.threshold_category_1 = configs['threshold_category_1']
            self.threshold_category_2 = configs['threshold_category_2']
          

class Server_Predict:
    '''
    预测器
    '''
    def __init__(self,model_dir,use_gpu,gpu_id = '0'):
        self.model_dir = model_dir
        self.use_gpu = use_gpu
        self.gpu_id = gpu_id
        self.score = Score_Config(os.path.join(self.model_dir,'score.yaml'))

    def load_model(self):
        '''
        加载模型
        '''
        if not os.path.exists(self.model_dir):
            return 'model dir not exist!'

        self.model = pdx.deploy.Predictor(self.model_dir)
        if self.use_gpu :
            os.environ['CUDA_VISIBLE_DEVICES'] = self.gpu_id
            return 'finished load - GPU '
        else:
            os.environ['CUDA_VISIBLE_DEVICES'] = ''
            return 'finished load - CPU '

    def predict(self,img):
        '''
        用已经加载的模型预测
        返回结果
        '''
        return self.model.predict(img)

# if __name__ == "__main__":
#     msg = load_model(model_dir='../model/',
#                 use_gpu=True,
#                 gpu_id='0')
#     print(msg)   