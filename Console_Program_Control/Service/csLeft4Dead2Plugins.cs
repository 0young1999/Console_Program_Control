using Azure;
using Console_Program_Control.Data;
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

			threadServerConnect = new Thread(ServerConnect);
			threadServerConnect.Name = "tL4D2ServerConnect";
			threadServerConnect.IsBackground = true;
			threadServerConnect.Start();

			threadProcessingReceive = new Thread(ProcessingReceive);
			threadProcessingReceive.Name = "tL4D2ProcessingReceive";
			threadProcessingReceive.IsBackground = true;
			threadProcessingReceive.Start();

			threadProcessLoadCheck = new Thread(ProcessLoadCheckThread);
			threadProcessLoadCheck.Name = "tL4D2ProcessLoadCheck";
			threadProcessLoadCheck.IsBackground = true;
			threadProcessLoadCheck.Start();

			threadDBBackup = new Thread(DBBackupData);
			threadDBBackup.Name = "tL4D2DBBackup";
			threadDBBackup.IsBackground = true;
			threadDBBackup.Start();
		}

		#region 설정 관련
		public string TargetServerName { get; set; }
		public string TargetChannelName { get; set; }
		#endregion

		private csLeft4Dead2PluginsData_Kill_Count kill_Count = csLeft4Dead2PluginsData_Kill_Count.GetInstance();
		private csLeft4Dead2CoolTimeServer ctServer = csLeft4Dead2CoolTimeServer.GetInstance();

		TcpClient tcpClient;
		NetworkStream stream;
		private string LastErrorServerConnect = string.Empty;
		private string LastErrorProcessingReceive = string.Empty;
		private ConcurrentQueue<RecevieStemp> ReceiveStack = new ConcurrentQueue<RecevieStemp>();
		private ConcurrentQueue<RecevieStemp> ProcessEndList = new ConcurrentQueue<RecevieStemp>();

		private Thread threadServerConnect;
		public DateTime dtLastStartServerConnect = DateTime.Now;
		private static object lockSendMsg = new object();
		private void ServerConnect()
		{
			FormMain main = FormMain.GetInstance();
			csDiscord dc = csDiscord.GetInstance();
			tcpClient = new TcpClient();
			DateTime lastWDT = DateTime.Now;
			main.MainLogAppend(eMainLogType.Left4Dead2Plugins, true, "서버 접속 대기 중...");
			dc.SendMessage(TargetServerName, TargetChannelName, "[LEFT4DEAD2]서버 접속 대기 중...");
			while (true)
			{
				try
				{
					dtLastStartServerConnect = DateTime.Now;
					tcpClient.Connect("127.0.0.1", 27020);
					main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, "서버 연결 완료!!!");
					dc.SendMessage(TargetServerName, TargetChannelName, "[LEFT4DEAD2]서버 연결 완료!!!");

					stream = tcpClient.GetStream();
					stream.ReadTimeout = 100;
					string ReceivedStack = string.Empty;

					while (tcpClient.Connected)
					{
						dtLastStartServerConnect = DateTime.Now;
						if (stream.DataAvailable)
						{
							byte[] buffer = new byte[1024];
							int bytesRead = stream.Read(buffer, 0, buffer.Length);
							if (bytesRead == 0) break;
							string receivedResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

							//string logPath = $"LEFT4DEAD\\TCP_LOG\\{DateTime.Now.Year}\\{DateTime.Now.Month}";
							//Directory.CreateDirectory(logPath);
							//logPath += $"\\{DateTime.Now.Day}.log";
							//File.AppendAllText(logPath, $"[{DateTime.Now.ToString("HH mm ss fff")}]{receivedResponse}\n");

							ReceivedStack += receivedResponse;

							DateTime startTime = DateTime.Now;

							while (startTime + new TimeSpan(0,0,5) > DateTime.Now)
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

								ReceiveStack.Enqueue(new RecevieStemp() { recevieTime = DateTime.Now, recevieMessage = sTemp });
							}
						}

						if (lastWDT + new TimeSpan(0, 0, 5) < DateTime.Now)
						{
							lock (lockSendMsg)
							{
								lastWDT = DateTime.Now;
								byte[] WDbytes = Encoding.UTF8.GetBytes(((char)2) + "WDT" + ((char)3));
								stream.Write(WDbytes, 0, WDbytes.Length);
							}
						}

						Thread.Sleep(1);
					}
				}
				catch (Exception e)
				{
					if (LastErrorServerConnect.Equals(e.ToString()) == false)
					{
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"ServerConnect ERROR EXCEPTION!\n{e.ToString()}");
						LastErrorServerConnect = e.ToString();
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
		public DateTime dtLastStartProcessingReceive = DateTime.Now;
		protected class RecevieStemp()
		{
			public DateTime recevieTime;
			public DateTime processingStartTime;
			public DateTime processingEndTime;
			public string recevieMessage = "";
		}
		private void ProcessingReceive()
		{
			FormMain main = FormMain.GetInstance();

			while (true)
			{
				try
				{
					dtLastStartProcessingReceive = DateTime.Now;
					if (ReceiveStack.Count > 0)
					{
						if (ReceiveStack.TryDequeue(out RecevieStemp? Receive) && Receive != null)
						{
							Receive.processingStartTime = DateTime.Now;
							string commandType = Receive.recevieMessage.Split("|").First();

							switch (commandType)
							{
								case "플레이어 접속 정보":
									PlayerConnectionStatus(Receive.recevieMessage);
									break;
								case "플레이어 업적 정보":
									kill_Count.PlayerKillCountStatus(Receive.recevieMessage);
									break;
								case "플레이어 업적 정보 탱크":
									kill_Count.PlayerKillCountStatusTANK(Receive.recevieMessage);
									break;
								case "플레이어 채팅 출력":
									GetChat(Receive.recevieMessage);
									break;
								case "탱크 생성 정보":
								case "탱크 체력 정보":
								case "탱크 사망 정보":
									ctServer.SendToAllClient(Receive.recevieMessage);
									break;
								case "스킬 가동":
									{
										string[] splits = Receive.recevieMessage.Split("|");
										if (splits.Length == 4)
										{
											string steamID64 = splits[1];
											string skillID = splits[2];
											string coolTime = splits[3];
											ctServer.SendSkill(steamID64, $"RunSkill|{skillID}|{coolTime}");
										}
									}
									break;
								default:
									main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"미확인 커멘드 : {Receive.recevieMessage}");
									break;
							}

							Receive.processingEndTime = DateTime.Now;
							ProcessEndList.Enqueue(Receive);
						}
						else
						{
							main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"ProcessingReceive ERROR TryDequeue!");
						}
					}
					else Thread.Sleep(1);
				}
				catch (Exception e)
				{
					if (LastErrorProcessingReceive.Equals(e.ToString()) == false)
					{
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"ProcessingReceive ERROR EXCEPTION!\n{e.ToString()}");
						LastErrorProcessingReceive = e.ToString();
					}
				}
			}
		}

		private Thread threadProcessLoadCheck;
		public string ReceiveThreadLoadString = string.Empty;
		public bool isResetMaxLoadFlag = false;
		public DateTime dtLastStartProcessLoadCheckThread = DateTime.Now;
		private void ProcessLoadCheckThread()
		{
			List<RecevieStemp> listM = new List<RecevieStemp>();
			List<RecevieStemp> listH = new List<RecevieStemp>();
			DateTime dtMDelayMaxLoadTime = DateTime.MinValue;
			DateTime dtMProcessMaxLoadTime = DateTime.MinValue;
			DateTime dtHDelayMaxLoadTime = DateTime.MinValue;
			DateTime dtHProcessMaxLoadTime = DateTime.MinValue;
			float fMDelayMaxLoad = 0f;
			float fMProcessMaxLoad = 0f;
			float fHDelayMaxLoad = 0f;
			float fHProcessMaxLoad = 0f;
			lock (ReceiveThreadLoadString) ReceiveThreadLoadString = "통계 시작 안됨";
			while (true)
			{
				dtLastStartProcessLoadCheckThread = DateTime.Now;

				while (ProcessEndList.Count > 0)
				{
					if (ProcessEndList.TryDequeue(out RecevieStemp? result) && result != null)
					{
						listM.Add(result);
						listH.Add(result);
					}
				}

				while (listM.Count > 0 && (DateTime.Now - listM[0].recevieTime).TotalSeconds > 60)
				{
					listM.RemoveAt(0);
				}

				while (listH.Count > 0 && (DateTime.Now - listH[0].recevieTime).TotalMinutes > 60)
				{
					listH.RemoveAt(0);
				}

				if (isResetMaxLoadFlag)
				{
					fMDelayMaxLoad = 0f;
					fMProcessMaxLoad = 0f;
					fHDelayMaxLoad = 0f;
					fHProcessMaxLoad = 0f;
					dtMDelayMaxLoadTime = DateTime.MinValue;
					dtMProcessMaxLoadTime = DateTime.MinValue;
					dtHDelayMaxLoadTime = DateTime.MinValue;
					dtHProcessMaxLoadTime = DateTime.MinValue;
					isResetMaxLoadFlag = false;
				}
				if (listM.Count == 0 && listH.Count == 0)
				{
					lock (ReceiveThreadLoadString)
					{
						ReceiveThreadLoadString = "최근 부하가 없어 분석할수 없습니다.";
					}
				}

				StringBuilder sb = new StringBuilder();

				if (listM.Count > 0)
				{
					int iDelayTotal = 0;
					int iDelayMax = 0;
					string sDelayMax = string.Empty;

					int iProcessTotal = 0;
					int iProcessMax = 0;
					string sProcessMax = string.Empty;

					foreach (RecevieStemp stemp in listM)
					{
						int iDelayTime = (int)(stemp.processingStartTime - stemp.recevieTime).TotalMilliseconds;
						int iProcessTime = (int)(stemp.processingEndTime - stemp.processingStartTime).TotalMilliseconds;

						iDelayTotal += iDelayTime;
						iProcessTotal += iProcessTime;

						if (iDelayMax <= iDelayTime)
						{
							iDelayMax = iDelayTime;
							sDelayMax = stemp.recevieMessage;
						}
						if (iProcessMax <= iProcessTime)
						{
							iProcessMax = iProcessTime;
							sProcessMax = stemp.recevieMessage;
						}
					}

					int iDelayAgv = iDelayTotal / listM.Count;
					int iProcessAgv = iProcessTotal / listM.Count;

					float fDelayPersent = ((float)iDelayTotal / 60000) * 100;
					float fProcessPersent = ((float)iProcessTotal / 60000) * 100;

					if (fDelayPersent >= fMDelayMaxLoad)
					{
						fMDelayMaxLoad = fDelayPersent;
						dtMDelayMaxLoadTime = DateTime.Now;
					}

					if (fProcessPersent >= fMProcessMaxLoad)
					{
						fMProcessMaxLoad = fProcessPersent;
						dtMProcessMaxLoadTime = DateTime.Now;
					}

					sb.AppendLine("====최근 1분 부하====");
					sb.AppendLine($"처리 갯수 : {listM.Count.ToString("N0")}EA");
					sb.AppendLine($"평균 대기 시간 : {iDelayAgv.ToString("N0")}ms");
					sb.AppendLine($"최대 대기 시간 : {iDelayMax.ToString("N0")}ms");
					sb.AppendLine($"최대 대기 메세지 : {sDelayMax}");
					sb.AppendLine($"대기 로드 : {fDelayPersent.ToString("N2")}%");
					sb.AppendLine($"최대 대기 로드 : {fMDelayMaxLoad.ToString("N2")}%[{dtMDelayMaxLoadTime.ToString("dd HH:mm:ss")}]");
					sb.AppendLine($"평균 처리 시간 : {iProcessAgv.ToString("N0")}ms");
					sb.AppendLine($"최대 처리 시간 : {iProcessMax.ToString("N0")}ms");
					sb.AppendLine($"최대 처리 메세지 : {sProcessMax}");
					sb.AppendLine($"처리 로드 : {fProcessPersent.ToString("N2")}%");
					sb.AppendLine($"최대 처리 로드 : {fMProcessMaxLoad.ToString("N2")}%[{dtMProcessMaxLoadTime.ToString("dd HH:mm:ss")}]");
				}
				else
				{
					sb.AppendLine("최근 1분 부하가 없습니다.");
					sb.AppendLine($"최대 대기 로드 : {fMDelayMaxLoad.ToString("N2")}%[{dtMDelayMaxLoadTime.ToString("dd HH:mm:ss")}]");
					sb.AppendLine($"최대 처리 로드 : {fMProcessMaxLoad.ToString("N2")}%[{dtMProcessMaxLoadTime.ToString("dd HH:mm:ss")}]");
				}

				if (listH.Count > 0)
				{
					int iDelayTotal = 0;
					int iDelayMax = 0;
					string sDelayMax = string.Empty;

					int iProcessTotal = 0;
					int iProcessMax = 0;
					string sProcessMax = string.Empty;
					foreach (RecevieStemp stemp in listH)
					{
						int iDelayTime = (int)(stemp.processingStartTime - stemp.recevieTime).TotalMilliseconds;
						int iProcessTime = (int)(stemp.processingEndTime - stemp.processingStartTime).TotalMilliseconds;
						iDelayTotal += iDelayTime;
						iProcessTotal += iProcessTime;
						if (iDelayMax <= iDelayTime)
						{
							iDelayMax = iDelayTime;
							sDelayMax = stemp.recevieMessage;
						}
						if (iProcessMax <= iProcessTime)
						{
							iProcessMax = iProcessTime;
							sProcessMax = stemp.recevieMessage;
						}
					}

					int iDelayAgv = iDelayTotal / listH.Count;
					int iProcessAgv = iProcessTotal / listH.Count;

					float fDelayPersent = ((float)iDelayTotal / 3600000) * 100;
					float fProcessPersent = ((float)iProcessTotal / 3600000) * 100;

					if (fDelayPersent >= fHDelayMaxLoad)
					{
						fHDelayMaxLoad = fDelayPersent;
						dtHDelayMaxLoadTime = DateTime.Now;
					}

					if (fProcessPersent >= fHProcessMaxLoad)
					{
						fHProcessMaxLoad = fProcessPersent;
						dtHProcessMaxLoadTime = DateTime.Now;
					}

					sb.AppendLine("====최근 1시간 부하====");
					sb.AppendLine($"처리 갯수 : {listH.Count.ToString("N0")}EA");
					sb.AppendLine($"평균 대기 시간 : {iDelayAgv.ToString("N0")}ms");
					sb.AppendLine($"최대 대기 시간 : {iDelayMax.ToString("N0")}ms");
					sb.AppendLine($"최대 대기 메세지 : {sDelayMax}");
					sb.AppendLine($"대기 로드 : {fDelayPersent.ToString("N2")}%");
					sb.AppendLine($"최대 대기 로드 : {fHDelayMaxLoad.ToString("N2")}%[{dtHDelayMaxLoadTime.ToString("dd HH:mm:ss")}]");
					sb.AppendLine($"평균 처리 시간 : {iProcessAgv.ToString("N0")}ms");
					sb.AppendLine($"최대 처리 시간 : {iProcessMax.ToString("N0")}ms");
					sb.AppendLine($"최대 처리 메세지 : {sProcessMax}");
					sb.AppendLine($"처리 로드 : {fProcessPersent.ToString("N2")}%");
					sb.AppendLine($"최대 처리 로드 : {fHProcessMaxLoad.ToString("N2")}%[{dtHProcessMaxLoadTime.ToString("dd HH:mm:ss")}]");
				}
				else
				{
					sb.AppendLine("최근 1시간 부하가 없습니다.");
					sb.AppendLine($"최대 대기 로드 : {fHDelayMaxLoad.ToString("N2")}%[{dtHDelayMaxLoadTime.ToString("dd HH:mm:ss")}]");
					sb.AppendLine($"최대 처리 로드 : {fHProcessMaxLoad.ToString("N2")}%[{dtHProcessMaxLoadTime.ToString("dd HH:mm:ss")}]");
				}

				sb.AppendLine("==== 그 외 ====");
				sb.AppendLine($"대기중인 메세지 갯수 : {ReceiveStack.Count.ToString("N0")}EA");
				sb.AppendLine($"ServerConnect 시작 시간 : {dtLastStartServerConnect.ToString("HH mm ss")}");
				sb.AppendLine($"ProcessingReceive 시작 시간 : {dtLastStartProcessingReceive.ToString("HH mm ss")}");
				sb.AppendLine($"ProcessLoadCheck 시작 시간 : {dtLastStartProcessLoadCheckThread.ToString("HH mm ss")}");

				lock (ReceiveThreadLoadString)
				{
					ReceiveThreadLoadString = sb.ToString();
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

				csUserProfile up = csUserProfile.GetInstance();
				lock (up.LockDatas)
				{
					if (up.datas.Any(item => item.SteamID == steamID && item.SteamID64 == steamID64)) return;
				}

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

		#region 플레이어 채팅
		private void GetChat(string receive)
		{
			if (string.IsNullOrEmpty(TargetServerName) || string.IsNullOrEmpty(TargetChannelName)) return;

			FormMain main = FormMain.GetInstance();
			csUserProfile up = csUserProfile.GetInstance();
			List<string> splits = receive.Split("|").ToList();
			if (splits.Count > 2)
			{
				string steamID64 = splits[1];
				splits.RemoveAt(0);
				splits.RemoveAt(0);
				string chat = string.Join('|', splits.ToArray());
				if (chat.IndexOf('!') == 0 || chat.IndexOf('/') == 0) return;

				lock (up.LockDatas)
				{
					csUserProfileData? request = up.datas.FirstOrDefault(item => item.SteamID64 == steamID64);

					if (request == null)
					{
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"해당 유저를 찾을수 없습니다. : {receive}");
						return;
					}

					string textout = $"[LEFT4DEAD2,{request.nick}]\n{chat}";

					csDiscord dc = csDiscord.GetInstance();
					dc.SendMessage(TargetServerName, TargetChannelName, textout);
				}
			}
			else
			{
				main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"알수 없는 구문 : {receive}");
			}
		}
		public void SendChat(string msg)
		{
			lock (lockSendMsg)
			{
				if (stream != null)
				{
					try
					{
						byte[] WDbytes = Encoding.UTF8.GetBytes(((char)2) + $"SendChat|{msg}" + ((char)3));
						stream.Write(WDbytes, 0, WDbytes.Length);
					}
					catch { }
				}
			}
		}
		#endregion

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
				DateTime timeDelay = DateTime.Now + new TimeSpan(0, 1, 0);
				while (timeDelay > DateTime.Now)
				{
					Thread.Sleep(1);
				}
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

								sb.AppendLine(kill_Count.GetReport(up.SteamID64));

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
