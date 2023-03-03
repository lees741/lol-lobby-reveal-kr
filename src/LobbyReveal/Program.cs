using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ekko;
using Spectre.Console;

namespace LobbyReveal
{
    internal class Program
    {
        private static List<LobbyHandler> _handlers = new List<LobbyHandler>();
        private static bool _update = true;

        public async static Task Main(string[] args)
        {
            Console.Title = "랭겜 아군 소환사명 탐지기";
            var watcher = new LeagueClientWatcher();
            watcher.OnLeagueClient += (clientWatcher, client) =>
            {
                /*Console.WriteLine(client.Pid);*/
                var handler = new LobbyHandler(new LeagueApi(client.ClientAuthInfo.RiotClientAuthToken,
                    client.ClientAuthInfo.RiotClientPort));
                _handlers.Add(handler);
                handler.OnUpdate += (lobbyHandler, names) => { _update = true; };
                handler.Start();
                _update = true;
            };
            new Thread(async () => { await watcher.Observe(); })
            {
                IsBackground = true
            }.Start();

            new Thread(() => { Refresh(); })
            {
                IsBackground = true
            }.Start();


            while (true)
            {
                try {
                    var input = Console.ReadKey(true);
                    var i = 1;
                    if (input.KeyChar.ToString().Equals("q") || input.KeyChar.ToString().Equals("Q"))
                    {
                        return;
                    }
                    if (!input.KeyChar.ToString().Equals("y") && !input.KeyChar.ToString().Equals("Y"))
                    {
                        AnsiConsole.Write(new Markup("[red]\n잘못된 입력입니다\n[/]"));
                        _update = true;
                        continue;
                    }

                    var region = _handlers[i - 1].GetRegion();

                    var link =
                        $"https://www.op.gg/multisearch/{region ?? Region.KR}?summoners=" +
                        HttpUtility.UrlEncode($"{string.Join(",", _handlers[i - 1].GetSummoners())}");
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", link);
                    }
                    else
                    {
                        Process.Start("open", link);
                    }
                    _update = true;

                } catch (System.ArgumentOutOfRangeException)
                {
                    AnsiConsole.Write(new Markup("[red]\n먼저 롤 클라이언트를 실행해주세요\n[/]"));
                    _update = true;
                    continue;
                }
            }

        }

        private static void Refresh()
        {
            while (true)
            {
                if (_update)
                {
                    Console.Clear();
                    AnsiConsole.Write(new Markup("[yellow]랭겜에서 아군의 소환사명을 알아보자[/]")
                        .Centered());
                    Console.WriteLine();
                    AnsiConsole.Write(new Markup("[lime]한국 서버 패치: 이현성[/]")
                        .Centered());
                    Console.WriteLine();
                    AnsiConsole.Write(new Markup("[cyan]출처:https://github.com/Riotphobia/LobbyReveal[/]")
                        .Centered());
                    Console.WriteLine();
                    Console.WriteLine();
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        var link =
                            $"https://www.op.gg/multisearch/{_handlers[i].GetRegion() ?? Region.KR}?summoners=" +
                            HttpUtility.UrlEncode($"{string.Join(",", _handlers[i].GetSummoners())}");

                        AnsiConsole.Write(
                            new Panel(new Text($"{string.Join("\n", _handlers[i].GetSummoners())}\n\n{link}")
                                    .LeftJustified())
                                .Expand()
                                .SquareBorder()
                                .Header($"[red]Client {i + 1}[/]"));
                        Console.WriteLine();
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                    AnsiConsole.Write(new Markup("[cyan]op.gg에서 멀티서치를 하려면 [[y]]를 입력하세요[/] / [red]종료하려면 [[q]]를 입력하세요[/]")
                        .LeftJustified());
                    Console.WriteLine();
                    _update = false;
                }

                Thread.Sleep(2000);
            }
        }
    }
}