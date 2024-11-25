using Young.Setting;

namespace Console_Program_Control.Service.AI
{
	[Serializable]
	public class csAutoResponse : csAutoSaveLoad
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
		}
		~csAutoResponse()
		{
			Save();
		}

		public List<csAutoResponseData> datas { get; set; } = new List<csAutoResponseData>();

		[Serializable]
		public class csAutoResponseData : csAutoSaveLoad
		{
			public string input { get; set; } = "";
			public string output { get; set; } = "";
		}

		public static object lockList = new object();

		public bool getOutPut(string input,out string output)
		{
			output = string.Empty;

			lock (lockList)
			{
				foreach (csAutoResponseData data in datas)
				{
					if (data.input.Equals(input))
					{
						output = data.output;
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
					datas.Add(new csAutoResponseData() { input = input });
					Save();
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
					total = datas.Count;
					use = datas.Count(item => !string.IsNullOrEmpty(item.output));
					return true;
				}
				catch { }
			}
			return false;
		}
	}
}
