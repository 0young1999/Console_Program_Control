using Console_Program_Control.Data;
using Console_Program_Control.Service;
using Console_Program_Control.Service.AI;
using GameSideProgramAutoStarter;
using Young.Setting;

namespace Console_Program_Control
{
	public partial class FormMain : Form
	{

		private static FormMain instance;
		public static FormMain GetInstance()
		{
			if (instance == null) instance = new FormMain();
			return instance;
		}

		private FormMain()
		{
			InitializeComponent();
		}

		private csDiscord _discord;

		private void FormMain_Load(object sender, EventArgs e)
		{
			tmTime.Enabled = true;

			Text += string.Format("({0})", "2024-11-26 01:30");
		}

		private void tmTime_Tick(object sender, EventArgs e)
		{
			// ���α׷� ���۽� ���ڵ� ����
			if (_discord == null)
			{
				_discord = csDiscord.GetInstance();
			}

			// �ð� ����
			lblTime.Invoke((MethodInvoker)delegate
			{
				lblTime.Text = DateTime.Now.ToString("HH:mm:ss");
			});

			// discord �α� ���� ��� ����
			if (rtbDiscord.TextLength > 2000000000)
			{
				rtbDiscord.Invoke((MethodInvoker)delegate
				{
					lock (rtbDiscord)
					{
						rtbDiscord.Text = rtbDiscord.Text.Remove(0, 1000000000);
					}
				});
			}

			// ���� ���� ��ư ���� ����
			bool isAlive = _control.isAlive();
			if ((isAlive == (btnProgramControl.Text != "����")) || ((isAlive == false) == (btnProgramControl.Text != "����")))
			{
				btnProgramControl.Invoke((MethodInvoker)delegate
				{
					btnProgramControl.Text = isAlive ? "����" : "����";
				});
			}
		}

		private string MainLogPath = "";
		public void MainLogAppend(bool isRequest, string msg)
		{
			try
			{
				lock (rtbDiscord)
				{
					string logMsg = string.Format("[{0}:{1}] {2}\r\n", isRequest ? "R" : "W", DateTime.Now.ToString("HH:mm:ss:fff"), msg);

					rtbDiscord.Invoke((MethodInvoker)delegate
					{
						rtbDiscord.AppendText(logMsg);
						rtbDiscord.ScrollToCaret();
					});

					if (string.IsNullOrEmpty(MainLogPath))
					{
						MainLogPath = string.Format("MainLog\\{0}.log", DateTime.Now.ToString("yyyyMMddHHmmss"));
					}

					if (Directory.Exists(Path.GetDirectoryName(MainLogPath)) == false)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(MainLogPath));
					}

					File.AppendAllText(MainLogPath, logMsg);
				}
			}
			catch { }
		}

		private SettingForm sf;
		public bool isSettingFormShow = false;
		private void btnSetting_Click(object sender, EventArgs e)
		{
			if (isSettingFormShow == true)
			{
				return;
			}

			sf = new SettingForm();
			sf._SetObject(csDiscordSetting.GetInstance(), "���ڵ�");
			sf._SetObject(csConsoleTargetControl.GetInstance(), "���α׷� ��Ʈ��");
			//sf._SetObject(csMinecraft.GetInstance(), "��ũ ����");
			sf._SetObject(csAutoResponse.GetInstance(), "�ڵ� ����");
			sf.Shown += (showSender, showE) =>
			{
				Invoke((MethodInvoker)delegate
				{
					isSettingFormShow = true;
				});
			};
			sf.FormClosed += (closeSender, closeE) =>
			{
				Invoke((MethodInvoker)delegate
				{
					isSettingFormShow = false;
				});
			};

			sf.Show();
		}

		private csConsoleProgramControl _control = csConsoleProgramControl.GetInstance();
		private void btnProgramControl_Click(object sender, EventArgs e)
		{
			if (isSettingFormShow)
			{
				new frmMessageBox(frmMessageBox.fmbButtonType.OK, frmMessageBox.fmbIconType.icon, false, "CPC", "������ �����ֽ��ϴ�.").Show();
			}

			if (_control.isAlive() == false)
			{
				if (StartConsoleProgram() == false)
				{
					new frmMessageBox(frmMessageBox.fmbButtonType.OK, frmMessageBox.fmbIconType.icon, false, "CPC", "���α׷� ���ۿ� ���� �Ͽ����ϴ�.").Show();
				}
			}
			else
			{
				_control.Close();
			}
		}

		public bool StartConsoleProgram()
		{
			return _control.Start();
		}

		private void btnKill_Click(object sender, EventArgs e)
		{
			_control.Kill();
		}
	}
}
