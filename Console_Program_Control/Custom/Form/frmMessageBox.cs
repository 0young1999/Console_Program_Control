using Console_Program_Control.Properties;
using System.Diagnostics;

namespace GameSideProgramAutoStarter
{
	public partial class frmMessageBox : Form
	{
		#region 변수
		/// <summary>
		/// 화면 표시시 해당창 타입
		/// </summary>
		private fmbButtonType buttonType = fmbButtonType.OK;
		private fmbIconType iconType = fmbIconType.icon;
		private bool ApplicationStop = true;
		#endregion

		#region 생성자
		private frmMessageBox()
		{
			InitializeComponent();
		}

		public frmMessageBox(fmbButtonType buttonType, fmbIconType iconType, bool ApplicationStop, string title, string msg) : this()
		{
			// 타입 가져오기
			if (buttonType != null)
			{
				this.buttonType = buttonType;
			}
			if (iconType != null)
			{
				this.iconType = iconType;
			}
			if (ApplicationStop != null)
			{
				this.ApplicationStop = ApplicationStop;
			}

			// 내용 가져오기
			lbTitle.Text = title;
			lbContent.Text = msg;

			// 버튼 내용 기록
			if (buttonType == fmbButtonType.OKNOCANCLE)
			{
				// 버튼 텍스트
				btn1.Text = "예";
				btn2.Text = "아니요";
				btn3.Text = "취소";

				// 버튼 이벤트
				btn1.Click += (sender, e) =>
				{
					DialogResult = DialogResult.OK;
					Close();
				};
				btn2.Click += (sender, e) =>
				{
					DialogResult = DialogResult.No;
					Close();
				};
				btn3.Click += (sender, e) =>
				{
					DialogResult = DialogResult.Cancel;
					Close();
				};

				// 숨김 처리된 버튼 활성화
				btn1.Visible = true;
				btn2.Visible = true;
				btn3.Visible = true;
			}
			else if (buttonType == fmbButtonType.OKNO)
			{
				// 버튼 텍스트
				btn2.Text = "예";
				btn3.Text = "아니요";

				// 버튼 이벤트
				btn2.Click += (sender, e) =>
				{
					DialogResult = DialogResult.OK;
					Close();
				};
				btn3.Click += (sender, e) =>
				{
					DialogResult = DialogResult.No;
					Close();
				};

				// 숨김 처리된 버튼 활성화
				btn2.Visible = true;
				btn3.Visible = true;
			}
			else
			{
				// 버튼 텍스트
				btn3.Text = "확인";

				// 버튼 이벤트
				btn3.Click += (sender, e) =>
				{
					Close();
				};

				// 숨김 처리된 버튼 활성화
				btn3.Visible = true;
			}

			// 이미지 그리기
			if (iconType == fmbIconType.infomation)
			{
				pbIcon.Image = Resources.IconNotice;
			}
			else if (iconType == fmbIconType.Warning)
			{
				pbIcon.Image = Resources.IconWarning;
			}
			else if (iconType == fmbIconType.icon)
			{
				pbIcon.Image = Resources.Nachoneko10_3;
			}
		}
		#endregion

		#region 이벤트
		private void frmMessageBox_Load(object sender, EventArgs e)
		{
			Rectangle workingArea = Screen.GetWorkingArea(this);
			this.Location = new Point((workingArea.Right / 2) - (Size.Width / 2),
									  (workingArea.Bottom / 2) - (Size.Height / 2));
		}

		private void frmMessageBox_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (ApplicationStop)
			{
				Application.Exit();
			}
		}
		#endregion

		#region 추가 항목
		public enum fmbButtonType
		{
			OK = 1,
			OKNO = 2,
			OKNOCANCLE = 3,
		}
		public enum fmbIconType
		{
			infomation = 0,
			Warning = 1,
			icon = 2,
		}
		#endregion
	}
}
