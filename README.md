# 简介
本项目基于C#开发，可以驱动海康威视黑白相机、basler黑白/彩色相机，获取图像后使用PaddleX深度学习库，实现图像分类、目标检测和语义分割功能

从功能层面，共分为三部分：

* 本地单模型模式
  * 部署于本地服务器
  * 每次可调用一个相机
  * 加载一个深度学习模型，实现一种任务
* 本地多模型模式
  * 部署于本地服务器
  * 每次可调用多个相机（目前支持2个）
  * 每个相机可独立加载一个深度学习模型，同步实现多种任务
* 远程起服务模式
  * 部署于远程服务器
  * 通过gRPC方式，调用远程服务器
  
## 单相机模式界面
相机操作及推理

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%8D%95%E7%9B%B8%E6%9C%BA-%E7%9B%B8%E6%9C%BA%E6%93%8D%E4%BD%9C.gif)

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%8D%95%E7%9B%B8%E6%9C%BA-%E6%8E%A8%E7%90%86.gif)

## 多相机模式界面
相机操作及推理

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%A4%9A%E7%9B%B8%E6%9C%BA-%E7%9B%B8%E6%9C%BA%E6%93%8D%E4%BD%9C.gif)

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%A4%9A%E7%9B%B8%E6%9C%BA-%E6%8E%A8%E7%90%86.gif)
