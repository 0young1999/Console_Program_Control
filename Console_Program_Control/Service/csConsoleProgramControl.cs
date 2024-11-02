using Console_Program_Control.Data;
using Discord.Commands;
using System.Diagnostics;
using System.Windows.Forms;

namespace Console_Program_Control.Service
{
	public class csConsoleProgramControl
	{
		private static csConsoleProgramControl instance;
		public static csConsoleProgramControl GetInstance()
		{
			if (instance == null) instance = new csConsoleProgramControl();
			return instance;
		}
		private csConsoleProgramControl() { }

		private csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

		private Process? process;
		private StreamReader? reader;
		private StreamWriter? writer;

		public bool Start()
		{
			if (_ctc.Selected == -1) return false;

			if (isAlive()) return false;

			if (File.Exists(_ctc.getTarget().ProgramPath))
			{
				ProcessStartInfo psi = new ProcessStartInfo();
				psi.WorkingDirectory = Path.GetDirectoryName(_ctc.getTarget().ProgramPath);
				psi.FileName = _ctc.getTarget().ProgramPath;
				psi.RedirectStandardOutput = false;
				psi.RedirectStandardInput = true;
				psi.CreateNoWindow = false;

				process = Process.Start(psi);
				if (process == null) return false;
				writer = process.StandardInput;
				return true;
			}

			return false;
		}

		public bool process_WriteMSG(string msg)
		{
			if (process != null)
			{
				try
				{
					writer.WriteLine(msg);
					return true;
				}
				catch { }
			}
			return false;
		}

		public bool isAlive()
		{
			if (process == null) return false;
			try
			{
				Process[] ps = Process.GetProcessesByName(process.ProcessName);
				return ps.Length > 0;
			}
			catch
			{
				process = null;
				return false;
			}
		}

		public void Kill()
		{
			if (isAlive())
			{
				Process[] ps = Process.GetProcessesByName(process.ProcessName);
				foreach (Process p in ps)
				{
					p.Kill(true);
				}
				process = null;
			}
		}

		public void Close()
		{
			if (isAlive())
			{
				if (_ctc.getTarget().KillCommand.Count > 0)
				{
					foreach (string msg in _ctc.getTarget().KillCommand)
					{
						process_WriteMSG(msg);

						WaitKillDelay();
					}
				}
				else
				{
					Kill();
				}
			}
		}

		private Thread? DiscordGameCloseThread = null;
		private SocketCommandContext DiscordGameCloseThread_commandContext;
		public void Close(SocketCommandContext commandContext)
		{
			if (DiscordGameCloseThread != null)
			{
				commandContext.Channel.SendMessageAsync("서버 종료가 진행 중이야!");
				return;
			}

			DiscordGameCloseThread_commandContext = commandContext;

			DiscordGameCloseThread = new Thread(DiscordGameCloseThreadMethod);
			DiscordGameCloseThread.IsBackground = true;
			DiscordGameCloseThread.Start();
		}

		public void DiscordGameCloseThreadMethod()
		{
			SocketCommandContext commandContext = DiscordGameCloseThread_commandContext;

			if (isAlive())
			{
				if (_ctc.getTarget().KillCommand.Count > 0)
				{
					for (int i = 0; i < _ctc.getTarget().KillCommand.Count; i++)
					{
						process_WriteMSG(_ctc.getTarget().KillCommand[i]);

						string responseMsg = string.Format("게임 서버에 명령 전송 중({1}/{2}) : {0}", _ctc.getTarget().KillCommand[i], i + 1, _ctc.getTarget().KillCommand.Count);
						commandContext.Channel.SendMessageAsync(responseMsg);
						FormMain.GetInstance().MainLogAppend(false, responseMsg);

						WaitKillDelay();
					}
				}
				else
				{
					Kill();
				}
			}


			if (isAlive() == false)
			{
				commandContext.Channel.SendMessageAsync("서버가 종료 됬어");
			}
			else
			{
				commandContext.Channel.SendMessageAsync("어라 안 꺼지내? 꺼지는게 늦는걸수도 있으니까 기다려봐");
			}

			DiscordGameCloseThread = null;
		}

		public void WaitKillDelay()
		{
			DateTime delayEndTime = DateTime.Now + new TimeSpan(0, 0, 0, 0, _ctc.getTarget().KillDelay);

			while (DateTime.Now < delayEndTime)
			{
				Thread.Sleep(5);
			}
		}
	} 
}
