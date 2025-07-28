using Discord.WebSocket;
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

		public class csDiscordVoiceChannelLogData
        {
            public DateTime EventTime = DateTime.Now;
            public string EventUser = "";
            public EDiscordVoiceChannelLog EventType = EDiscordVoiceChannelLog.입장;
            public SocketVoiceState? BChannel;
            public SocketVoiceState? AChannel;
			public string UID = "";

			static char splitChar = '\t';

			public string OUT()
			{
				StringBuilder sb = new StringBuilder();

				sb.Append(EventTime.Ticks).Append(splitChar);
				sb.Append(EventUser).Append(splitChar);
				sb.Append(EventType).Append(splitChar);
				sb.Append(BChannel?.VoiceChannel?.Name ?? "null").Append(splitChar);
				sb.Append(AChannel?.VoiceChannel?.Name ?? "null").Append(splitChar);
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
                        result = string.Format("[{0}] {1}({3})님이 {2}에 입장", EventTime.ToString("HH:mm:ss:fff"), EventUser, AChannel, UID);
						break;
					case EDiscordVoiceChannelLog.퇴장:
						result = string.Format("[{0}] {1}({3})님이 {2}에서 퇴장", EventTime.ToString("HH:mm:ss:fff"), EventUser, BChannel, UID);
						break;
					case EDiscordVoiceChannelLog.이동:
						result = string.Format("[{0}] {1}({4})님이 {2}에서 {3}으로 이동", EventTime.ToString("HH:mm:ss:fff"), EventUser, BChannel, AChannel, UID);
						break;
					case EDiscordVoiceChannelLog.상태변경:
						result = $"[{EventTime.ToString("HH:mm:ss:fff")}] {EventUser}({UID}) 상태 변경됨";
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
			상태변경 = 3,
        }
	}
}
