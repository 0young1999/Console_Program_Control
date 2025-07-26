using Console_Program_Control.Data;
using Console_Program_Control.Service;
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
		private csLeft4Dead2Plugins _l4d2Plugins;

		private void FormMain_Load(object sender, EventArgs e)
		{
			tmTime.Enabled = true;

			Text += "2025-07-26 AM 11:55";
		}

		private void tmTime_Tick(object sender, EventArgs e)
		{
			// ���α׷� ���۽� ���ڵ� ����
			if (_discord == null)
			{
				_discord = csDiscord.GetInstance();
			}

			// ���α׷� ���۽� ������ �÷����� ���� ����
			if (_l4d2Plugins == null)
			{
				_l4d2Plugins = csLeft4Dead2Plugins.GetInstance();
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

		private DateTime LogStartTime = DateTime.Now;
		public void MainLogAppend(eMainLogType type, bool isRequest, string msg)
		{
			try
			{
				lock (rtbDiscord)
				{
					string logMsg = string.Format("[{0}:{1}]{3}\r\n{2}\r\n",
						isRequest ? "R" : "W", DateTime.Now.ToString("HH:mm:ss:fff"), msg, type.ToString());

					rtbDiscord.Invoke((MethodInvoker)delegate
					{
						rtbDiscord.AppendText(logMsg);
						rtbDiscord.ScrollToCaret();
					});

					// ���� �α�
					string totalLogPath = string.Format("MainLog\\{0}.log", LogStartTime.ToString("yyyyMMddHHmmss"));
					if (Directory.Exists(Path.GetDirectoryName(totalLogPath)) == false)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(totalLogPath));
					}

					File.AppendAllText(totalLogPath, logMsg);

					// ���� �α�
					string logPath = string.Format("MainLog\\{1}\\{0}.log", LogStartTime.ToString("yyyyMMddHHmmss"), type.ToString());

					if (Directory.Exists(Path.GetDirectoryName(logPath)) == false)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(logPath));
					}

					File.AppendAllText(logPath, logMsg);
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
			//sf._SetObject(csAutoResponse.GetInstance(), "�ڵ� ����");
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
	public enum eMainLogType
	{
		System = 0,
		DiscordAPI = 1,
		DiscordCustomCommand = 2,
		DiscordAutoResponse = 3,
		DiscordVoiceChatLog = 4,
		DiscordSendCommandConsoleServer = 5,
		DiscordCommandWikiParse = 6,
		Left4Dead2Plugins = 7,
	}
}
