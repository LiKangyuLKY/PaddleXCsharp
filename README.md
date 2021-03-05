## 项目背景

百度飞桨团队开发了一款全流程深度学习模型开发工具：[PaddleX](https://github.com/PaddlePaddle/PaddleX)，分为API版本和GUI版本。使用PaddleX可以低代码甚至零代码实现图像分类、目标检测、语义分割、实例分割等任务，非常适合于非计算机背景（如工业领域）的从业者完成深度学习开发。

为了更便捷地与多类传感器和上位系统通信、工业领域内上位机软件常常用C#来开发，但深度学习本身以C++和Python为主。因此，本Demo的目标就是基于PaddleX，打通深度学习C#部署的最后一步。


## 项目简介

本项目基于C#开发，可以驱动海康威视黑白相机、basler黑白/彩色相机，获取图像后使用PaddleX深度学习库，实现图像分类、目标检测和语义分割功能。

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


## 项目目录

* PaddleXCsharp文件内容使用C#开发，实现本项目的基本功能
* PaddleXDll为PaddleX提供的C++程序，用于编译DLL
* gRPC_demo为远程起服务模式，使用python开发

## 使用方法

1.将项目克隆（下载）至本地
2.使用PaddleXDll文件下内容，编译可供C#下调用的DLL（这里为大家提供一份编译好的DLL，[百度网盘链接](https://pan.baidu.com/s/1zxqA_cl-pY1xCtCVRpritg)，提取码：2luj）
3.将DLL文件添加至C#bin文件下
4.安装[PaddleX](https://github.com/PaddlePaddle/PaddleX)，根据文档，训练出深度学习模型（注意，本demo仅支持PaddleX训练出的模型，不支持PaddlePaddle训练的模型）
5.连接相机，加载模型，启动测试

## 工作环境

`CUDA 10.0`,`cudnn 7.5.0`
其他环境下可能会有问题


## 演示示例

下图以目标检测为例，演示如何调用相机、加载模板检测模型，实现压力表的检测。

### 本地单模型模式

相机操作及推理

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%8D%95%E7%9B%B8%E6%9C%BA-%E7%9B%B8%E6%9C%BA%E6%93%8D%E4%BD%9C.gif)

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%8D%95%E7%9B%B8%E6%9C%BA-%E6%8E%A8%E7%90%86.gif)

### 本地多模型模式

相机操作及推理

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%A4%9A%E7%9B%B8%E6%9C%BA-%E7%9B%B8%E6%9C%BA%E6%93%8D%E4%BD%9C.gif)

![Alt text](https://github.com/LiKangyuLKY/PaddleXCsharp/blob/master/images/%E5%A4%9A%E7%9B%B8%E6%9C%BA-%E6%8E%A8%E7%90%86.gif)
