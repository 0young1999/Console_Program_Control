using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Young.Setting;

namespace Console_Program_Control.Data
{
	internal class csMinecraft : csAutoSaveLoad
    {
        private static csMinecraft instance;
        public static csMinecraft GetInstance()
        {
            if (instance == null) instance = new csMinecraft();
            return instance;
        }
        private csMinecraft() { Load(); }

        [DisplayName("응답 포트 설정")]
        [Description("응답 서버 포트를 설정합니다.")]
        [DefaultValue(9000)]
        public int ResponsePort { get; set; }

        [DisplayName("요청 포트 설정")]
        [Description("요청 서버 포트를 설정합니다.")]
        public int RequestPort { get; set; }

        public bool isAlive = false;
        public bool isReseting = false;
    }
}
