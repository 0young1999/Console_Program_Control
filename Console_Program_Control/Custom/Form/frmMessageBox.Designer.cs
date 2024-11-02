namespace GameSideProgramAutoStarter
{
	partial class frmMessageBox
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
			lbTitle = new Label();
			pbIcon = new PictureBox();
			lbContent = new Label();
			btn3 = new Button();
			btn2 = new Button();
			btn1 = new Button();
			((System.ComponentModel.ISupportInitialize)pbIcon).BeginInit();
			SuspendLayout();
			// 
			// lbTitle
			// 
			lbTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			lbTitle.Font = new Font("맑은 고딕", 50.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
			lbTitle.Location = new Point(118, 12);
			lbTitle.Name = "lbTitle";
			lbTitle.Size = new Size(670, 100);
			lbTitle.TabIndex = 0;
			lbTitle.Text = "TITLE : N/A";
			lbTitle.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// pbIcon
			// 
			pbIcon.Location = new Point(12, 12);
			pbIcon.Name = "pbIcon";
			pbIcon.Size = new Size(100, 100);
			pbIcon.SizeMode = PictureBoxSizeMode.Zoom;
			pbIcon.TabIndex = 1;
			pbIcon.TabStop = false;
			// 
			// lbContent
			// 
			lbContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			lbContent.Font = new Font("맑은 고딕", 25F);
			lbContent.Location = new Point(12, 115);
			lbContent.Name = "lbContent";
			lbContent.Size = new Size(776, 250);
			lbContent.TabIndex = 2;
			lbContent.Text = "Content : N/A";
			lbContent.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// btn3
			// 
			btn3.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btn3.Font = new Font("맑은 고딕", 25F);
			btn3.Location = new Point(638, 368);
			btn3.Name = "btn3";
			btn3.Size = new Size(150, 70);
			btn3.TabIndex = 3;
			btn3.TabStop = false;
			btn3.Text = "btn3";
			btn3.UseVisualStyleBackColor = true;
			btn3.Visible = false;
			// 
			// btn2
			// 
			btn2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btn2.Font = new Font("맑은 고딕", 25F);
			btn2.Location = new Point(482, 368);
			btn2.Name = "btn2";
			btn2.Size = new Size(150, 70);
			btn2.TabIndex = 4;
			btn2.TabStop = false;
			btn2.Text = "btn2";
			btn2.UseVisualStyleBackColor = true;
			btn2.Visible = false;
			// 
			// btn1
			// 
			btn1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btn1.Font = new Font("맑은 고딕", 25F);
			btn1.Location = new Point(326, 368);
			btn1.Name = "btn1";
			btn1.Size = new Size(150, 70);
			btn1.TabIndex = 5;
			btn1.TabStop = false;
			btn1.Text = "btn1";
			btn1.UseVisualStyleBackColor = true;
			btn1.Visible = false;
			// 
			// frmMessageBox
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			BackColor = SystemColors.ActiveBorder;
			ClientSize = new Size(800, 450);
			ControlBox = false;
			Controls.Add(btn1);
			Controls.Add(btn2);
			Controls.Add(btn3);
			Controls.Add(lbContent);
			Controls.Add(pbIcon);
			Controls.Add(lbTitle);
			DoubleBuffered = true;
			FormBorderStyle = FormBorderStyle.None;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "frmMessageBox";
			ShowIcon = false;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			Text = "frmMessageBox";
			TopMost = true;
			FormClosing += frmMessageBox_FormClosing;
			Load += frmMessageBox_Load;
			((System.ComponentModel.ISupportInitialize)pbIcon).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private Label lbTitle;
		private PictureBox pbIcon;
		private Label lbContent;
		private Button btn3;
		private Button btn2;
		private Button btn1;
	}
}