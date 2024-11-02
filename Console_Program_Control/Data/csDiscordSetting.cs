using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Young.Setting;

namespace Console_Program_Control.Data
{
	public class csDiscordSetting : csAutoSaveLoad
    {
        private static csDiscordSetting instance;
        public static csDiscordSetting GetInstance()
        {
            if (instance == null) instance = new csDiscordSetting();
            return instance;
        }
        private csDiscordSetting() { Load(); }

        [DisplayName("명령어 식별자")]
        [Description("맨앞에 해당 문자열이 있어야 명령어로 인식합니다.")]
        [DefaultValue("!")]
        public string CommandSTX { get; set; }
    }
}
