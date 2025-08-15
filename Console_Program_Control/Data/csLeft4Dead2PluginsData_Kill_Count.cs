using System.Text;

namespace Console_Program_Control.Data
{
	public class csLeft4Dead2PluginsData_Kill_Count
	{
		private static csLeft4Dead2PluginsData_Kill_Count instance;
		public static csLeft4Dead2PluginsData_Kill_Count GetInstance()
		{
			if (instance == null) instance = new csLeft4Dead2PluginsData_Kill_Count();
			return instance;
		}
		private csLeft4Dead2PluginsData_Kill_Count()
		{
			main = FormMain.GetInstance();
			datas = new List<csData>();

			LoadKillCount();
		}

		private FormMain main;

		private List<csData> datas;

		public void ResetToDayCount()
		{
			lock (datas)
			{
				foreach (csData data in datas)
				{
					data.ResetToDayCount();
				}
			}
		}
		private void LoadKillCount()
		{
			ResetToDayCount();

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
					ResetToDayCount();
				}
			}
		}
		public void PlayerKillCountStatus(string command, bool isNotAppendLog = false)
		{
			FormMain main = FormMain.GetInstance();
			csUserProfile up = csUserProfile.GetInstance();
			string[] splits = command.Split("|");
			if (splits.Length == 3)
			{
				string steamID64 = splits[1];
				string target = splits[2];

				AddKillCount(steamID64, target);

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
		public void PlayerKillCountStatusTANK(string command, bool isNotAppendLog = false)
		{
			FormMain main = FormMain.GetInstance();
			csUserProfile up = csUserProfile.GetInstance();
			string[] splits = command.Split("|");
			if (splits.Length == 2)
			{
				string[] steamID64s = splits[1].Split(',');

				foreach (string steamID in steamID64s)
				{
					AddKillCount(steamID, "Tank");
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

		public void AddKillCount(string SteamID64, string Target)
		{
			lock (datas)
			{
				csData data = datas.Where(item => item.SteamID64 == SteamID64).FirstOrDefault();

				if (data == null)
				{
					data = new csData();
					data.SteamID64 = SteamID64;
					datas.Add(data);
				}


				if (data.lastUpdateDate.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) == false)
				{
					data.ResetToDayCount();
					data.lastUpdateDate = DateTime.Now;
				}

				switch (Target)
				{
					case "Smoker":
						data.ToDay_Kill_Smoker++;
						data.Kill_Smoker++;
						break;
					case "Boomer":
						data.ToDay_Kill_Boomer++;
						data.Kill_Boomer++;
						break;
					case "Hunter":
						data.ToDay_Kill_Hunter++;
						data.Kill_Hunter++;
						break;
					case "Spitter":
						data.ToDay_Kill_Spitter++;
						data.Kill_Spitter++;
						break;
					case "Jockey":
						data.ToDay_Kill_Jockey++;
						data.Kill_Jockey++;
						break;
					case "Charger":
						data.ToDay_Kill_Charger++;
						data.Kill_Charger++;
						break;
					case "Common":
						data.ToDay_Kill_Common++;
						data.Kill_Common++;
						break;
					case "Witch":
						data.ToDay_Kill_Witch++;
						data.Kill_Witch++;
						break;
					case "Tank":
						data.ToDay_Kill_Tank++;
						data.Kill_Tank++;
						break;
					case "":
						return;
					default:
						main.MainLogAppend(eMainLogType.Left4Dead2Plugins, false, $"죽인 대상에 대한 처리가 되어있지 않습니다. : {Target}");
						return;
				}
			}
		}

		public string GetReport(string SteamID64)
		{
			StringBuilder sb = new StringBuilder();

			lock (datas)
			{
				csData data = datas.Where(item => item.SteamID64 == SteamID64).FirstOrDefault();

				if (data == null)
				{
					data = new csData();
					data.SteamID64 = SteamID64;
					datas.Add(data);
				}

				if (data.lastUpdateDate.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) == false)
				{
					data.ResetToDayCount();
				}

				sb.AppendLine("======당일/종합 킬 카운트======");
				sb.AppendLine($"탱크(협동) : {data.ToDay_Kill_Tank.ToString("N0")}마리/{data.Kill_Tank.ToString("N0")}마리");
				sb.AppendLine($"부머(개인) : {data.ToDay_Kill_Boomer.ToString("N0")}마리/{data.Kill_Boomer.ToString("N0")}마리");
				sb.AppendLine($"차저(개인) : {data.ToDay_Kill_Charger.ToString("N0")}마리/{data.Kill_Charger.ToString("N0")}마리");
				sb.AppendLine($"헌터(개인) : {data.ToDay_Kill_Hunter.ToString("N0")}마리/{data.Kill_Hunter.ToString("N0")}마리");
				sb.AppendLine($"자키(개인) : {data.ToDay_Kill_Jockey.ToString("N0")}마리/{data.Kill_Jockey.ToString("N0")}마리");
				sb.AppendLine($"스모커(개인) : {data.ToDay_Kill_Smoker.ToString("N0")}마리/{data.Kill_Smoker.ToString("N0")}마리");
				sb.AppendLine($"스피터(개인) : {data.ToDay_Kill_Spitter.ToString("N0")}마리/{data.Kill_Spitter.ToString("N0")}마리");
				sb.AppendLine($"위치(개인) : {data.ToDay_Kill_Witch.ToString("N0")}마리/{data.Kill_Witch.ToString("N0")}마리");
				sb.AppendLine($"일반 좀비(개인) : {data.ToDay_Kill_Common.ToString("N0")}마리/{data.Kill_Common.ToString("N0")}마리");
			}

			return sb.ToString();
		}

		protected class csData()
		{
			public string SteamID64 = string.Empty;

			public DateTime lastUpdateDate = DateTime.MinValue;
			public decimal Kill_Tank = 0;
			public decimal Kill_Smoker = 0;
			public decimal Kill_Boomer = 0;
			public decimal Kill_Hunter = 0;
			public decimal Kill_Spitter = 0;
			public decimal Kill_Jockey = 0;
			public decimal Kill_Charger = 0;
			public decimal Kill_Common = 0;
			public decimal Kill_Witch = 0;
			public decimal ToDay_Kill_Tank = 0;
			public decimal ToDay_Kill_Smoker = 0;
			public decimal ToDay_Kill_Boomer = 0;
			public decimal ToDay_Kill_Hunter = 0;
			public decimal ToDay_Kill_Spitter = 0;
			public decimal ToDay_Kill_Jockey = 0;
			public decimal ToDay_Kill_Charger = 0;
			public decimal ToDay_Kill_Common = 0;
			public decimal ToDay_Kill_Witch = 0;

			public void ResetToDayCount()
			{
				ToDay_Kill_Tank = 0;
				ToDay_Kill_Smoker = 0;
				ToDay_Kill_Boomer = 0;
				ToDay_Kill_Hunter = 0;
				ToDay_Kill_Spitter = 0;
				ToDay_Kill_Jockey = 0;
				ToDay_Kill_Charger = 0;
				ToDay_Kill_Common = 0;
				ToDay_Kill_Witch = 0;
				lastUpdateDate = DateTime.Now;
			}
		}
	}
}
