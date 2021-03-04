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

/*
.\paddlex_inference\detector.exe --model_dir=D:\projects\meter_det_inference_model --image=D:\projects\meter_det_inference_model\123123.jpg
*/

#include <glog/logging.h>
#include <omp.h>

#include <algorithm>
#include <chrono>  // NOLINT
#include <fstream>
#include <iostream>
#include <string>
#include <vector>
#include <utility>

#include "include/paddlex/paddlex.h"
#include "include/paddlex/paddlexcpp.h"
#include "include/paddlex/visualize.h"

using namespace std::chrono;  // NOLINT

DEFINE_string(model_dir, "", "Path of inference model");
DEFINE_bool(use_gpu, false, "Infering with GPU or CPU");
DEFINE_bool(use_trt, false, "Infering with TensorRT");
DEFINE_bool(use_mkl, true, "Infering with MKL");
DEFINE_int32(mkl_thread_num,
             omp_get_num_procs(),
             "Number of mkl threads");
DEFINE_int32(gpu_id, 0, "GPU card id");
DEFINE_string(key, "", "key of encryption");
DEFINE_string(image, "", "Path of test image file");
DEFINE_string(image_list, "", "Path of test image list file");
DEFINE_string(save_dir, "output", "Path to save visualized image");
DEFINE_int32(batch_size, 1, "Batch size of infering");
DEFINE_double(threshold,
              0.5,
              "The minimum scores of target boxes which are shown");
DEFINE_int32(thread_num,
             omp_get_num_procs(),
             "Number of preprocessing threads");

int main(int argc, char** argv) {
	// Parsing command-line
	google::ParseCommandLineFlags(&argc, &argv, true);

	std::cout << "PaddleX Detector Start." << std::endl;

	if (FLAGS_image == "") {
	std::cerr << "--image or --image_list need to be defined" << std::endl;
	return -1;
	}
	// Load model

	#if 0
	int modeltype;
	void *detmodel = PaddleXCpp::CreatePaddlexModel(&modeltype,
			 FLAGS_model_dir,
	         FLAGS_use_gpu,
	         FLAGS_use_trt,
	         FLAGS_use_mkl,
	         FLAGS_mkl_thread_num,
	         FLAGS_gpu_id,
	         FLAGS_key);

	std::string save_dir = "output";

	// Predict
	PaddleX::DetResult result;
	cv::Mat im = cv::imread(FLAGS_image, 1);
	((PaddleX::Model *)detmodel)->paddlex_predict(im, &result);
	// Output predicted bounding boxes
	for (int i = 0; i < result.boxes.size(); ++i) {
	  std::cout << "image file: " << FLAGS_image << std::endl;
	  std::cout << ", predict label: " << result.boxes[i].category
	            << ", label_id:" << result.boxes[i].category_id
	            << ", score: " << result.boxes[i].score
	            << ", box(xmin, ymin, w, h):(" << result.boxes[i].coordinate[0]
	            << ", " << result.boxes[i].coordinate[1] << ", "
	            << result.boxes[i].coordinate[2] << ", "
	            << result.boxes[i].coordinate[3] << ")" << std::endl;
	}

	// Visualize results
	cv::Mat vis_img =
	    PaddleX::Visualize(im, result, ((PaddleX::Model *)detmodel)->labels, FLAGS_threshold);
	std::string save_path =
	    PaddleX::generate_save_path(FLAGS_save_dir, FLAGS_image);
	cv::imwrite(save_path, vis_img);
	result.clear();
	std::cout << "Visualized output saved as " << save_path << std::endl;	
	#else
	void *p_model = NULL;
	int model_type;
//	char *model_dir = "D:\\projects\\meter_det_inference_model";
	std::string model_dir = FLAGS_model_dir;
	p_model = PaddleXCpp::CreatePaddlexModel(&model_type,
			 (char *)model_dir.c_str(),
	         FLAGS_use_gpu,
	         FLAGS_use_trt,
	         FLAGS_use_mkl,
	         FLAGS_mkl_thread_num,
	         FLAGS_gpu_id);
	if(!p_model)
	{
		std::cout << "CreatePaddlexModel failed." << std::endl;
		return 0;
	}

	cv::Mat srcimg = cv::imread(FLAGS_image, 1);
	char *result = NULL;
	std::cout << "srcimg : height:" << srcimg.rows << "  width:" << srcimg.cols << std::endl;
	if(!PaddleXCpp::PaddlexDetPredict(p_model, srcimg.data, srcimg.rows, srcimg.cols, srcimg.channels(), &result, true))
	{
		std::cout << "PaddlexDetPredict failed." << std::endl;
	}
	
	std::string& output_image = model_dir + "\\" + "result.jpg";
	cv::imwrite(output_image, srcimg);
	
	std::cout << "PaddleX Detector Finish." << std::endl;
	return 0;

#if 0
	PaddleX::Model model;
	model.paddlex_init(FLAGS_model_dir,
	         FLAGS_use_gpu,
	         FLAGS_use_trt,
	         FLAGS_use_mkl,
	         FLAGS_mkl_thread_num,
	         FLAGS_gpu_id,
	         FLAGS_key);
	
	int imgs = 1;
	std::string save_dir = "output";

	// Predict
	PaddleX::DetResult result;
	cv::Mat im = cv::imread(FLAGS_image, 1);
	model.paddlex_predict(im, &result);
	// Output predicted bounding boxes
	for (int i = 0; i < result.boxes.size(); ++i) {
	  std::cout << "image file: " << FLAGS_image << std::endl;
	  std::cout << ", predict label: " << result.boxes[i].category
	            << ", label_id:" << result.boxes[i].category_id
	            << ", score: " << result.boxes[i].score
	            << ", box(xmin, ymin, w, h):(" << result.boxes[i].coordinate[0]
	            << ", " << result.boxes[i].coordinate[1] << ", "
	            << result.boxes[i].coordinate[2] << ", "
	            << result.boxes[i].coordinate[3] << ")" << std::endl;
	}

	// Visualize results
	cv::Mat vis_img =
	    PaddleX::Visualize(im, result, model.labels, FLAGS_threshold);
	std::string save_path =
	    PaddleX::generate_save_path(FLAGS_save_dir, FLAGS_image);
	cv::imwrite(save_path, vis_img);
	result.clear();
	std::cout << "Visualized output saved as " << save_path << std::endl;
#endif	
	#endif
	return 0;
}
