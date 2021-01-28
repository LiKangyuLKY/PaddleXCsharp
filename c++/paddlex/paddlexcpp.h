//   Copyright (c) 2020 PaddlePaddle Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#ifndef _PADDLEXCPP_H_
#define _PADDLEXCPP_H_

namespace PaddleXCpp {

#define _PADDLEX_DEBUG_
#define MAX_CATEGORY_STR_LEN 63
	
#pragma pack (1)
	
	typedef struct cls_result {
//	char category[MAX_CATEGORY_STR_LEN+1];
	int category_id;
	float score;
	}t_cls_result;
	
	typedef struct det_result {
//	char category[MAX_CATEGORY_STR_LEN+1];
	float category_id;
	float score;
	float coordinate[4];
	}t_det_result;
	
#pragma pack ()

	extern "C" __declspec(dllexport) void* __cdecl CreatePaddlexModel(int *model_type,
            										char* model_dir,
            										bool use_gpu = false,
            										bool use_trt = false,
            										bool use_mkl = true,
            										int mkl_thread_num = 4,
            										int gpu_id = 0,
            										char* key = "",
            										bool use_ir_optim = true);
	
	/*
	PaddlexClsPredict函数接口说明:
	入参:
		model:模型指针，即CreatePaddlexModel函数返回值。
		img:需要推理的图片数据指针
		height:图片高度(像素)
		width:图片宽度(像素)
		channels:图片通道数
		category:返回的推理结果中的类别ID值
		score:返回的推理结果中的分数
	返回值:
		推理是否成功
	*/
	extern "C" __declspec(dllexport) bool __cdecl PaddlexClsPredict(void* model, unsigned char *img, int height, int width, int channels, int *category, float *score);

	/*
	PaddlexDetPredict函数接口说明:
	入参:
		model:模型指针，即CreatePaddlexModel函数返回值。
		img:需要推理的图片数据指针
		height:图片高度(像素)
		width:图片宽度(像素)
		channels:图片通道数
		result:返回的推理结果，内部结构为:"flost类型的box_num"+box_num个"类别+分数+回归框(xmin, ymin, w, h)"
		visualize:是否进行结果可视化。默认进行可视化，即在原图img上画出预测的box框。
	返回值:
		推理是否成功
	*/
	extern "C" __declspec(dllexport) bool __cdecl PaddlexDetPredict(void* model, unsigned char *img, int height, int width, int channels, int max_box, float *result, bool visualize = true);

	/*
	PaddlexDetPredict函数接口说明:
	入参:
		model:模型指针，即CreatePaddlexModel函数返回值。
		img:需要推理的图片数据指针
		height:图片高度(像素)
		width:图片宽度(像素)
		channels:图片通道数
		label_map:返回的label_map结果，数据类型为int64类型，长度为height*width
		score_map:返回的score_map结果，数据类型为float类型，长度为height*width
		visualize:是否进行结果可视化。默认进行可视化，即在原图img上叠加分割结果。
	返回值:
		推理是否成功
	*/
	extern "C" __declspec(dllexport) bool __cdecl PaddlexSegPredict(void* model, unsigned char *img, int height, int width, int channels, int64_t *label_map, float *score_map, bool visualize = true);
}  // namespace PaddleXCpp

#endif//_PADDLEXCPP_H_

