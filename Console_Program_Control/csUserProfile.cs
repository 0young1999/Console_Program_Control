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
					data.l4d2pd.ResetToDayCount();
				}
			}
		}
	}

	[Serializable]
	public class csUserProfileData : csAutoSaveLoad
	{
		public ulong uid { get; set; }
		public string nick { get; set; }
		public string MinecraftName { get; set; }
		public string SteamID { get; set; }
		public string SteamID64 { get; set; }
		public csLeft4Dead2PluginsData l4d2pd = new csLeft4Dead2PluginsData();
	}

	public class csLeft4Dead2PluginsData
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
}
