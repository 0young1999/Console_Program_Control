using Console_Program_Control.Data;
using Console_Program_Control.HIDE.CODE;
using Console_Program_Control.MiniGame;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;
using System.Threading.Channels;
using static Console_Program_Control.Data.csDiscordVoiceChannelLog;

namespace Console_Program_Control.Service
{
	public class csDiscord
	{
		private static csDiscord instance;
		public static csDiscord GetInstance()
		{
			if (instance == null) instance = new csDiscord();
			return instance;
		}
		private csDiscord()
		{
			Thread thread = new Thread(Run);
			thread.IsBackground = true;
			thread.Start();
		}

		DiscordSocketClient client;
		CommandService commands;
		SocketCommandContext commandContext;

		private csDiscordSetting setting = csDiscordSetting.GetInstance();

		private void Run()
		{
			BotMain().GetAwaiter().GetResult();   //봇의 진입점 실행
		}

		public async Task BotMain()
		{
			//client = new DiscordSocketClient(new DiscordSocketConfig()
			//{
			//	LogLevel = LogSeverity.Verbose,                              //봇의 로그 레벨 설정 
			//	GatewayIntents = Discord.GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
			//});
			client = new DiscordSocketClient(new DiscordSocketConfig()
			{
				LogLevel = LogSeverity.Info,                              //봇의 로그 레벨 설정 
				GatewayIntents = GatewayIntents.Guilds | 
								 GatewayIntents.GuildVoiceStates |
								 GatewayIntents.GuildMessages |
								 GatewayIntents.AllUnprivileged | 
								 GatewayIntents.MessageContent |
								 GatewayIntents.DirectMessages
			});
			commands = new CommandService(new CommandServiceConfig()        //명령어 수신 클라이언트 초기화
			{
				LogLevel = LogSeverity.Verbose,                              //봇의 로그 레벨 설정
			});

			//로그 수신 시 로그 출력 함수에서 출력되도록 설정
			client.Log += OnClientLogReceived;
			client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
			commands.Log += OnClientLogReceived;

			await client.LoginAsync(TokenType.Bot, csPrivateCode.DiscordToken); //봇의 토큰을 사용해 서버에 로그인
			await client.StartAsync();                         //봇이 이벤트를 수신하기 시작

			client.MessageReceived += OnClientMessage;         //봇이 메시지를 수신할 때 처리하도록 설정

			await Task.Delay(-1);   //봇이 종료되지 않도록 블로킹
		}

		private async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
		{
			csDiscordVoiceChannelLog log = csDiscordVoiceChannelLog.GetInstance();
			csDiscordVoiceChannelLogData data = new csDiscordVoiceChannelLogData();
			data.EventTime = DateTime.Now;
			data.EventUser = (user as SocketGuildUser).Nickname;
			data.BChannelName = before.VoiceChannel?.Name ?? "";
			data.AChannelName = after.VoiceChannel?.Name ?? "";

			// 유저가 채널에 들어갈 때
			if (before.VoiceChannel == null && after.VoiceChannel != null)
			{
				//Console.WriteLine($"{user.Username} has joined voice channel: {after.VoiceChannel.Name}");

				data.EventType = EDiscordVoiceChannelLog.입장;
			}
			// 유저가 채널에서 나갈 때
			else if (before.VoiceChannel != null && after.VoiceChannel == null)
			{
				//Console.WriteLine($"{user.Username} has left voice channel: {before.VoiceChannel.Name}");

				data.EventType = EDiscordVoiceChannelLog.퇴장;
			}
			// 유저가 다른 보이스 채널로 이동할 때
			else if (before.VoiceChannel != after.VoiceChannel)
			{
				//Console.WriteLine($"{user.Username} moved from {before.VoiceChannel.Name} to {after.VoiceChannel.Name}");

				data.EventType = EDiscordVoiceChannelLog.이동;
			}

			FormMain.GetInstance().MainLogAppend(true, data.ToString());
			log.AppendLog(data);
		}

		private async Task OnClientMessage(SocketMessage arg)
		{
			//수신한 메시지가 사용자가 보낸 게 아닐 때 취소
			var message = arg as SocketUserMessage;
			if (message == null) return;

			// 봇 채팅 방어
			if (message.Author.IsBot)
				return;

			string msg = arg.Content;
			if (msg.IndexOf(setting.CommandSTX) == 0)
			{
				string SenderName = (message.Author as SocketGuildUser).Nickname;

				FormMain.GetInstance().MainLogAppend(true, string.Format("{0} : {1}", SenderName, msg));
				string response = string.Empty;
				csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
				csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

				commandContext = new SocketCommandContext(client, message);
				var dmChannel = await arg.Author.CreateDMChannelAsync();

				msg = msg.Remove(0, setting.CommandSTX.Length).Trim();

				string[] commandSplit = msg.Split(' ');

				if (FormMain.GetInstance().isSettingFormShow)
				{
					response = "설정이 진행중이라 명령을 받을 수 없서.";
					_ = commandContext.Channel.SendMessageAsync(string.Format("{0}", response));
					FormMain.GetInstance().MainLogAppend(false, response);
					return;
				}

				bool isResponseDM = false;

				switch (commandSplit[0].ToLower())
				{
					case "상태":
					case "state":
						{
							response = string.Format("선택된 게임 서버는 {0}이고 현재 {1} 상태.",
										_ctc.getTarget().Title,
										control.isAlive() ? "가동" : "비 가동");
						}
						break;
					case "서버목록":
					case "목록":
					case "serverlist":
					case "list":
						{
							StringBuilder sb = new StringBuilder();
							sb.AppendLine("============== 서버 목록 =============");
							for (int i = 0; i < _ctc.consoles.Count; i++)
							{
								sb.AppendLine(string.Format("{0:D2} : {1}", i + 1, _ctc.consoles[i].Title));
							}
							response = sb.ToString();
						}
						break;
					case "명령어":
					case "help":
						response = GetHelp();
						break;
					case "시작":
					case "start":
						if (control.isAlive())
						{
							response = "서버가 이미 시작 되어있서";
							break;
						}
						control.Start();
						if (control.isAlive())
						{
							response = "서버가 시작 중이야";
						}
						else
						{
							response = "서버가 시작을 할수 없서";
						}
						break;
					case "정지":
					case "stop":
					case "종료":
					case "close":
						if (control.isAlive())
						{
							response = "서버 정지를 시작할거야";
						}
						else
						{
							response = "서버가 이미 꺼져있는 걸";
							break;
						}

						control.Close(commandContext);

						break;
					case "강제종료":
					case "kill":
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
						break;
					case "서버명령":
					case "명령":
					case "servercommand":
					case "command":
						{
							response = ServerCommand(message, commandSplit.ToList());
						}
						break;
					case "서버접속방법":
					case "접속방법":
						{
							StringBuilder sb = new StringBuilder();

							sb.AppendLine("============== 접속 방법 =============");

							for (int i = 0; i < _ctc.getTarget().AccessData.Count; i++)
							{
								sb.Append(i + 1).Append(" : ").AppendLine(_ctc.getTarget().AccessData[i]);
							}

							sb.AppendLine("================= 끝 =================");

							response = sb.ToString();
						}
						break;
					case "섹스":
					case "sex":
						{
							response = "님 혹시 기계 박이?";
						}
						break;
					case "서버변경":
					case "변경":
					case "serverchange":
					case "change":
						{
							if (control.isAlive())
							{
								response = "서버가 가동중이라 변경이 불가능해";
							}
							else
							{
								if (int.TryParse(commandSplit[1], out int serverIndex))
								{
									if (serverIndex > _ctc.consoles.Count + 1 || serverIndex <= 0)
									{
										response = "해당 번호를 가지고 있는 서버는 없서";
									}
									else
									{
										_ctc.Selected = serverIndex - 1;
										response = string.Format("{0}번의 번호를 가진 {1}(으)로 변경 완료", serverIndex, _ctc.consoles[_ctc.Selected].Title);
									}
								}
								else
								{
									bool isChangeDone = false;
									for (int i = 0; i < _ctc.consoles.Count; i++)
									{
										if (_ctc.consoles[i].Title.Equals(commandSplit[1]))
										{
											isChangeDone = true;
											_ctc.Selected = i;
											break;
										}
									}

									if (isChangeDone)
									{
										response = string.Format("{0}(으)로 변경 완료", _ctc.consoles[_ctc.Selected].Title);
									}
									else
									{
										response = string.Format("{0}을(를) 찾을 수 없서", commandSplit[1]);
									}
								}
							}
							_ctc.Save();
						}
						break;
					case "가위바위보":
						{
							response = csRPS.GetInstance().ActiveRPS(commandSplit[1].Trim());
						}
						break;
					case "음성로그":
					case "voicelog":
						{
							isResponseDM = true;
							if (message.Author.Id != csPrivateCode.DiscordAdminID)
							{
								response = "본인에게 그런 권한이 있을 꺼라 생각한건가?";
							}
							else
							{
								csDiscordVoiceChannelLog log = csDiscordVoiceChannelLog.GetInstance();
								response = log.ShowLastLog(commandSplit.Length >= 1 ? commandSplit[1] : "10");
							}
						}
						break;
					default:
						response = string.Format("{0}\r\n{1}", "그게 머야???", GetHelp());
						break;
				}

				if (isResponseDM == false)
				{
					_ = message.ReplyAsync(response);
				}
				else
				{
					_ = dmChannel.SendMessageAsync(response);
				}
				//_ = commandContext.Channel.SendMessageAsync(string.Format("{0}", response));
				FormMain.GetInstance().MainLogAppend(false, response);
			}
		}

		private string GetHelp()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("============== 봇 명령어 =============");
			sb.Append(setting.CommandSTX).AppendLine("명령어(help)");
			sb.AppendLine();
			sb.AppendLine("======== 게임 서버 제어 시스템 =======");
			sb.Append(setting.CommandSTX).AppendLine("상태(state)");
			sb.Append(setting.CommandSTX).AppendLine("서버목록,목록(serverlist,list)");
			sb.Append(setting.CommandSTX).AppendLine("서버변경,변경(serverchange,change)");
			sb.Append(setting.CommandSTX).AppendLine("서버접속방법,접속방법");
			sb.Append(setting.CommandSTX).AppendLine("서버명령,명령(servercommand,command)");
			sb.AppendLine();
			sb.AppendLine("============== 게임 서버 =============");
			sb.Append(setting.CommandSTX).AppendLine("시작(start)");
			sb.Append(setting.CommandSTX).AppendLine("정지(stop)");
			sb.Append(setting.CommandSTX).AppendLine("종료(close)");
			sb.Append(setting.CommandSTX).AppendLine("강제종료(kill)");
			sb.AppendLine();
			sb.AppendLine("============== 미니 게임 =============");
			sb.Append(setting.CommandSTX).AppendLine("가위바위보 (가위/바위/보)");
			sb.AppendLine();
			sb.AppendLine("============ 디스코드 로그 ===========");
			sb.Append(setting.CommandSTX).AppendLine("음성로그(voicelog) (숫자)");
			sb.AppendLine();
			return sb.ToString();
		}

		private string ServerCommand(SocketUserMessage message, List<string> commandSplit)
		{
			string response = string.Empty;
			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();

			if (message.Author.Id != csPrivateCode.DiscordAdminID)
			{
				response = "본인에게 그런 권한이 있을 꺼라 생각한건가?";
			}
			else
			{
				commandSplit.RemoveAt(0);

				if (commandSplit.Count != 0)
				{
					bool isDone = control.process_WriteMSG(string.Join(' ', commandSplit.ToArray()));
					response = string.Format("명령 전송 {1} : {0}",
						string.Join(' ', commandSplit.ToArray()),
						isDone ? "성공" : "실패");
				}
				else
				{
					response = "명령어가 넘겨지지 않았서";
				}
			}

			return response;
		}

		/// <summary>
		/// 봇의 로그를 출력하는 함수
		/// </summary>
		/// <param name="msg">봇의 클라이언트에서 수신된 로그</param>
		/// <returns></returns>
		private Task OnClientLogReceived(LogMessage msg)
		{
			//Console.WriteLine(msg.ToString());  //로그 출력
			FormMain.GetInstance().MainLogAppend(true, string.Format("{0} : {1}", "system", msg.ToString()));
			return Task.CompletedTask;
		}
	}
}