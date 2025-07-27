using System.Net.Sockets;
using System.Text;
using Young.Setting;

namespace Console_Program_Control.Data
{
	public class csLeft4Dead2Plugins : csAutoSaveLoad
	{
		private static csLeft4Dead2Plugins instance;
		public static csLeft4Dead2Plugins GetInstance()
		{
			if (instance == null) instance = new csLeft4Dead2Plugins();
			return instance;
		}
		private csLeft4Dead2Plugins()
		{
			Load();

			threadServerConnect = new Thread(ServerConnect);
			threadServerConnect.IsBackground = true;
			threadServerConnect.Start();
		}

		TcpClient tcpClient;
		NetworkStream stream;
		private string LastError = string.Empty;

		private Thread threadServerConnect;

		private void ServerConnect()
		{
			FormMain main = FormMain.GetInstance();
			tcpClient = new TcpClient();
			main.MainLogAppend(eMainLogType.Left4Dead2Plugins, true, "서버 접속 대기 중...");
			while (true)
			{
				try
				{
					tcpClient.Connect("127.0.0.1", 27020);
					main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, "서버 연결 완료!!!");

					stream = tcpClient.GetStream();
					stream.ReadTimeout = 100;
					string ReceivedStack = string.Empty;

					while (tcpClient.Connected)
					{
						try
						{
							byte[] buffer = new byte[1024];
							int bytesRead = stream.Read(buffer, 0, buffer.Length);
							string receivedResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
							if (bytesRead == 0) break;
							main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, receivedResponse);

							ReceivedStack += receivedResponse;

							while (true)
							{
								int stxIndex = ReceivedStack.IndexOf('\x02');
								int etxIndex = ReceivedStack.IndexOf('\x03');
								if (stxIndex == -1 || etxIndex == -1)
								{
									ReceivedStack = string.Empty;
									break;
								}

								if (stxIndex == etxIndex)
								{
									ReceivedStack = string.Empty;
									break;
								}

								if (stxIndex >= etxIndex)
								{
									ReceivedStack = ReceivedStack.Substring(stxIndex - 1);
									continue;
								}

								// s01234e
								// 0123456
								string sTemp = ReceivedStack.Substring(stxIndex + 1, etxIndex - stxIndex - 1);
								ReceivedStack = ReceivedStack.Substring(etxIndex + 1);

								string commandType = sTemp.Split("|").First();

								switch (commandType)
								{
									case "플레이어 접속 정보":
										PlayerConnectionStatus(sTemp);
										break;
									default:
										main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"미확인 커멘드 : {sTemp}");
										break;
								}
							}
						}
						catch
						{
							ReceivedStack = string.Empty;
						}
					}
				}
				catch (Exception e)
				{
					if (e.ToString().IndexOf("대상 컴퓨터에서 연결을") == -1 && LastError.Equals(e.ToString()) == false)
					{
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, true, $"ERROR EXCEPTION!\n{e.ToString()}");
						LastError = e.ToString();
					}
				}
				finally
				{
					if (tcpClient.Connected)
					{
						tcpClient.Dispose();
						tcpClient = new TcpClient();
					}
				}
			}
		}

		private static object Lock_ID_Name_Matching = new object();
		private void PlayerConnectionStatus(string command)
		{
			FormMain main = FormMain.GetInstance();
			if (command.Split("|").Length == 4)
			{
				string[] splits = command.Split("|");
				string steamID64 = splits[1];
				string steamID = splits[2];
				string steamName = splits[3];

				string appendString = $"[{DateTime.Now.ToString("dd-HH-mm-ss")}]{steamID64}|{steamID}|{steamName}\n";

				lock (Lock_ID_Name_Matching)
				{
					string path = "LEFT4DEAD";

					if (Directory.Exists(path) == false)
					{
						Directory.CreateDirectory(path);
					}

					path += $"\\ID_Name_Matching_{DateTime.Now.ToString("yyyyMM")}.log";

					File.AppendAllText(path, appendString);
				}
			}
			else
			{
				main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"알수 없는 구문 : {command}");
			}
		}

	}
}
