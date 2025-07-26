using Young.Setting;

namespace Console_Program_Control.Service.AI
{
	[Serializable]
	public class csAutoResponse
	{
		private static csAutoResponse instance;
		public static csAutoResponse GetInstance()
		{
			return instance ??= new csAutoResponse();
		}

		private csAutoResponse()
		{
			Load();
		}
		~csAutoResponse()
		{
			Save();
		}

		public void Load()
		{
			(enable ??= csAutoResponseEnable.GetInstance()).Load();
			(disable ??= csAutoResponseDisable.GetInstance()).Load();
			(needCheck ??= csAutoResponseNeedCheck.GetInstance()).Load();

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

			for (int i = 0; i < needCheck.datas.Count; i++)
			{
				if (string.IsNullOrEmpty(needCheck.datas[i].output) == false)
				{
					enable.datas.Add((csAutoResponseData)needCheck.datas[i].Copy());
					needCheck.datas.RemoveAt(i);
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
			(needCheck ??= csAutoResponseNeedCheck.GetInstance()).Save();
		}

		public bool isActive = true;
		public csAutoResponseEnable enable;
		public csAutoResponseDisable disable;
		public csAutoResponseNeedCheck needCheck;

		[Serializable]
		public class csAutoResponseData : csAutoSaveLoad
		{
			public string input { get; set; } = "";
			public string output { get; set; } = "";

			public List<csAutoResponseReason> reasons { get; set; }

			[Serializable]
			public class csAutoResponseReason : csAutoSaveLoad
			{
				public eAutoResponseSituationType type { get; set; } = eAutoResponseSituationType.None;

				public string reason { get; set; } = string.Empty;
			}
		}

		public enum eAutoResponseSituationType
		{
			None = 0,
			Hi = 1,
			Bye = 2,
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
				IsNeedBackup = false;
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
				IsNeedBackup = false;
				Load();
			}
			~csAutoResponseDisable()
			{
				Save();
			}

			public List<csAutoResponseData> datas { get; set; } = new List<csAutoResponseData>();
		}

		[Serializable]
		public class csAutoResponseNeedCheck : csAutoSaveLoad
		{
			private static csAutoResponseNeedCheck instance;
			public static csAutoResponseNeedCheck GetInstance()
			{
				if (instance == null) instance = new csAutoResponseNeedCheck();
				return instance;
			}
			private csAutoResponseNeedCheck()
			{
				AutoSavePath = "Data\\AI\\NeedCheckData.xml";
				IsNeedBackup = false;
				Load();
			}
			~csAutoResponseNeedCheck()
			{
				Save();
			}

			public List<csAutoResponseData> datas { get; set; } = new List<csAutoResponseData>();
		}

		public static object lockList = new object();

		public bool tryGetOutPut(string input, out string output)
		{
			output = string.Empty;

			lock (lockList)
			{
				foreach (csAutoResponseData data in enable.datas)
				{
					if (data.input.ToUpper().Equals(input))
					{
						output = data.output;
						return true;
					}
				}
				foreach (csAutoResponseData data in disable.datas)
				{
					if (data.input.ToUpper().Equals(input))
					{
						csDiscord discord = csDiscord.GetInstance();

						if (discord.lastVoiceInTime >= DateTime.Now || discord.lastVoiceOutTime >= DateTime.Now)
						{
							disable.datas.Remove(data);
							return false;
						}
						else
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public bool tryAddInput(eAutoResponseSituationType type, string reason, string input, out string errorMsg)
		{
			errorMsg = string.Empty;
			lock (lockList)
			{
				try
				{
					if (type == eAutoResponseSituationType.None)
					{
						disable.datas.Add(new csAutoResponseData() { input = input.ToUpper() });
						disable.Save();
					}
					else
					{
						bool isActive = false;

						foreach (csAutoResponseData data in needCheck.datas)
						{
							if (data.input.ToUpper() == input.ToUpper())
							{
								isActive = true;
								data.reasons.Add(new csAutoResponseData.csAutoResponseReason() { type = type, reason = reason });
							}
						}

						if (isActive == false)
						{
							needCheck.datas.Add(new csAutoResponseData() { input = input.ToUpper() });
							needCheck.datas[needCheck.datas.Count - 1].reasons.Add(new csAutoResponseData.csAutoResponseReason() { type = type, reason = reason });
						}

						needCheck.Save();
					}
					return true;
				}
				catch (Exception e)
				{
					errorMsg = e.Message;
					return false;
				}
			}
		}

		public bool tryCheckLoad(out int use, out int total)
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
