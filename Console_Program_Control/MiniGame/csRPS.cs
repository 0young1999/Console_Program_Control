using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Console_Program_Control.MiniGame
{
	public class csRPS
    {
        private static csRPS instance;
        public static csRPS GetInstance()
        {
            if (instance == null) instance = new csRPS();
            return instance;
        }
        private csRPS() { }

        private static object LockRPS = new object();

        public string ActiveRPS(string nickName, string requestActionCommand)
        {
            lock (LockRPS)
            {
				RPSAction requestAction = RPSAction.ErrorAction;

				switch (requestActionCommand)
				{
					case "S":
						requestAction = RPSAction.가위;
						break;
					case "R":
						requestAction = RPSAction.바위;
						break;
					case "P":
						requestAction = RPSAction.보;
						break;
				}

				if (requestAction == RPSAction.ErrorAction)
				{
					return string.Format("{0}을(를) 이해 하지 못했서.", requestActionCommand);
				}

				RPSAction responseAction = (RPSAction)(new Random()).Next(0, 3);

				StringBuilder sb = new StringBuilder();

				sb.Append(responseAction.ToString()).AppendLine("!!");

				sb.Append(nickName).Append("의 게임에서 ");

				if (requestAction == responseAction)
				{
					sb.Append("비겼다!");
				}
				else
				{
					bool WOL = false;
					switch (requestAction)
					{
						case RPSAction.가위:
							WOL = responseAction == RPSAction.바위;
							break;
						case RPSAction.바위:
							WOL = responseAction == RPSAction.보;
							break;
						case RPSAction.보:
							WOL = responseAction == RPSAction.가위;
							break;
					}

					sb.Append(WOL ? "이겼다!" : "졌다!");
				}

				return sb.ToString();
			}
        }

        public enum RPSAction
        {
            ErrorAction = -1,
            가위 = 0,
            바위 = 1,
            보 = 2,
        }
    }
}
