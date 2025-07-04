using Console_Program_Control.Data;
using Console_Program_Control.HIDE.CODE;
using Console_Program_Control.Service.AI;
using Console_Program_Control.Service.Command;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
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
		InteractionService interactionService;
		IServiceProvider services;

		private csDiscordSetting setting = csDiscordSetting.GetInstance();
		public DateTime lastVoiceOutTime = DateTime.MinValue;
		public string lastVoiceOutName = "";
		public DateTime lastVoiceInTime = DateTime.MinValue;
		public string lastVoiceInName = "";

		private void Run()
		{
			BotMain().GetAwaiter().GetResult();   //봇의 진입점 실행
		}

		public async Task BotMain()
		{
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

			interactionService = new InteractionService(client.Rest);

			services = new ServiceCollection()
				.AddSingleton(client).AddSingleton(interactionService)
				.AddSingleton<csInteractionHandler>().BuildServiceProvider();

			//로그 수신 시 로그 출력 함수에서 출력되도록 설정
			client.Log += OnClientLogReceived;
			client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
			client.InteractionCreated += InteractionCreated;
			commands.Log += OnClientLogReceived;

			await services.GetRequiredService<csInteractionHandler>().InitializeAsync();
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
			data.EventUser = (user as SocketGuildUser).DisplayName;
			data.BChannel = before;
			data.AChannel = after;
			data.UID = (user as SocketGuildUser).Id.ToString();

			// 유저가 채널에 들어갈 때
			if (before.VoiceChannel == null && after.VoiceChannel != null)
			{
				data.EventType = EDiscordVoiceChannelLog.입장;
				lastVoiceInTime = DateTime.Now + new TimeSpan(0, 0, 30);
				lastVoiceInName = data.EventUser.ToString();
			}
			// 유저가 채널에서 나갈 때
			else if (before.VoiceChannel != null && after.VoiceChannel == null)
			{
				data.EventType = EDiscordVoiceChannelLog.퇴장;
				lastVoiceOutTime = DateTime.Now + new TimeSpan(0, 0, 30);
				lastVoiceOutName = data.EventUser.ToString();
			}
			// 유저가 다른 보이스 채널로 이동할 때
			else if (before.VoiceChannel != after.VoiceChannel)
			{
				data.EventType = EDiscordVoiceChannelLog.이동;
			}

			FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordVoiceChatLog, true, data.ToString());
			log.AppendLog(data);
		}

		// 자동 응답 최소 시간
		private DateTime lastSendAutoResponseTime = DateTime.Now;

		private async Task OnClientMessage(SocketMessage arg)
		{
			//수신한 메시지가 사용자가 보낸 게 아닐 때 취소
			SocketUserMessage message = arg as SocketUserMessage;
			if (message == null) return;

			// 봇 채팅 방어
			if (message.Author.IsBot)
				return;

			// 유저 프로필 생성 여부 검사
			csUserProfile up = csUserProfile.GetInstance();

			lock (up.LockDatas)
			{
				if (!up.datas.Any(item => item.uid == (message.Author as SocketGuildUser).Id))
				{
					up.datas.Add(new csUserProfileData()
					{
						nick = (message.Author as SocketGuildUser).DisplayName,
						uid = (message.Author as SocketGuildUser).Id
					});
					up.Save();
				}
			}

			string msg = arg.Content;
			if (msg.IndexOf(setting.CommandSTX) == 0)
			{
				string SenderName = (message.Author as SocketGuildUser).DisplayName;
				string SenderUID = (message.Author as SocketGuildUser).Id.ToString();

				FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordCustomCommand, true, string.Format("{0}({2}) : {1}", SenderName, msg, SenderUID));
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
					FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordCustomCommand, false, response);
					return;
				}

				bool isResponseDM = false;

				switch (commandSplit[0].ToLower())
				{
					case "섹스":
					case "sex":
						{
							response = "님 혹시 기계 박이?";
						}
						break;
					case "유저등록":
						{
							if (csPrivateCode.DiscordAdminID.ToString() != SenderUID)
							{
								response = "지능 미달";
								break;
							}

							if (ulong.TryParse(commandSplit[1], out ulong targetUid) == false)
							{
								response = "UID 파싱 실패";
								break;
							}

							bool isProccessDone = false;

							lock (up.LockDatas)
							{
								for (int i = 0; i < up.datas.Count; i++)
								{
									if (up.datas[i].uid == targetUid)
									{
										up.datas[i].MinecraftName = commandSplit[2];
										isProccessDone = true;
									}
								}
							}

							response = isProccessDone ? "등록 성공" : "등록 실패\n해당 유저 데이터를 찾을수 없서";

							if (isProccessDone) up.Save();
						}
						break;
					default:
						return;
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
				FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordCustomCommand, false, response);
			}
			else
			{
				// 자동 응답
				csAutoResponse auto = csAutoResponse.GetInstance();
				if (string.IsNullOrEmpty(msg) || auto.isActive == false) return;

				msg = msg.Trim().ToUpper();

				if (auto.tryGetOutPut(msg, out string outPut))
				{
					if (string.IsNullOrEmpty(outPut))
					{
						return;
					}
					else
					{
						if (DateTime.Now - lastSendAutoResponseTime < new TimeSpan(0, 0, 5))
						{
							FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordAutoResponse, false, "자동 응답\n상태 : 자동 응답 제한 시간 미달");
							return;
						}
						lastSendAutoResponseTime = DateTime.Now;

						string SenderUID = (message.Author as SocketGuildUser).Id.ToString();
						StringBuilder sb = new StringBuilder();
						sb.AppendLine("상태 : 정상");
						sb.Append("INPUT : ");
						sb.Append((message.Author as SocketGuildUser).DisplayName);
						sb.Append('(').Append((message.Author as SocketGuildUser).Id.ToString()).Append(") ");
						sb.AppendLine(msg);
						sb.Append("OUTPUT : ").Append(outPut);

						FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordAutoResponse, false, sb.ToString());
						commandContext = new SocketCommandContext(client, message);
						_ = commandContext.Channel.SendMessageAsync(outPut);
					}
				}
				else
				{
					if (lastVoiceInTime >= DateTime.Now || lastVoiceOutTime >= DateTime.Now)
					{
						if (lastVoiceInTime > lastVoiceOutTime)
						{
							string reason = string.Format("입장 : {0}", lastVoiceInName);
							auto.tryAddInput(csAutoResponse.eAutoResponseSituationType.Hi, reason, msg, out _);
						}
						else
						{
							string reason = string.Format("퇴장 : {0}", lastVoiceInName);
							auto.tryAddInput(csAutoResponse.eAutoResponseSituationType.Bye, reason, msg, out _);
						}
					}
					else
					{
						auto.tryAddInput(csAutoResponse.eAutoResponseSituationType.None, "", msg, out _);
					}
				}
			}
		}

		private async Task InteractionCreated(SocketInteraction arg)
		{
			csConsoleProgramControl control = csConsoleProgramControl.GetInstance();
			csConsoleTargetControl _ctc = csConsoleTargetControl.GetInstance();

			if (arg is SocketMessageComponent messageComponent)
			{
				if (messageComponent.Data.CustomId == "Server_Select_Menu")
				{
					var values = messageComponent.Data.Values.First().Split("|");

					if (csDiscordCommand.LastRequestServerChangeBoxIndex != values[1])
					{
						await messageComponent.RespondAsync("해당 응답은 만료되었어 다시 명령을 내려줘");
						return;
					}
					csDiscordCommand.LastRequestServerChangeBoxIndex = string.Empty;

					if (control.isAlive())
					{
						await messageComponent.RespondAsync("서버가 가동중이라 변경이 불가능해");
						return;
					}

					if (int.TryParse(values[0], out int serverIndex))
					{
						if (serverIndex > _ctc.consoles.Count || serverIndex < 0)
						{
							await messageComponent.RespondAsync("해당 번호를 가지고 있는 서버는 없서");
						}
						else
						{
							_ctc.Selected = serverIndex;
							await messageComponent.RespondAsync(string.Format("{0}(으)로 변경 완료", _ctc.consoles[_ctc.Selected].Title));
						}

						_ctc.Save();
					}
				}
				else if (messageComponent.Data.CustomId == "Select_Menu_PlugIn_Minecraft")
				{
					var values = messageComponent.Data.Values.First().Split("|");

					if (arg.User.Id.ToString() != values[2])
					{
						await messageComponent.RespondAsync("당신이 실행한 명령이 아닐텐대?");
						return;
					}

					if (_ctc.getTarget().GameType != GameType.Minecraft)
					{
						await messageComponent.RespondAsync("서버가 마인크래프트 서버가 아니야");
						return;
					}

					if (control.isAlive() == false)
					{
						await messageComponent.RespondAsync("서버가 살아있는 상태가 아니야");
						return;
					}

					bool isPass = control.process_WriteMSG($"tp {values[0]} {values[1]}");
					await messageComponent.RespondAsync(isPass ? "텔레포트 성공" : "텔레포트 실패");
				}
			}
		}

		/// <summary>
		/// 봇의 로그를 출력하는 함수
		/// </summary>
		/// <param name="msg">봇의 클라이언트에서 수신된 로그</param>
		/// <returns></returns>
		private Task OnClientLogReceived(LogMessage msg)
		{
			//Console.WriteLine(msg.ToString());  //로그 출력
			FormMain.GetInstance().MainLogAppend(eMainLogType.DiscordAPI, true, msg.ToString());
			return Task.CompletedTask;
		}
	}
}