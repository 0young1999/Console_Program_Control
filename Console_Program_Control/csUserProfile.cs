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
	public class csUserProfileData
	{
		public ulong uid { get; set; }
		public string nick { get; set; }
		public string MinecraftName { get; set; }
	}
}
