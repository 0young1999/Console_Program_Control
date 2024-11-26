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
			data.BChannelName = before.VoiceChannel?.Name ?? "";
			data.AChannelName = after.VoiceChannel?.Name ?? "";
			data.UID = (user as SocketGuildUser).Id.ToString();

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
			SocketUserMessage message = arg as SocketUserMessage;
			if (message == null) return;

			// 봇 채팅 방어
			if (message.Author.IsBot)
				return;

			string msg = arg.Content;
			if (msg.IndexOf(setting.CommandSTX) == 0)
			{
				string SenderName = (message.Author as SocketGuildUser).DisplayName;
				string SenderUID = (message.Author as SocketGuildUser).Id.ToString();

				FormMain.GetInstance().MainLogAppend(true, string.Format("{0}({2}) : {1}", SenderName, msg, SenderUID));
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
								response = log.ShowLastLog(commandSplit.Length >= 2 ? commandSplit[1] : "10");
							}
						}
						break;
					case "섹스":
					case "sex":
						{
							response = "님 혹시 기계 박이?";
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
				FormMain.GetInstance().MainLogAppend(false, response);
			}
			else
			{
				// 자동 응답
				csAutoResponse auto = csAutoResponse.GetInstance();
				if (string.IsNullOrEmpty(msg) || auto.isActive == false) return;

				if (auto.getOutPut(msg, out string outPut))
				{
					if (string.IsNullOrEmpty(outPut))
					{
						return;
					}
					else
					{
						string SenderUID = (message.Author as SocketGuildUser).Id.ToString();
						StringBuilder sb = new StringBuilder();
						sb.AppendLine("자동 응답");
						sb.Append("INPUT : ");
						sb.Append((message.Author as SocketGuildUser).DisplayName);
						sb.Append('(').Append((message.Author as SocketGuildUser).Id.ToString()).Append(") ");
						sb.AppendLine(msg);
						sb.Append("OUTPUT : ").Append(outPut);

						FormMain.GetInstance().MainLogAppend(false, sb.ToString());
						commandContext = new SocketCommandContext(client, message);
						_ = commandContext.Channel.SendMessageAsync(outPut);
					}
				}
				else
				{
					auto.addInput(msg, out _);
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
			FormMain.GetInstance().MainLogAppend(true, string.Format("{0} : {1}", "system", msg.ToString()));
			return Task.CompletedTask;
		}
	}
}