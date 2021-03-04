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

#include <math.h>
#include <omp.h>
#include <algorithm>
#include <fstream>
#include <cstring>
#include <string>
#include <stdlib.h>
#include <stdio.h>
#include <windows.h>
#include <io.h>

#include "include/paddlex/paddlex.h"
#include "include/paddlex/paddlexcpp.h"
#include "include/paddlex/visualize.h"

#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>

namespace PaddleX {

void Model::create_predictor(const std::string& model_dir,
                             bool use_gpu,
                             bool use_trt,
                             bool use_mkl,
                             int mkl_thread_num,
                             int gpu_id,
                             std::string key,
                             bool use_ir_optim) {
  paddle::AnalysisConfig config;
  std::string model_file = model_dir + OS_PATH_SEP + "__model__";
  std::string params_file = model_dir + OS_PATH_SEP + "__params__";
  std::string yaml_file = model_dir + OS_PATH_SEP + "model.yml";
  std::string yaml_input = "";
#ifdef WITH_ENCRYPTION
  if (key != "") {
    model_file = model_dir + OS_PATH_SEP + "__model__.encrypted";
    params_file = model_dir + OS_PATH_SEP + "__params__.encrypted";
    yaml_file = model_dir + OS_PATH_SEP + "model.yml.encrypted";
    paddle_security_load_model(
        &config, key.c_str(), model_file.c_str(), params_file.c_str());
    yaml_input = decrypt_file(yaml_file.c_str(), key.c_str());
  }
#endif
  if (yaml_input == "") {
    // read yaml file
    std::ifstream yaml_fin(yaml_file);
    yaml_fin.seekg(0, std::ios::end);
    size_t yaml_file_size = yaml_fin.tellg();
    yaml_input.assign(yaml_file_size, ' ');
    yaml_fin.seekg(0);
    yaml_fin.read(&yaml_input[0], yaml_file_size);
  }
  // load yaml file
  if (!load_config(yaml_input)) {
    std::cerr << "Parse file 'model.yml' failed!" << std::endl;
    exit(-1);
  }

  if (key == "") {
    config.SetModel(model_file, params_file);
  }
  if (use_mkl && !use_gpu) {
    if (name != "HRNet" && name != "DeepLabv3p" && name != "PPYOLO") {
        config.EnableMKLDNN();
        config.SetCpuMathLibraryNumThreads(mkl_thread_num);
    } else {
        std::cerr << "HRNet/DeepLabv3p/PPYOLO are not supported "
                  << "for the use of mkldnn" << std::endl;
    }
  }
  if (use_gpu) {
    config.EnableUseGpu(100, gpu_id);
  } else {
    config.DisableGpu();
  }
  config.SwitchUseFeedFetchOps(false);
  config.SwitchSpecifyInputNames(true);
  // enable graph Optim
#if defined(__arm__) || defined(__aarch64__)
  config.SwitchIrOptim(false);
#else
  config.SwitchIrOptim(use_ir_optim);
#endif
  // enable Memory Optim
  config.EnableMemoryOptim();
  if (use_trt && use_gpu) {
    config.EnableTensorRtEngine(
        1 << 20 /* workspace_size*/,
        32 /* max_batch_size*/,
        20 /* min_subgraph_size*/,
        paddle::AnalysisConfig::Precision::kFloat32 /* precision*/,
        true /* use_static*/,
        false /* use_calib_mode*/);
  }
  predictor_ = std::move(CreatePaddlePredictor(config));
}

bool Model::load_config(const std::string& yaml_input) {
  YAML::Node config = YAML::Load(yaml_input);
  type = config["_Attributes"]["model_type"].as<std::string>();
  num_classes = config["_Attributes"]["num_classes"].as<int>();
  name = config["Model"].as<std::string>();
  std::string version = config["version"].as<std::string>();
  if (version[0] == '0') {
    std::cerr << "[Init] Version of the loaded model is lower than 1.0.0, "
              << "deployment cannot be done, please refer to "
              << "https://github.com/PaddlePaddle/PaddleX/blob/develop/docs"
              << "/tutorials/deploy/upgrade_version.md "
              << "to transfer version." << std::endl;
    return false;
  }
  bool to_rgb = true;
  if (config["TransformsMode"].IsDefined()) {
    std::string mode = config["TransformsMode"].as<std::string>();
    if (mode == "BGR") {
      to_rgb = false;
    } else if (mode != "RGB") {
      std::cerr << "[Init] Only 'RGB' or 'BGR' is supported for TransformsMode"
                << std::endl;
      return false;
    }
  }
  // build data preprocess stream
  transforms_.Init(config["Transforms"], to_rgb);
  // read label list
  labels.clear();
  for (const auto& item : config["_Attributes"]["labels"]) {
    int index = labels.size();
    labels[index] = item.as<std::string>();
  }
  if (config["_init_params"]["input_channel"].IsDefined()) {
    input_channel_ = config["_init_params"]["input_channel"].as<int>();
  } else {
    input_channel_ = 3;
  }
  return true;
}

bool Model::preprocess(const cv::Mat& input_im, ImageBlob* blob) {
  cv::Mat im = input_im.clone();
  if (!transforms_.Run(&im, blob)) {
    return false;
  }
  return true;
}

// use openmp
bool Model::preprocess(const std::vector<cv::Mat>& input_im_batch,
                       std::vector<ImageBlob>* blob_batch,
                       int thread_num) {
  int batch_size = input_im_batch.size();
  bool success = true;
  thread_num = std::min(thread_num, batch_size);
  #pragma omp parallel for num_threads(thread_num)
  for (int i = 0; i < input_im_batch.size(); ++i) {
    cv::Mat im = input_im_batch[i].clone();
    if (!transforms_.Run(&im, &(*blob_batch)[i])) {
      success = false;
    }
  }
  return success;
}

bool Model::predict(const cv::Mat& im, ClsResult* result) {
  inputs_.clear();
  if (type == "detector") {
    std::cerr << "Loading model is a 'detector', DetResult should be passed to "
                 "function predict()!"
                 "to function predict()!" << std::endl;
    return false;
  }
  // im preprocess
  if (!preprocess(im, &inputs_)) {
    std::cerr << "Preprocess failed!" << std::endl;
    return false;
  }
  // predict
  auto in_tensor = predictor_->GetInputTensor("image");
  int h = inputs_.new_im_size_[0];
  int w = inputs_.new_im_size_[1];
  in_tensor->Reshape({1, input_channel_, h, w});
  in_tensor->copy_from_cpu(inputs_.im_data_.data());
  predictor_->ZeroCopyRun();
  // get result
  auto output_names = predictor_->GetOutputNames();
  auto output_tensor = predictor_->GetOutputTensor(output_names[0]);
  std::vector<int> output_shape = output_tensor->shape();
  int size = 1;
  for (const auto& i : output_shape) {
    size *= i;
  }
  outputs_.resize(size);
  output_tensor->copy_to_cpu(outputs_.data());
  // postprocess
  auto ptr = std::max_element(std::begin(outputs_), std::end(outputs_));
  result->category_id = std::distance(std::begin(outputs_), ptr);
  result->score = *ptr;
  result->category = labels[result->category_id];
  return true;
}

bool Model::predict(const std::vector<cv::Mat>& im_batch,
                    std::vector<ClsResult>* results,
                    int thread_num) {
  for (auto& inputs : inputs_batch_) {
    inputs.clear();
  }
  if (type == "detector") {
    std::cerr << "Loading model is a 'detector', DetResult should be passed to "
                 "function predict()!" << std::endl;
    return false;
  } else if (type == "segmenter") {
    std::cerr << "Loading model is a 'segmenter', SegResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  }
  inputs_batch_.assign(im_batch.size(), ImageBlob());
  // preprocess
  if (!preprocess(im_batch, &inputs_batch_, thread_num)) {
    std::cerr << "Preprocess failed!" << std::endl;
    return false;
  }
  // predict
  int batch_size = im_batch.size();
  auto in_tensor = predictor_->GetInputTensor("image");
  int h = inputs_batch_[0].new_im_size_[0];
  int w = inputs_batch_[0].new_im_size_[1];
  in_tensor->Reshape({batch_size, input_channel_, h, w});
  std::vector<float> inputs_data(batch_size * input_channel_ * h * w);
  for (int i = 0; i < batch_size; ++i) {
    std::copy(inputs_batch_[i].im_data_.begin(),
              inputs_batch_[i].im_data_.end(),
              inputs_data.begin() + i * input_channel_ * h * w);
  }
  in_tensor->copy_from_cpu(inputs_data.data());
  // in_tensor->copy_from_cpu(inputs_.im_data_.data());
  predictor_->ZeroCopyRun();
  // get result
  auto output_names = predictor_->GetOutputNames();
  auto output_tensor = predictor_->GetOutputTensor(output_names[0]);
  std::vector<int> output_shape = output_tensor->shape();
  int size = 1;
  for (const auto& i : output_shape) {
    size *= i;
  }
  outputs_.resize(size);
  output_tensor->copy_to_cpu(outputs_.data());
  // postprocess
  (*results).clear();
  (*results).resize(batch_size);
  int single_batch_size = size / batch_size;
  for (int i = 0; i < batch_size; ++i) {
    auto start_ptr = std::begin(outputs_);
    auto end_ptr = std::begin(outputs_);
    std::advance(start_ptr, i * single_batch_size);
    std::advance(end_ptr, (i + 1) * single_batch_size);
    auto ptr = std::max_element(start_ptr, end_ptr);
    (*results)[i].category_id = std::distance(start_ptr, ptr);
    (*results)[i].score = *ptr;
    (*results)[i].category = labels[(*results)[i].category_id];
  }
  return true;
}

bool Model::predict(const cv::Mat& im, DetResult* result) {
  inputs_.clear();
  result->clear();
  if (type == "classifier") {
    std::cerr << "Loading model is a 'classifier', ClsResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  } else if (type == "segmenter") {
    std::cerr << "Loading model is a 'segmenter', SegResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  }

  // preprocess
  if (!preprocess(im, &inputs_)) {
    std::cerr << "Preprocess failed!" << std::endl;
    return false;
  }

  int h = inputs_.new_im_size_[0];
  int w = inputs_.new_im_size_[1];
  auto im_tensor = predictor_->GetInputTensor("image");
  im_tensor->Reshape({1, input_channel_, h, w});
  im_tensor->copy_from_cpu(inputs_.im_data_.data());

  if (name == "YOLOv3" || name == "PPYOLO") {
    auto im_size_tensor = predictor_->GetInputTensor("im_size");
    im_size_tensor->Reshape({1, 2});
    im_size_tensor->copy_from_cpu(inputs_.ori_im_size_.data());
  } else if (name == "FasterRCNN" || name == "MaskRCNN") {
    auto im_info_tensor = predictor_->GetInputTensor("im_info");
    auto im_shape_tensor = predictor_->GetInputTensor("im_shape");
    im_info_tensor->Reshape({1, 3});
    im_shape_tensor->Reshape({1, 3});
    float ori_h = static_cast<float>(inputs_.ori_im_size_[0]);
    float ori_w = static_cast<float>(inputs_.ori_im_size_[1]);
    float new_h = static_cast<float>(inputs_.new_im_size_[0]);
    float new_w = static_cast<float>(inputs_.new_im_size_[1]);
    float im_info[] = {new_h, new_w, inputs_.scale};
    float im_shape[] = {ori_h, ori_w, 1.0};
    im_info_tensor->copy_from_cpu(im_info);
    im_shape_tensor->copy_from_cpu(im_shape);
  }
  // predict
  predictor_->ZeroCopyRun();

  std::vector<float> output_box;
  auto output_names = predictor_->GetOutputNames();
  auto output_box_tensor = predictor_->GetOutputTensor(output_names[0]);
  std::vector<int> output_box_shape = output_box_tensor->shape();
  int size = 1;
  for (const auto& i : output_box_shape) {
    size *= i;
  }
  output_box.resize(size);
  output_box_tensor->copy_to_cpu(output_box.data());
  if (size < 6) {
    std::cerr << "[WARNING] There's no object detected." << std::endl;
    return true;
  }
  int num_boxes = size / 6;
  // box postprocess
  for (int i = 0; i < num_boxes; ++i) {
    Box box;
    box.category_id = static_cast<int>(round(output_box[i * 6]));
    box.category = labels[box.category_id];
    box.score = output_box[i * 6 + 1];
    float xmin = output_box[i * 6 + 2];
    float ymin = output_box[i * 6 + 3];
    float xmax = output_box[i * 6 + 4];
    float ymax = output_box[i * 6 + 5];
    float w = xmax - xmin + 1;
    float h = ymax - ymin + 1;
    box.coordinate = {xmin, ymin, w, h};
    result->boxes.push_back(std::move(box));
  }
  // mask postprocess
  if (name == "MaskRCNN") {
    std::vector<float> output_mask;
    auto output_mask_tensor = predictor_->GetOutputTensor(output_names[1]);
    std::vector<int> output_mask_shape = output_mask_tensor->shape();
    int masks_size = 1;
    for (const auto& i : output_mask_shape) {
      masks_size *= i;
    }
    int mask_pixels = output_mask_shape[2] * output_mask_shape[3];
    int classes = output_mask_shape[1];
    output_mask.resize(masks_size);
    output_mask_tensor->copy_to_cpu(output_mask.data());
    result->mask_resolution = output_mask_shape[2];
    for (int i = 0; i < result->boxes.size(); ++i) {
      Box* box = &result->boxes[i];
      box->mask.shape = {static_cast<int>(box->coordinate[2]),
                         static_cast<int>(box->coordinate[3])};
      auto begin_mask =
          output_mask.data() + (i * classes + box->category_id) * mask_pixels;
      cv::Mat bin_mask(result->mask_resolution,
                     result->mask_resolution,
                     CV_32FC1,
                     begin_mask);
      cv::resize(bin_mask,
               bin_mask,
               cv::Size(box->mask.shape[0], box->mask.shape[1]));
      cv::threshold(bin_mask, bin_mask, 0.5, 1, cv::THRESH_BINARY);
      auto mask_int_begin = reinterpret_cast<float*>(bin_mask.data);
      auto mask_int_end =
        mask_int_begin + box->mask.shape[0] * box->mask.shape[1];
      box->mask.data.assign(mask_int_begin, mask_int_end);
    }
  }
  return true;
}

bool Model::predict(const std::vector<cv::Mat>& im_batch,
                    std::vector<DetResult>* results,
                    int thread_num) {
  for (auto& inputs : inputs_batch_) {
    inputs.clear();
  }
  if (type == "classifier") {
    std::cerr << "Loading model is a 'classifier', ClsResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  } else if (type == "segmenter") {
    std::cerr << "Loading model is a 'segmenter', SegResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  }

  inputs_batch_.assign(im_batch.size(), ImageBlob());
  int batch_size = im_batch.size();
  // preprocess
  if (!preprocess(im_batch, &inputs_batch_, thread_num)) {
    std::cerr << "Preprocess failed!" << std::endl;
    return false;
  }
  // RCNN model padding
  if (batch_size > 1) {
    if (name == "FasterRCNN" || name == "MaskRCNN") {
      int max_h = -1;
      int max_w = -1;
      for (int i = 0; i < batch_size; ++i) {
        max_h = std::max(max_h, inputs_batch_[i].new_im_size_[0]);
        max_w = std::max(max_w, inputs_batch_[i].new_im_size_[1]);
        // std::cout << "(" << inputs_batch_[i].new_im_size_[0]
        //          << ", " << inputs_batch_[i].new_im_size_[1]
        //          <<  ")" << std::endl;
      }
      thread_num = std::min(thread_num, batch_size);
      #pragma omp parallel for num_threads(thread_num)
      for (int i = 0; i < batch_size; ++i) {
        int h = inputs_batch_[i].new_im_size_[0];
        int w = inputs_batch_[i].new_im_size_[1];
        int c = im_batch[i].channels();
        if (max_h != h || max_w != w) {
          std::vector<float> temp_buffer(c * max_h * max_w);
          float* temp_ptr = temp_buffer.data();
          float* ptr = inputs_batch_[i].im_data_.data();
          for (int cur_channel = c - 1; cur_channel >= 0; --cur_channel) {
            int ori_pos = cur_channel * h * w + (h - 1) * w;
            int des_pos = cur_channel * max_h * max_w + (h - 1) * max_w;
            int last_pos = cur_channel * h * w;
            for (; ori_pos >= last_pos; ori_pos -= w, des_pos -= max_w) {
              memcpy(temp_ptr + des_pos, ptr + ori_pos, w * sizeof(float));
            }
          }
          inputs_batch_[i].im_data_.swap(temp_buffer);
          inputs_batch_[i].new_im_size_[0] = max_h;
          inputs_batch_[i].new_im_size_[1] = max_w;
        }
      }
    }
  }
  int h = inputs_batch_[0].new_im_size_[0];
  int w = inputs_batch_[0].new_im_size_[1];
  auto im_tensor = predictor_->GetInputTensor("image");
  im_tensor->Reshape({batch_size, input_channel_, h, w});
  std::vector<float> inputs_data(batch_size * input_channel_ * h * w);
  for (int i = 0; i < batch_size; ++i) {
    std::copy(inputs_batch_[i].im_data_.begin(),
              inputs_batch_[i].im_data_.end(),
              inputs_data.begin() + i * input_channel_ * h * w);
  }
  im_tensor->copy_from_cpu(inputs_data.data());
  if (name == "YOLOv3" || name == "PPYOLO") {
    auto im_size_tensor = predictor_->GetInputTensor("im_size");
    im_size_tensor->Reshape({batch_size, 2});
    std::vector<int> inputs_data_size(batch_size * 2);
    for (int i = 0; i < batch_size; ++i) {
      std::copy(inputs_batch_[i].ori_im_size_.begin(),
                inputs_batch_[i].ori_im_size_.end(),
                inputs_data_size.begin() + 2 * i);
    }
    im_size_tensor->copy_from_cpu(inputs_data_size.data());
  } else if (name == "FasterRCNN" || name == "MaskRCNN") {
    auto im_info_tensor = predictor_->GetInputTensor("im_info");
    auto im_shape_tensor = predictor_->GetInputTensor("im_shape");
    im_info_tensor->Reshape({batch_size, 3});
    im_shape_tensor->Reshape({batch_size, 3});

    std::vector<float> im_info(3 * batch_size);
    std::vector<float> im_shape(3 * batch_size);
    for (int i = 0; i < batch_size; ++i) {
      float ori_h = static_cast<float>(inputs_batch_[i].ori_im_size_[0]);
      float ori_w = static_cast<float>(inputs_batch_[i].ori_im_size_[1]);
      float new_h = static_cast<float>(inputs_batch_[i].new_im_size_[0]);
      float new_w = static_cast<float>(inputs_batch_[i].new_im_size_[1]);
      im_info[i * 3] = new_h;
      im_info[i * 3 + 1] = new_w;
      im_info[i * 3 + 2] = inputs_batch_[i].scale;
      im_shape[i * 3] = ori_h;
      im_shape[i * 3 + 1] = ori_w;
      im_shape[i * 3 + 2] = 1.0;
    }
    im_info_tensor->copy_from_cpu(im_info.data());
    im_shape_tensor->copy_from_cpu(im_shape.data());
  }
  // predict
  predictor_->ZeroCopyRun();

  // get all box
  std::vector<float> output_box;
  auto output_names = predictor_->GetOutputNames();
  auto output_box_tensor = predictor_->GetOutputTensor(output_names[0]);
  std::vector<int> output_box_shape = output_box_tensor->shape();
  int size = 1;
  for (const auto& i : output_box_shape) {
    size *= i;
  }
  output_box.resize(size);
  output_box_tensor->copy_to_cpu(output_box.data());
  if (size < 6) {
    std::cerr << "[WARNING] There's no object detected." << std::endl;
    return true;
  }
  auto lod_vector = output_box_tensor->lod();
  int num_boxes = size / 6;
  // box postprocess
  (*results).clear();
  (*results).resize(batch_size);
  for (int i = 0; i < lod_vector[0].size() - 1; ++i) {
    for (int j = lod_vector[0][i]; j < lod_vector[0][i + 1]; ++j) {
      Box box;
      box.category_id = static_cast<int>(round(output_box[j * 6]));
      box.category = labels[box.category_id];
      box.score = output_box[j * 6 + 1];
      float xmin = output_box[j * 6 + 2];
      float ymin = output_box[j * 6 + 3];
      float xmax = output_box[j * 6 + 4];
      float ymax = output_box[j * 6 + 5];
      float w = xmax - xmin + 1;
      float h = ymax - ymin + 1;
      box.coordinate = {xmin, ymin, w, h};
      (*results)[i].boxes.push_back(std::move(box));
    }
  }

  // mask postprocess
  if (name == "MaskRCNN") {
    std::vector<float> output_mask;
    auto output_mask_tensor = predictor_->GetOutputTensor(output_names[1]);
    std::vector<int> output_mask_shape = output_mask_tensor->shape();
    int masks_size = 1;
    for (const auto& i : output_mask_shape) {
      masks_size *= i;
    }
    int mask_pixels = output_mask_shape[2] * output_mask_shape[3];
    int classes = output_mask_shape[1];
    output_mask.resize(masks_size);
    output_mask_tensor->copy_to_cpu(output_mask.data());
    int mask_idx = 0;
    for (int i = 0; i < lod_vector[0].size() - 1; ++i) {
      (*results)[i].mask_resolution = output_mask_shape[2];
      for (int j = 0; j < (*results)[i].boxes.size(); ++j) {
        Box* box = &(*results)[i].boxes[i];
        int category_id = box->category_id;
        box->mask.shape = {static_cast<int>(box->coordinate[2]),
                          static_cast<int>(box->coordinate[3])};
        auto begin_mask =
          output_mask.data() + (i * classes + box->category_id) * mask_pixels;
        cv::Mat bin_mask(output_mask_shape[2],
                      output_mask_shape[2],
                      CV_32FC1,
                      begin_mask);
        cv::resize(bin_mask,
                bin_mask,
                cv::Size(box->mask.shape[0], box->mask.shape[1]));
        cv::threshold(bin_mask, bin_mask, 0.5, 1, cv::THRESH_BINARY);
        auto mask_int_begin = reinterpret_cast<float*>(bin_mask.data);
        auto mask_int_end =
          mask_int_begin + box->mask.shape[0] * box->mask.shape[1];
        box->mask.data.assign(mask_int_begin, mask_int_end);
        mask_idx++;
      }
    }
  }
  return true;
}

bool Model::predict(const cv::Mat& im, SegResult* result) {
  result->clear();
  inputs_.clear();
  if (type == "classifier") {
    std::cerr << "Loading model is a 'classifier', ClsResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  } else if (type == "detector") {
    std::cerr << "Loading model is a 'detector', DetResult should be passed to "
                 "function predict()!" << std::endl;
    return false;
  }

  // preprocess
  if (!preprocess(im, &inputs_)) {
    std::cerr << "Preprocess failed!" << std::endl;
    return false;
  }

  int h = inputs_.new_im_size_[0];
  int w = inputs_.new_im_size_[1];
  auto im_tensor = predictor_->GetInputTensor("image");
  im_tensor->Reshape({1, input_channel_, h, w});
  im_tensor->copy_from_cpu(inputs_.im_data_.data());

  // predict
  predictor_->ZeroCopyRun();

  // get labelmap
  auto output_names = predictor_->GetOutputNames();
  auto output_label_tensor = predictor_->GetOutputTensor(output_names[0]);
  std::vector<int> output_label_shape = output_label_tensor->shape();
  int size = 1;
  for (const auto& i : output_label_shape) {
    size *= i;
    result->label_map.shape.push_back(i);
  }

  result->label_map.data.resize(size);
  output_label_tensor->copy_to_cpu(result->label_map.data.data());

  // get scoremap
  auto output_score_tensor = predictor_->GetOutputTensor(output_names[1]);
  std::vector<int> output_score_shape = output_score_tensor->shape();
  size = 1;
  for (const auto& i : output_score_shape) {
    size *= i;
    result->score_map.shape.push_back(i);
  }

  result->score_map.data.resize(size);
  output_score_tensor->copy_to_cpu(result->score_map.data.data());

  // get origin image result
  std::vector<uint8_t> label_map(result->label_map.data.begin(),
                                 result->label_map.data.end());
  cv::Mat mask_label(result->label_map.shape[1],
                     result->label_map.shape[2],
                     CV_8UC1,
                     label_map.data());

  cv::Mat mask_score(result->score_map.shape[2],
                     result->score_map.shape[3],
                     CV_32FC1,
                     result->score_map.data.data());
  int idx = 1;
  int len_postprocess = inputs_.im_size_before_resize_.size();
  for (std::vector<std::string>::reverse_iterator iter =
           inputs_.reshape_order_.rbegin();
       iter != inputs_.reshape_order_.rend();
       ++iter) {
    if (*iter == "padding") {
      auto before_shape = inputs_.im_size_before_resize_[len_postprocess - idx];
      inputs_.im_size_before_resize_.pop_back();
      auto padding_w = before_shape[0];
      auto padding_h = before_shape[1];
      mask_label = mask_label(cv::Rect(0, 0, padding_h, padding_w));
      mask_score = mask_score(cv::Rect(0, 0, padding_h, padding_w));
    } else if (*iter == "resize") {
      auto before_shape = inputs_.im_size_before_resize_[len_postprocess - idx];
      inputs_.im_size_before_resize_.pop_back();
      auto resize_w = before_shape[0];
      auto resize_h = before_shape[1];
      cv::resize(mask_label,
                 mask_label,
                 cv::Size(resize_h, resize_w),
                 0,
                 0,
                 cv::INTER_NEAREST);
      cv::resize(mask_score,
                 mask_score,
                 cv::Size(resize_h, resize_w),
                 0,
                 0,
                 cv::INTER_LINEAR);
    }
    ++idx;
  }
  result->label_map.data.assign(mask_label.begin<uint8_t>(),
                                mask_label.end<uint8_t>());
  result->label_map.shape = {mask_label.rows, mask_label.cols};
  result->score_map.data.assign(mask_score.begin<float>(),
                                mask_score.end<float>());
  result->score_map.shape = {mask_score.rows, mask_score.cols};
  return true;
}

bool Model::predict(const std::vector<cv::Mat>& im_batch,
                    std::vector<SegResult>* results,
                    int thread_num) {
  for (auto& inputs : inputs_batch_) {
    inputs.clear();
  }
  if (type == "classifier") {
    std::cerr << "Loading model is a 'classifier', ClsResult should be passed "
                 "to function predict()!" << std::endl;
    return false;
  } else if (type == "detector") {
    std::cerr << "Loading model is a 'detector', DetResult should be passed to "
                 "function predict()!" << std::endl;
    return false;
  }

  // preprocess
  inputs_batch_.assign(im_batch.size(), ImageBlob());
  if (!preprocess(im_batch, &inputs_batch_, thread_num)) {
    std::cerr << "Preprocess failed!" << std::endl;
    return false;
  }

  int batch_size = im_batch.size();
  (*results).clear();
  (*results).resize(batch_size);
  int h = inputs_batch_[0].new_im_size_[0];
  int w = inputs_batch_[0].new_im_size_[1];
  auto im_tensor = predictor_->GetInputTensor("image");
  im_tensor->Reshape({batch_size, input_channel_, h, w});
  std::vector<float> inputs_data(batch_size * input_channel_ * h * w);
  for (int i = 0; i < batch_size; ++i) {
    std::copy(inputs_batch_[i].im_data_.begin(),
              inputs_batch_[i].im_data_.end(),
              inputs_data.begin() + i * input_channel_ * h * w);
  }
  im_tensor->copy_from_cpu(inputs_data.data());
  // im_tensor->copy_from_cpu(inputs_.im_data_.data());

  // predict
  predictor_->ZeroCopyRun();

  // get labelmap
  auto output_names = predictor_->GetOutputNames();
  auto output_label_tensor = predictor_->GetOutputTensor(output_names[0]);
  std::vector<int> output_label_shape = output_label_tensor->shape();
  int size = 1;
  for (const auto& i : output_label_shape) {
    size *= i;
  }

  std::vector<int64_t> output_labels(size, 0);
  output_label_tensor->copy_to_cpu(output_labels.data());
  auto output_labels_iter = output_labels.begin();

  int single_batch_size = size / batch_size;
  for (int i = 0; i < batch_size; ++i) {
    (*results)[i].label_map.data.resize(single_batch_size);
    (*results)[i].label_map.shape.push_back(1);
    for (int j = 1; j < output_label_shape.size(); ++j) {
      (*results)[i].label_map.shape.push_back(output_label_shape[j]);
    }
    std::copy(output_labels_iter + i * single_batch_size,
              output_labels_iter + (i + 1) * single_batch_size,
              (*results)[i].label_map.data.data());
  }

  // get scoremap
  auto output_score_tensor = predictor_->GetOutputTensor(output_names[1]);
  std::vector<int> output_score_shape = output_score_tensor->shape();
  size = 1;
  for (const auto& i : output_score_shape) {
    size *= i;
  }

  std::vector<float> output_scores(size, 0);
  output_score_tensor->copy_to_cpu(output_scores.data());
  auto output_scores_iter = output_scores.begin();

  int single_batch_score_size = size / batch_size;
  for (int i = 0; i < batch_size; ++i) {
    (*results)[i].score_map.data.resize(single_batch_score_size);
    (*results)[i].score_map.shape.push_back(1);
    for (int j = 1; j < output_score_shape.size(); ++j) {
      (*results)[i].score_map.shape.push_back(output_score_shape[j]);
    }
    std::copy(output_scores_iter + i * single_batch_score_size,
              output_scores_iter + (i + 1) * single_batch_score_size,
              (*results)[i].score_map.data.data());
  }

  // get origin image result
  for (int i = 0; i < batch_size; ++i) {
    std::vector<uint8_t> label_map((*results)[i].label_map.data.begin(),
                                   (*results)[i].label_map.data.end());
    cv::Mat mask_label((*results)[i].label_map.shape[1],
                       (*results)[i].label_map.shape[2],
                       CV_8UC1,
                       label_map.data());

    cv::Mat mask_score((*results)[i].score_map.shape[2],
                       (*results)[i].score_map.shape[3],
                       CV_32FC1,
                       (*results)[i].score_map.data.data());
    int idx = 1;
    int len_postprocess = inputs_batch_[i].im_size_before_resize_.size();
    for (std::vector<std::string>::reverse_iterator iter =
             inputs_batch_[i].reshape_order_.rbegin();
         iter != inputs_batch_[i].reshape_order_.rend();
         ++iter) {
      if (*iter == "padding") {
        auto before_shape =
            inputs_batch_[i].im_size_before_resize_[len_postprocess - idx];
        inputs_batch_[i].im_size_before_resize_.pop_back();
        auto padding_w = before_shape[0];
        auto padding_h = before_shape[1];
        mask_label = mask_label(cv::Rect(0, 0, padding_h, padding_w));
        mask_score = mask_score(cv::Rect(0, 0, padding_h, padding_w));
      } else if (*iter == "resize") {
        auto before_shape =
            inputs_batch_[i].im_size_before_resize_[len_postprocess - idx];
        inputs_batch_[i].im_size_before_resize_.pop_back();
        auto resize_w = before_shape[0];
        auto resize_h = before_shape[1];
        cv::resize(mask_label,
                   mask_label,
                   cv::Size(resize_h, resize_w),
                   0,
                   0,
                   cv::INTER_NEAREST);
        cv::resize(mask_score,
                   mask_score,
                   cv::Size(resize_h, resize_w),
                   0,
                   0,
                   cv::INTER_LINEAR);
      }
      ++idx;
    }
    (*results)[i].label_map.data.assign(mask_label.begin<uint8_t>(),
                                       mask_label.end<uint8_t>());
    (*results)[i].label_map.shape = {mask_label.rows, mask_label.cols};
    (*results)[i].score_map.data.assign(mask_score.begin<float>(),
                                       mask_score.end<float>());
    (*results)[i].score_map.shape = {mask_score.rows, mask_score.cols};
  }
  return true;
}

bool Model::load_score_thresholds_config(const std::string& yaml_input) {
  if(access(yaml_input.c_str(), 0)==0){
	  YAML::Node config = YAML::LoadFile(yaml_input);
	  float score_threshold;
	  std::stringstream category_threshold;
	  
	  for(int i=0; i<num_classes; i++) {
		category_threshold.str("");
		category_threshold << "threshold_category_" << i;
		if (config[category_threshold.str().c_str()].IsDefined()) {
		  score_threshold = config[category_threshold.str().c_str()].as<float>();
		} else {
		  if (config["default_threshold"].IsDefined()) {
			  score_threshold = config["default_threshold"].as<float>();
		  } else {
			  return false;
		  }
		}
		score_thresholds.push_back(score_threshold);
	  }
  } else {
	  for(int i=0; i<num_classes; i++) {
		score_thresholds.push_back(0);
	  }
  }
  return true;
}

int Model::paddlex_init(const std::string& model_dir,
                             bool use_gpu,
                             bool use_trt,
                             bool use_mkl,
                             int mkl_thread_num,
                             int gpu_id,
                             std::string key,
                             bool use_ir_optim) {
  int ret;
  
  create_predictor(
				   model_dir,
				   use_gpu,
				   use_trt,
				   use_mkl,
				   mkl_thread_num,
				   gpu_id,
				   key,
				   use_ir_optim);

  std::string score_thresholds_yaml_file = model_dir + OS_PATH_SEP + "score_thresholds.yml";

  if (!load_score_thresholds_config(score_thresholds_yaml_file)) {
	std::cerr << "Parse file 'score_thresholds.yml' failed!" << std::endl;
	exit(-1);
  }

  if (type == "classifier") {
    ret = Model::CLS;
  } else if (type == "detector") {
    ret = Model::DET;
  } else {
    ret = Model::SEG;
  }

  return ret;
}

bool Model::paddlex_predict(const cv::Mat& im, ClsResult* result) {
  ClsResult tmp_result;
  float score;
  bool ret;
  ret = predict(im, &tmp_result);
  if(ret){
  	score = score_thresholds[tmp_result.category_id];
  	if(tmp_result.score>score){
      *result = tmp_result;
	}
  }
  return ret;
}

bool Model::paddlex_predict(const cv::Mat& im, DetResult* result) {
  DetResult in_result;
  DetResult out_result;
  bool flag = false;
  int num_boxes;
  bool ret;
  ret = predict(im, &in_result);
  if(ret){
    num_boxes = in_result.boxes.size();
    for (int i = 0; i < num_boxes; ++i) {
	  Box* box = &in_result.boxes[i];
	  int category_id = box->category_id;
	  if(box->score > score_thresholds[category_id]){
        result->boxes.push_back(*box);
		flag = true;
	  }
    }  
	if(flag){
      result->mask_resolution = in_result.mask_resolution;
      result->type = in_result.type;    
	}
  }
  return ret;
}

bool Model::paddlex_predict(const cv::Mat& im, SegResult* result) {
  return predict(im, result);
}

/*
bool Model::paddlex_predict(unsigned char *img, int height, int width, int channels, ClsResult* result) {
  int format;	 
  switch (channels)	{    
  	case 1:		  
		format = CV_8UC1; 	   
		break;	 
	case 2:		
		format = CV_8UC2;		 
		break;    
	case 3:		  
		format = CV_8UC3; 	   
		break;	 
	default:		 
		format = CV_8UC4;		  
		break;	
  }
  cv::Mat image(height ,width, format, img);
  ClsResult tmp_result;
  float score;
  bool ret;
  ret = predict(image, &tmp_result);
  if(ret){
  	score = score_thresholds[tmp_result.category_id];
  	if(tmp_result.score>score){
      *result = tmp_result;
	}
  }
  return ret;
}

bool Model::paddlex_predict(unsigned char *img, int height, int width, int channels, DetResult* result) {
  int format;	 
  switch (channels)	{    
  	case 1:		  
		format = CV_8UC1; 	   
		break;	 
	case 2:		
		format = CV_8UC2;		 
		break;    
	case 3:		  
		format = CV_8UC3; 	   
		break;	 
	default:		 
		format = CV_8UC4;		  
		break;	
  }
  cv::Mat image(height ,width, format, img);
  DetResult in_result;
  DetResult out_result;
  bool flag = false;
  int num_boxes;
  bool ret;
  ret = predict(image, &in_result);
  if(ret){
    num_boxes = in_result.boxes.size();
    for (int i = 0; i < num_boxes; ++i) {
	  Box* box = &in_result.boxes[i];
	  int category_id = box->category_id;
	  if(box->score > score_thresholds[category_id]){
        result->boxes.push_back(*box);
		flag = true;
	  }
    }  
	if(flag){
      result->mask_resolution = in_result.mask_resolution;
      result->type = in_result.type;    
	}
  }
  return ret;
}

bool Model::paddlex_predict(unsigned char *img, int height, int width, int channels, SegResult* result) {
  int format;    
  switch (channels) {    
    case 1:	  
      format = CV_8UC1;	   
	  break;	 
    case 2:	
	  format = CV_8UC2;		 
	  break;	  
    case 3:	  
	  format = CV_8UC3;	   
	  break;	 
    default:			 
	  format = CV_8UC4;		  
	  break;	
  }
  cv::Mat image(height ,width, format, img);
  return predict(image, result);
}
*/

}  // namespace PaddleX

#define PADDLEXLOG LogPrint((char *)__FILE__,__LINE__,

FILE *g_logfile = NULL;

int LogInit(const std::string& dir)
{
#ifdef _PADDLEX_DEBUG_
	std::string& file_dir = dir + "\\" + "PaddleXLog.txt";
	g_logfile=fopen(file_dir.c_str(),"a");

	if (NULL == g_logfile)
	{
		return 0;
	}
#endif
	return 1;
}

void LogPrint(char *file, int line, const char *fmt,...)
{
#ifdef _PADDLEX_DEBUG_
	char    	buf[4096];
	int		buf_size=4096;
	va_list		args;
    
    va_start(args,fmt);
    _vsnprintf(buf,buf_size,fmt,args);
    va_end(args);
    buf[buf_size -1]='\0';
	
	fprintf(g_logfile, "%s",buf);
	fflush(g_logfile);
#endif
    return;
}

namespace PaddleXCpp {

static std::vector<int> GenerateColorMap(int num_class) {
  auto colormap = std::vector<int>(3 * num_class, 0);
  for (int i = 0; i < num_class; ++i) {
    int j = 0;
    int lab = i;
    while (lab) {
      colormap[i * 3] |= (((lab >> 0) & 1) << (7 - j));
      colormap[i * 3 + 1] |= (((lab >> 1) & 1) << (7 - j));
      colormap[i * 3 + 2] |= (((lab >> 2) & 1) << (7 - j));
      ++j;
      lab >>= 3;
    }
  }
  return colormap;
}

static void DetVisualize(cv::Mat& vis_img,
                     const PaddleX::DetResult& result,
                     const std::map<int, std::string>& labels) {
  auto colormap = GenerateColorMap(labels.size());
//  cv::Mat vis_img = img.clone();
  auto boxes = result.boxes;
  for (int i = 0; i < boxes.size(); ++i) {

    cv::Rect roi = cv::Rect(boxes[i].coordinate[0],
                            boxes[i].coordinate[1],
                            boxes[i].coordinate[2],
                            boxes[i].coordinate[3]);

    // draw box and title
    std::string text = boxes[i].category;
    int c1 = colormap[3 * boxes[i].category_id + 0];
    int c2 = colormap[3 * boxes[i].category_id + 1];
    int c3 = colormap[3 * boxes[i].category_id + 2];
    cv::Scalar roi_color = cv::Scalar(c1, c2, c3);
    text += std::to_string(static_cast<int>(boxes[i].score * 100)) + "%";
    int font_face = cv::FONT_HERSHEY_SIMPLEX;
    double font_scale = 0.5f;
    float thickness = 0.5;
    cv::Size text_size =
        cv::getTextSize(text, font_face, font_scale, thickness, nullptr);
    cv::Point origin;
    origin.x = roi.x;
    origin.y = roi.y;

    // background
    cv::Rect text_back = cv::Rect(boxes[i].coordinate[0],
                                  boxes[i].coordinate[1] - text_size.height,
                                  text_size.width,
                                  text_size.height);

    // draw
    cv::rectangle(vis_img, roi, roi_color, 2);
    cv::rectangle(vis_img, text_back, roi_color, -1);
    cv::putText(vis_img,
                text,
                origin,
                font_face,
                font_scale,
                cv::Scalar(255, 255, 255),
                thickness);

    // mask
    if (boxes[i].mask.data.size() == 0) {
      continue;
    }
    std::vector<float> mask_data;
    mask_data.assign(boxes[i].mask.data.begin(), boxes[i].mask.data.end());
    cv::Mat bin_mask(boxes[i].mask.shape[1],
                     boxes[i].mask.shape[0],
                     CV_32FC1,
                     mask_data.data());
    cv::Mat full_mask = cv::Mat::zeros(vis_img.size(), CV_8UC1);
    bin_mask.copyTo(full_mask(roi));
    cv::Mat mask_ch[3];
    mask_ch[0] = full_mask * c1;
    mask_ch[1] = full_mask * c2;
    mask_ch[2] = full_mask * c3;
    cv::Mat mask;
    cv::merge(mask_ch, 3, mask);
    cv::addWeighted(vis_img, 1, mask, 0.5, 0, vis_img);
  }
  return;
}

static void SegVisualize(cv::Mat& img,
                     const PaddleX::SegResult& result,
                     const std::map<int, std::string>& labels) {
  auto colormap = GenerateColorMap(labels.size());
  std::vector<uint8_t> label_map(result.label_map.data.begin(),
                                 result.label_map.data.end());
  cv::Mat mask(result.label_map.shape[0],
               result.label_map.shape[1],
               CV_8UC1,
               label_map.data());

  int rows = img.rows;
  int cols = img.cols;
  for (int i = 0; i < rows; i++) {
    for (int j = 0; j < cols; j++) {
      int category_id = static_cast<int>(mask.at<uchar>(i, j));
	  if(category_id>0) {
	      img.at<cv::Vec3b>(i, j)[0] = colormap[3 * category_id + 0]/2 + img.at<cv::Vec3b>(i, j)[0]/2;
	      img.at<cv::Vec3b>(i, j)[1] = colormap[3 * category_id + 1]/2 + img.at<cv::Vec3b>(i, j)[1]/2;
	      img.at<cv::Vec3b>(i, j)[2] = colormap[3 * category_id + 2]/2 + img.at<cv::Vec3b>(i, j)[2]/2;
	  }
    }
  }
  return;
}

__declspec(dllexport) void* __cdecl CreatePaddlexModel(int *model_type,
	                         char* model_dir,
                             bool use_gpu,
                             bool use_trt,
                             bool use_mkl,
                             int mkl_thread_num,
                             int gpu_id,
                             char* key,
                             bool use_ir_optim) {
	if(!model_type || !model_dir || !key){
		return NULL;
	}
	
	PaddleX::Model *model = new PaddleX::Model;
	if(!model){
		return NULL;
	}

	std::string modeldir = model_dir;
	
#ifdef _PADDLEX_DEBUG_
	if (!LogInit(modeldir))
	{
		printf("Open log file failed, exit.\n");
		exit(0);
	}
	else
	{
		PADDLEXLOG "\n======== PaddleX Start ========\n");
	}
#endif

	model->model_path = model_dir;
	std::string modelkey = key;
	*model_type = model->paddlex_init(modeldir,
						 use_gpu,
						 use_trt,
						 use_mkl,
						 mkl_thread_num,
						 gpu_id,
						 modelkey,
						 use_ir_optim);
	
	return (void *)(model);
}

__declspec(dllexport) bool __cdecl PaddlexClsPredict(void* model, unsigned char *img, int height, int width, int channels, char **result) {

	PADDLEXLOG "Enter PaddlexClsPredict.\n");
	
	if(!model){
		PADDLEXLOG "ERROR: model is NULL.\n");
		return false;
	}
	
	if(!img){
		PADDLEXLOG "ERROR: img is NULL.\n");
		*result = NULL;
		return false;
	}

	if((!height)||(!width)||(!channels)||(channels>4)){
		PADDLEXLOG "ERROR: img size[height:%d, width:%d] or channels[%d] error.\n", height, width, channels);
		*result = NULL;
		return false;
	}

	int format;    
	switch (channels) {    
	  case 1:	  
		format = CV_8UC1;	   
		break;	 
	  case 2:	
		format = CV_8UC2;		 
		break;	  
	  case 3:	  
		format = CV_8UC3;	   
		break;	 
	  default:			 
		format = CV_8UC4;		  
		break;	
	}
	cv::Mat image(height ,width, format, img);
	
	PaddleX::Model *p_model = (PaddleX::Model *)model;
	PaddleX::ClsResult cls_result;
	char *p_res = NULL;
	
#ifdef _PADDLEX_DEBUG_
	std::string& input_image = p_model->model_path + "\\" + "input_image.jpg";
	cv::imwrite(input_image, image);
#endif

	if(p_model->paddlex_predict(image, &cls_result)){
		p_res = (char *)malloc(sizeof(t_cls_result));
		memset(p_res, 0, sizeof(t_cls_result));
		
		t_cls_result *detres = (t_cls_result *)p_res;
//		int len = cls_result.category.length();
//		strncpy(detres->category, cls_result.category.c_str(), (len>(MAX_CATEGORY_STR_LEN))?len:(MAX_CATEGORY_STR_LEN));
		detres->category_id = cls_result.category_id;
		detres->score = cls_result.score;

		PADDLEXLOG "Cls result : category_id[%f], score[%f].\n", detres->category_id, detres->score);
		
		*result = p_res;
		return true;
	} else {
		PADDLEXLOG "ERROR: Paddlex Cls Predict failed.\n");
		*result = NULL;
		return false;
	}
}

__declspec(dllexport) bool __cdecl PaddlexDetPredict(void* model, unsigned char *img, int height, int width, int channels, char **result, bool visualize) {

	PADDLEXLOG "Enter PaddlexDetPredict\n");

	if(!model){
		PADDLEXLOG "ERROR: model is NULL.\n");
		*result = NULL;
		return false;
	}
	
	if(!img){
		PADDLEXLOG "ERROR: img is NULL.\n");
		*result = NULL;
		return false;
	}

	if((!height)||(!width)||(!channels)||(channels>4)){
		PADDLEXLOG "ERROR: img size[height:%d, width:%d] or channels[%d] error.\n", height, width, channels);
		*result = NULL;
		return false;
	}

	int format;    
	switch (channels) {    
	  case 1:	  
		format = CV_8UC1;	   
		break;	 
	  case 2:	
		format = CV_8UC2;		 
		break;	  
	  case 3:	  
		format = CV_8UC3;	   
		break;	 
	  default:			 
		format = CV_8UC4;		  
		break;	
	}
	cv::Mat image(height ,width, format, img);
	
	PaddleX::Model *p_model = (PaddleX::Model *)model;
	PaddleX::DetResult det_result;
	char *p_res = NULL;

#ifdef _PADDLEX_DEBUG_
	std::string& input_image = p_model->model_path + "\\" + "input_image.jpg";
	cv::imwrite(input_image, image);
#endif
	
	if(p_model->paddlex_predict(image, &det_result)){
		PADDLEXLOG "Paddlex Det Predict SUCC.\n");
		int box_num = det_result.boxes.size();
		if(box_num == 0){
			PADDLEXLOG "box_num is 0.\n");
			p_res = (char *)malloc(sizeof(float));
			*result = p_res;
			*((float *)p_res) = (float)(box_num);
		} else {
			PADDLEXLOG "box_num is %d.\n", box_num);
			int res_size = sizeof(float) + (box_num*sizeof(t_det_result));
			p_res = (char *)malloc(res_size);
			*result = p_res;
			memset(p_res, 0, res_size);
			
			*((float *)p_res) = (float)(box_num);
			
			p_res = p_res + sizeof(float);
			for (int i_box=0; i_box<box_num; i_box++) {
				t_det_result *detres = (t_det_result *)p_res;
//				int len = det_result.boxes[i_box].category.length();
//				strncpy(detres->category, det_result.boxes[i_box].category.c_str(), (len>(MAX_CATEGORY_STR_LEN))?len:(MAX_CATEGORY_STR_LEN));
				detres->category_id = (float)(det_result.boxes[i_box].category_id);
				detres->score = det_result.boxes[i_box].score;
				detres->coordinate[0] = det_result.boxes[i_box].coordinate[0];
				detres->coordinate[1] = det_result.boxes[i_box].coordinate[1];
				detres->coordinate[2] = det_result.boxes[i_box].coordinate[2];
				detres->coordinate[3] = det_result.boxes[i_box].coordinate[3];
				p_res = p_res + sizeof(t_det_result);
				PADDLEXLOG "Box %d : category_id[%f], score[%f], [xmin:%f, ymin:%f, w:%f, h:%f].\n", 
					i_box+1, detres->category_id, detres->score, detres->coordinate[0], detres->coordinate[1], detres->coordinate[2], detres->coordinate[3]);
			}
		}

		if(visualize){
			DetVisualize(image, det_result, p_model->labels);
		}
		
#ifdef _PADDLEX_DEBUG_
		std::string& output_image = p_model->model_path + "\\" + "output_image.jpg";
		cv::imwrite(output_image, image);
#endif
		
		return true;
	} else {
		PADDLEXLOG "ERROR: Paddlex Det Predict failed.\n");
		*result = NULL;
		return false;
	}
}

__declspec(dllexport) bool __cdecl PaddlexSegPredict(void* model, unsigned char *img, int height, int width, int channels, int64_t **label_map, float **score_map, bool visualize) {

	PADDLEXLOG "Enter PaddlexSegPredict\n");

	if(!model){
		PADDLEXLOG "ERROR: model is NULL.\n");
		*label_map = NULL;
		*score_map = NULL;
		return false;
	}
	
	if(!img){
		PADDLEXLOG "ERROR: img is NULL.\n");
		*label_map = NULL;
		*score_map = NULL;
		return false;
	}

	if((!height)||(!width)||(!channels)||(channels>4)){
		PADDLEXLOG "ERROR: img size[height:%d, width:%d] or channels[%d] error.\n", height, width, channels);
		*label_map = NULL;
		*score_map = NULL;
		return false;
	}

	int format;    
	switch (channels) {    
	  case 1:	  
		format = CV_8UC1;	   
		break;	 
	  case 2:	
		format = CV_8UC2;		 
		break;	  
	  case 3:	  
		format = CV_8UC3;	   
		break;	 
	  default:			 
		format = CV_8UC4;		  
		break;	
	}
	cv::Mat image(height ,width, format, img);
	
	PaddleX::Model *p_model = (PaddleX::Model *)model;
	PaddleX::SegResult seg_result;

#ifdef _PADDLEX_DEBUG_
	std::string& input_image = p_model->model_path + "\\" + "input_image.jpg";
	cv::imwrite(input_image, image);
#endif

	if(p_model->paddlex_predict(image, &seg_result)){
		int label_size = seg_result.label_map.data.size();
		int score_size = seg_result.score_map.data.size();
		if(label_size==score_size) {
			unsigned int label_map_memsize = sizeof(int64_t) * label_size;
			void *p_label_map = malloc(label_map_memsize);
			memcpy(p_label_map, (void *)(seg_result.label_map.data.data()), label_map_memsize);
			*label_map = (int64_t *)p_label_map;
			
			unsigned int score_map_memsize = sizeof(float) * score_size;
			void *p_score_map = malloc(score_map_memsize);
			memcpy(p_score_map, (void *)(seg_result.score_map.data.data()), score_map_memsize);
			*score_map = (float *)p_score_map;

			PADDLEXLOG "Paddlex Seg Predict SUCC.\n");

			if(visualize){
				PADDLEXLOG "Paddlex Seg Visualize.\n");
				SegVisualize(image, seg_result, p_model->labels);
			}

#ifdef _PADDLEX_DEBUG_
			std::string& output_image = p_model->model_path + "\\" + "output_image.jpg";
			cv::imwrite(output_image, image);
#endif
					
			return true;
		} else {
			PADDLEXLOG "ERROR: label_size[%d] is not equal to score_size[%d].\n", label_size, score_size);
			*label_map = NULL;
			*score_map = NULL;
			return false;
		}
	} else {
		PADDLEXLOG "ERROR: Paddlex Seg Predict failed.\n");
		*label_map = NULL;
		*score_map = NULL;
		return false;
	}
}

}
