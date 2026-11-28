using System.Text.Json;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Services;
using Spic.Infrastructure.Data;
using SpicAPI.Controllers;
using Y = SPIC.Core.FarmerPortal.YieldEstimator;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 7: the harvest yield forecast. The factor curves (NDVI monotonic up to the optimum, NPK
/// monotonic with diminishing returns, the rainfall band with the irrigated buffer), the owner's sketch defaults
/// inside the crop's realistic range, range ordering, the confidence width, the factor breakdown and tips,
/// validation, the prefill, the seeded norms and the screens' helpers / menu.
/// </summary>
internal static class FarmerPortalYieldTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal: harvest yield forecast");
		NdviCurve();
		NpkCurve();
		RainCurve();
		Irrigation();
		Sketch();
		Ranges();
		Breakdown();
		Validation();
		Prefill();
		Seed();
		UiAndMenu();
	}

	private static CropYieldNorm Norm(string crop, string cls) =>
		CropYieldNormSeed.Rows.Single(n => n.Crop == crop && n.IrrigationClass == cls);

	private static bool Near(double a, double b, double tol = 0.005) => Math.Abs(a - b) <= tol;

	private static YieldPredictRequest Req(string crop = "Paddy", string method = Y.MethodCanalBorewell, decimal acres = 3.0m,
		double ndvi = 0.72, int rain = 850, int npk = 90, string source = Y.SourceValue) => new()
	{
		Crop = crop, Method = method, AreaAcres = acres, Ndvi = ndvi, NdviSource = source, RainfallMm = rain, NpkPercent = npk
	};

	private static YieldPredictionDto Run(YieldPredictRequest r) =>
		Y.Estimate(Y.PickNorm(CropYieldNormSeed.Rows, r.Crop, r.Method)!, r);

	private static void NdviCurve()
	{
		Check.That("ndvi: 0.35 (sparse) gives about 0.45", Near(Y.NdviFactor(0.35), 0.45));
		Check.That("ndvi: 0.60 gives about 0.85", Near(Y.NdviFactor(0.60), 0.85));
		Check.That("ndvi: 0.72-0.85 is the optimum (1.0)", Near(Y.NdviFactor(0.72), 1) && Near(Y.NdviFactor(0.80), 1) && Near(Y.NdviFactor(0.85), 1) && Near(Y.NdviFactor(0.88), 1));
		Check.That("ndvi: above 0.88 drops slightly (lodging risk)", Y.NdviFactor(0.92) < 1 && Y.NdviFactor(0.92) >= 0.95, Y.NdviFactor(0.92).ToString("0.###"));
		var increasing = true;
		var prev = Y.NdviFactor(0.35);
		for (var v = 0.36; v <= 0.7201; v += 0.01)
		{
			var f = Y.NdviFactor(v);
			if (f <= prev) increasing = false;
			prev = f;
		}
		Check.That("ndvi: strictly increasing from 0.35 to 0.72", increasing);
		var nonDecreasing = true;
		prev = Y.NdviFactor(0.35);
		for (var v = 0.35; v <= 0.8801; v += 0.01)
		{
			var f = Y.NdviFactor(v);
			if (f < prev - 1e-12) nonDecreasing = false;
			prev = f;
		}
		Check.That("ndvi: never falls up to 0.88", nonDecreasing);
		Check.That("ndvi: clamped outside the slider", Near(Y.NdviFactor(0.1), 0.45) && Near(Y.NdviFactor(1.0), Y.NdviFactor(0.92)));
		var lo = Run(Req(ndvi: 0.5)).ExpectedT;
		var mid = Run(Req(ndvi: 0.65)).ExpectedT;
		var top = Run(Req(ndvi: 0.8)).ExpectedT;
		Check.That("ndvi: the forecast rises with NDVI to the optimum", lo < mid && mid < top, $"{lo} {mid} {top}");
	}

	private static void NpkCurve()
	{
		Check.That("npk: 50 % gives 0.80", Near(Y.NpkFactor(50), 0.80));
		Check.That("npk: 75 % gives about 0.93", Near(Y.NpkFactor(75), 0.93, 0.003), Y.NpkFactor(75).ToString("0.####"));
		Check.That("npk: 100 % gives 1.0", Near(Y.NpkFactor(100), 1));
		Check.That("npk: 120 % gives 1.0 (no extra yield)", Near(Y.NpkFactor(120), 1));
		var ok = true;
		for (var p = 55; p <= 100; p += 5)
			if (Y.NpkFactor(p) <= Y.NpkFactor(p - 5)) ok = false;
		Check.That("npk: strictly increasing to 100 %", ok);
		var gains = Enumerable.Range(0, 5).Select(i => Y.NpkFactor(60 + i * 10) - Y.NpkFactor(50 + i * 10)).ToList();
		Check.That("npk: diminishing returns (each 10 % adds less)", gains.Zip(gains.Skip(1)).All(g => g.Second < g.First), string.Join(",", gains.Select(g => g.ToString("0.####"))));
		Check.That("npk: the forecast never falls with more NPK", Enumerable.Range(10, 15).Select(i => Run(Req(npk: i * 5)).ExpectedT).Zip(Enumerable.Range(11, 14).Select(i => Run(Req(npk: i * 5)).ExpectedT)).All(p => p.Second >= p.First));
		Check.That("npk: over 100 % warns about over-application", Run(Req(npk: 120)).Warnings.Any(w => w.Contains("adds cost, not yield")));
		Check.That("npk: 100 % has no warning", Run(Req(npk: 100)).Warnings.Count == 0);
	}

	private static void RainCurve()
	{
		var pi = Norm("Paddy", Y.Irrigated);
		var pr = Norm("Paddy", Y.Rainfed);
		Check.That("rain: inside the band is 1.0", Near(Y.RainFactor(800, pi), 1) && Near(Y.RainFactor(1000, pi), 1) && Near(Y.RainFactor(1300, pi), 1));
		var rainfedLoss = 1 - Y.RainFactor(600, pr);
		var irrigatedLoss = 1 - Y.RainFactor(600, pi);
		Check.That("rain: rainfed deficit follows Ky (paddy 600 mm: 1 - 1.1 x 0.25)", Near(rainfedLoss, 1.1 * 0.25), rainfedLoss.ToString("0.####"));
		Check.That("rain: an irrigated crop feels 30 % of the same deficit", Near(irrigatedLoss, 0.3 * rainfedLoss), $"{irrigatedLoss:0.####} vs {rainfedLoss:0.####}");
		var below = Enumerable.Range(40, 41).Select(i => Y.RainFactor(i * 10, pr)).ToList();
		Check.That("rain: less rain below the band never helps", below.Zip(below.Skip(1)).All(p => p.Second >= p.First));
		Check.That("rain: above the band drops (paddy 1400 mm)", Y.RainFactor(1400, pi) < 1 && Y.RainFactor(1400, pi) > 0.95);
		var maize = Norm("Maize", Y.Irrigated);
		Check.That("rain: heavy rain hurts maize (1400 mm vs band 500-900)", Y.RainFactor(1400, maize) < 0.85 && Y.RainFactor(1400, maize) >= Y.RainExcessFloor);
		Check.That("rain: excess hurts irrigated and rainfed alike", Near(Y.RainFactor(1400, maize), Y.RainFactor(1400, Norm("Maize", Y.Rainfed))));
		Check.That("rain: the floors hold", CropYieldNormSeed.Rows.All(n => Y.RainFactor(Y.RainMin, n) >= Y.RainDeficitFloor && Y.RainFactor(Y.RainMax, n) >= Y.RainExcessFloor));
		Check.That("rain: the rainfed forecast drops faster below the band",
			Run(Req(method: Y.MethodRainfed, rain: 500)).ExpectedT / Run(Req(method: Y.MethodRainfed, rain: 850)).ExpectedT
			< Run(Req(rain: 500)).ExpectedT / Run(Req(rain: 850)).ExpectedT);
	}

	private static void Irrigation()
	{
		Check.That("irrigation: canal / borewell and rainfed are 1.0", Near(Y.IrrigationFactor(Y.MethodCanalBorewell, "Maize"), 1) && Near(Y.IrrigationFactor(Y.MethodRainfed, "Maize"), 1));
		Check.That("irrigation: drip adds 5 %, sprinkler 2 %", Near(Y.IrrigationFactor(Y.MethodDrip, "Maize"), 1.05) && Near(Y.IrrigationFactor(Y.MethodSprinkler, "Cotton"), 1.02));
		Check.That("irrigation: paddy under drip or sprinkler is aerobic (0.90)", Near(Y.IrrigationFactor(Y.MethodDrip, "Paddy"), 0.9) && Near(Y.IrrigationFactor(Y.MethodSprinkler, "Paddy"), 0.9));
		Check.That("irrigation: drip, sprinkler and canal use the Irrigated norm", new[] { Y.MethodCanalBorewell, Y.MethodDrip, Y.MethodSprinkler }
			.All(m => Y.PickNorm(CropYieldNormSeed.Rows, "Maize", m)!.IrrigationClass == Y.Irrigated));
		Check.Equal("irrigation: rainfed uses the Rainfed norm", Y.Rainfed, Y.PickNorm(CropYieldNormSeed.Rows, "maize", Y.MethodRainfed)!.IrrigationClass);
		Check.That("irrigation: no rainfed sugarcane or banana", Y.PickNorm(CropYieldNormSeed.Rows, "Sugarcane", Y.MethodRainfed) is null && Y.PickNorm(CropYieldNormSeed.Rows, "Banana", Y.MethodRainfed) is null);
		Check.Equal("irrigation: the sketch's four methods in order", "canal-borewell,rainfed,drip,sprinkler", string.Join(",", Y.Methods.Select(m => m.Code)));
		Check.Equal("irrigation: sketch label", "Canal / Borewell Irrigated", Y.Methods[0].Label);
	}

	private static void Sketch()
	{
		// The owner's sketch: Paddy, 3 acres, canal, NDVI 0.72, 850 mm, 90 %.
		var r = Run(Req());
		var norm = Norm("Paddy", Y.Irrigated);
		Check.That("sketch: per hectare inside paddy's realistic range", r.PerHectareT >= (double)norm.LowTPerHa && r.PerHectareT <= (double)norm.HighTPerHa, r.PerHectareT.ToString());
		var expectedPerHa = 5.8 * 1.0 * 1.0 * Y.NpkFactor(90) * 1.0;
		Check.That("sketch: per hectare = norm x factors (5.8 x 0.98)", Near(r.PerHectareT, expectedPerHa, 0.006), $"{r.PerHectareT} vs {expectedPerHa:0.###}");
		Check.That("sketch: total = per hectare x acres x 0.4047", Near(r.ExpectedT, expectedPerHa * 3 * 0.4047, 0.006), r.ExpectedT.ToString());
		Check.That("sketch: about 6.9 t (69 quintals)", r.ExpectedT is > 6.8 and < 7.0 && r.ExpectedQuintals is > 68 and < 70, $"{r.ExpectedT} t, {r.ExpectedQuintals} q");
		Check.That("sketch: low <= expected <= high", r.LowT <= r.ExpectedT && r.ExpectedT <= r.HighT);
		Check.Equal("sketch: +/- 12 % with an NDVI value", 12, r.RangePercent);
		Check.Equal("sketch: high confidence", "High confidence", r.ConfidenceLabel);
		Check.Equal("sketch: confidence score 70", 70, r.ConfidenceScore);
		Check.Equal("sketch: per acre", Math.Round(r.PerHectareT * 0.4047, 2), r.PerAcreT);

		foreach (var n in CropYieldNormSeed.Rows)
		{
			var method = n.IrrigationClass == Y.Rainfed ? Y.MethodRainfed : Y.MethodCanalBorewell;
			var f = Y.Estimate(n, Req(n.Crop, method));
			Check.That($"sketch values: {n.Crop} ({n.IrrigationClass}) inside {n.LowTPerHa}-{n.HighTPerHa} t/ha",
				f.PerHectareT >= (double)n.LowTPerHa && f.PerHectareT <= (double)n.HighTPerHa, f.PerHectareT.ToString());
		}
		var best = Run(Req(ndvi: 0.8, npk: 100, rain: 1000));
		Check.That("sketch: best inputs give the norm itself", Near(best.PerHectareT, 5.8));
	}

	private static void Ranges()
	{
		var allOrdered = true;
		var allPositive = true;
		var widthsOk = true;
		var count = 0;
		foreach (var n in CropYieldNormSeed.Rows)
		foreach (var m in Y.Methods.Where(m => m.IrrigationClass == n.IrrigationClass))
		foreach (var ndvi in new[] { 0.35, 0.6, 0.72, 0.92 })
		foreach (var rain in new[] { 400, 850, 1400 })
		foreach (var npk in new[] { 50, 90, 120 })
		foreach (var src in new[] { Y.SourceValue, Y.SourceLook })
		{
			var f = Y.Estimate(n, Req(n.Crop, m.Code, 2.5m, ndvi, rain, npk, src));
			count++;
			if (!(f.LowT <= f.ExpectedT && f.ExpectedT <= f.HighT)) allOrdered = false;
			if (!(f.ExpectedT > 0 && f.PerHectareT > 0 && f.Factors.All(x => x.Value > 0 && x.Value <= 1.05 + 1e-9))) allPositive = false;
			if (f.RangePercent < Y.RangeValue || f.RangePercent > Y.RangeMax) widthsOk = false;
		}
		Check.That($"range: low <= expected <= high in all {count} combinations", allOrdered);
		Check.That("range: every forecast and factor is positive (factors at most 1.05)", allPositive);
		Check.That("range: width stays 12-25 %", widthsOk);

		var look = Run(Req(ndvi: 0.62, source: Y.SourceLook));
		Check.Equal("range: NDVI picked by look widens to 20 %", 20, look.RangePercent);
		Check.Equal("range: by look is medium confidence", "Medium confidence", look.ConfidenceLabel);
		Check.That("range: by look is wider than by value", look.HighT - look.LowT > Run(Req(ndvi: 0.62)).HighT - Run(Req(ndvi: 0.62)).LowT);
		var rough = Run(Req(method: Y.MethodRainfed, ndvi: 0.45, rain: 500, npk: 60, source: Y.SourceLook));
		Check.Equal("range: look + rain outside + low NPK is capped at 25 %", 25, rough.RangePercent);
		Check.Equal("range: capped range is low confidence", "Low confidence", rough.ConfidenceLabel);
		Check.Equal("range: rain outside the band adds 4 %", 16, Run(Req(rain: 500)).RangePercent);
		Check.Equal("range: NPK under 70 % adds 3 %", 15, Run(Req(npk: 65)).RangePercent);
		Check.That("range: confidence reasons explain the width", rough.ConfidenceReasons.Count == 3);
		Check.That("range: confidence bands match the ring (70 / 40)", Y.ConfidenceLabel(70) == "High confidence" && Y.ConfidenceLabel(69) == "Medium confidence"
			&& Y.ConfidenceLabel(40) == "Medium confidence" && Y.ConfidenceLabel(39) == "Low confidence");
	}

	private static void Breakdown()
	{
		var lowNpk = Run(Req(npk: 50));
		Check.Equal("breakdown: low NPK is the biggest loss", "npk", lowNpk.BiggestLoss);
		var npk = lowNpk.Factors.Single(f => f.Code == "npk");
		Check.That("breakdown: loss = what the area gives without that factor's cut", Near(npk.LossT, lowNpk.ExpectedT / npk.Value - lowNpk.ExpectedT, 0.01), $"{npk.LossT}");
		Check.Equal("breakdown: low NPK tip comes first", "npk", lowNpk.Tips[0].Code);
		Check.That("breakdown: the NPK tip names the gain", lowNpk.Tips[0].Text.StartsWith("Applying the full recommended fertilizer (100 %) could add about") && lowNpk.Tips[0].Href == "/farmer/fertilizer");
		var thin = Run(Req(ndvi: 0.45, npk: 95));
		Check.Equal("breakdown: a sparse crop is the biggest loss", "ndvi", thin.BiggestLoss);
		Check.Equal("breakdown: the canopy tip leads", "ndvi", thin.Tips[0].Code);
		Check.That("breakdown: four factors, in order", string.Join(",", thin.Factors.Select(f => f.Code)) == "ndvi,rain,npk,irrigation");
		Check.That("breakdown: at most three tips", CropYieldNormSeed.Rows.All(n => Y.Estimate(n, Req(n.Crop, n.IrrigationClass == Y.Rainfed ? Y.MethodRainfed : Y.MethodCanalBorewell, 2, 0.45, 450, 55)).Tips.Count <= 3));
		var best = Run(Req(ndvi: 0.8, npk: 100, rain: 1000));
		Check.That("breakdown: best inputs have no loss and the keep-it-up tip", best.BiggestLoss is null && best.Tips.Count == 1 && best.Tips[0].Code == "keep");
		var drip = Run(Req("Maize", Y.MethodDrip, ndvi: 0.8, npk: 100, rain: 700));
		Check.That("breakdown: drip shows as an addition", drip.Factors.Single(f => f.Code == "irrigation").LossT < 0 && drip.BiggestLoss is null);
		var canalMaize = Run(Req("Maize", ndvi: 0.8, npk: 100, rain: 700));
		Check.That("breakdown: canal maize at best inputs suggests drip after the keep tip", canalMaize.Tips.Select(t => t.Code).SequenceEqual(new[] { "keep", "drip" }));
		var dry = Run(Req(method: Y.MethodRainfed, rain: 500, ndvi: 0.8, npk: 100));
		Check.That("breakdown: a rainfed rain deficit gives the protective irrigation tip", dry.Tips.Any(t => t.Code == "rain" && t.Text.StartsWith("Low rain costs")));
		var wet = Run(Req("Maize", rain: 1400, ndvi: 0.8, npk: 100));
		Check.That("breakdown: heavy rain gives the drains tip", wet.Tips.Any(t => t.Code == "rain" && t.Text.StartsWith("Heavy rain")));
		Check.That("breakdown: no tip says \"about about\"", CropYieldNormSeed.Rows.SelectMany(n => new[] { 450, 1400 }.Select(rain =>
			Y.Estimate(n, Req(n.Crop, n.IrrigationClass == Y.Rainfed ? Y.MethodRainfed : Y.MethodCanalBorewell, 2, 0.5, rain, 60))))
			.SelectMany(f => f.Tips).All(t => !t.Text.Contains("about about")));
		Check.That("breakdown: very dense canopy warns about lodging", Run(Req(ndvi: 0.92)).Warnings.Any(w => w.Contains("lodge")));
		Check.Equal("amount: under a tonne in kg", "about 120 kg", Y.Amount(0.123));
		Check.Equal("amount: tonnes and quintals", "about 1.4 t (14 quintals)", Y.Amount(1.4));
		Check.Equal("amount: tiny gains are at least 10 kg", "about 10 kg", Y.Amount(0.002));
		Check.Equal("tonnes: sugarcane-size numbers have no decimals", "139", Y.Tonnes(139.36));
		Check.That("json: the result round-trips through the stored JSON", JsonSerializer.Deserialize<YieldPredictionDto>(JsonSerializer.Serialize(thin))!.Factors.Count == 4);
	}

	private static void Validation()
	{
		var norm = Norm("Paddy", Y.Irrigated);
		Check.That("validate: the sketch request is valid", Y.Validate(Req(), norm, true).Count == 0);
		Check.That("validate: area 0 / 1001 refused", Y.Validate(Req(acres: 0m), norm, true).ContainsKey("areaAcres") && Y.Validate(Req(acres: 1001m), norm, true).ContainsKey("areaAcres"));
		Check.That("validate: NDVI outside 0.35-0.92 refused", Y.Validate(Req(ndvi: 0.3), norm, true).ContainsKey("ndvi") && Y.Validate(Req(ndvi: 0.95), norm, true).ContainsKey("ndvi"));
		Check.That("validate: NDVI NaN refused", Y.Validate(Req(ndvi: double.NaN), norm, true).ContainsKey("ndvi"));
		Check.That("validate: rainfall outside 400-1400 refused", Y.Validate(Req(rain: 399), norm, true).ContainsKey("rainfallMm") && Y.Validate(Req(rain: 1401), norm, true).ContainsKey("rainfallMm"));
		Check.That("validate: NPK outside 50-120 refused", Y.Validate(Req(npk: 49), norm, true).ContainsKey("npkPercent") && Y.Validate(Req(npk: 121), norm, true).ContainsKey("npkPercent"));
		Check.That("validate: unknown NDVI source refused", Y.Validate(Req(source: "satellite"), norm, true).ContainsKey("ndviSource"));
		Check.That("validate: unknown method refused", Y.Validate(Req(method: "flood"), null, true).ContainsKey("method"));
		Check.That("validate: unknown crop refused", Y.Validate(Req(crop: "Poppy"), null, false).ContainsKey("crop"));
		var rainfedCane = Y.Validate(Req("Sugarcane", Y.MethodRainfed), null, true);
		Check.That("validate: rainfed sugarcane is explained", rainfedCane.TryGetValue("method", out var msg) && msg.Contains("grown with irrigation"), msg);
		Check.That("validate: null request refused", Y.Validate(null, null, false).Count > 0);
	}

	private static void Prefill()
	{
		Check.Equal("profile: canal is canal / borewell", Y.MethodCanalBorewell, Y.MethodForProfile(FarmerIrrigationType.Canal));
		Check.Equal("profile: borewell is canal / borewell", Y.MethodCanalBorewell, Y.MethodForProfile(FarmerIrrigationType.Borewell));
		Check.Equal("profile: mixed is canal / borewell", Y.MethodCanalBorewell, Y.MethodForProfile(FarmerIrrigationType.Mixed));
		Check.Equal("profile: none is canal / borewell", Y.MethodCanalBorewell, Y.MethodForProfile(null));
		Check.Equal("profile: rainfed", Y.MethodRainfed, Y.MethodForProfile(FarmerIrrigationType.Rainfed));
		Check.Equal("profile: drip", Y.MethodDrip, Y.MethodForProfile(FarmerIrrigationType.Drip));
		Check.Equal("profile: sprinkler", Y.MethodSprinkler, Y.MethodForProfile(FarmerIrrigationType.Sprinkler));

		var norms = CropYieldNormSeed.Rows;
		var p = FarmerYieldController.Prefill(norms, new[] { "Coconut", "banana", "Paddy" }, 4.5m, FarmerIrrigationType.Drip, "Paddy", new DateTime(2026, 11, 28, 4, 0, 0, DateTimeKind.Utc));
		Check.That("prefill: first profile crop with a norm, area and drip from the profile",
			p.Crop == "Banana" && p.CropFromProfile && p.AreaAcres == 4.5m && p.AreaFromProfile && p.Method == Y.MethodDrip && p.MethodFromProfile);
		Check.Equal("prefill: NPK stays 90 % (a run records advice, not what was applied)", 90, p.NpkPercent);
		Check.Equal("prefill: the latest fertilizer advice is passed on", "Paddy", p.LastFertilizerCrop);
		var none = FarmerYieldController.Prefill(norms, Array.Empty<string>(), null, null, null, null);
		Check.That("prefill: no profile gives the sketch (Paddy, 3 acres, canal / borewell)",
			none.Crop == "Paddy" && !none.CropFromProfile && none.AreaAcres == 3.0m && !none.AreaFromProfile && none.Method == Y.MethodCanalBorewell && !none.MethodFromProfile);
		var cane = FarmerYieldController.Prefill(norms, new[] { "Sugarcane" }, 2000m, FarmerIrrigationType.Rainfed, null, null);
		Check.That("prefill: rainfed sugarcane falls back to canal / borewell; an area over 1000 acres is not used",
			cane.Crop == "Sugarcane" && cane.Method == Y.MethodCanalBorewell && !cane.MethodFromProfile && cane.AreaAcres == 3.0m);

		var options = FarmerYieldController.BuildOptions(norms);
		Check.Equal("options: eight crops in seed order", "Paddy,Maize,Cotton,Groundnut,Sugarcane,Banana,Black gram,Green gram", string.Join(",", options.Crops.Select(c => c.Crop)));
		Check.That("options: Irrigated norm listed first", options.Crops.All(c => c.Norms[0].IrrigationClass == Y.Irrigated));
		Check.That("options: sketch sliders", options.Ndvi.Min == 0.35 && options.Ndvi.Max == 0.92 && options.Ndvi.Default == 0.72
			&& options.Rainfall.Min == 400 && options.Rainfall.Max == 1400 && options.Rainfall.Default == 850
			&& options.Npk.Min == 50 && options.Npk.Max == 120 && options.Npk.Default == 90);
		Check.That("options: sparse / medium / dense looks inside the NDVI slider", options.Looks.Select(l => l.Key).SequenceEqual(new[] { "sparse", "medium", "dense" })
			&& options.Looks.All(l => l.Ndvi >= 0.35 && l.Ndvi <= 0.92));
	}

	private static void Seed()
	{
		var rows = CropYieldNormSeed.Rows;
		Check.That("seed: ids unique", rows.Select(r => r.Id).Distinct().Count() == rows.Length);
		Check.That("seed: one row per crop and class", rows.GroupBy(r => (r.Crop, r.IrrigationClass)).All(g => g.Count() == 1));
		Check.That("seed: classes are Irrigated / Rainfed", rows.All(r => r.IrrigationClass is Y.Irrigated or Y.Rainfed));
		Check.That("seed: low < typical < high", rows.All(r => r.LowTPerHa < r.TypicalTPerHa && r.TypicalTPerHa < r.HighTPerHa));
		Check.That("seed: rainfed yields below irrigated", rows.Where(r => r.IrrigationClass == Y.Rainfed).All(r => r.TypicalTPerHa < Norm(r.Crop, Y.Irrigated).TypicalTPerHa));
		Check.That("seed: state average inside the range and not above the typical yield",
			rows.Where(r => r.StateAverageTPerHa is not null).All(r => r.StateAverageTPerHa >= r.LowTPerHa && r.StateAverageTPerHa <= r.TypicalTPerHa));
		Check.That("seed: rainfall bands sit inside the slider", rows.All(r => r.RainLowMm >= Y.RainMin && r.RainHighMm <= Y.RainMax && r.RainLowMm < r.RainHighMm));
		Check.That("seed: water need and Ky are sane", rows.All(r => r.WaterNeedMinMm < r.WaterNeedMaxMm && r.YieldResponseFactor is >= 0.5m and <= 1.5m));
		Check.That("seed: every row names TNAU or ICAR and FAO", rows.All(r => (r.Source.Contains("TNAU") || r.Source.Contains("ICAR")) && r.Source.Contains("FAO")));
		Check.That("seed: every source fits the column and has an https page", rows.All(r => r.Source.Length <= 500 && r.SourceUrl.StartsWith("https://") && (r.Notes?.Length ?? 0) <= 500));
		Check.That("seed: unit is tonnes and a product is named", rows.All(r => r.Unit == "t" && r.Product.Length > 0 && r.Product.Length <= 40));
		foreach (var crop in new[] { "Paddy", "Maize", "Cotton", "Groundnut", "Sugarcane", "Banana", "Black gram", "Green gram" })
			Check.That($"seed: {crop} has an irrigated norm", rows.Any(r => r.Crop == crop && r.IrrigationClass == Y.Irrigated));
		Check.That("seed: crop names match the fertilizer tool's", rows.Select(r => r.Crop).Distinct().All(c => FertilizerAdviceSeed.Norms.Any(n => n.Crop == c) || c == "Sugarcane"));
		Check.That("seed: paddy irrigated about 5.5-6 t/ha (brief)", Norm("Paddy", Y.Irrigated).TypicalTPerHa is >= 5.5m and <= 6.0m);
		Check.That("seed: maize Ky 1.25 (FAO)", Norm("Maize", Y.Irrigated).YieldResponseFactor == 1.25m);
	}

	private static void UiAndMenu()
	{
		var farmer = TestLogin.As(AppRole.Farmer, "");
		Check.Equal("menu: the harvest forecast tile comes fourth", "yield", FarmerFeatureTiles.For(farmer).Skip(3).First().Id);
		Check.That("menu: /farmer/yield and its history open for a farmer", RouteAccess.CanOpen(farmer, "/farmer/yield", null)
			&& RouteAccess.CanOpen(farmer, "/farmer/yield/history", null) && RouteAccess.CanOpen(farmer, "/farmer/yield/history/5", null));
		var more = ShellNavigation.MoreItems(farmer).Select(t => t.Key).ToList();
		Check.That("menu: farmer More sheet offers the harvest forecast", more.Contains(ShellNavigation.FarmerYieldKey), string.Join(",", more));
		Check.That("menu: the harvest forecast is not a phone tab", !ShellNavigation.GetPhoneTabs(farmer).Any(t => t.Key == ShellNavigation.FarmerYieldKey));
		var staff = TestLogin.As(AppRole.MDO, "");
		Check.That("menu: staff never get the harvest forecast", !ShellNavigation.MoreItems(staff).Any(t => t.Key == ShellNavigation.FarmerYieldKey)
			&& !RouteAccess.CanOpen(staff, "/farmer/yield", null));
		Check.Equal("menu: the forecast highlights the farmer home", ShellNavigation.FarmerHomeKey, ShellNavigation.ActiveKey("/farmer/yield/history"));

		var options = FarmerYieldController.BuildOptions(CropYieldNormSeed.Rows);
		var cane = options.Crops.Single(c => c.Crop == "Sugarcane");
		Check.That("ui: rainfed is not offered for sugarcane", !YieldUi.Offers(cane, Y.MethodRainfed) && YieldUi.Offers(cane, Y.MethodDrip));
		Check.Equal("ui: norm for the method's class", Y.Rainfed, YieldUi.NormFor(options.Crops[0], Y.MethodRainfed)!.IrrigationClass);
		Check.Equal("ui: hectares under the area box", "= 1.21 ha", YieldUi.Hectares(3.0m));
		Check.Equal("ui: quintals", "69.2", YieldUi.Quintals(6.92));
		Check.Equal("ui: big quintals", "1,394", YieldUi.Quintals(139.36));
		Check.Equal("ui: NDVI 0.88 mark hides the 0.92 label", "fpy-zone--nomax", YieldUi.ScaleCss(0.35, 0.92, 0.72, 0.88));
		Check.Equal("ui: paddy's 1,300 mm mark hides the 1,400 label", "fpy-zone--nomax", YieldUi.ScaleCss(400, 1400, 800, 1300));
		Check.Equal("ui: pulses' 450 mm mark hides the 400 label", "fpy-zone--nomin", YieldUi.ScaleCss(400, 1400, 450, 700));
		Check.Equal("ui: NPK scale keeps both ends", null, YieldUi.ScaleCss(50, 120, 75, 100));
		Check.Equal("ui: factor percent", 98, YieldUi.Percent(0.982));
		Check.Equal("ui: bar width caps at 100 %", "100%", YieldUi.BarWidth(1.05));
		Check.That("ui: bar colours", YieldUi.FactorCss(1) == "good" && YieldUi.FactorCss(0.95) == "mild" && YieldUi.FactorCss(0.7) == "poor" && YieldUi.FactorCss(1.05) == "plus");
		Check.Equal("ui: loss words", "costs about 120 kg", YieldUi.LossText(0.12));
		Check.Equal("ui: gain words", "adds about 300 kg", YieldUi.LossText(-0.3));
		Check.Equal("ui: no loss", "no loss", YieldUi.LossText(0));
		Check.Equal("ui: look key from the value", "medium", YieldUi.LookKey(options.Looks, 0.62));
		Check.That("ui: NPK hint names the last fertilizer advice", YieldUi.NpkHint("Paddy", new DateTime(2026, 11, 27, 20, 0, 0, DateTimeKind.Utc)).Contains("Paddy, 28 Nov"));
		Check.That("ui: NPK hint without advice", !YieldUi.NpkHint(null, null).Contains("last fertilizer"));
		var r = Run(Req());
		Check.That("ui: formula line", YieldUi.Formula(r).StartsWith("5.8 t/ha x 1.00 vigour x 1.00 rain x 0.98 fertilizer x 1.00 irrigation = "), YieldUi.Formula(r));
		Check.That("ui: disclaimer says it is a model estimate, not a guarantee", YieldUi.Disclaimer.StartsWith("Estimate from crop norms and your inputs, not a guarantee.") && YieldUi.Disclaimer.Contains("model estimate"));
	}
}
