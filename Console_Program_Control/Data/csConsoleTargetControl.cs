using Console_Program_Control.HIDE.Setting.CustomSetting;
using System.ComponentModel;
using Young.Setting;
using static Young.Setting.SettingForm;

namespace Console_Program_Control.Data
{
	[Serializable]
	public class csConsoleTargetControl : csAutoSaveLoad
	{
		private static csConsoleTargetControl instance;
		public static csConsoleTargetControl GetInstance()
		{
			if (instance == null) instance = new csConsoleTargetControl();
			return instance;
		}
		public csConsoleTargetControl()
		{
			Load();
		}

		public ISettingPanel GetSettingPanel()
		{
			return new ctlConsoleTarget();
		}

		public csConsoleTarget getTarget()
		{
			return consoles[Selected];
		}

		[DefaultValue(-1)]
		public int Selected { get; set; } = -1;

		public List<csConsoleTarget> consoles { get; set; } = new List<csConsoleTarget>();
	}
}
