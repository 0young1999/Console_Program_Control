using Console_Program_Control.Data;
using CoreRCON;
using Discord.Commands;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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

				if (string.IsNullOrEmpty(_ctc.getTarget().StartOption) == false)
				{
					psi.Arguments = _ctc.getTarget().StartOption;
				}

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
					// 텔넷
					if (_ctc.getTarget().GameType == GameType._7DaysToDie)
					{
						// Telnet 서버 정보
						string server = "127.0.0.1";            // 서버 IP
						int port = 8081;                        // Telnet 포트
						string password = "20250704";   // Telnet 비밀번호

						using (TcpClient client = new TcpClient())
						{
							//Console.WriteLine("서버에 연결 중...");
							client.Connect(server, port);

							using (NetworkStream stream = client.GetStream())
							using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
							using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
							{
								// 초기 연결 메시지 읽기
								string line = reader.ReadLine();
								Console.WriteLine(line);

								// 비밀번호 전송
								writer.WriteLine(password);

								// 인증 결과 읽기
								line = reader.ReadLine();
								Console.WriteLine(line);

								// 단발성 명령어 전송
								writer.WriteLine(msg);

								Thread.Sleep(1000);

								return true;
							}
						}
					}
					else if (_ctc.getTarget().GameType == GameType.Left4Dead2)
					{
						RCONAsync("192.168.0.12", 27015, "ThePigeonThatLostItsFear", msg);
					}
					// 기본 타입
					else
					{
						writer.WriteLine(msg);
						return true;
					}
				}
				catch { }
			}
			return false;
		}

		//private async Task RCON(string ip, int port, string password, string msg)
		private async Task RCONAsync(string ip, int port, string password, string msg)
		{
			// 서버 IP와 포트
			var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);

			// RCON 연결
			using (var rcon = new RCON(endpoint, password))
			{
				// 연결 시도
				await rcon.ConnectAsync();

				// RCON 명령 실행
				string result = await rcon.SendCommandAsync(msg);
			}
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

		public bool getLongActiveTime(out string _RunTime)
		{
			_RunTime = string.Empty;

			if (isAlive() == false) return false;

			try
			{
				Process[] pss = Process.GetProcessesByName(process.ProcessName);

				DateTime fs = DateTime.Now;

				foreach (Process ps in pss)
				{
					DateTime dt = ps.StartTime;

					if (dt < fs)
					{
						fs = dt;
					}
				}

				TimeSpan lt = DateTime.Now - fs;

				_RunTime = string.Format("{0:D2}일 {1:D2}시 {2:D2}분 {3:D2}초", 
					lt.Days, lt.Hours, lt.Minutes, lt.Seconds);

				return true;
			}
			catch
			{
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

				foreach (string s in _ctc.getTarget().KillTogether)
				{
					Process[] ktps = Process.GetProcessesByName(s);

					for (int i = 0; i < ktps.Length; i++) ktps[i].Kill();
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

			Thread.Sleep(1000);

			if (isAlive())
			{
				if (_ctc.getTarget().KillCommand.Count > 0)
				{
					for (int i = 0; i < _ctc.getTarget().KillCommand.Count; i++)
					{
						process_WriteMSG(_ctc.getTarget().KillCommand[i]);

						string responseMsg = string.Format("게임 서버에 명령 전송 중({1}/{2}) : {0}", _ctc.getTarget().KillCommand[i], i + 1, _ctc.getTarget().KillCommand.Count);
						commandContext.Channel.SendMessageAsync(responseMsg);
						FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordSendCommandConsoleServer, false, responseMsg);

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
