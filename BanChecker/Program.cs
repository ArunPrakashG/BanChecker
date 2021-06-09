using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BanChecker
{
	class Program
	{
		static string API_KEY = "";
		static HttpClient Client = new();

		// format: steamid pass profilelink
		static async Task Main(string[] args)
		{
			if (string.IsNullOrEmpty(API_KEY))
			{
				Console.WriteLine($"Please provide a valid steam api key: ");
				Console.WriteLine("You can find it here: https://steamcommunity.com/dev/apikey");
				API_KEY = Console.ReadLine();
			}

			if (string.IsNullOrEmpty(API_KEY))
			{
				Console.WriteLine("Without api key, not possible to continue.");
				return;
			}

			string[] array = await File.ReadAllLinesAsync("accounts.txt");
			List<(string, string, string)> accounts = new ();
			List<(string, string, ulong)> goodAccounts = new();
			List<(string, string, ulong)> vacBannedAccounts = new();
			List<(string, string, ulong)> gameBannedAccs = new();
			List<(string, string, ulong)> economyBannedAccs = new();

			for (int i = 0; i < array.Length; i++)
			{
				string accountLine = array[i];
				if (string.IsNullOrEmpty(accountLine))
				{
					continue;
				}

				var split = accountLine.Split(new char[] { '\t', ' ' });
				string userName = split[0];
				string password = split[1];
				string profileUrl = split[2];
				accounts.Add((userName, password, profileUrl));

				if (i > 0 && (i % 100) == 0)
				{
					await initProcess();
				}
			}

			if(accounts.Count > 0)
			{
				await initProcess();
			}

			await File.WriteAllLinesAsync("finish_all.txt", goodAccounts.Select(x => $"{x.Item1},{x.Item2},{x.Item3}"));
			await File.WriteAllLinesAsync("finish_vac.txt", vacBannedAccounts.Select(x => $"{x.Item1},{x.Item2},{x.Item3}"));
			await File.WriteAllLinesAsync("finish_gameban.txt", gameBannedAccs.Select(x => $"{x.Item1},{x.Item2},{x.Item3}"));
			await File.WriteAllLinesAsync("finish_economyban.txt", economyBannedAccs.Select(x => $"{x.Item1},{x.Item2},{x.Item3}"));

			Console.WriteLine("All process completed");
			Console.ReadLine();

			async Task initProcess()
			{
				try
				{
					var steamIds = accounts.Select(x => GetSteam64ID(x.Item3));
					var response = await Client.GetAsync($"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={API_KEY}&steamids=[{string.Join(",", steamIds)}]");
					if (!response.IsSuccessStatusCode)
					{
						Console.WriteLine("One ban request failed. Possibly steam might be down.");
						return;
					}

					try
					{
						var banModel = JsonConvert.DeserializeObject<BanModel>(await response.Content.ReadAsStringAsync());

						foreach (var acc in banModel.players)
						{
							var matchingAccount = accounts.Where(x => GetSteam64ID(x.Item3) == ulong.Parse(acc.SteamId)).FirstOrDefault();

							if (matchingAccount.Item3 == null)
							{
								continue;
							}

							if (acc.NumberOfGameBans <= 0 && acc.NumberOfVACBans <= 0 && acc.VACBanned == false && acc.EconomyBan == "none")
							{
								Console.WriteLine($"No ban on {matchingAccount.Item1}");
								goodAccounts.Add((matchingAccount.Item1, matchingAccount.Item2, GetSteam64ID(matchingAccount.Item3)));
								continue;
							}

							if (acc.NumberOfGameBans > 0)
							{
								Console.WriteLine($"Game ban in {matchingAccount.Item1} account");
								gameBannedAccs.Add((matchingAccount.Item1, matchingAccount.Item2, GetSteam64ID(matchingAccount.Item3)));
							}

							if (acc.NumberOfVACBans > 0 || acc.VACBanned)
							{
								Console.WriteLine($"VAC ban in {matchingAccount.Item1} account");
								vacBannedAccounts.Add((matchingAccount.Item1, matchingAccount.Item2, GetSteam64ID(matchingAccount.Item3)));
							}

							if (acc.EconomyBan != "none")
							{
								Console.WriteLine($"Economy Ban in {matchingAccount.Item1} account");
								economyBannedAccs.Add((matchingAccount.Item1, matchingAccount.Item2, GetSteam64ID(matchingAccount.Item3)));
							}
						}
					}
					catch
					{
						Console.WriteLine("Failed to parse response.");
						return;
					}
				}
				finally
				{
					accounts.Clear();
					await Task.Delay(3000);
				}
			}
		}

		static ulong GetSteam64ID(string url)
		{
			if (url == null)
			{
				return 0;
			}

			return ulong.TryParse(string.Join("", url.Where(char.IsDigit)), out ulong id) ? id : 0;
		}
	}
}
