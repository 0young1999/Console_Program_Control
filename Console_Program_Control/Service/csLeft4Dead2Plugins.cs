using Console_Program_Control.HIDE.CODE;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using System.Data;
using System.Net.Sockets;
using System.Text;
using Young.Setting;

namespace Console_Program_Control.Service
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

			LoadKillCount();

			threadServerConnect = new Thread(ServerConnect);
			threadServerConnect.Name = "threadServerConnect";
			threadServerConnect.IsBackground = true;
			threadServerConnect.Start();

			threadProcessingReceive = new Thread(ProcessingReceive);
			threadProcessingReceive.Name = "threadProcessingReceive";
			threadProcessingReceive.IsBackground = true;
			threadProcessingReceive.Start();

			threadDBBackup = new Thread(DBBackupData);
			threadDBBackup.Name = "threadDBBackup";
			threadDBBackup.IsBackground = true;
			threadDBBackup.Start();
		}

		TcpClient tcpClient;
		NetworkStream stream;
		private string LastError = string.Empty;
		private ConcurrentQueue<string> ReceiveStack = new ConcurrentQueue<string>();

		private Thread threadServerConnect;
		private void ServerConnect()
		{
			FormMain main = FormMain.GetInstance();
			tcpClient = new TcpClient();
			DateTime lastWDT = DateTime.Now;
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
						if (stream.DataAvailable)
						{
							byte[] buffer = new byte[1024];
							int bytesRead = stream.Read(buffer, 0, buffer.Length);
							string receivedResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
							if (bytesRead == 0) break;
							main.MainLogAppend(eMainLogType.Left4Dead2Plugins, true, receivedResponse);

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

								ReceiveStack.Enqueue(sTemp);
							}
						}

						if (lastWDT + new TimeSpan(0, 0, 5) < DateTime.Now)
						{
							lastWDT = DateTime.Now;
							byte[] WDbytes = Encoding.UTF8.GetBytes(((char)2) + "WDT" + ((char)3));
							stream.Write(WDbytes, 0, WDbytes.Length);
						}

						Thread.Sleep(1);
					}
				}
				catch (Exception e)
				{
					if (LastError.Equals(e.ToString()) == false)
					{
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"ERROR EXCEPTION!\n{e.ToString()}");
						LastError = e.ToString();
					}
				}
				finally
				{
					tcpClient.Dispose();
					tcpClient = new TcpClient();
				}

				Thread.Sleep(1);
			}
		}

		private Thread threadProcessingReceive;
		private void ProcessingReceive()
		{
			FormMain main = FormMain.GetInstance();

			while (true)
			{
				if (ReceiveStack.Count > 0)
				{
					if (ReceiveStack.TryDequeue(out string Receive))
					{
						string commandType = Receive?.Split("|")?.First() ?? "";

						switch (commandType)
						{
							case "플레이어 접속 정보":
								PlayerConnectionStatus(Receive);
								break;
							case "플레이어 업적 정보":
								PlayerKillCountStatus(Receive);
								break;
							case "플레이어 업적 정보 탱크":
								PlayerKillCountStatusTANK(Receive);
								break;
							default:
								main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"미확인 커멘드 : {Receive}");
								break;
						}
					}
				}
				Thread.Sleep(1);
			}
		}

		private static object Lock_ID_Name_Matching = new object();
		private void PlayerConnectionStatus(string command)
		{
			FormMain main = FormMain.GetInstance();
			string[] splits = command.Split("|");
			if (splits.Length == 4)
			{
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
		private void PlayerKillCountStatus(string command, bool isNotAppendLog = false)
		{
			FormMain main = FormMain.GetInstance();
			csUserProfile up = csUserProfile.GetInstance();
			string[] splits = command.Split("|");
			if (splits.Length == 3)
			{
				string steamID64 = splits[1];
				string target = splits[2];

				lock (up.LockDatas)
				{
					csUserProfileData request = up.datas.Where(item => item.SteamID64 == steamID64).First();

					if (request == null)
					{
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"해당 유저를 찾을수 없습니다. : {command}");
						return;
					}

					if (request.l4d2pd.lastUpdateDate.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) == false)
					{
						request.l4d2pd.ResetToDayCount();
						request.l4d2pd.lastUpdateDate = DateTime.Now;
					}

					switch (target)
					{
						case "Smoker":
							request.l4d2pd.ToDay_Kill_Smoker++;
							request.l4d2pd.Kill_Smoker++;
							break;
						case "Boomer":
							request.l4d2pd.ToDay_Kill_Boomer++;
							request.l4d2pd.Kill_Boomer++;
							break;
						case "Hunter":
							request.l4d2pd.ToDay_Kill_Hunter++;
							request.l4d2pd.Kill_Hunter++;
							break;
						case "Spitter":
							request.l4d2pd.ToDay_Kill_Spitter++;
							request.l4d2pd.Kill_Spitter++;
							break;
						case "Jockey":
							request.l4d2pd.ToDay_Kill_Jockey++;
							request.l4d2pd.Kill_Jockey++;
							break;
						case "Charger":
							request.l4d2pd.ToDay_Kill_Charger++;
							request.l4d2pd.Kill_Charger++;
							break;
						case "Common":
							request.l4d2pd.ToDay_Kill_Common++;
							request.l4d2pd.Kill_Common++;
							break;
						case "Witch":
							request.l4d2pd.ToDay_Kill_Witch++;
							request.l4d2pd.Kill_Witch++;
							break;
						case "":
							return;
						default:
							main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"죽인 대상에 대한 처리가 되어있지 않습니다. : {command}");
							return;
					}
				}

				if (isNotAppendLog) return;
				string path = "LEFT4DEAD\\KILL_COUNT";
				Directory.CreateDirectory(path);
				path += $"\\KILL_COUNT({DateTime.Now.ToString("yyyy-MM-dd")}).log";
				File.AppendAllText(path, command + "\n");
			}
			else
			{
				main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"알수 없는 구문 : {command}");
			}
		}
		private void PlayerKillCountStatusTANK(string command, bool isNotAppendLog = false)
		{
			FormMain main = FormMain.GetInstance();
			csUserProfile up = csUserProfile.GetInstance();
			string[] splits = command.Split("|");
			if (splits.Length == 2)
			{
				string[] steamID64s = splits[1].Split(',');

				foreach (string steamID in steamID64s)
				{
					lock (up.LockDatas)
					{
						csUserProfileData request = up.datas.Where(item => item.SteamID64 == steamID).First();

						if (request == null)
						{
							main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"해당 유저를 찾을수 없습니다. : {command}");
							return;
						}

						if (request.l4d2pd.lastUpdateDate.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) == false)
						{
							request.l4d2pd.ResetToDayCount();
							request.l4d2pd.lastUpdateDate = DateTime.Now;
						}

						request.l4d2pd.ToDay_Kill_Tank++;
						request.l4d2pd.Kill_Tank++;
					}
				}

				if (isNotAppendLog) return;
				string path = "LEFT4DEAD\\KILL_COUNT";
				Directory.CreateDirectory(path);
				path += $"\\KILL_COUNT({DateTime.Now.ToString("yyyy-MM-dd")}).log";
				File.AppendAllText(path, command + "\n");
			}
			else
			{
				main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"알수 없는 구문 : {command}");
			}
		}
		private void LoadKillCount()
		{
			csUserProfile up = csUserProfile.GetInstance();
			lock (up.LockDatas)
			{
				up.ResetL4D2KillToDayCount();
			}
			string path = "LEFT4DEAD\\KILL_COUNT";
			if (Directory.Exists(path) == false) return;
			string[] files = Directory.GetFiles(path);
			foreach (string file in files)
			{
				string[] lines = File.ReadAllLines(file);
				foreach (string line in lines)
				{
					string nline = line?.Replace("\n", " ")?.Trim() ?? "";
					string commandType = nline?.Split('|')?.First() ?? "";
					switch (commandType)
					{
						case "플레이어 업적 정보":
							PlayerKillCountStatus(nline, true);
							break;
						case "플레이어 업적 정보 탱크":
							PlayerKillCountStatusTANK(nline, true);
							break;
					}
				}

				if (file.Substring(file.LastIndexOf('(') + 1, 10).Equals(DateTime.Now.ToString("yyyy-MM-dd")) == false)
				{
					lock (up.LockDatas)
					{
						up.ResetL4D2KillToDayCount();
					}
				}
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

								StringBuilder sb = new StringBuilder();
								sb.AppendLine($"[{up.nick}]님의 LEFT 4 DEAD 2 프로필");
								sb.AppendLine($"RANK : {iRank.ToString("N0")} | LEVEL : {iLevel.ToString("N0")} | 잔여 포인트 : {iPoint.ToString("N0")} | 경험치 : {iExp.ToString("N0")}");
								sb.AppendLine($"힘 : {iStr.ToString("N0")} | 민첩 : {iAgi.ToString("N0")} | 체력 : {iCon.ToString("N0")} | 지능 : {iInt.ToString("N0")} | 점프 : {iJump}");
								sb.AppendLine();

								if (up.l4d2pd.lastUpdateDate.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) == false)
								{
									up.l4d2pd.ResetToDayCount();
								}

								sb.AppendLine("======당일/종합 킬 카운트======");
								sb.AppendLine($"탱크(협동) : {up.l4d2pd.ToDay_Kill_Tank.ToString("N0")}마리/{up.l4d2pd.Kill_Tank.ToString("N0")}마리");
								sb.AppendLine($"부머(개인) : {up.l4d2pd.ToDay_Kill_Boomer.ToString("N0")}마리/{up.l4d2pd.Kill_Boomer.ToString("N0")}마리");
								sb.AppendLine($"차저(개인) : {up.l4d2pd.ToDay_Kill_Charger.ToString("N0")}마리/{up.l4d2pd.Kill_Charger.ToString("N0")}마리");
								sb.AppendLine($"헌터(개인) : {up.l4d2pd.ToDay_Kill_Hunter.ToString("N0")}마리/{up.l4d2pd.Kill_Hunter.ToString("N0")}마리");
								sb.AppendLine($"자키(개인) : {up.l4d2pd.ToDay_Kill_Jockey.ToString("N0")}마리/{up.l4d2pd.Kill_Jockey.ToString("N0")}마리");
								sb.AppendLine($"스모커(개인) : {up.l4d2pd.ToDay_Kill_Smoker.ToString("N0")}마리/{up.l4d2pd.Kill_Smoker.ToString("N0")}마리");
								sb.AppendLine($"스피터(개인) : {up.l4d2pd.ToDay_Kill_Spitter.ToString("N0")}마리/{up.l4d2pd.Kill_Spitter.ToString("N0")}마리");
								sb.AppendLine($"위치(개인) : {up.l4d2pd.ToDay_Kill_Witch.ToString("N0")}마리/{up.l4d2pd.Kill_Witch.ToString("N0")}마리");
								sb.AppendLine($"일반 좀비(개인) : {up.l4d2pd.ToDay_Kill_Common.ToString("N0")}마리/{up.l4d2pd.Kill_Common.ToString("N0")}마리");

								response = sb.ToString();
							}
							else
							{
								response = $"[{up.nick}]님의 LEFT 4 DEAD 2 프로필\n놀라울 정도로 비어있음";
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
