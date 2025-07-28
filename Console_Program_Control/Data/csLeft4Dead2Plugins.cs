using Console_Program_Control.HIDE.CODE;
using MySql.Data.MySqlClient;
using System.Data;
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

			threadDBBackup = new Thread(DBBackupData);
			threadDBBackup.IsBackground = true;
			threadDBBackup.Start();
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
					tcpClient.Dispose();
					tcpClient = new TcpClient();
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
					string path = $"LEFT4DEAD\\ID_Name_Matching";
					Directory.CreateDirectory(path);

					path += $"\\ID_Name_Matching_{DateTime.Now.ToString("yyyyMM")}.log";

					File.AppendAllText(path, appendString);
				}
			}
			else
			{
				main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"알수 없는 구문 : {command}");
			}
		}

		private static object LockDBCommunication = new object();
		private Thread threadDBBackup;
		private void DBBackupData()
		{
			// 혹시 모를 상황을 대비해 최소값으로 하여 프로그램 시작시 항상 백업 뜨도록
			DateTime lastTime = DateTime.MinValue;

			while (true)
			{
				// 날짜가 변경되었을때
				if (DateTime.Now.ToString("yyyy MM dd").Equals(lastTime.ToString("yyyy MM dd")) == false)
				{
					lock (LockDBCommunication)
					{
						try
						{
							string constring = $"Server={csPrivateCode.LEFT4DEAD2DBServer};" +
								$"Port={csPrivateCode.LEFT4DEAD2DBPort};" +
								$"Database={csPrivateCode.LEFT4DEAD2DB};" +
								$"Uid={csPrivateCode.LEFT4DEAD2ID};" +
								$"Pwd={csPrivateCode.LEFT4DEAD2PW};";

							using (MySqlConnection conn = new MySqlConnection(constring))
							{
								conn.Open();

								string sqlText = "SELECT * FROM user_info";
								using (MySqlDataAdapter adapter = new MySqlDataAdapter(sqlText, conn))
								{
									DataSet dsResult = new DataSet();
									adapter.Fill(dsResult);

									string backupDir = Path.Combine("LEFT4DEAD", "DB_BACKUP", "user_info", DateTime.Now.ToString("yyyy"));
									Directory.CreateDirectory(backupDir);

									string backupFile = Path.Combine(backupDir, DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".xml");
									dsResult.WriteXml(backupFile);

									/*
									 DataRow row = dsResult.Tables[0].AsEnumerable()
										.FirstOrDefault(r => r.Field<string>("steam_id") == "76561199018715799");

									if (row != null)
									{
										int rank = row.Field<int>("rank");
										Console.WriteLine($"Rank: {rank}");
									}
									 */
								}

								lastTime = DateTime.Now;
							}
						}
						catch (Exception e)
						{
							FormMain.GetInstance().MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"[DB]{e}");
						}
					}
				}

				// 1분마다
				Thread.Sleep(60000);
			}
		}
		public string GetProfile(csUserProfileData up)
		{
			string response = string.Empty;

			if (string.IsNullOrEmpty(up.SteamID64))
			{
				response = "당신의 스팀 ID가 등록 되지 않았습니다.";
				return response;
			}

			lock (LockDBCommunication)
			{
				try
				{
					string constring = $"Server={csPrivateCode.LEFT4DEAD2DBServer};" +
						$"Port={csPrivateCode.LEFT4DEAD2DBPort};" +
						$"Database={csPrivateCode.LEFT4DEAD2DB};" +
						$"Uid={csPrivateCode.LEFT4DEAD2ID};" +
						$"Pwd={csPrivateCode.LEFT4DEAD2PW};";

					using (MySqlConnection conn = new MySqlConnection(constring))
					{
						conn.Open();

						string sqlText = "SELECT * FROM user_info";
						using (MySqlDataAdapter adapter = new MySqlDataAdapter(sqlText, conn))
						{
							DataSet dsResult = new DataSet();
							adapter.Fill(dsResult);


							DataRow row = dsResult.Tables[0].AsEnumerable()
							   .FirstOrDefault(r => r.Field<string>("steam_id") == up.SteamID64);

							if (row != null)
							{
								int iRank = row.Field<byte>("rank");
								int iLevel = row.Field<byte>("level");
								uint iStr = row.Field<uint>("str");
								uint iAgi = row.Field<uint>("agi");
								uint iCon = row.Field<uint>("con");
								uint iInt = row.Field<uint>("int");
								uint iPoint = row.Field<uint>("point");
								uint iJump = row.Field<uint>("jump");
								uint iExp = row.Field<uint>("exp");

								response =
									$"[{up.nick}]님의 LEFT 4 DEAD 2 프로필\n" +
									$"RANK : {iRank} | LEVEL : {iLevel}\n" +
									$"힘 : {iStr}\n" +
									$"민첩 : {iAgi}\n" +
									$"체력 : {iCon}\n" +
									$"지능 : {iInt}\n" +
									$"점프 : {iJump}\n" +
									$"잔여 포인트 : {iPoint} | 경험치 : {iExp}";
							}
							else
							{
								response = $"[{up.nick}]님의 LEFT 4 DEAD 2 프로필\n" +
									$"놀라울 정도로 비어있음";
							}
						}
					}
				}
				catch (Exception e)
				{
					FormMain.GetInstance().MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"[DB GetProfile]{e}");
					response = $"[DB GetProfile]{e}";
				}
			}

			return response;
		}
	}
}
