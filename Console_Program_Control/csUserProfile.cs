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
		public void ResetL4D2KillToDayCount()
		{
			lock (LockDatas)
			{
				foreach (csUserProfileData data in datas)
				{
					data.l4d2_Kill_Count.ResetToDayCount();
				}
			}
		}
		public void SetL4D2Status(string SteamID64, int Status)
		{
			lock (LockDatas)
			{
				csUserProfileData data = datas.Find(item => item.SteamID64 == SteamID64);

				if (data != null)
				{
					data.l4d2_Position.status = (csLeft4Dead2PluginsData_Position.Status)Status;
				}
			}
		}
		public void SetL4D2Health(string SteamID64, int currentHealth, int maxHealth)
		{
			lock (LockDatas)
			{
				csUserProfileData data = datas.Find(item => item.SteamID64 == SteamID64);

				if (data != null)
				{
					data.l4d2_Position.curruntHealth = currentHealth;
					data.l4d2_Position.maxHealth = maxHealth;
				}
			}
		}
		public void SetL4D2Position(string SteamID64, int X, int Y, int Z)
		{
			lock (LockDatas)
			{
				csUserProfileData data = datas.Find(item => item.SteamID64 == SteamID64);

				if (data != null)
				{
					data.l4d2_Position.X = X;
					data.l4d2_Position.Y = Y;
					data.l4d2_Position.Z = Z;
				}
			}
		}
	}

	[Serializable]
	public class csUserProfileData : csAutoSaveLoad
	{
		public ulong uid { get; set; }
		public string nick { get; set; } = string.Empty;
		public string MinecraftName { get; set; } = string.Empty;
		public string SteamID { get; set; } = string.Empty;
		public string SteamID64 { get; set; } = string.Empty;

		public csLeft4Dead2PluginsData_Kill_Count l4d2_Kill_Count = new csLeft4Dead2PluginsData_Kill_Count();
		public csLeft4Dead2PluginsData_Position l4d2_Position = new csLeft4Dead2PluginsData_Position();
	}

	public class csLeft4Dead2PluginsData_Kill_Count
	{
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
