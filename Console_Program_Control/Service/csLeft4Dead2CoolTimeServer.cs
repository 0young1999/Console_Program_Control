using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Console_Program_Control.Service
{
	public class csLeft4Dead2CoolTimeServer
	{
		private static csLeft4Dead2CoolTimeServer instance;
		public static csLeft4Dead2CoolTimeServer GetInstance()
		{
			if (instance == null) instance = new csLeft4Dead2CoolTimeServer();
			return instance;
		}
		private csLeft4Dead2CoolTimeServer()
		{
			_Main = new Thread(Connect);
			_Main.Name = "threadOutSideProgramServer";
			_Main.IsBackground = true;
			_Main.Start();
		}

		// THREAD
		public Thread _Main = null;
		private List<Client> _list = new List<Client>();
		private object _ListLock = new object();

		private void Connect()  // thread1에 연결된 함수. 메인폼과는 별도로 동작한다.
		{
			// 서버 객체 생성 및 IP주소와 Port번호를 할당
			TcpListener tcpListener1 = new TcpListener(IPAddress.Parse("192.168.0.12"), 30001);

			// 서버 시작
			try
			{
				tcpListener1.Start();
			}
			catch (Exception e)
			{
				MessageBox.Show("서버 오픈 실패!");
				MessageBox.Show($"{e.Message}");
			}

			while (true)
			{
				// 클라이언트 접속 확인
				Client client = new Client();
				client.client = tcpListener1.AcceptTcpClient();

				client.stream = client.client.GetStream();
				client.stream.WriteTimeout = 100;

				client.ReadStart();

				client.Send("GetSteamID64");

				lock (_ListLock) { _list.Add(client); }

				Thread.Sleep(1);
			}
		}

		public void SendSkill(string steamID64, string msg)
		{
			lock (_ListLock)
			{
				foreach (Client client in _list)
				{
					if (client.SteamID64 == steamID64)
					{
						client.Send(msg);
					}
				}
			}
		}

		public void SendToAllClient(string msg)
		{
			lock (_ListLock)
			{
				foreach (Client client in _list)
				{
					client.Send(msg);
				}
			}
		}

		public void ResetClient()
		{
			lock (_ListLock)
			{
				foreach (Client client in _list)
				{
					client.isRead = false;
				}
				_list.Clear();
			}
		}
	}

	class Client
	{
		public TcpClient client { get; set; }
		public NetworkStream stream;

		public string SteamID64 = string.Empty;

		private bool isInited = false;

		// THREAD
		private Thread readThread = null;
		public bool IsAliveReadThread() { return readThread.IsAlive; }

		public void ReadStart()
		{
			readThread = new Thread(Read);
			readThread.IsBackground = true;
			readThread.Start();
		}

		public bool isRead = true;
		private void Read()
		{
			string ReceivedStack = string.Empty;
			while (isRead)
			{
				try
				{
					if (stream.DataAvailable)
					{
						byte[] buffer = new byte[1024];
						int bytesRead = stream.Read(buffer, 0, buffer.Length);
						if (bytesRead == 0) break;
						string receivedResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

						ReceivedStack += receivedResponse;

						DateTime startTime = DateTime.Now;

						while (startTime + new TimeSpan(0, 0, 5) > DateTime.Now)
						{
							int stxIndex = ReceivedStack.IndexOf('\x02');
							int etxIndex = ReceivedStack.IndexOf('\x03');
							if (stxIndex == -1 || etxIndex == -1)
							{
								ReceivedStack = string.Empty;
								break;
							}

							if (stxIndex >= etxIndex)
							{
								ReceivedStack = ReceivedStack.Substring(stxIndex);
								continue;
							}

							// s01234e
							// 0123456
							string sTemp = ReceivedStack.Substring(stxIndex + 1, etxIndex - stxIndex - 1);
							ReceivedStack = ReceivedStack.Substring(etxIndex + 1);

							stxIndex = sTemp.LastIndexOf('\x02');
							if (stxIndex >= 0)
							{
								sTemp.Substring(stxIndex + 1);
							}

							string[] splits = sTemp.Split('|');

							switch (splits[0])
							{
								case "SetSteamID64":
									{
										if (splits.Length > 1)
										{
											SteamID64 = splits[1];
										}
									}
									break;
							}
						}
					}
				}
				catch { }
				Thread.Sleep(1);
			}
		}

		private bool isError = false;
		public bool Send(string msg)
		{
			lock (this)
			{
				if (isError) return false;
				try
				{
					byte[] WDbytes = Encoding.UTF8.GetBytes(((char)2) + msg + ((char)3));
					stream.Write(WDbytes, 0, WDbytes.Length);
					return true;
				}
				catch
				{
					isError = true;
					return false;
				}
			}
		}
	}
}
