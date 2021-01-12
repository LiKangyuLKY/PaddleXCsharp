namespace PaddleXCsharp
{
    partial class DoubleCamera
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DoubleCamera));
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tbUseNum = new System.Windows.Forms.TextBox();
            this.tbOnlineNum = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cameraType = new System.Windows.Forms.ComboBox();
            this.bnEnum = new System.Windows.Forms.Button();
            this.bnClose = new System.Windows.Forms.Button();
            this.bnOpen = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lblCam2 = new System.Windows.Forms.Label();
            this.lblCam1 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rbCamera2 = new System.Windows.Forms.RadioButton();
            this.rbCamera1 = new System.Windows.Forms.RadioButton();
            this.bnSetParam = new System.Windows.Forms.Button();
            this.bnGetParam = new System.Windows.Forms.Button();
            this.bnStopGrab = new System.Windows.Forms.Button();
            this.bnStartGrab = new System.Windows.Forms.Button();
            this.tbGain = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbExposure = new System.Windows.Forms.TextBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.bnThreshold = new System.Windows.Forms.Button();
            this.tbModeltype = new System.Windows.Forms.TextBox();
            this.bnLoadModel = new System.Windows.Forms.Button();
            this.bnStartDetection = new System.Windows.Forms.Button();
            this.bnSaveImage = new System.Windows.Forms.Button();
            this.bnStopDetection = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox3.BackgroundImage")));
            this.pictureBox3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox3.Location = new System.Drawing.Point(12, 12);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(212, 170);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox3.TabIndex = 40;
            this.pictureBox3.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tbUseNum);
            this.groupBox3.Controls.Add(this.tbOnlineNum);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.cameraType);
            this.groupBox3.Controls.Add(this.bnEnum);
            this.groupBox3.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox3.Location = new System.Drawing.Point(235, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(243, 170);
            this.groupBox3.TabIndex = 41;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "初始化";
            // 
            // tbUseNum
            // 
            this.tbUseNum.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbUseNum.Location = new System.Drawing.Point(131, 127);
            this.tbUseNum.Name = "tbUseNum";
            this.tbUseNum.Size = new System.Drawing.Size(100, 23);
            this.tbUseNum.TabIndex = 44;
            // 
            // tbOnlineNum
            // 
            this.tbOnlineNum.Enabled = false;
            this.tbOnlineNum.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbOnlineNum.Location = new System.Drawing.Point(131, 78);
            this.tbOnlineNum.Name = "tbOnlineNum";
            this.tbOnlineNum.Size = new System.Drawing.Size(100, 23);
            this.tbOnlineNum.TabIndex = 42;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(15, 131);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 14);
            this.label2.TabIndex = 43;
            this.label2.Text = "使用设备数量";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(15, 82);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 14);
            this.label1.TabIndex = 42;
            this.label1.Text = "在线设备数量";
            // 
            // cameraType
            // 
            this.cameraType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cameraType.FormattingEnabled = true;
            this.cameraType.Items.AddRange(new object[] {
            "海康相机",
            "Basler相机"});
            this.cameraType.Location = new System.Drawing.Point(15, 25);
            this.cameraType.Name = "cameraType";
            this.cameraType.Size = new System.Drawing.Size(91, 22);
            this.cameraType.TabIndex = 1;
            // 
            // bnEnum
            // 
            this.bnEnum.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnEnum.Location = new System.Drawing.Point(131, 24);
            this.bnEnum.Name = "bnEnum";
            this.bnEnum.Size = new System.Drawing.Size(100, 25);
            this.bnEnum.TabIndex = 2;
            this.bnEnum.Text = "查找设备";
            this.bnEnum.UseVisualStyleBackColor = true;
            this.bnEnum.Click += new System.EventHandler(this.BnEnum_Click);
            // 
            // bnClose
            // 
            this.bnClose.Enabled = false;
            this.bnClose.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnClose.Location = new System.Drawing.Point(126, 77);
            this.bnClose.Name = "bnClose";
            this.bnClose.Size = new System.Drawing.Size(100, 25);
            this.bnClose.TabIndex = 4;
            this.bnClose.Text = "关闭设备";
            this.bnClose.UseVisualStyleBackColor = true;
            this.bnClose.Click += new System.EventHandler(this.BnClose_Click);
            // 
            // bnOpen
            // 
            this.bnOpen.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnOpen.Location = new System.Drawing.Point(18, 77);
            this.bnOpen.Name = "bnOpen";
            this.bnOpen.Size = new System.Drawing.Size(100, 25);
            this.bnOpen.TabIndex = 3;
            this.bnOpen.Text = "打开设备";
            this.bnOpen.UseVisualStyleBackColor = true;
            this.bnOpen.Click += new System.EventHandler(this.BnOpen_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.lblCam2);
            this.groupBox4.Controls.Add(this.lblCam1);
            this.groupBox4.Controls.Add(this.pictureBox2);
            this.groupBox4.Controls.Add(this.pictureBox1);
            this.groupBox4.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox4.Location = new System.Drawing.Point(12, 188);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(1210, 561);
            this.groupBox4.TabIndex = 42;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "检测界面";
            // 
            // lblCam2
            // 
            this.lblCam2.AutoSize = true;
            this.lblCam2.BackColor = System.Drawing.Color.Red;
            this.lblCam2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCam2.ForeColor = System.Drawing.Color.White;
            this.lblCam2.Location = new System.Drawing.Point(613, 22);
            this.lblCam2.Name = "lblCam2";
            this.lblCam2.Size = new System.Drawing.Size(40, 16);
            this.lblCam2.TabIndex = 30;
            this.lblCam2.Text = "CAM2";
            // 
            // lblCam1
            // 
            this.lblCam1.AutoSize = true;
            this.lblCam1.BackColor = System.Drawing.Color.Red;
            this.lblCam1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCam1.ForeColor = System.Drawing.Color.White;
            this.lblCam1.Location = new System.Drawing.Point(16, 22);
            this.lblCam1.Name = "lblCam1";
            this.lblCam1.Size = new System.Drawing.Size(40, 16);
            this.lblCam1.TabIndex = 29;
            this.lblCam1.Text = "CAM1";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pictureBox2.Location = new System.Drawing.Point(613, 22);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(581, 525);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 28;
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pictureBox1.Location = new System.Drawing.Point(16, 22);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(581, 525);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 27;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rbCamera2);
            this.groupBox2.Controls.Add(this.rbCamera1);
            this.groupBox2.Controls.Add(this.bnSetParam);
            this.groupBox2.Controls.Add(this.bnGetParam);
            this.groupBox2.Controls.Add(this.bnStopGrab);
            this.groupBox2.Controls.Add(this.bnClose);
            this.groupBox2.Controls.Add(this.bnStartGrab);
            this.groupBox2.Controls.Add(this.bnOpen);
            this.groupBox2.Controls.Add(this.tbGain);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.tbExposure);
            this.groupBox2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox2.Location = new System.Drawing.Point(489, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(470, 170);
            this.groupBox2.TabIndex = 43;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "相机操作";
            // 
            // rbCamera2
            // 
            this.rbCamera2.AutoSize = true;
            this.rbCamera2.Location = new System.Drawing.Point(126, 27);
            this.rbCamera2.Name = "rbCamera2";
            this.rbCamera2.Size = new System.Drawing.Size(74, 18);
            this.rbCamera2.TabIndex = 45;
            this.rbCamera2.Text = "2# 相机";
            this.rbCamera2.UseVisualStyleBackColor = true;
            // 
            // rbCamera1
            // 
            this.rbCamera1.AutoSize = true;
            this.rbCamera1.Checked = true;
            this.rbCamera1.Location = new System.Drawing.Point(18, 27);
            this.rbCamera1.Name = "rbCamera1";
            this.rbCamera1.Size = new System.Drawing.Size(74, 18);
            this.rbCamera1.TabIndex = 44;
            this.rbCamera1.TabStop = true;
            this.rbCamera1.Text = "1# 相机";
            this.rbCamera1.UseVisualStyleBackColor = true;
            // 
            // bnSetParam
            // 
            this.bnSetParam.Enabled = false;
            this.bnSetParam.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnSetParam.Location = new System.Drawing.Point(356, 25);
            this.bnSetParam.Name = "bnSetParam";
            this.bnSetParam.Size = new System.Drawing.Size(100, 25);
            this.bnSetParam.TabIndex = 8;
            this.bnSetParam.Text = "设置参数";
            this.bnSetParam.UseVisualStyleBackColor = true;
            this.bnSetParam.Click += new System.EventHandler(this.BnSetParam_Click);
            // 
            // bnGetParam
            // 
            this.bnGetParam.Enabled = false;
            this.bnGetParam.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnGetParam.Location = new System.Drawing.Point(245, 24);
            this.bnGetParam.Name = "bnGetParam";
            this.bnGetParam.Size = new System.Drawing.Size(100, 25);
            this.bnGetParam.TabIndex = 7;
            this.bnGetParam.Text = "获取参数";
            this.bnGetParam.UseVisualStyleBackColor = true;
            this.bnGetParam.Click += new System.EventHandler(this.BnGetParam_Click);
            // 
            // bnStopGrab
            // 
            this.bnStopGrab.Enabled = false;
            this.bnStopGrab.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnStopGrab.Location = new System.Drawing.Point(126, 126);
            this.bnStopGrab.Name = "bnStopGrab";
            this.bnStopGrab.Size = new System.Drawing.Size(100, 25);
            this.bnStopGrab.TabIndex = 6;
            this.bnStopGrab.Text = "停止采集";
            this.bnStopGrab.UseVisualStyleBackColor = true;
            this.bnStopGrab.Click += new System.EventHandler(this.BnStopGrab_Click);
            // 
            // bnStartGrab
            // 
            this.bnStartGrab.Enabled = false;
            this.bnStartGrab.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnStartGrab.Location = new System.Drawing.Point(18, 126);
            this.bnStartGrab.Name = "bnStartGrab";
            this.bnStartGrab.Size = new System.Drawing.Size(100, 25);
            this.bnStartGrab.TabIndex = 5;
            this.bnStartGrab.Text = "开始采集";
            this.bnStartGrab.UseVisualStyleBackColor = true;
            this.bnStartGrab.Click += new System.EventHandler(this.BnStartGrab_Click);
            // 
            // tbGain
            // 
            this.tbGain.Enabled = false;
            this.tbGain.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbGain.Location = new System.Drawing.Point(356, 128);
            this.tbGain.Name = "tbGain";
            this.tbGain.Size = new System.Drawing.Size(100, 23);
            this.tbGain.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(245, 131);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 14);
            this.label3.TabIndex = 11;
            this.label3.Text = "增  益";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(245, 82);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 14);
            this.label4.TabIndex = 9;
            this.label4.Text = "曝  光";
            // 
            // tbExposure
            // 
            this.tbExposure.Enabled = false;
            this.tbExposure.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbExposure.Location = new System.Drawing.Point(356, 79);
            this.tbExposure.Name = "tbExposure";
            this.tbExposure.Size = new System.Drawing.Size(100, 23);
            this.tbExposure.TabIndex = 10;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.bnThreshold);
            this.groupBox5.Controls.Add(this.tbModeltype);
            this.groupBox5.Controls.Add(this.bnLoadModel);
            this.groupBox5.Controls.Add(this.bnStartDetection);
            this.groupBox5.Controls.Add(this.bnSaveImage);
            this.groupBox5.Controls.Add(this.bnStopDetection);
            this.groupBox5.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox5.Location = new System.Drawing.Point(977, 10);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(244, 172);
            this.groupBox5.TabIndex = 44;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "检测操作";
            // 
            // bnThreshold
            // 
            this.bnThreshold.Enabled = false;
            this.bnThreshold.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnThreshold.Location = new System.Drawing.Point(18, 128);
            this.bnThreshold.Name = "bnThreshold";
            this.bnThreshold.Size = new System.Drawing.Size(100, 25);
            this.bnThreshold.TabIndex = 43;
            this.bnThreshold.Text = "阈值调整";
            this.bnThreshold.UseVisualStyleBackColor = true;
            this.bnThreshold.Click += new System.EventHandler(this.BnThreshold_Click);
            // 
            // tbModeltype
            // 
            this.tbModeltype.Enabled = false;
            this.tbModeltype.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbModeltype.Location = new System.Drawing.Point(134, 25);
            this.tbModeltype.Name = "tbModeltype";
            this.tbModeltype.Size = new System.Drawing.Size(100, 23);
            this.tbModeltype.TabIndex = 41;
            this.tbModeltype.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // bnLoadModel
            // 
            this.bnLoadModel.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnLoadModel.Location = new System.Drawing.Point(18, 24);
            this.bnLoadModel.Name = "bnLoadModel";
            this.bnLoadModel.Size = new System.Drawing.Size(100, 25);
            this.bnLoadModel.TabIndex = 13;
            this.bnLoadModel.Text = "加载模型";
            this.bnLoadModel.UseVisualStyleBackColor = true;
            this.bnLoadModel.Click += new System.EventHandler(this.BnLoadModel_Click);
            // 
            // bnStartDetection
            // 
            this.bnStartDetection.Enabled = false;
            this.bnStartDetection.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnStartDetection.Location = new System.Drawing.Point(18, 78);
            this.bnStartDetection.Name = "bnStartDetection";
            this.bnStartDetection.Size = new System.Drawing.Size(100, 25);
            this.bnStartDetection.TabIndex = 14;
            this.bnStartDetection.Text = "开始检测";
            this.bnStartDetection.UseVisualStyleBackColor = true;
            this.bnStartDetection.Click += new System.EventHandler(this.BnStartDetection_Click);
            // 
            // bnSaveImage
            // 
            this.bnSaveImage.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnSaveImage.Location = new System.Drawing.Point(134, 128);
            this.bnSaveImage.Name = "bnSaveImage";
            this.bnSaveImage.Size = new System.Drawing.Size(100, 25);
            this.bnSaveImage.TabIndex = 16;
            this.bnSaveImage.Text = "保存图片";
            this.bnSaveImage.UseVisualStyleBackColor = true;
            this.bnSaveImage.Click += new System.EventHandler(this.BnSaveImage_Click);
            // 
            // bnStopDetection
            // 
            this.bnStopDetection.Enabled = false;
            this.bnStopDetection.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnStopDetection.Location = new System.Drawing.Point(134, 79);
            this.bnStopDetection.Name = "bnStopDetection";
            this.bnStopDetection.Size = new System.Drawing.Size(100, 25);
            this.bnStopDetection.TabIndex = 15;
            this.bnStopDetection.Text = "停止检测";
            this.bnStopDetection.UseVisualStyleBackColor = true;
            this.bnStopDetection.Click += new System.EventHandler(this.BnStopDetection_Click);
            // 
            // DoubleCamera
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1234, 761);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.pictureBox3);
            this.Name = "DoubleCamera";
            this.Text = "深度学习工业检测（多相机模式）";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SingleCamera_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button bnClose;
        private System.Windows.Forms.Button bnEnum;
        private System.Windows.Forms.Button bnOpen;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.ComboBox cameraType;
        private System.Windows.Forms.TextBox tbUseNum;
        private System.Windows.Forms.TextBox tbOnlineNum;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button bnSetParam;
        private System.Windows.Forms.Button bnGetParam;
        private System.Windows.Forms.Button bnStopGrab;
        private System.Windows.Forms.Button bnStartGrab;
        private System.Windows.Forms.TextBox tbGain;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbExposure;
        private System.Windows.Forms.RadioButton rbCamera2;
        private System.Windows.Forms.RadioButton rbCamera1;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TextBox tbModeltype;
        private System.Windows.Forms.Button bnLoadModel;
        private System.Windows.Forms.Button bnStartDetection;
        private System.Windows.Forms.Button bnSaveImage;
        private System.Windows.Forms.Button bnStopDetection;
        private System.Windows.Forms.Label lblCam2;
        private System.Windows.Forms.Label lblCam1;
        private System.Windows.Forms.Button bnThreshold;
    }
}