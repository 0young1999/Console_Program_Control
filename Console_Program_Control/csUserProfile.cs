using Young.Setting;

namespace Console_Program_Control
{
	[Serializable]
	public class csUserProfile : csAutoSaveLoad
	{
		private static csUserProfile instance;
		public static csUserProfile GetInstance()
		{
			if (instance == null) instance = new csUserProfile();
			return instance;
		}
		private csUserProfile()
		{
			Load();
		}

		public object LockDatas = new object();
		public List<csUserProfileData> datas { get; set; } = new List<csUserProfileData>();
	}

	[Serializable]
	public class csUserProfileData : csAutoSaveLoad
	{
		public ulong uid { get; set; }
		public string nick { get; set; } = string.Empty;
		public string MinecraftName { get; set; } = string.Empty;
		public string SteamID { get; set; } = string.Empty;
		public string SteamID64 { get; set; } = string.Empty;
		public csLeft4Dead2PluginsData_Position l4d2_Position = new csLeft4Dead2PluginsData_Position();
	}

	public class csLeft4Dead2PluginsData_Position
	{
		public Status status = Status.Unknown;

		public int curruntHealth = 0;
		public int maxHealth = 0;

		public int X = 0;
		public int Y = 0;
		public int Z = 0;

		public enum Status
		{
			Unknown = 0,
			Alive = 1,
			Down = 2,
			Die = 3,
		}
	}
}
