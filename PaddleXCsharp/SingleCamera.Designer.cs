namespace PaddleXCsharp
{
    partial class SingleCamera
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SingleCamera));
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.bnLoadModel = new System.Windows.Forms.Button();
            this.bnStartDetection = new System.Windows.Forms.Button();
            this.bnSaveImage = new System.Windows.Forms.Button();
            this.bnStopDetection = new System.Windows.Forms.Button();
            this.bnSetParam = new System.Windows.Forms.Button();
            this.bnGetParam = new System.Windows.Forms.Button();
            this.bnStopGrab = new System.Windows.Forms.Button();
            this.bnStartGrab = new System.Windows.Forms.Button();
            this.tbGain = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbExposure = new System.Windows.Forms.TextBox();
            this.cbDeviceList = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.cameraType = new System.Windows.Forms.ComboBox();
            this.bnClose = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.bnEnum = new System.Windows.Forms.Button();
            this.bnOpen = new System.Windows.Forms.Button();
            this.groupBox5.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.bnLoadModel);
            this.groupBox5.Controls.Add(this.bnStartDetection);
            this.groupBox5.Controls.Add(this.bnSaveImage);
            this.groupBox5.Controls.Add(this.bnStopDetection);
            this.groupBox5.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox5.Location = new System.Drawing.Point(831, 508);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(260, 198);
            this.groupBox5.TabIndex = 40;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "检测操作";
            // 
            // bnLoadModel
            // 
            this.bnLoadModel.Enabled = false;
            this.bnLoadModel.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnLoadModel.Location = new System.Drawing.Point(25, 36);
            this.bnLoadModel.Name = "bnLoadModel";
            this.bnLoadModel.Size = new System.Drawing.Size(100, 25);
            this.bnLoadModel.TabIndex = 13;
            this.bnLoadModel.Text = "加载模型";
            this.bnLoadModel.UseVisualStyleBackColor = true;
            this.bnLoadModel.Click += new System.EventHandler(this.BnLoadModel_Click);
            // 
            // bnStartDetection
            // 
            this.bnStartDetection.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnStartDetection.Location = new System.Drawing.Point(141, 36);
            this.bnStartDetection.Name = "bnStartDetection";
            this.bnStartDetection.Size = new System.Drawing.Size(100, 25);
            this.bnStartDetection.TabIndex = 14;
            this.bnStartDetection.Text = "开始检测";
            this.bnStartDetection.UseVisualStyleBackColor = true;
            this.bnStartDetection.Click += new System.EventHandler(this.BnStartDetection_Click);
            // 
            // bnSaveImage
            // 
            this.bnSaveImage.Enabled = false;
            this.bnSaveImage.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnSaveImage.Location = new System.Drawing.Point(141, 77);
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
            this.bnStopDetection.Location = new System.Drawing.Point(25, 78);
            this.bnStopDetection.Name = "bnStopDetection";
            this.bnStopDetection.Size = new System.Drawing.Size(100, 25);
            this.bnStopDetection.TabIndex = 15;
            this.bnStopDetection.Text = "停止检测";
            this.bnStopDetection.UseVisualStyleBackColor = true;
            this.bnStopDetection.Click += new System.EventHandler(this.BnStopDetection_Click);
            // 
            // bnSetParam
            // 
            this.bnSetParam.Enabled = false;
            this.bnSetParam.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnSetParam.Location = new System.Drawing.Point(141, 76);
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
            this.bnGetParam.Location = new System.Drawing.Point(25, 76);
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
            this.bnStopGrab.Location = new System.Drawing.Point(141, 31);
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
            this.bnStartGrab.Location = new System.Drawing.Point(25, 31);
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
            this.tbGain.Location = new System.Drawing.Point(141, 164);
            this.tbGain.Name = "tbGain";
            this.tbGain.Size = new System.Drawing.Size(100, 23);
            this.tbGain.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(25, 167);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 14);
            this.label2.TabIndex = 11;
            this.label2.Text = "增  益";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(25, 124);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 14);
            this.label1.TabIndex = 9;
            this.label1.Text = "曝  光";
            // 
            // tbExposure
            // 
            this.tbExposure.Enabled = false;
            this.tbExposure.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbExposure.Location = new System.Drawing.Point(141, 121);
            this.tbExposure.Name = "tbExposure";
            this.tbExposure.Size = new System.Drawing.Size(100, 23);
            this.tbExposure.TabIndex = 10;
            // 
            // cbDeviceList
            // 
            this.cbDeviceList.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbDeviceList.FormattingEnabled = true;
            this.cbDeviceList.Location = new System.Drawing.Point(12, 21);
            this.cbDeviceList.Name = "cbDeviceList";
            this.cbDeviceList.Size = new System.Drawing.Size(803, 22);
            this.cbDeviceList.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.bnSetParam);
            this.groupBox2.Controls.Add(this.bnGetParam);
            this.groupBox2.Controls.Add(this.bnStopGrab);
            this.groupBox2.Controls.Add(this.bnStartGrab);
            this.groupBox2.Controls.Add(this.tbGain);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.tbExposure);
            this.groupBox2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox2.Location = new System.Drawing.Point(831, 293);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(260, 209);
            this.groupBox2.TabIndex = 18;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "相机操作";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox2.BackgroundImage")));
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox2.Location = new System.Drawing.Point(849, -11);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(223, 191);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 39;
            this.pictureBox2.TabStop = false;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.pictureBox1);
            this.groupBox4.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox4.Location = new System.Drawing.Point(12, 50);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(803, 656);
            this.groupBox4.TabIndex = 19;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "检测界面";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pictureBox1.Location = new System.Drawing.Point(18, 29);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(769, 621);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 27;
            this.pictureBox1.TabStop = false;
            // 
            // cameraType
            // 
            this.cameraType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cameraType.FormattingEnabled = true;
            this.cameraType.Items.AddRange(new object[] {
            "海康相机",
            "Basler相机"});
            this.cameraType.Location = new System.Drawing.Point(25, 26);
            this.cameraType.Name = "cameraType";
            this.cameraType.Size = new System.Drawing.Size(100, 22);
            this.cameraType.TabIndex = 1;
            // 
            // bnClose
            // 
            this.bnClose.Enabled = false;
            this.bnClose.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnClose.Location = new System.Drawing.Point(141, 68);
            this.bnClose.Name = "bnClose";
            this.bnClose.Size = new System.Drawing.Size(100, 25);
            this.bnClose.TabIndex = 4;
            this.bnClose.Text = "关闭设备";
            this.bnClose.UseVisualStyleBackColor = true;
            this.bnClose.Click += new System.EventHandler(this.BnClose_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cameraType);
            this.groupBox3.Controls.Add(this.bnClose);
            this.groupBox3.Controls.Add(this.bnEnum);
            this.groupBox3.Controls.Add(this.bnOpen);
            this.groupBox3.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox3.Location = new System.Drawing.Point(831, 160);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(260, 114);
            this.groupBox3.TabIndex = 17;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "初始化";
            // 
            // bnEnum
            // 
            this.bnEnum.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnEnum.Location = new System.Drawing.Point(141, 26);
            this.bnEnum.Name = "bnEnum";
            this.bnEnum.Size = new System.Drawing.Size(100, 25);
            this.bnEnum.TabIndex = 2;
            this.bnEnum.Text = "查找设备";
            this.bnEnum.UseVisualStyleBackColor = true;
            this.bnEnum.Click += new System.EventHandler(this.bnEnum_Click);
            // 
            // bnOpen
            // 
            this.bnOpen.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnOpen.Location = new System.Drawing.Point(25, 68);
            this.bnOpen.Name = "bnOpen";
            this.bnOpen.Size = new System.Drawing.Size(100, 25);
            this.bnOpen.TabIndex = 3;
            this.bnOpen.Text = "打开设备";
            this.bnOpen.UseVisualStyleBackColor = true;
            this.bnOpen.Click += new System.EventHandler(this.bnOpen_Click);
            // 
            // SingleCamera
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1103, 718);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.cbDeviceList);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.pictureBox2);
            this.Name = "SingleCamera";
            this.Text = " 深度学习工业检测（单相机模式）";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SingleCamera_FormClosing);
            this.groupBox5.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button bnLoadModel;
        private System.Windows.Forms.Button bnStartDetection;
        private System.Windows.Forms.Button bnSaveImage;
        private System.Windows.Forms.Button bnStopDetection;
        private System.Windows.Forms.Button bnSetParam;
        private System.Windows.Forms.Button bnGetParam;
        private System.Windows.Forms.Button bnStopGrab;
        private System.Windows.Forms.Button bnStartGrab;
        private System.Windows.Forms.TextBox tbGain;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbExposure;
        private System.Windows.Forms.ComboBox cbDeviceList;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.PictureBox pictureBox1;
        public System.Windows.Forms.ComboBox cameraType;
        private System.Windows.Forms.Button bnClose;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button bnEnum;
        private System.Windows.Forms.Button bnOpen;
    }
}