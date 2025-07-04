using System.ComponentModel;
using Young.Setting;

namespace Console_Program_Control.Data
{
	[Serializable]
	public class csConsoleTarget : csAutoSaveLoad
	{
		public csConsoleTarget() { }

		[DisplayName("이 컨트롤의 이름 입니다.")]
		[DefaultValue("")]
		[Category("기본")]
		public string Title { get; set; }

		[DisplayName("타겟 프로그램 경로")]
		[String(StringAttributeEnum.File)]
		[Category("기본")]
		[DefaultValue("")]
		public string ProgramPath { get; set; }

		[DisplayName("시작 옵션")]
		[Category("기본")]
		[DefaultValue("")]
		public string StartOption { get; set; }

		[DisplayName("게임 타입")]
		[Category("기본")]
		public GameType GameType { get; set; }

		[DisplayName("서버 접속 방법")]
		[Category("기본")]
		public List<string> AccessData { get; set; } = new List<string>();

		[DisplayName("강제종료 사용")]
		[Description("강제종료를 허용할것인지 설정합니다.")]
		[Category("종료")]
		[DefaultValue(true)]
		public bool NotUseKill { get; set; }

		[DisplayName("타겟 프로그램 종료시 딜레이(ms)")]
		[Description("종료전 딜레이될 시간입니다, 명령간 딜레이 시간으로도 사용됩니다.")]
		[Category("종료")]
		[DefaultValue(10_000)]
		public int KillDelay { get; set; }

		[DisplayName("타겟 프로그램 종료 명령")]
		[Description("해당 항목이 존재할 경우 프로세스 강제 종료가 아닌 아래 명령을 입력함으로서 종료 합니다.")]
		[Category("종료")]
		public List<string> KillCommand { get; set; }

		[DisplayName("종료 명령시 같이 죽을 프로세스 이름")]
		[Category("종료")]
		public List<string> KillTogether { get; set; }
	}

	public enum GameType
	{
		None = 0,
		Minecraft = 1,
		_7DaysToDie = 2,
	}
}
