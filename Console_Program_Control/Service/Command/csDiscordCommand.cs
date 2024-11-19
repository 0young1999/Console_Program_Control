using Console_Program_Control.Data;
using Console_Program_Control.HIDE.CODE;
using Console_Program_Control.MiniGame;
using Discord;
using Discord.Interactions;
using System.Text;

namespace Console_Program_Control.Service.Command
{
	public class csDiscordCommand : InteractionModuleBase<SocketInteractionContext>
	{
		private static string LastRequestServerStateButtonIndex = "";

		[SlashCommand("서버상태", "관리 중인 게임서버의 상태를 보고합니다.")]
		public async Task AsyncState()
		{
			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			StringBuilder sb = new StringBuilder();

			sb.Append("선택된 게임 서버는 ");
			sb.Append(_ctc.getTarget().Title);
			sb.Append("이고 현재 ");

			if (control.isAlive())
			{
				if (control.getLongActiveTime(out string runTime))
				{
					sb.Append(runTime).Append("째 가동 상태.");
				}
				else
				{
					sb.Append("가동 상태.");
				}
			}
			else
			{
				sb.Append("비 가동 상태.");
			}

			LastRequestServerStateButtonIndex = DateTime.Now.Ticks.ToString();
			// 버튼 생성
			var button = new ComponentBuilder()
				.WithButton("시작", $"Console_Server_Start_{LastRequestServerStateButtonIndex.ToString()}", ButtonStyle.Secondary)
				.WithButton("종료", $"Console_Server_Stop_{LastRequestServerStateButtonIndex.ToString()}", ButtonStyle.Secondary)
				.WithButton("강제 종료", $"Console_Server_Kill_{LastRequestServerStateButtonIndex.ToString()}", ButtonStyle.Danger);

			// 버튼이 포함된 메시지 응답
			await RespondAsync(sb.ToString(), components: button.Build());
		}

		[ComponentInteraction("Console_Server_Start_*")]
		public async Task Button_Console_Server_Start(string dynamicData)
		{
			if (dynamicData != LastRequestServerStateButtonIndex)
			{
				await RespondAsync("해당 응답은 만료되었어 다시 명령을 내려줘");
				return;
			}
			LastRequestServerStateButtonIndex = string.Empty;

			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			if (control.isAlive())
			{
				await RespondAsync("서버가 이미 시작 되어있서");
				return;
			}
			control.Start();
			if (control.isAlive())
			{
				await RespondAsync("서버가 시작 중이야");
				return;
			}
			else
			{
				await RespondAsync("서버가 시작을 할수 없서");
				return;
			}
		}

		private bool isActiveDiscordGameClose = false;

		[ComponentInteraction("Console_Server_Stop_*")]
		public async Task Button_Console_Server_Stop(string dynamicData)
		{
			if (dynamicData != LastRequestServerStateButtonIndex)
			{
				await RespondAsync("해당 응답은 만료되었어 다시 명령을 내려줘");
				return;
			}
			LastRequestServerStateButtonIndex = string.Empty;

			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			StringBuilder sb = new StringBuilder();

			if (control.isAlive())
			{
				sb.AppendLine("서버 정지를 시작할거야");
				await RespondAsync(sb.ToString());
			}
			else
			{
				await RespondAsync("서버가 이미 꺼져있는 걸");
				return;
			}

			if (isActiveDiscordGameClose)
			{
				sb.AppendLine("서버 종료가 진행 중이야!");
				await ModifyOriginalResponseAsync(msg =>
				{
					msg.Content = sb.ToString();
				});
				return;
			}

			isActiveDiscordGameClose = true;

			if (control.isAlive())
			{
				if (_ctc.getTarget().KillCommand.Count > 0)
				{
					for (int i = 0; i < _ctc.getTarget().KillCommand.Count; i++)
					{
						control.process_WriteMSG(_ctc.getTarget().KillCommand[i]);

						sb.AppendLine(string.Format("게임 서버에 명령 전송 중({1}/{2}) : {0}", _ctc.getTarget().KillCommand[i], i + 1, _ctc.getTarget().KillCommand.Count));
						await ModifyOriginalResponseAsync(msg =>
						{
							msg.Content = sb.ToString();
						});

						control.WaitKillDelay();
					}
				}
				else
				{
					control.Kill();
				}
			}


			if (control.isAlive() == false)
			{
				sb.AppendLine("서버가 종료 됬어");
			}
			else
			{
				sb.AppendLine("어라 안 꺼지내? 꺼지는게 늦는걸수도 있으니까 기다려봐");
			}
			await ModifyOriginalResponseAsync(msg =>
			{
				msg.Content = sb.ToString();
			});

			isActiveDiscordGameClose = true;
		}

		[ComponentInteraction("Console_Server_Kill_*")]
		public async Task Button_Console_Server_Kill(string dynamicData)
		{
			if (dynamicData != LastRequestServerStateButtonIndex)
			{
				await RespondAsync("해당 응답은 만료되었어 다시 명령을 내려줘");
				return;
			}
			LastRequestServerStateButtonIndex = string.Empty;

			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			string response = string.Empty;

			if (_ctc.getTarget().NotUseKill == false)
			{
				response = "강제종료가 허용되지 않는 서버야";
			}
			else
			{
				control.Kill();
				Thread.Sleep(_ctc.getTarget().KillDelay);
				if (control.isAlive() == false)
				{
					response = "서버를 죽였서";
				}
				else
				{
					response = "어라 안 죽네?";
				}
			}

			await RespondAsync(response);
		}

		public static string LastRequestServerChangeBoxIndex = "";
		[SlashCommand("서버목록", "관리 중인 게임서버 리스트를 출력합니다.")]
		public async Task AsyncServerList()
		{
			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			LastRequestServerChangeBoxIndex = DateTime.Now.Ticks.ToString();

			StringBuilder sb = new StringBuilder();
			List<SelectMenuOptionBuilder> lSMB = new List<SelectMenuOptionBuilder>();
			sb.AppendLine("============== 서버 목록 =============");
			for (int i = 0; i < _ctc.consoles.Count; i++)
			{
				string label = string.Format("{0:D2} : {1}", i + 1, _ctc.consoles[i].Title);

				sb.AppendLine(label);

				lSMB.Add(new SelectMenuOptionBuilder()
					.WithLabel(label)
					.WithValue(string.Format("{0}|{1}", i.ToString(), LastRequestServerChangeBoxIndex)));
			}

			sb.AppendLine();
			sb.AppendLine("============== 서버 선택 =============");

			var components = new ComponentBuilder().WithSelectMenu("Server_Select_Menu", lSMB).Build();

			await RespondAsync(sb.ToString(), components: components);
		}

		[SlashCommand("서버접속방법", "관리 중인 게임서버의 접속 방법을 출력합니다.")]
		public async Task AsyncServerAccess()
		{
			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			StringBuilder sb = new StringBuilder();

			sb.AppendLine("============== 접속 방법 =============");

			for (int i = 0; i < _ctc.getTarget().AccessData.Count; i++)
			{
				sb.Append(string.Format("{0:D2}", i + 1)).Append(" : ").AppendLine(_ctc.getTarget().AccessData[i]);
			}

			sb.AppendLine("================= 끝 =================");

			await RespondAsync(sb.ToString());
		}

		[SlashCommand("서버명령전송", "관리 중인 게임서버에 명령어를 전달합니다.")]
		public async Task AsyncServerCommandSend([Summary("명령어", "게임 서버에 전달될 명령어")] string command)
		{
			string response = string.Empty;
			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();

			if (Context.User.Id != csPrivateCode.DiscordAdminID)
			{
				response = "본인에게 그런 권한이 있을 꺼라 생각한건가?";
			}
			else
			{
				bool isDone = control.process_WriteMSG(command);
				response = string.Format("명령 전송 {1} : {0}",
					command, isDone ? "성공" : "실패");
			}

			await RespondAsync(response);
		}

		private static Dictionary<ulong, string> LastRequestRPSIndex = new Dictionary<ulong, string>();
		[SlashCommand("가위바위보", "봇이랑 하는 가위바위보")]
		public async Task AsyncRPS()
		{
			if (LastRequestRPSIndex.TryGetValue(Context.User.Id, out _))
			{
				LastRequestRPSIndex.Remove(Context.User.Id);
			}

			string index = DateTime.Now.Ticks.ToString();
			LastRequestRPSIndex.Add(Context.User.Id, index);

			StringBuilder sb = new StringBuilder();
			sb.Append("가위바위보를 합시다~");

			// 버튼 생성
			var button = new ComponentBuilder()
				.WithButton("가위", $"RPS_S_{index}", ButtonStyle.Secondary)
				.WithButton("바위", $"RPS_R_{index}", ButtonStyle.Secondary)
				.WithButton("보", $"RPS_P_{index}", ButtonStyle.Secondary);

			// 버튼이 포함된 메시지 응답
			await RespondAsync(sb.ToString(), components: button.Build());
		}

		[ComponentInteraction("RPS_*")]
		public async Task Button_RPS(string dynamicData)
		{
			string[] splits = dynamicData.Split('_');

			string nickName = Context.Guild.GetUser(Context.User.Id)?.Nickname;

			if (LastRequestRPSIndex.TryGetValue(Context.User.Id, out string index) == false ||
				index != splits[1])
			{
				await RespondAsync(string.Format("{0} 당신 게임이 아닐 건데?", nickName));
				return;
			}

			LastRequestRPSIndex.Remove(Context.User.Id);

			await RespondAsync(csRPS.GetInstance().ActiveRPS(nickName, splits[0]));
		}
	}
}