using Console_Program_Control.Data;
using System.Net.Sockets;

namespace Console_Program_Control.Service
{
	public class csMinecraftTCPResponse
    {
        private static csMinecraftTCPResponse instance;
        public static csMinecraftTCPResponse GetInstance()
        {
            if (instance == null) instance = new csMinecraftTCPResponse();
            return instance;
        }
		private csMinecraftTCPResponse() { }

		private csMinecraft option = csMinecraft.GetInstance();
		private csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
		private csConsoleTarget target = csConsoleTarget.GetInstance();
		private Thread ServerThread;

		public void RUN()
		{
			if (option.isAlive) return;

			try
			{
				// Thread 객채 생성, Form과는 별도 쓰레드에서 connect 함수가 실행됨.
				ServerThread = new Thread(StartServer);
				// Form이 종료되면 thread1도 종료.
				ServerThread.IsBackground = true;
				// thread1 시작.
				ServerThread.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

        public void StartServer()
		{
			option.isAlive = true;

			TcpListener listener = new TcpListener(option.ResponsePort);

			// 서버 시작
			try
			{
				listener.Start();
			}
			catch (Exception e)
			{
				MessageBox.Show("서버 오픈 실패!");
				MessageBox.Show($"{e.Message}");

				option.isAlive = false;
			}

			while (option.isAlive)
			{
				// 클라이언트 접속 확인
				Client client = new Client();
				client.client = listener.AcceptTcpClient();

				client.streamReader = new StreamReader(client.client.GetStream());  // 읽기 스트림 연결
				client.streamWriter = new StreamWriter(client.client.GetStream());  // 쓰기 스트림 연결
				client.streamWriter.AutoFlush = true;  // 쓰기 버퍼 자동으로 뭔가 처리..

				client.ReadEvent += (s, e) =>
				{
					if (string.IsNullOrEmpty(e.msg) == false)
					{
						FormMain.GetInstance().DiscordLogAppend(true, e.msg);
					}
					else
					{
						return;
					}

					string msg = e.msg;

					// 마크 서버 리셋
					if (msg.Substring(1, msg.IndexOf((char)02) - 1).ToUpper() == "SERVERRESET")
					{
						control.Close();

						while (control.isAlive()) continue;

						int index1 = msg.IndexOf((char)02) + 1;
						int index2 = msg.IndexOf((char)03) - 1 - msg.IndexOf((char)02);
						string[] splits = msg.Substring(index1, index2).Split(',');

						foreach (string dir in splits)
						{
							while (Directory.Exists(Path.GetDirectoryName(target.ProgramPath) + "\\" + dir))
							{
								Directory.Delete(Path.GetDirectoryName(target.ProgramPath) + "\\" + dir, true);
								FormMain.GetInstance().DiscordLogAppend(false, "폴더 삭제 : " + Path.GetDirectoryName(target.ProgramPath) + "\\" + dir);
							}
						}

						control.Start();
					}
				};
				client.ReadStart();
			}

			option.isAlive = false;
		}
    }

	public class Client
	{
		public TcpClient client { get; set; }
		public StreamReader streamReader { get; set; }
		public StreamWriter streamWriter { get; set; }

		// THREAD
		private Thread readThread = null;
		public bool IsAliveReadThread() { return readThread.IsAlive; }

		public void ReadStart()
		{
			readThread = new Thread(Read);
			readThread.IsBackground = true;
			readThread.Start();
		}

		private void Read()
		{
			try
			{
				while (true)
				{
					string read = streamReader.ReadLine();  // 수신 데이타를 읽어서 receiveData1 변수에 저장
					Toss(read);
				}
			}
			catch { }
		}

		public class ReadEventArg : EventArgs
		{
			public string msg;
		}
		public EventHandler<ReadEventArg> ReadEvent;
		public void Toss(string msg)
		{
			ReadEvent.Invoke(this, new ReadEventArg()
			{
				msg = msg,
			});
		}

		public bool Send(string msg)
		{
			try
			{
				streamWriter.WriteLine(msg);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
