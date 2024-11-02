using Microsoft.VisualBasic.Devices;

namespace GameSideProgramAutoStarter
{
	public partial class frmAlarm : Form
	{
		private static frmAlarm instance;
		public static frmAlarm GetInstance()
		{
			if (instance == null) instance = new frmAlarm();
			return instance;
		}

		private int ShowTime = 3000;

		private frmAlarm()
		{
			InitializeComponent();

			Rectangle workingArea = Screen.GetWorkingArea(this);
			this.Location = new Point(workingArea.Right - Size.Width,
									  workingArea.Bottom - Size.Height);

			timer1.Interval = ShowTime;
		}
		public void ShowMSG(string msg)
		{
			this.Invoke((MethodInvoker)delegate
			{
				timer1.Stop();
				Thread.Sleep(10);
				timer1.Start();
				this.Visible = true;
				label1.Text = msg;
				TopMost = true;
			});
		}
		public void SetIMG(Bitmap img)
		{
			pictureBox1.Image = img;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			lock (this)
			{
				Invoke((MethodInvoker)delegate
				{
					this.Visible = false;
				});
			}
		}
	}
}
