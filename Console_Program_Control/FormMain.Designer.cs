namespace Console_Program_Control
{
	partial class FormMain
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
			btnSetting = new Button();
			lblTime = new Label();
			tmTime = new System.Windows.Forms.Timer(components);
			btnProgramControl = new Button();
			btnKill = new Button();
			rtbDiscord = new RichTextBox();
			tableLayoutPanel1 = new TableLayoutPanel();
			tableLayoutPanel1.SuspendLayout();
			SuspendLayout();
			// 
			// btnSetting
			// 
			btnSetting.Font = new Font("D2Coding", 20.25F, FontStyle.Bold);
			btnSetting.Location = new Point(399, 13);
			btnSetting.Margin = new Padding(3, 2, 3, 2);
			btnSetting.Name = "btnSetting";
			btnSetting.Size = new Size(86, 58);
			btnSetting.TabIndex = 0;
			btnSetting.Text = "설정";
			btnSetting.UseVisualStyleBackColor = true;
			btnSetting.Click += btnSetting_Click;
			// 
			// lblTime
			// 
			lblTime.Font = new Font("D2Coding", 14.9999981F, FontStyle.Regular, GraphicsUnit.Point, 129);
			lblTime.Location = new Point(10, 13);
			lblTime.Name = "lblTime";
			lblTime.Size = new Size(135, 58);
			lblTime.TabIndex = 1;
			lblTime.Text = "00:00:00";
			lblTime.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// tmTime
			// 
			tmTime.Interval = 800;
			tmTime.Tick += tmTime_Tick;
			// 
			// btnProgramControl
			// 
			btnProgramControl.Font = new Font("D2Coding", 20.25F, FontStyle.Bold);
			btnProgramControl.Location = new Point(490, 13);
			btnProgramControl.Margin = new Padding(3, 2, 3, 2);
			btnProgramControl.Name = "btnProgramControl";
			btnProgramControl.Size = new Size(86, 58);
			btnProgramControl.TabIndex = 4;
			btnProgramControl.Text = "시작";
			btnProgramControl.UseVisualStyleBackColor = true;
			btnProgramControl.Click += btnProgramControl_Click;
			// 
			// btnKill
			// 
			btnKill.Font = new Font("D2Coding", 20.25F, FontStyle.Bold);
			btnKill.Location = new Point(581, 13);
			btnKill.Margin = new Padding(3, 2, 3, 2);
			btnKill.Name = "btnKill";
			btnKill.Size = new Size(86, 58);
			btnKill.TabIndex = 5;
			btnKill.Text = "강종";
			btnKill.UseVisualStyleBackColor = true;
			btnKill.Click += btnKill_Click;
			// 
			// rtbDiscord
			// 
			rtbDiscord.Dock = DockStyle.Fill;
			rtbDiscord.Location = new Point(3, 4);
			rtbDiscord.Margin = new Padding(3, 4, 3, 4);
			rtbDiscord.Name = "rtbDiscord";
			rtbDiscord.ReadOnly = true;
			rtbDiscord.Size = new Size(650, 428);
			rtbDiscord.TabIndex = 6;
			rtbDiscord.Text = "";
			// 
			// tableLayoutPanel1
			// 
			tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			tableLayoutPanel1.ColumnCount = 1;
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			tableLayoutPanel1.Controls.Add(rtbDiscord, 1, 0);
			tableLayoutPanel1.Location = new Point(10, 77);
			tableLayoutPanel1.Margin = new Padding(3, 4, 3, 4);
			tableLayoutPanel1.Name = "tableLayoutPanel1";
			tableLayoutPanel1.RowCount = 1;
			tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableLayoutPanel1.Size = new Size(656, 436);
			tableLayoutPanel1.TabIndex = 7;
			// 
			// FormMain
			// 
			AutoScaleDimensions = new SizeF(6F, 14F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(674, 527);
			Controls.Add(btnKill);
			Controls.Add(btnProgramControl);
			Controls.Add(lblTime);
			Controls.Add(btnSetting);
			Controls.Add(tableLayoutPanel1);
			Font = new Font("D2Coding", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 129);
			Icon = (Icon)resources.GetObject("$this.Icon");
			Margin = new Padding(3, 2, 3, 2);
			Name = "FormMain";
			Text = "Console_Program_Control";
			Load += FormMain_Load;
			tableLayoutPanel1.ResumeLayout(false);
			ResumeLayout(false);
		}

		#endregion

		private Button btnSetting;
		private Label lblTime;
		private System.Windows.Forms.Timer tmTime;
		private Button btnProgramControl;
		private Button btnKill;
		private RichTextBox rtbDiscord;
		private TableLayoutPanel tableLayoutPanel1;
	}
}
