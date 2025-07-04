using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Console_Program_Control.Service
{
	public class csInteractionHandler
	{
		private readonly DiscordSocketClient _client;
		private readonly InteractionService _interactionService;
		private readonly IServiceProvider _services;

		public csInteractionHandler(DiscordSocketClient client, InteractionService interactionService, IServiceProvider services)
		{
			_client = client;
			_interactionService = interactionService;
			_services = services;
		}

		public async Task InitializeAsync()
		{
			_client.Ready += OnReadyAsync;
			_client.InteractionCreated += HandleInteractionAsync;

			await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
		}

		private async Task OnReadyAsync()
		{
			await _interactionService.RegisterCommandsGloballyAsync(); // 전역 명령어 등록
		}

		private async Task HandleInteractionAsync(SocketInteraction interaction)
		{
			var context = new SocketInteractionContext(_client, interaction);

			await _interactionService.ExecuteCommandAsync(context, _services);
		}
	}
}
