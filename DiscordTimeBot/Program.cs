using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

internal class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;
    private IConfiguration _configuration;

    private static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    public Program()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.Guilds
        });
        _commands = new CommandService();
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .BuildServiceProvider();

        string botToken = _configuration["BotToken"];
        ulong guildId = ulong.Parse(_configuration["GuildId"]);

        _client.Log += Log;
        _client.Ready += () => ReadyAsync(guildId);

        await RegisterCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, botToken);

        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync(ulong guildId)
    {
        Console.WriteLine("Bot is connected to the following guilds:");
        foreach (var g in _client.Guilds)
        {
            Console.WriteLine($"- {g.Name} (ID: {g.Id})");
        }

        var guild = _client.GetGuild(guildId);

        if (guild == null)
        {
            Console.WriteLine($"Guild with ID {guildId} not found.");
            return;
        }

        await guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
            .WithName("time")
            .WithDescription("Get the current server time")
            .Build());

        Console.WriteLine("Slash command /time registered successfully.");
    }

    public async Task RegisterCommandsAsync()
    {
        _client.SlashCommandExecuted += HandleSlashCommandAsync;
    }

    private async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName == "time")
        {
            await command.RespondAsync($"Current server time is: {DateTime.Now}");
        }
    }
}