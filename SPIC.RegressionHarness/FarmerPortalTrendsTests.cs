using System.Text.Json;
using Employeelogin = SPIC.Core.Entities.EmployeeRegistration.Employeelogin;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Services;
using SpicAPI.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 3: the weather advisory rules, the Open-Meteo parsing, the area-number privacy
/// rules (minimum samples / farmers, aggregates only), "talked about" ordering, the Community "near
/// me" author rule, routes / menu / tiles and the plain-words text. All pure, on in-memory inputs.
/// </summary>
internal static class FarmerPortalTrendsTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal: weather, trends in my area and community near me");
		Weather();
		Advisories();
		OpenMeteoParse();
		SoilPrivacy();
		Crops();
		Talked();
		Near();
		RoutesAndMenu();
		UiText();
	}

	private static FarmerWeatherDayDto Day(double rain = 0, int chance = 0, double max = 31, double min = 23, double wind = 10, int code = 1) =>
		new() { Date = "2026-11-28", RainMm = rain, RainChancePct = chance, MaxTempC = max, MinTempC = min, WindKmh = wind, WeatherCode = code };

	private static List<FarmerWeatherDayDto> Week(params FarmerWeatherDayDto[] first)
	{
		var days = first.ToList();
		while (days.Count < 7) days.Add(Day(rain: 2, chance: 45));   // a neutral, slightly damp day
		return days;
	}

	private static string Codes(IEnumerable<FarmerAdvisoryDto> a) => string.Join(",", a.Select(x => x.Code));

	private static void Weather()
	{
		Check.Equal("weather: point rounded to 0.1 deg (lat)", 9.2, FarmerWeatherRules.RoundForForecast(9.1742, 77.8689).Lat);
		Check.Equal("weather: point rounded to 0.1 deg (lng)", 77.9, FarmerWeatherRules.RoundForForecast(9.1742, 77.8689).Lng);
		Check.Equal("weather: .05 rounds away from zero", 9.3, FarmerWeatherRules.RoundForForecast(9.25, 77.85).Lat);
		Check.Equal("weather: negative coordinates round too", -12.3, FarmerWeatherRules.RoundForForecast(-12.34, 10).Lat);
		Check.Equal("weather: code 0 is sunny", "Sunny", FarmerWeatherRules.Describe(0).Label);
		Check.Equal("weather: code 95 is a thunderstorm", "bi-cloud-lightning-rain", FarmerWeatherRules.Describe(95).Icon);
		Check.Equal("weather: code 65 is heavy rain", "Heavy rain", FarmerWeatherRules.Describe(65).Label);
		Check.Equal("weather: code 61 is rain", "Rain", FarmerWeatherRules.Describe(61).Label);
		Check.Equal("weather: code 80 is showers", "Showers", FarmerWeatherRules.Describe(80).Label);
		Check.Equal("weather: code 53 is drizzle", "Drizzle", FarmerWeatherRules.Describe(53).Label);
		Check.Equal("weather: unknown / missing code has a neutral icon", "bi-cloud-sun", FarmerWeatherRules.Describe(null).Icon);
	}

	private static void Advisories()
	{
		Check.Equal("advice: no forecast, no advice", 0, FarmerWeatherRules.Advisories(new List<FarmerWeatherDayDto>()).Count);

		// Heavy rain: MORE than 50 mm over the next 3 days.
		var exactly50 = FarmerWeatherRules.Advisories(Week(Day(rain: 20, chance: 60), Day(rain: 20, chance: 60), Day(rain: 10, chance: 60)));
		Check.That("advice: exactly 50 mm in 3 days is not heavy rain", exactly50.All(a => a.Code != "heavy-rain"), Codes(exactly50));
		Check.That("advice: ...but 20 mm today is rain soon", exactly50.Any(a => a.Code == "rain-soon"), Codes(exactly50));

		var heavy = FarmerWeatherRules.Advisories(Week(Day(rain: 5, chance: 50), Day(rain: 30, chance: 90), Day(rain: 16, chance: 90)));
		Check.Equal("advice: 51 mm in 3 days is heavy rain, first", "heavy-rain", heavy.FirstOrDefault()?.Code);
		Check.That("advice: heavy rain says avoid top-dressing and spraying before the rain",
			heavy.First().Text.Contains("avoid fertilizer top-dressing and spraying before the rain", StringComparison.Ordinal));
		Check.That("advice: heavy rain names the amount", heavy.First().Title.Contains("51 mm"), heavy.First().Title);
		Check.That("advice: heavy rain is a caution", heavy.First().Level == FarmerAdvisoryLevel.Caution);
		Check.That("advice: heavy rain replaces rain soon", heavy.All(a => a.Code != "rain-soon"), Codes(heavy));
		Check.That("advice: rain on day 4 does not count for the 3-day total",
			FarmerWeatherRules.Advisories(Week(Day(), Day(), Day(rain: 10), Day(rain: 80))).All(a => a.Code != "heavy-rain"));

		var chance = FarmerWeatherRules.Advisories(Week(Day(rain: 1, chance: 20), Day(rain: 2, chance: 70)));
		Check.That("advice: 70 % chance tomorrow is rain soon", chance.Any(a => a.Code == "rain-soon"), Codes(chance));
		var chance69 = FarmerWeatherRules.Advisories(Week(Day(rain: 1, chance: 20), Day(rain: 2, chance: 69)));
		Check.That("advice: 69 % and 2 mm is not rain soon", chance69.All(a => a.Code != "rain-soon"), Codes(chance69));
		var dayThree = FarmerWeatherRules.Advisories(Week(Day(), Day(), Day(rain: 20, chance: 95)));
		Check.That("advice: rain soon looks at the next 2 days only", dayThree.All(a => a.Code != "rain-soon"), Codes(dayThree));

		var hot = FarmerWeatherRules.Advisories(Week(Day(max: 36), Day(max: 37.9), Day(max: 38)));
		Check.That("advice: 38 deg on day 3 is the heat advice", hot.Any(a => a.Code == "heat"), Codes(hot));
		Check.That("advice: 37.9 deg is not",
			FarmerWeatherRules.Advisories(Week(Day(max: 37.9), Day(max: 37.9), Day(max: 37.9))).All(a => a.Code != "heat"));

		var windy = FarmerWeatherRules.Advisories(Week(Day(wind: 12), Day(wind: 30)));
		Check.That("advice: 30 km/h tomorrow is strong wind", windy.Any(a => a.Code == "wind"), Codes(windy));
		Check.That("advice: 29.9 km/h is not", FarmerWeatherRules.Advisories(Week(Day(wind: 29.9), Day(wind: 29.9))).All(a => a.Code != "wind"));

		var dry = Enumerable.Range(0, 7).Select(_ => Day(rain: 0.5, chance: 10)).ToList();
		var dryAdvice = FarmerWeatherRules.Advisories(dry);
		Check.Equal("advice: a dry calm week gives dry week + good days", "dry-week,spray-window", Codes(dryAdvice));
		Check.That("advice: good days is the Good level", dryAdvice.Last().Level == FarmerAdvisoryLevel.Good);
		var dryButLikely = Enumerable.Range(0, 7).Select(i => Day(rain: 0.2, chance: i == 5 ? 40 : 10)).ToList();
		Check.That("advice: a 40 % day breaks the dry week",
			FarmerWeatherRules.Advisories(dryButLikely).All(a => a.Code != "dry-week"));
		Check.That("advice: no good-days window in strong wind",
			FarmerWeatherRules.Advisories(Enumerable.Range(0, 7).Select(_ => Day(rain: 0, chance: 0, wind: 22)).ToList()).All(a => a.Code != "spray-window"));

		var everything = FarmerWeatherRules.Advisories(Week(Day(rain: 40, chance: 95, max: 39, wind: 35), Day(rain: 30, chance: 95, max: 39, wind: 35)));
		Check.Equal("advice: at most 3, most important first", "heavy-rain,heat,wind", Codes(everything));

		var nulls = new List<FarmerWeatherDayDto> { new() { Date = "2026-11-28" }, new() { Date = "2026-11-29" } };
		Check.That("advice: missing values do not throw and read as dry / calm",
			Codes(FarmerWeatherRules.Advisories(nulls)) == "dry-week,spray-window", Codes(FarmerWeatherRules.Advisories(nulls)));
	}

	private static void OpenMeteoParse()
	{
		const string json = """
			{"daily":{"time":["2026-11-28","2026-11-29","2026-11-30"],
			 "weather_code":[95,53,null],
			 "temperature_2m_max":[29.74,29.6,29.0],
			 "temperature_2m_min":[23.8,23.7],
			 "precipitation_sum":[13.1,3.2,0.0],
			 "precipitation_probability_max":[100,63,0],
			 "wind_speed_10m_max":[13.3,19.2,17.0]}}
			""";
		using var doc = JsonDocument.Parse(json);
		var days = FarmerWeatherService.Parse(doc.RootElement);
		Check.Equal("open-meteo: one row per day", 3, days.Count);
		Check.Equal("open-meteo: date kept", "2026-11-29", days[1].Date);
		Check.Equal("open-meteo: code read", 95, days[0].WeatherCode);
		Check.That("open-meteo: null code stays null", days[2].WeatherCode is null);
		Check.Equal("open-meteo: temperatures rounded to 0.1", 29.7, days[0].MaxTempC);
		Check.That("open-meteo: a short array gives null, not an error", days[2].MinTempC is null);
		Check.Equal("open-meteo: rain chance as a whole number", 63, days[1].RainChancePct);
		using var bad = JsonDocument.Parse("""{"error":true,"reason":"bad"}""");
		Check.Equal("open-meteo: an error body gives no days", 0, FarmerWeatherService.Parse(bad.RootElement).Count);
		using var noTime = JsonDocument.Parse("""{"daily":{"temperature_2m_max":[30]}}""");
		Check.Equal("open-meteo: no time array gives no days", 0, FarmerWeatherService.Parse(noTime.RootElement).Count);
	}

	private static IEnumerable<TrendSoilResult> Samples(int from, int count, int? district, string parameter, int low)
	{
		for (var i = 0; i < count; i++)
			yield return new TrendSoilResult(from + i, district, parameter, i < low ? LabResultStatus.Deficient : LabResultStatus.Normal);
	}

	private static void SoilPrivacy()
	{
		const int D = 1, Other = 2;

		var four = Samples(1, 4, D, "Nitrogen", 4).ToList();
		var none = FarmerTrendRules.SoilProblems(four, D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("soil: 4 samples in the whole state are hidden", FarmerAreaScope.None, none.Scope);
		Check.That("soil: hidden means no numbers at all", none.Items.Count == 0 && none.SampleCount == 0 && none.AreaName is null);

		var five = FarmerTrendRules.SoilProblems(Samples(1, 5, D, "Nitrogen", 2), D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("soil: 5 samples in the district show the district", FarmerAreaScope.District, five.Scope);
		Check.Equal("soil: district name", "Thoothukudi", five.AreaName);
		Check.Equal("soil: 2 of 5 low = 40 %", 40, five.Items.Single().LowPct);
		Check.Equal("soil: short label", "N", five.Items.Single().Short);
		Check.Equal("soil: sample count", 5, five.SampleCount);

		var fallback = FarmerTrendRules.SoilProblems(Samples(1, 4, D, "Zinc", 1).Concat(Samples(10, 3, Other, "Zinc", 3)), D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("soil: 4 in the district + 3 elsewhere in the state = the state", FarmerAreaScope.State, fallback.Scope);
		Check.Equal("soil: state name", "Tamil Nadu", fallback.AreaName);
		Check.Equal("soil: state share 4 of 7 = 57 %", 57, fallback.Items.Single().LowPct);

		// A nutrient tested on fewer than 5 samples is hidden even when the area has enough samples.
		var mixed = Samples(1, 6, D, "Nitrogen", 3).Concat(Samples(1, 4, D, "Zinc", 4)).ToList();
		var mixedTrend = FarmerTrendRules.SoilProblems(mixed, D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("soil: zinc on 4 samples is hidden", "Nitrogen", string.Join(",", mixedTrend.Items.Select(i => i.Parameter)));
		Check.Equal("soil: the hidden count is reported", 1, mixedTrend.HiddenForFewSamples);

		var noLow = FarmerTrendRules.SoilProblems(Samples(1, 8, D, "Potassium", 0), D, "Thoothukudi", "Tamil Nadu");
		Check.That("soil: a nutrient with no Low sample is not listed", noLow.Scope == FarmerAreaScope.District && noLow.Items.Count == 0);

		var ph = FarmerTrendRules.SoilProblems(Samples(1, 9, D, "pH", 9), D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("soil: non-nutrient parameters (pH) are ignored", FarmerAreaScope.None, ph.Scope);

		// One sample entered twice counts once (the last value wins).
		var twice = Samples(1, 5, D, "Nitrogen", 0).Append(new TrendSoilResult(1, D, "Nitrogen", LabResultStatus.Deficient)).ToList();
		var twiceTrend = FarmerTrendRules.SoilProblems(twice, D, "Thoothukudi", "Tamil Nadu");
		Check.That("soil: a re-entered result counts once", twiceTrend.Items.Single() is { Samples: 5, LowSamples: 1 });

		var order = Samples(1, 10, D, "Nitrogen", 2).Concat(Samples(1, 10, D, "Zinc", 6)).Concat(Samples(1, 10, D, "Organic Carbon", 4)).ToList();
		Check.Equal("soil: biggest shortage first", "Zinc,Organic Carbon,Nitrogen",
			string.Join(",", FarmerTrendRules.SoilProblems(order, D, "T", "S").Items.Select(i => i.Parameter)));
		Check.That("soil: moderate / excess are not low",
			FarmerTrendRules.SoilProblems(new[] { 1, 2, 3, 4, 5 }.Select(i => new TrendSoilResult(i, D, "Nitrogen", i % 2 == 0 ? LabResultStatus.Moderate : LabResultStatus.Excess)), D, "T", "S").Items.Count == 0);
		Check.Equal("soil: minimum is 5", 5, FarmerTrendRules.MinSamples);
	}

	private static TrendCropFarmer F(string key, int district, params string[] crops) => new(key, district, crops);

	private static void Crops()
	{
		const int D = 1;
		var four = new[] { F("u:a", D, "Paddy"), F("u:b", D, "Paddy"), F("u:c", D, "Banana"), F("u:d", D, "Paddy") };
		Check.Equal("crops: 4 farmers in the state are hidden", FarmerAreaScope.None, FarmerTrendRules.Crops(four, D, "T", "S").Scope);

		var five = four.Append(F("s:9", D, "paddy ", "Banana")).ToList();
		var trend = FarmerTrendRules.Crops(five, D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("crops: 5 farmers in the district show the district", FarmerAreaScope.District, trend.Scope);
		Check.Equal("crops: paddy first, names normalised", "Paddy:4:80,Banana:2:40",
			string.Join(",", trend.Items.Select(i => $"{i.Crop}:{i.Farmers}:{i.SharePct}")));

		var withOdd = five.Append(F("u:e", D, "Turmeric")).ToList();
		Check.That("crops: a crop grown by one farmer is not listed",
			FarmerTrendRules.Crops(withOdd, D, "T", "S").Items.All(i => i.Crop != "Turmeric"));

		var dup = new[] { F("u:a", D, "Paddy"), F("u:a", D, "Paddy", "Cotton"), F("u:b", D, "Paddy"), F("u:c", D, "Paddy"), F("u:d", D, "Paddy") };
		var dupTrend = FarmerTrendRules.Crops(dup, D, "T", "S");
		Check.Equal("crops: one person with a profile and a SAS record counts once (4 farmers: hidden)", FarmerAreaScope.None, dupTrend.Scope);

		var state = new[] { F("u:a", D, "Paddy"), F("u:b", D, "Paddy"), F("u:c", 2, "Cotton"), F("u:d", 2, "Cotton"), F("u:e", 3, "Paddy") };
		var stateTrend = FarmerTrendRules.Crops(state, D, "Thoothukudi", "Tamil Nadu");
		Check.Equal("crops: too few in the district falls back to the state", FarmerAreaScope.State, stateTrend.Scope);
		Check.Equal("crops: state count", 5, stateTrend.FarmerCount);
		Check.Equal("crops: farmers without crops are not counted", FarmerAreaScope.None,
			FarmerTrendRules.Crops(Enumerable.Range(0, 9).Select(i => F("u:" + i, D)), D, "T", "S").Scope);
	}

	private static void Talked()
	{
		var t0 = new DateTime(2026, 11, 1);
		TrendPost P(int id, int recent, int likes, int views, int replies = 0, int day = 0) =>
			new(id, "Post " + id, null, null, CommunityDiscussionStatus.Open, replies, recent, views, likes, t0.AddDays(day));
		Check.Equal("talk: score = recent replies x3 + likes + views / 10", 3 * 2 + 1 + 4, FarmerTrendRules.TalkScore(P(1, 2, 1, 45)));
		var top = FarmerTrendRules.TopTalked(new[]
		{
			P(1, 0, 0, 0), P(2, 1, 0, 0), P(3, 0, 5, 0), P(4, 3, 0, 0), P(5, 0, 0, 30, day: 2), P(6, 0, 0, 30, day: 5), P(7, 0, 1, 0), P(8, 0, 0, 9, replies: 2)
		});
		Check.Equal("talk: top 5 by score, newer first on ties", "4,3,6,5,2", string.Join(",", top.Select(t => t.Id)));
		Check.That("talk: a discussion with no activity is left out",
			FarmerTrendRules.TopTalked(new[] { P(1, 0, 0, 5) }).Count == 0);
		Check.That("talk: an old reply still counts the discussion in",
			FarmerTrendRules.TopTalked(new[] { P(8, 0, 0, 9, replies: 2) }).Count == 1);
	}

	private static CommunityNearResolver.NearSources NearData()
	{
		FarmerProfile Profile(string user, int district, int state) => new()
			{ UserId = user, Name = user, Phone = "6000000000", DistrictId = district, StateId = state, Village = "V", PinCode = "600000" };
		var profiles = new List<FarmerProfile> { Profile("f-d", 1, 10), Profile("f-s", 2, 10), Profile("f-x", 5, 20) };
		var sas = new List<SasFarmer> {
			new SasFarmer { Name = "sas", Mobile = "1", UserId = "sas-d", DistrictId = 1, StateId = 10 },          // linked SAS farmer, no profile
			new SasFarmer { Name = "sas", Mobile = "2", UserId = "f-x", DistrictId = 1, StateId = 10 },            // has a profile elsewhere: the profile wins
			new SasFarmer { Name = "sas", Mobile = "3", UserId = "sas-del", DistrictId = 1, StateId = 10, IsDeleted = true } };
		Employeelogin Login(string user, int state, int region, int hq, int[]? regions = null, int[]? hqs = null, bool active = true) => new()
			{ UserId = user, Role = AppRole.MDO, StateId = state, RegionId = region, HeadquartersId = hq, RegionIds = regions, HeadquartersIds = hqs, IsActive = active };
		var logins = new List<Employeelogin> {
			Login("s-hq", 10, 0, 100),                          // HQ 100 serves the district
			Login("s-hqs", 10, 0, 0, hqs: new[] { 7, 101 }),     // multi-HQ login
			Login("s-rm", 10, 50, 0),                           // RM of region 50, no HQ
			Login("s-other-hq", 10, 50, 999),                   // an HQ elsewhere: its region does not count
			Login("s-far", 20, 60, 600),
			Login("s-off", 10, 0, 100, active: false) };
		var posts = new List<CommunityPost>();
		var n = 0;
		foreach (var author in new[] { "f-d", "f-s", "f-x", "sas-d", "sas-del", "s-hq", "s-hqs", "s-rm", "s-other-hq", "s-far", "s-off", "dealer" })
			posts.Add(new CommunityPost { Id = ++n, Title = author, Body = "b", AuthorUserId = author });
		posts.Add(new CommunityPost { Id = ++n, Title = "deleted", Body = "b", AuthorUserId = "f-d", IsDeleted = true });
		return new CommunityNearResolver.NearSources(posts.AsQueryable(), profiles.AsQueryable(), sas.AsQueryable(), logins.AsQueryable());
	}

	private static string Titles(IQueryable<CommunityPost> q) => string.Join(",", q.Select(p => p.Title).ToList().OrderBy(t => t, StringComparer.Ordinal));

	private static void Near()
	{
		var db = NearData();
		var area = new CommunityArea(1, 10, "Thoothukudi", "Tamil Nadu");
		var hqs = new[] { 100, 101 };
		var regions = new[] { 50 };
		Check.Equal("near: district authors (profile, linked SAS, HQ, multi-HQ, region without HQ)",
			"f-d,s-hq,s-hqs,s-rm,sas-d", Titles(CommunityNearResolver.NearPosts(db, area, FarmerAreaScope.District, hqs, regions)));
		Check.Equal("near: the state adds the rest of the state, never other states, inactive logins, deleted posts or dealers",
			"f-d,f-s,s-hq,s-hqs,s-other-hq,s-rm,sas-d", Titles(CommunityNearResolver.NearPosts(db, area, FarmerAreaScope.State, hqs, regions)));
		Check.Equal("near: a district no dealer HQ serves has only its farmers",
			"f-d,sas-d", Titles(CommunityNearResolver.NearPosts(db, area, FarmerAreaScope.District, Array.Empty<int>(), Array.Empty<int>())));
		Check.Equal("near: staff caller (state only, no district)",
			"f-d,f-s,s-hq,s-hqs,s-other-hq,s-rm,sas-d", Titles(CommunityNearResolver.NearPosts(db, new CommunityArea(0, 10, null, "TN"), FarmerAreaScope.State, hqs, regions)));
		Check.Equal("near: no area gives nothing", "", Titles(CommunityNearResolver.NearPosts(db, new CommunityArea(0, 0, null, null), FarmerAreaScope.State, hqs, regions)));
		Check.Equal("near: scope None gives nothing", "", Titles(CommunityNearResolver.NearPosts(db, area, FarmerAreaScope.None, hqs, regions)));

		Check.Equal("near scope: 3 district posts show the district", FarmerAreaScope.District, CommunityNearRules.ChooseScope(3, 9));
		Check.Equal("near scope: 2 district posts fall back to the richer state", FarmerAreaScope.State, CommunityNearRules.ChooseScope(2, 9));
		Check.Equal("near scope: 1 district post, state known: the state", FarmerAreaScope.State, CommunityNearRules.ChooseScope(1, 1));
		Check.Equal("near scope: no state known keeps the thin district", FarmerAreaScope.District, CommunityNearRules.ChooseScope(1, 0));
		Check.Equal("near scope: nothing anywhere", FarmerAreaScope.None, CommunityNearRules.ChooseScope(0, 0));
		Check.Equal("near scope: threshold is 3", 3, CommunityNearRules.MinDistrictPosts);
	}

	private static void RoutesAndMenu()
	{
		var farmer = TestLogin.As(AppRole.Farmer, "");
		foreach (var url in new[] { "/farmer/trends", "/farmer/community", "/Community", "/Community/discussions?tab=near", "/Community/discussion/12", "/Community/new", "/Community/posted/12" })
			Check.That($"route: farmer opens {url}", RouteAccess.CanOpen(farmer, url, null));
		Check.Equal("route: MDO is sent back from /farmer/trends", LoginState.DefaultLandingPath,
			RouteAccess.Decide(TestLogin.As(AppRole.MDO, "Dashboard.View"), "/farmer/trends", null).RedirectTo);
		Check.Equal("route: staff without a designation still sees only Welcome (Community)", "/Welcome",
			RouteAccess.Decide(TestLogin.As(AppRole.MDO, ""), "/Community", null).RedirectTo);
		Check.That("route: a farmer still cannot open staff pages", !RouteAccess.CanOpen(farmer, "/Dashboard", null));

		var more = ShellNavigation.MoreItems(farmer).Select(t => t.Key).ToList();
		Check.That("menu: farmer More sheet offers Trends and Community near me",
			more.Contains(ShellNavigation.FarmerTrendsKey) && more.Contains(ShellNavigation.FarmerCommunityKey), string.Join(",", more));
		Check.That("menu: they never take a farmer phone tab",
			ShellNavigation.GetPhoneTabs(farmer).All(t => t.Key is not (ShellNavigation.FarmerTrendsKey or ShellNavigation.FarmerCommunityKey)));
		Check.That("menu: staff never get the farmer entries",
			ShellNavigation.MoreItems(TestLogin.As(AppRole.MDO, "Dashboard.View")).All(t => t.Key is not (ShellNavigation.FarmerTrendsKey or ShellNavigation.FarmerCommunityKey))
			&& ShellNavigation.MoreItems(TestLogin.As(AppRole.Admin, "")).All(t => t.Key is not (ShellNavigation.FarmerTrendsKey or ShellNavigation.FarmerCommunityKey)));
		Check.That("tiles: trends and community are on the farmer home",
			FarmerFeatureTiles.For(farmer).Any(t => t.Route == "/farmer/trends") && FarmerFeatureTiles.For(farmer).Any(t => t.Route == "/farmer/community"));
	}

	private static void UiText()
	{
		var noonIst = new DateTime(2026, 11, 28, 6, 30, 0, DateTimeKind.Utc);       // 12:00 IST, Saturday
		Check.Equal("ui: today", "Today", FarmerTrendsUi.DayLabel("2026-11-28", noonIst));
		Check.Equal("ui: tomorrow", "Tomorrow", FarmerTrendsUi.DayLabel("2026-11-29", noonIst));
		Check.Equal("ui: tomorrow (short)", "Tmrw", FarmerTrendsUi.ShortDayLabel("2026-11-29", noonIst));
		Check.Equal("ui: weekday", "Mon", FarmerTrendsUi.DayLabel("2026-11-30", noonIst));
		var lateUtc = new DateTime(2026, 11, 28, 19, 0, 0, DateTimeKind.Utc);       // 00:30 IST on the 29th
		Check.Equal("ui: today follows IST, not UTC", "Today", FarmerTrendsUi.DayLabel("2026-11-29", lateUtc));
		Check.Equal("ui: a bad date is shown as is", "soon", FarmerTrendsUi.DayLabel("soon", noonIst));
		Check.Equal("ui: temp", "30°", FarmerTrendsUi.Temp(29.6));
		Check.Equal("ui: no temp", "–", FarmerTrendsUi.Temp(null));
		Check.Equal("ui: rain mm", "13 mm", FarmerTrendsUi.Rain(13.1));
		Check.Equal("ui: light rain", "0.4 mm", FarmerTrendsUi.Rain(0.4));
		Check.Equal("ui: no rain", "No rain", FarmerTrendsUi.Rain(0));
		var weather = new FarmerWeatherDto
		{
			Status = FarmerWeatherStatus.Ok,
			Advisories = { new() { Code = "dry-week", Level = FarmerAdvisoryLevel.Info }, new() { Code = "heat", Level = FarmerAdvisoryLevel.Caution } }
		};
		Check.Equal("ui: the home shows a caution first", "heat", FarmerTrendsUi.TopAdvice(weather)?.Code);
		Check.That("ui: no advice without a forecast", FarmerTrendsUi.TopAdvice(new FarmerWeatherDto { Status = FarmerWeatherStatus.NoLocation }) is null);
		Check.Equal("ui: state fallback note", "Not enough soil tests from Thoothukudi yet, so this shows all of Tamil Nadu.",
			FarmerTrendsUi.ScopeNote(FarmerAreaScope.State, "Thoothukudi", "Tamil Nadu", "soil tests"));
		Check.That("ui: no note for the district", FarmerTrendsUi.ScopeNote(FarmerAreaScope.District, "T", "S", "x") is null);
		Check.Equal("ui: in area", "in Thoothukudi", FarmerTrendsUi.InArea("Thoothukudi"));
		Check.Equal("ui: count", "1 reply · 12 views", FarmerTrendsUi.TalkLine(1, 12));
		Check.Equal("ui: count plural", "3 replies · 1 view", FarmerTrendsUi.TalkLine(3, 1));
		Check.Equal("ui: tiny share still shows a bar", 4, FarmerTrendsUi.BarWidth(1));
		Check.Equal("ui: zero share has no bar", 0, FarmerTrendsUi.BarWidth(0));
		Check.Equal("ui: role label", "SPIC expert", FarmerTrendsUi.RoleLabel("SPIC Expert"));
		Check.That("ui: an ordinary open discussion has no status chip", FarmerTrendsUi.StatusChip(CommunityDiscussionStatus.Open) is null);
		Check.Equal("ui: the location prompt opens the profile's location", "/farmer/profile?focus=location", FarmerTrendsUi.AddLocationHref);
		Check.That("ui: the location prompt route opens for a farmer",
			RouteAccess.CanOpen(TestLogin.As(AppRole.Farmer, ""), FarmerTrendsUi.AddLocationHref, null));
	}
}
