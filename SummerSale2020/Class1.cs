using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm;
using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Json;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Plugins;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace SummerSale2020 {
	[Export(typeof(IPlugin))]
	public class Class1 : IBotCommand {
		public string Name => "SummerSale2020";
		public Version Version => typeof(Class1).Assembly.GetName().Version;

		private static async Task<string> GetSticker(Bot bot) {

			const string post_request = "https://api.steampowered.com/ISummerSale2020Service/ClaimItem/v1?access_token=";
			const string html_request = "/points/shop";

			IDocument html = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(ArchiWebHandler.SteamStoreURL, html_request).ConfigureAwait(false);
			if (html == null) {
				return "<" + bot.BotName + "> Failed!";
			}

			Regex re = new Regex("&quot;webapi_token&quot;:&quot;([^&]*)&quot;");
			MatchCollection reResult = re.Matches(html.DocumentElement.InnerHtml);
			if (reResult.Count < 1 || reResult[0].Groups.Count < 2) {
				return "<" + bot.BotName + "> Failed!";
			}
			string webApiToken = reResult[0].Groups[1].Value;

			Dictionary<string, string> data = new Dictionary<string, string>(1, StringComparer.Ordinal) { { "input_protobuf_encoded", "" } };

			WebBrowser.BasicResponse response = await ArchiWebHandler.WebLimitRequest(WebAPI.DefaultBaseAddress.Host, async () => await bot.ArchiWebHandler.WebBrowser.UrlPost(post_request + webApiToken, data, ArchiWebHandler.SteamStoreURL + html_request).ConfigureAwait(false)).ConfigureAwait(false);

			if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK) {
				return "<" + bot.BotName + "> Done!";
			} else {
				return "<" + bot.BotName + "> Failed!";
			}

		}

		private static async Task<string> ClaimSticker(string botNames) {
			HashSet<Bot> bots = Bot.GetBots(botNames);
			if ((bots == null) || (bots.Count == 0)) {
				return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
			}

			IList<string> results = await Utilities.InParallel(bots.Select(bot => GetSticker(bot))).ConfigureAwait(false);

			List<string> responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result)));

			ASF.ArchiLogger.LogGenericInfo(Environment.NewLine + string.Join(Environment.NewLine, responses));
			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}
		public async Task<string> OnBotCommand([NotNull] Bot bot, ulong steamID, [NotNull] string message, [ItemNotNull, NotNull] string[] args) {
			if (!bot.HasPermission(steamID, BotConfig.EPermission.Master)) {
				return null;
			}

			switch (args[0].ToUpperInvariant()) {
				case "GETSTICKER" when args.Length > 1:
					return await ClaimSticker(Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
				case "GETSTICKER":
					return await ClaimSticker(bot.BotName).ConfigureAwait(false);
				default:
					return null;
			}

		}

		public void OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("SummerSale2020 Plugin by Ryzhehvost, powered by ginger cats");
			new Timer(
				async e => await ClaimSticker("ASF").ConfigureAwait(false),
				null,
				TimeSpan.FromHours(1),
				TimeSpan.FromHours(8)
			);
		}
	}
}
