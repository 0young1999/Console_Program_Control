using System.Text;

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

        public string ActiveRPS(string requestActionCommand)
        {
            lock (LockRPS)
            {
				RPSAction requestAction = RPSAction.ErrorAction;

				switch (requestActionCommand)
				{
					case "가위":
					case "찌":
						requestAction = RPSAction.가위;
						break;
					case "바위":
					case "묵":
						requestAction = RPSAction.바위;
						break;
					case "보":
					case "빠":
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

					sb.Append(WOL ? "내가 이겼다!" : "내가 졌다!");
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
