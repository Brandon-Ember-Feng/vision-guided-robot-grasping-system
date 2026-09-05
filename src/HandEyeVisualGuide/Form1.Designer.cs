namespace HandEyeVisualGuide;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.toolStrip1 = new System.Windows.Forms.ToolStrip();
        this.btnOpenCamera = new System.Windows.Forms.ToolStripButton();
        this.btnGrab = new System.Windows.Forms.ToolStripButton();
        this.btnContinuous = new System.Windows.Forms.ToolStripButton();
        this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
        this.btnDetectBlob = new System.Windows.Forms.ToolStripButton();
        this.btnDetectEdge = new System.Windows.Forms.ToolStripButton();
        this.btnDetectCompare = new System.Windows.Forms.ToolStripButton();
        this.btnAutoGrab = new System.Windows.Forms.ToolStripButton();
        this.btnViewRecords = new System.Windows.Forms.ToolStripButton();
        this.btnReport = new System.Windows.Forms.ToolStripButton();
        this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
        this.lblFps = new System.Windows.Forms.ToolStripLabel();
        this.splitContainer1 = new System.Windows.Forms.SplitContainer();
        this.pictureBox1 = new System.Windows.Forms.PictureBox();
        this.panelRight = new System.Windows.Forms.Panel();
        this.groupBox3 = new System.Windows.Forms.GroupBox();
        this.txtCalibPoints = new System.Windows.Forms.TextBox();
        this.lblCalibResult = new System.Windows.Forms.Label();
        this.panelCalibBtn = new System.Windows.Forms.Panel();
        this.btnCalibrate = new System.Windows.Forms.Button();
        this.btnGen9Points = new System.Windows.Forms.Button();
        this.groupBox2 = new System.Windows.Forms.GroupBox();
        this.txtResult = new System.Windows.Forms.TextBox();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.lblCameraInfo = new System.Windows.Forms.Label();
        this.statusStrip1 = new System.Windows.Forms.StatusStrip();
        this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
        this.timer1 = new System.Windows.Forms.Timer(this.components);
        this.toolStrip1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
        this.splitContainer1.Panel1.SuspendLayout();
        this.splitContainer1.Panel2.SuspendLayout();
        this.splitContainer1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
        this.panelRight.SuspendLayout();
        this.groupBox3.SuspendLayout();
        this.panelCalibBtn.SuspendLayout();
        this.groupBox2.SuspendLayout();
        this.groupBox1.SuspendLayout();
        this.statusStrip1.SuspendLayout();
        this.SuspendLayout();
        //
        // toolStrip1
        //
        this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
        this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnOpenCamera,
            this.btnGrab,
            this.btnContinuous,
            this.toolStripSeparator1,
            this.btnDetectBlob,
            this.btnDetectEdge,
            this.btnDetectCompare,
            this.btnAutoGrab,
            this.btnViewRecords,
            this.btnReport,
            this.toolStripSeparator2,
            this.lblFps});
        this.toolStrip1.Location = new System.Drawing.Point(0, 0);
        this.toolStrip1.Name = "toolStrip1";
        this.toolStrip1.Size = new System.Drawing.Size(1200, 38);
        this.toolStrip1.TabIndex = 0;
        this.toolStrip1.Text = "toolStrip1";
        //
        // btnOpenCamera
        //
        this.btnOpenCamera.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnOpenCamera.Name = "btnOpenCamera";
        this.btnOpenCamera.Size = new System.Drawing.Size(72, 35);
        this.btnOpenCamera.Text = "打开相机";
        this.btnOpenCamera.Click += new System.EventHandler(this.btnOpenCamera_Click);
        //
        // btnGrab
        //
        this.btnGrab.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnGrab.Name = "btnGrab";
        this.btnGrab.Size = new System.Drawing.Size(72, 35);
        this.btnGrab.Text = "单帧采集";
        this.btnGrab.Click += new System.EventHandler(this.btnGrab_Click);
        //
        // btnContinuous
        //
        this.btnContinuous.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnContinuous.Name = "btnContinuous";
        this.btnContinuous.Size = new System.Drawing.Size(72, 35);
        this.btnContinuous.Text = "连续采集";
        this.btnContinuous.Click += new System.EventHandler(this.btnContinuous_Click);
        //
        // toolStripSeparator1
        //
        this.toolStripSeparator1.Name = "toolStripSeparator1";
        this.toolStripSeparator1.Size = new System.Drawing.Size(6, 38);
        //
        // lblFps
        //
        this.lblFps.Name = "lblFps";
        this.lblFps.Size = new System.Drawing.Size(56, 35);
        this.lblFps.Text = "FPS: 0";
        //
        // btnDetectBlob
        //
        this.btnDetectBlob.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnDetectBlob.Name = "btnDetectBlob";
        this.btnDetectBlob.Size = new System.Drawing.Size(84, 35);
        this.btnDetectBlob.Text = "Blob检测";
        this.btnDetectBlob.Click += new System.EventHandler(this.btnDetectBlob_Click);
        //
        // btnDetectEdge
        //
        this.btnDetectEdge.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnDetectEdge.Name = "btnDetectEdge";
        this.btnDetectEdge.Size = new System.Drawing.Size(84, 35);
        this.btnDetectEdge.Text = "边缘检测";
        this.btnDetectEdge.Click += new System.EventHandler(this.btnDetectEdge_Click);
        //
        // btnDetectCompare
        //
        this.btnDetectCompare.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnDetectCompare.Name = "btnDetectCompare";
        this.btnDetectCompare.Size = new System.Drawing.Size(84, 35);
        this.btnDetectCompare.Text = "算法对比";
        this.btnDetectCompare.Click += new System.EventHandler(this.btnDetectCompare_Click);
        //
        // toolStripSeparator2
        //
        this.toolStripSeparator2.Name = "toolStripSeparator2";
        this.toolStripSeparator2.Size = new System.Drawing.Size(6, 38);
        //
        // btnAutoGrab
        //
        this.btnAutoGrab.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnAutoGrab.Name = "btnAutoGrab";
        this.btnAutoGrab.Size = new System.Drawing.Size(96, 35);
        this.btnAutoGrab.Text = "视觉引导抓取";
        this.btnAutoGrab.Click += new System.EventHandler(this.btnAutoGrab_Click);
        //
        // btnViewRecords
        //
        this.btnViewRecords.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnViewRecords.Name = "btnViewRecords";
        this.btnViewRecords.Size = new System.Drawing.Size(84, 35);
        this.btnViewRecords.Text = "查看记录";
        this.btnViewRecords.Click += new System.EventHandler(this.btnViewRecords_Click);
        //
        // btnReport
        //
        this.btnReport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.btnReport.Name = "btnReport";
        this.btnReport.Size = new System.Drawing.Size(84, 35);
        this.btnReport.Text = "生产报表";
        this.btnReport.Click += new System.EventHandler(this.btnReport_Click);
        //
        // splitContainer1
        //
        this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.splitContainer1.Location = new System.Drawing.Point(0, 38);
        this.splitContainer1.Name = "splitContainer1";
        //
        // splitContainer1.Panel1
        //
        this.splitContainer1.Panel1.Controls.Add(this.pictureBox1);
        //
        // splitContainer1.Panel2
        //
        this.splitContainer1.Panel2.Controls.Add(this.panelRight);
        this.splitContainer1.Size = new System.Drawing.Size(1200, 662);
        this.splitContainer1.SplitterDistance = 880;
        this.splitContainer1.TabIndex = 1;
        //
        // pictureBox1
        //
        this.pictureBox1.BackColor = System.Drawing.Color.Black;
        this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pictureBox1.Location = new System.Drawing.Point(0, 0);
        this.pictureBox1.Name = "pictureBox1";
        this.pictureBox1.Size = new System.Drawing.Size(880, 662);
        this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBox1.TabIndex = 0;
        this.pictureBox1.TabStop = false;
        //
        // panelRight
        //
        this.panelRight.Controls.Add(this.groupBox3);
        this.panelRight.Controls.Add(this.groupBox2);
        this.panelRight.Controls.Add(this.groupBox1);
        this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelRight.Location = new System.Drawing.Point(0, 0);
        this.panelRight.Name = "panelRight";
        this.panelRight.Padding = new System.Windows.Forms.Padding(8);
        this.panelRight.Size = new System.Drawing.Size(316, 662);
        this.panelRight.TabIndex = 0;
        //
        // groupBox3 (手眼标定)
        //
        this.groupBox3.Controls.Add(this.txtCalibPoints);
        this.groupBox3.Controls.Add(this.lblCalibResult);
        this.groupBox3.Controls.Add(this.panelCalibBtn);
        this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
        this.groupBox3.Location = new System.Drawing.Point(8, 338);
        this.groupBox3.Name = "groupBox3";
        this.groupBox3.Size = new System.Drawing.Size(300, 316);
        this.groupBox3.TabIndex = 2;
        this.groupBox3.TabStop = false;
        this.groupBox3.Text = "手眼标定（九点法）";
        //
        // txtCalibPoints
        //
        this.txtCalibPoints.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtCalibPoints.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtCalibPoints.Location = new System.Drawing.Point(3, 113);
        this.txtCalibPoints.Multiline = true;
        this.txtCalibPoints.Name = "txtCalibPoints";
        this.txtCalibPoints.ReadOnly = true;
        this.txtCalibPoints.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtCalibPoints.Size = new System.Drawing.Size(294, 200);
        this.txtCalibPoints.TabIndex = 2;
        this.txtCalibPoints.Text = "标定点列表：点击\"生成仿真九点\"开始";
        //
        // lblCalibResult
        //
        this.lblCalibResult.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblCalibResult.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lblCalibResult.Location = new System.Drawing.Point(3, 63);
        this.lblCalibResult.Name = "lblCalibResult";
        this.lblCalibResult.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
        this.lblCalibResult.Size = new System.Drawing.Size(294, 85);
        this.lblCalibResult.TabIndex = 1;
        this.lblCalibResult.Text = "标定状态：未标定";
        //
        // panelCalibBtn
        //
        this.panelCalibBtn.Controls.Add(this.btnCalibrate);
        this.panelCalibBtn.Controls.Add(this.btnGen9Points);
        this.panelCalibBtn.Dock = System.Windows.Forms.DockStyle.Top;
        this.panelCalibBtn.Location = new System.Drawing.Point(3, 23);
        this.panelCalibBtn.Name = "panelCalibBtn";
        this.panelCalibBtn.Size = new System.Drawing.Size(294, 40);
        this.panelCalibBtn.TabIndex = 0;
        //
        // btnCalibrate
        //
        this.btnCalibrate.Location = new System.Drawing.Point(110, 6);
        this.btnCalibrate.Name = "btnCalibrate";
        this.btnCalibrate.Size = new System.Drawing.Size(100, 28);
        this.btnCalibrate.TabIndex = 1;
        this.btnCalibrate.Text = "执行标定";
        this.btnCalibrate.UseVisualStyleBackColor = true;
        this.btnCalibrate.Click += new System.EventHandler(this.btnCalibrate_Click);
        //
        // btnGen9Points
        //
        this.btnGen9Points.Location = new System.Drawing.Point(6, 6);
        this.btnGen9Points.Name = "btnGen9Points";
        this.btnGen9Points.Size = new System.Drawing.Size(100, 28);
        this.btnGen9Points.TabIndex = 0;
        this.btnGen9Points.Text = "生成仿真九点";
        this.btnGen9Points.UseVisualStyleBackColor = true;
        this.btnGen9Points.Click += new System.EventHandler(this.btnGen9Points_Click);
        //
        // groupBox2
        //
        this.groupBox2.Controls.Add(this.txtResult);
        this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
        this.groupBox2.Location = new System.Drawing.Point(8, 148);
        this.groupBox2.Name = "groupBox2";
        this.groupBox2.Size = new System.Drawing.Size(300, 240);
        this.groupBox2.TabIndex = 1;
        this.groupBox2.TabStop = false;
        this.groupBox2.Text = "检测结果";
        //
        // txtResult
        //
        this.txtResult.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtResult.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtResult.Location = new System.Drawing.Point(3, 23);
        this.txtResult.Multiline = true;
        this.txtResult.Name = "txtResult";
        this.txtResult.ReadOnly = true;
        this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtResult.Size = new System.Drawing.Size(294, 214);
        this.txtResult.TabIndex = 0;
        this.txtResult.Text = "等待采集...";
        //
        // groupBox1
        //
        this.groupBox1.Controls.Add(this.lblCameraInfo);
        this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
        this.groupBox1.Location = new System.Drawing.Point(8, 8);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(300, 140);
        this.groupBox1.TabIndex = 0;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "相机参数";
        //
        // lblCameraInfo
        //
        this.lblCameraInfo.Dock = System.Windows.Forms.DockStyle.Fill;
        this.lblCameraInfo.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lblCameraInfo.Location = new System.Drawing.Point(3, 23);
        this.lblCameraInfo.Name = "lblCameraInfo";
        this.lblCameraInfo.Padding = new System.Windows.Forms.Padding(8);
        this.lblCameraInfo.Size = new System.Drawing.Size(294, 96);
        this.lblCameraInfo.TabIndex = 0;
        this.lblCameraInfo.Text = "分辨率: 1280x960\r\n像素当量: 0.05 mm/pix\r\n视野: 64x48 mm\r\n状态: 未打开";
        //
        // statusStrip1
        //
        this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
        this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
        this.statusStrip1.Location = new System.Drawing.Point(0, 700);
        this.statusStrip1.Name = "statusStrip1";
        this.statusStrip1.Size = new System.Drawing.Size(1200, 26);
        this.statusStrip1.TabIndex = 2;
        this.statusStrip1.Text = "statusStrip1";
        //
        // lblStatus
        //
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(80, 20);
        this.lblStatus.Text = "系统就绪";
        //
        // timer1
        //
        this.timer1.Interval = 50;
        this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
        //
        // Form1
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 726);
        this.Controls.Add(this.splitContainer1);
        this.Controls.Add(this.statusStrip1);
        this.Controls.Add(this.toolStrip1);
        this.Name = "Form1";
        this.Text = "视觉引导机械手抓取定位系统 - 手眼协同仿真平台";
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        this.toolStrip1.ResumeLayout(false);
        this.toolStrip1.PerformLayout();
        this.splitContainer1.Panel1.ResumeLayout(false);
        this.splitContainer1.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
        this.splitContainer1.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
        this.panelRight.ResumeLayout(false);
        this.groupBox3.ResumeLayout(false);
        this.groupBox3.PerformLayout();
        this.panelCalibBtn.ResumeLayout(false);
        this.groupBox2.ResumeLayout(false);
        this.groupBox1.ResumeLayout(false);
        this.statusStrip1.ResumeLayout(false);
        this.statusStrip1.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.ToolStrip toolStrip1;
    private System.Windows.Forms.ToolStripButton btnOpenCamera;
    private System.Windows.Forms.ToolStripButton btnGrab;
    private System.Windows.Forms.ToolStripButton btnContinuous;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton btnDetectBlob;
    private System.Windows.Forms.ToolStripButton btnDetectEdge;
    private System.Windows.Forms.ToolStripButton btnDetectCompare;
    private System.Windows.Forms.ToolStripButton btnAutoGrab;
    private System.Windows.Forms.ToolStripButton btnViewRecords;
    private System.Windows.Forms.ToolStripButton btnReport;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    private System.Windows.Forms.ToolStripLabel lblFps;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.Panel panelRight;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Label lblCameraInfo;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.TextBox txtResult;
    private System.Windows.Forms.GroupBox groupBox3;
    private System.Windows.Forms.TextBox txtCalibPoints;
    private System.Windows.Forms.Label lblCalibResult;
    private System.Windows.Forms.Panel panelCalibBtn;
    private System.Windows.Forms.Button btnCalibrate;
    private System.Windows.Forms.Button btnGen9Points;
    private System.Windows.Forms.StatusStrip statusStrip1;
    private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    private System.Windows.Forms.Timer timer1;
}
