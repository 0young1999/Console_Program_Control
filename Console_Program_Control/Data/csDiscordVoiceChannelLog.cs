using System.Text;
using Young.Setting;

namespace Console_Program_Control.Data
{
	public class csDiscordVoiceChannelLog
	{
        private static csDiscordVoiceChannelLog instance;
        public static csDiscordVoiceChannelLog GetInstance()
        {
            if (instance == null) instance = new csDiscordVoiceChannelLog();
            return instance;
        }
        private csDiscordVoiceChannelLog() { }

		private string logPath = string.Format("LOG\\VOICECHANNEL\\{0}.log", DateTime.Now.ToString("yyyyMMddHHmmssffff"));
		public void AppendLog(csDiscordVoiceChannelLogData data)
		{
			lock (this)
			{
				if (Directory.Exists(Path.GetDirectoryName(logPath)) == false)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(logPath));
				}

				File.AppendAllText(logPath, data.OUT());
			}
		}

		public string ShowLastLog(string sCount = "10")
		{
			if (int.TryParse(sCount, out int count) == false)
			{
				return "숫자만 입력!";
			}

			if (count < 0)
			{
				return "0개 이하 입력하면 뻗을줄 알았음?";
			}

			lock (this)
			{
				List<csDiscordVoiceChannelLogData> logs = new List<csDiscordVoiceChannelLogData>();

				string[] logFiles = Directory.GetFiles(Path.GetDirectoryName(logPath));

				for (int i = logFiles.Length - 1; i >= 0; i--)
				{
					string[] logSplits = File.ReadAllLines(logFiles[i]);

					for (int j = logSplits.Length - 1; j >= 0; j--)
					{
						if (logs.Count >= count) break;

						csDiscordVoiceChannelLogData? data = csDiscordVoiceChannelLogData.parse(logSplits[j]);

						if (data != null) logs.Add(data);
					}

					if (logs.Count >= count) break;
				}

				StringBuilder sb = new StringBuilder();
				sb.AppendLine();
				sb.AppendLine("=========== 음성 채팅 로그 ===========");

				for (int i = 0; i < (logs.Count < count ? logs.Count : count) ; i++)
				{
					sb.AppendLine(logs[i].ToString());
				}

				sb.AppendLine("================= 끝 =================");

				return sb.ToString();
			}
		}

		public class csDiscordVoiceChannelLogData
        {
            public DateTime EventTime = DateTime.Now;
            public string EventUser = "";
            public EDiscordVoiceChannelLog EventType = EDiscordVoiceChannelLog.입장;
            public string BChannelName = "";
            public string AChannelName = "";
			public string UID = "";

			public static csDiscordVoiceChannelLogData? parse(string log)
			{
				csDiscordVoiceChannelLogData data = new csDiscordVoiceChannelLogData();

				string[] splits = log.Split(splitChar);

				if (splits.Length >= 6)
				{
					// Check
					// Check - EventTime
					if (long.TryParse(splits[0], out long _EventTime) == false)
					{
						return null;
					}
					// Check - EventType
					if (int.TryParse(splits[2], out int _EventType) == false)
					{
						return null;
					}

					// EventTime
					data.EventTime = new DateTime(_EventTime);

					// EventUser
					data.EventUser = splits[1];

					// EventType
					data.EventType = (EDiscordVoiceChannelLog)_EventType;
					
					// BChannelName
					data.BChannelName = splits[3];

					// AChannelName
					data.AChannelName = splits[4];

					// UID
					data.UID = splits[5];

					return data;
				}

				return null;
			}

			static char splitChar = '\t';

			public string OUT()
			{
				StringBuilder sb = new StringBuilder();

				sb.Append(EventTime.Ticks).Append(splitChar);
				sb.Append(EventUser).Append(splitChar);
				sb.Append((int)EventType).Append(splitChar);
				sb.Append(BChannelName).Append(splitChar);
				sb.Append(AChannelName).Append(splitChar);
				sb.Append(UID);
				sb.AppendLine();

				return sb.ToString();
			}

			public override string ToString()
			{
                string result = string.Empty;
                switch(EventType)
                {
                    case EDiscordVoiceChannelLog.입장:
                        result = string.Format("[{0}] {1}({3})님이 {2}에 입장", EventTime.ToString("HH:mm:ss:fff"), EventUser, AChannelName, UID);
						break;
					case EDiscordVoiceChannelLog.퇴장:
						result = string.Format("[{0}] {1}({3})님이 {2}에서 퇴장", EventTime.ToString("HH:mm:ss:fff"), EventUser, BChannelName, UID);
						break;
					case EDiscordVoiceChannelLog.이동:
						result = string.Format("[{0}] {1}({4})님이 {2}에서 {3}으로 이동", EventTime.ToString("HH:mm:ss:fff"), EventUser, BChannelName, AChannelName, UID);
						break;
                }
                return result;
			}
		}

		public enum EDiscordVoiceChannelLog
        {
            입장 = 0,
            퇴장 = 1,
            이동 = 2,
        }
	}
}
