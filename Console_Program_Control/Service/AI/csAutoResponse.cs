using Young.Setting;

namespace Console_Program_Control.Service.AI
{
	[Serializable]
	public class csAutoResponse
	{
		private static csAutoResponse instance;
		public static csAutoResponse GetInstance()
		{
			if (instance == null) instance = new csAutoResponse();
			return instance;
		}

		private csAutoResponse()
		{
			Load();
			enable = csAutoResponseEnable.GetInstance();
			disable = csAutoResponseDisable.GetInstance();
		}
		~csAutoResponse()
		{
			Save();
		}

		public void Load()
		{
			(enable ??= csAutoResponseEnable.GetInstance()).Load();
			(disable ??= csAutoResponseDisable.GetInstance()).Load();
			
			for (int i = 0; i < enable.datas.Count; i++)
			{
				if (string.IsNullOrEmpty(enable.datas[i].output))
				{
					disable.datas.Add((csAutoResponseData)enable.datas[i].Copy());
					enable.datas.RemoveAt(i);
					i--;
					continue;
				}
			}

			for (int i = 0; i < disable.datas.Count; i++)
			{
				if (string.IsNullOrEmpty(disable.datas[i].output) == false)
				{
					enable.datas.Add((csAutoResponseData)disable.datas[i].Copy());
					disable.datas.RemoveAt(i);
					i--;
					continue;
				}
			}

			Save();
		}

		public void Save()
		{
			(enable ??= csAutoResponseEnable.GetInstance()).Save();
			(disable ??= csAutoResponseDisable.GetInstance()).Save();
		}

		public bool isActive = true;
		public csAutoResponseEnable enable;
		public csAutoResponseDisable disable;

		[Serializable]
		public class csAutoResponseData : csAutoSaveLoad
		{
			public string input { get; set; } = "";
			public string output { get; set; } = "";
		}

		[Serializable]
		public class csAutoResponseEnable : csAutoSaveLoad
		{
			private static csAutoResponseEnable instance;
			public static csAutoResponseEnable GetInstance()
			{
				if (instance == null) instance = new csAutoResponseEnable();
				return instance;
			}
			private csAutoResponseEnable()
			{
				AutoSavePath = "Data\\AI\\EnableData.xml";
				Load();
			}
			~csAutoResponseEnable()
			{
				Save();
			}

			public List<csAutoResponseData> datas { get; set; } = new List<csAutoResponseData>();
		}

		[Serializable]
		public class csAutoResponseDisable : csAutoSaveLoad
		{
			private static csAutoResponseDisable instance;
			public static csAutoResponseDisable GetInstance()
			{
				if (instance == null) instance = new csAutoResponseDisable();
				return instance;
			}
			private csAutoResponseDisable()
			{
				AutoSavePath = "Data\\AI\\DisableData.xml";
				Load();
			}
			~csAutoResponseDisable()
			{
				Save();
			}

			public List<csAutoResponseData> datas { get; set; } = new List<csAutoResponseData>();
		}

		public static object lockList = new object();

		public bool getOutPut(string input, out string output)
		{
			output = string.Empty;

			lock (lockList)
			{
				foreach (csAutoResponseData data in enable.datas)
				{
					if (data.input.Equals(input))
					{
						output = data.output;
						return true;
					}
				}
				foreach (csAutoResponseData data in disable.datas)
				{
					if (data.input.Equals(input))
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool addInput(string input, out string errorMsg)
		{
			errorMsg = string.Empty;
			lock (lockList)
			{
				try
				{
					disable.datas.Add(new csAutoResponseData() { input = input });
					disable.Save();
					return true;
				}
				catch (Exception e)
				{
					errorMsg = e.Message;
					return false;
				}
			}
		}

		public bool checkLoad(out int use, out int total)
		{
			use = 0;
			total = 0;
			lock (lockList)
			{
				try
				{
					use = enable.datas.Count;
					total = use + disable.datas.Count;
					return true;
				}
				catch { }
			}
			return false;
		}
	}
}
