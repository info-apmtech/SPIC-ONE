using Microsoft.Extensions.Configuration;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Services;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Lab;
using SpicAPI.Controllers;
using A = SPIC.Core.FarmerPortal.FertilizerAdvisor;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 4: fertilizer recommendation. Ratings at the thresholds (brief defaults and the lab
/// master), the dose factors (same as the soil report), unit conversion, product conversion (DAP first,
/// its N counted), pH bands, the seeded norms (splits add up, sources present, the brief's checks), the
/// full recommendation, the soil report reader and the screens' helpers / menu.
/// </summary>
internal static class FarmerPortalFertilizerTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal: fertilizer recommendation");
		Ratings();
		Factors();
		Units();
		Products();
		PhBands();
		Seed();
		Recommendation();
		PerPlant();
		Validation();
		SoilReport();
		UiAndMenu();
	}

	// Fix round 1: the farmer tool rates on its own elemental thresholds (not the lab master).
	private static readonly A.Threshold LabN = A.DefaultN;
	private static readonly A.Threshold LabP = A.DefaultP;
	private static readonly A.Threshold LabK = A.DefaultK;
	private static readonly A.Threshold LabPh = A.DefaultPh;

	private static string Lv(decimal v, A.Threshold t) => A.Level(A.Rate(v, t));

	private static void Ratings()
	{
		// The owner's sketch basis: N 280 / 560, P 11 / 22 kg P/ha, K 118 / 280 kg K/ha.
		Check.Equal("rating: N 279 is Low", "Low", Lv(279m, A.DefaultN));
		Check.Equal("rating: N 280 is Medium", "Medium", Lv(280m, A.DefaultN));
		Check.Equal("rating: N 560 is Medium", "Medium", Lv(560m, A.DefaultN));
		Check.Equal("rating: N 561 is High", "High", Lv(561m, A.DefaultN));
		Check.Equal("rating: P 10.9 is Low", "Low", Lv(10.9m, A.DefaultP));
		Check.Equal("rating: P 11 is Medium", "Medium", Lv(11m, A.DefaultP));
		Check.Equal("rating: P 22 is Medium", "Medium", Lv(22m, A.DefaultP));
		Check.Equal("rating: P 22.1 is High", "High", Lv(22.1m, A.DefaultP));
		Check.Equal("rating: K 117 is Low", "Low", Lv(117m, A.DefaultK));
		Check.Equal("rating: K 118 is Medium", "Medium", Lv(118m, A.DefaultK));
		Check.Equal("rating: K 280 is Medium", "Medium", Lv(280m, A.DefaultK));
		Check.Equal("rating: K 281 is High", "High", Lv(281m, A.DefaultK));

		// P2O5 / K2O entry is converted to P / K before rating (x 2.29, x 1.2).
		// Fix round 2: ONE rule (RateAs on the entered value against the exact InBasis thresholds) for the chip and the
		// result, checked through Build (the real path) and the slider's chip at every edge in both units.
		Check.Equal("basis: P2O5 thresholds are exact 25.19 / 50.38", "25.19/50.38",
			$"{A.InBasis(A.DefaultP, "P2O5").LowBelow:0.##}/{A.InBasis(A.DefaultP, "P2O5").HighAbove:0.##}");
		Check.Equal("basis: K2O thresholds are exact 141.6 / 336", "141.6/336",
			$"{A.InBasis(A.DefaultK, "K2O").LowBelow:0.##}/{A.InBasis(A.DefaultK, "K2O").HighAbove:0.##}");
		var rangesForChip = FarmerFertilizerController.Ranges();
		var pRange = rangesForChip.Single(x => x.Code == "P");
		var kRange = rangesForChip.Single(x => x.Code == "K");
		foreach (var (value, basis, expected) in new (decimal, string, string)[]
		{
			(10.99m, "P", "Low"), (11m, "P", "Medium"), (22m, "P", "Medium"), (22.01m, "P", "High"),
			(25.18m, "P2O5", "Low"), (25.19m, "P2O5", "Medium"), (25.2m, "P2O5", "Medium"),
			(50.38m, "P2O5", "Medium"), (50.39m, "P2O5", "High"), (50.4m, "P2O5", "High")
		})
		{
			var r = Build(6, "Black Cotton Soil", 1m, 300m, value, 150m, 7m, pBasis: basis);
			var chip = FertilizerUi.LevelAs((double)value, pRange, basis);
			var shown = FertilizerUi.Shown(pRange, basis);
			var zoneChip = FpUi.ZoneLevel((double)value, shown.LowBelow, shown.HighAbove);
			Check.Equal($"basis (Build): P {value} kg {basis}/ha is {expected}", expected, r.Nutrients[1].Level);
			Check.Equal($"basis (chip = result): P {value} {basis}", r.Nutrients[1].Level, FpUi.LevelLabel(chip));
			Check.Equal($"basis (slider zones = result): P {value} {basis}", r.Nutrients[1].Level, FpUi.LevelLabel(zoneChip));
		}
		foreach (var (value, basis, expected) in new (decimal, string, string)[]
		{
			(117.99m, "K", "Low"), (118m, "K", "Medium"), (280m, "K", "Medium"), (280.01m, "K", "High"),
			(141.59m, "K2O", "Low"), (141.6m, "K2O", "Medium"), (336m, "K2O", "Medium"), (336.01m, "K2O", "High")
		})
		{
			var r = Build(6, "Black Cotton Soil", 1m, 300m, 15m, value, 7m, kBasis: basis);
			var shown = FertilizerUi.Shown(kRange, basis);
			Check.Equal($"basis (Build): K {value} kg {basis}/ha is {expected}", expected, r.Nutrients[2].Level);
			Check.Equal($"basis (chip = result): K {value} {basis}", r.Nutrients[2].Level, FpUi.LevelLabel(FertilizerUi.LevelAs((double)value, kRange, basis)));
			Check.Equal($"basis (slider zones = result): K {value} {basis}", r.Nutrients[2].Level, FpUi.LevelLabel(FpUi.ZoneLevel((double)value, shown.LowBelow, shown.HighAbove)));
		}

		// Toggling keeps the level (ConvertKeepingLevel), so the chip never flips on a unit switch.
		foreach (var v in new[] { 25.18, 25.19, 25.2, 50.38, 50.39, 50.4, 18.0, 40.0 })
		{
			var before = FertilizerUi.LevelAs(v, pRange, "P2O5");
			var asP = FertilizerUi.Convert(v, pRange, "P2O5", "P");
			var back = FertilizerUi.Convert(asP, pRange, "P", "P2O5");
			Check.That($"toggle: P2O5 {v} -> P {asP} -> P2O5 {back} keeps {FpUi.LevelLabel(before)}",
				FertilizerUi.LevelAs(asP, pRange, "P") == before && FertilizerUi.LevelAs(back, pRange, "P2O5") == before);
		}
		Check.Equal("toggle: P2O5 25.18 (Low) becomes P 10.99, not 11.00", 10.99, FertilizerUi.Convert(25.18, pRange, "P2O5", "P"));
		Check.Equal("toggle: P2O5 50.38 (Medium) becomes P 22.00", 22.0, FertilizerUi.Convert(50.38, pRange, "P2O5", "P"));
		Check.Equal("toggle: P 11 (Medium) becomes P2O5 25.19", 25.19, FertilizerUi.Convert(11.0, pRange, "P", "P2O5"));
		Check.Equal("toggle: K 280 (Medium) becomes K2O 336", 336.0, FertilizerUi.Convert(280.0, kRange, "K", "K2O"));
		Check.Equal("basis: blank or P is elemental", 18m, A.ToElemental(18m, null));
		Check.That("basis: P accepts P / P2O5, K accepts K / K2O", A.ValidBasis("P", "P2O5") && A.ValidBasis("P", "P") && A.ValidBasis("K", "K2O")
			&& !A.ValidBasis("P", "K2O") && !A.ValidBasis("K", "P2O5") && A.ValidBasis("K", null));
		Check.That("rating: the farmer tool does not use the lab master", !A.DefaultP.FromLabMaster && A.DefaultP.LowBelow == 11m && A.DefaultK.LowBelow == 118m);

		// Moderate exactly as LabAutoResultEngine: RangeMin <= v < ModerateFrom.
		var moderate = A.DefaultN with { ModerateFrom = 350m };
		Check.Equal("rating: with ModerateFrom, 300 is Moderate", LabResultStatus.Moderate, A.Rate(300m, moderate));
		Check.Equal("rating: with ModerateFrom, 350 is Normal", LabResultStatus.Normal, A.Rate(350m, moderate));
		Check.Equal("rating: Moderate reads Medium", "Medium", A.Level(LabResultStatus.Moderate));
		Check.Equal("rating: pH label below the range is the lab's 'Acidic'", "Acidic", A.LevelLabel(A.Rate(6.4m, LabPh), LabPh));
		Check.Equal("rating: pH label above the range is the lab's 'Alkaline'", "Alkaline", A.LevelLabel(A.Rate(7.6m, LabPh), LabPh));
	}

	private static void Factors()
	{
		var schedule = new LabFertilizerSchedule();
		foreach (var status in new[] { LabResultStatus.Deficient, LabResultStatus.Moderate, LabResultStatus.Normal, LabResultStatus.Excess })
			Check.Equal($"factor: advisor default for {status} equals the soil report's", LabFertilizerSchedule.DefaultFactors[status.ToString()], A.DefaultFactor(status));
		Check.Equal("factor: soil report N Deficient = 1.25 (+25 %)", 1.25m, schedule.FactorFor("N", "Deficient"));
		Check.Equal("factor: soil report K Excess = 0.75 (-25 %)", 0.75m, schedule.FactorFor("K", "Excess"));
		Check.Equal("factor: soil report P Normal = 1.00", 1.00m, schedule.FactorFor("P", "Normal"));
		Check.Equal("factor: N maps to the lab code S-N", "S-N", schedule.ParameterCodeFor("N"));

		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Sas:Lab:DoseFactors:Deficient"] = "1.30",
			["Sas:Lab:NutrientDoseFactors:K:Excess"] = "0.50"
		}).Build();
		var configured = new LabFertilizerSchedule(config);
		Check.Equal("factor: a configured Deficient factor is used", 1.30m, configured.FactorFor("N", "Deficient"));
		Check.Equal("factor: a nutrient's own table wins", 0.50m, configured.FactorFor("K", "Excess"));

		var r = Build(1, "Black Cotton Soil", 1m, 100m, 5m, 50m, 7m, configured);
		Check.Equal("factor: the recommendation follows the configured factor", 1.30m, r.Nutrients[0].Factor);

		Check.Equal("factor text: +25 %", "+25 %", A.AdjustmentText(1.25m));
		Check.Equal("factor text: -25 %", "-25 %", A.AdjustmentText(0.75m));
		Check.Equal("factor text: as recommended", "as recommended", A.AdjustmentText(1m));
		Check.Equal("factor text: +10 %", "+10 %", A.AdjustmentText(1.10m));
	}

	private static void Units()
	{
		Check.Equal("units: kg/ha stays", 300m, A.ToKgPerHa(300m, "kg/ha", out var n1));
		Check.That("units: kg/ha has no note", n1 is null);
		Check.Equal("units: blank unit is kg/ha", 42m, A.ToKgPerHa(42m, null, out _));
		Check.Equal("units: 40 kg/acre = 98.8 kg/ha", 98.8m, A.ToKgPerHa(40m, "kg/acre", out var n2));
		Check.Equal("units: kg/acre note", "converted from kg/acre", n2);
		Check.Equal("units: 10 ppm = 22.4 kg/ha", 22.4m, A.ToKgPerHa(10m, "ppm", out var n3));
		Check.Equal("units: ppm note", "converted from ppm", n3);
		Check.Equal("units: mg/kg is ppm", 112m, A.ToKgPerHa(50m, "mg/kg", out _));
		Check.Equal("units: 'KG / Acre' spelling", 247.1m, A.ToKgPerHa(100m, "KG / Acre", out _));
		Check.That("units: an unknown unit gives null", A.ToKgPerHa(1m, "%", out var n4) is null && n4!.Contains('%'));
		Check.Equal("units: 1 acre = 0.40469 ha", 0.4047m, Math.Round(A.Hectares(1m), 4));

		// SPIC soil report values on the farmer tool's basis: the lab's P (kg/ha, band 22-56) is P2O5.
		Check.Equal("report units: P 18 kg/ha (P2O5) = 7.86 kg P/ha", 7.86m, A.ReportToElemental("P", 18m, "kg/ha", out var r1));
		Check.That("report units: the P note says read as P2O5", r1!.Contains("P2O5"));
		Check.Equal("report units: P 22.9 kg P2O5/ha = 10.0 kg P/ha (all-India Low edge)", 10.00m, A.ReportToElemental("P", 22.9m, "kg/ha", out _));
		Check.Equal("report units: P 10 kg/acre (P2O5) = 10.79 kg P/ha", 10.79m, A.ReportToElemental("P", 10m, "kg/acre", out _));
		Check.Equal("report units: P 5 ppm (Olsen P) = 11.2 kg P/ha, no P2O5 step", 11.2m, A.ReportToElemental("P", 5m, "ppm", out var r2));
		Check.That("report units: the ppm note does not mention P2O5", !r2!.Contains("P2O5"));
		Check.Equal("report units: K 95 kg/ha stays elemental", 95m, A.ReportToElemental("K", 95m, "kg/ha", out var r3));
		Check.That("report units: K kg/ha has no note", r3 is null);
		Check.Equal("report units: K 50 ppm = 112 kg K/ha", 112m, A.ReportToElemental("K", 50m, "ppm", out _));
		Check.Equal("report units: N 120 kg/acre = 296.5 kg/ha", 296.5m, A.ReportToElemental("N", 120m, "kg/acre", out _));
	}

	private static decimal Kg(List<FertilizerProductLineDto> lines, string code) => lines.FirstOrDefault(l => l.Code == code)?.Kg ?? 0m;

	private static void Products()
	{
		// 120 kg N, 60 kg P2O5, 60 kg K2O: DAP first, its 18 % N counted.
		var dap = A.DapRoute(120m, 60m, 60m);
		Check.Equal("products: DAP for 60 kg P2O5 = 130.4 kg", 130.43m, Math.Round(dap["DAP"], 2));
		Check.Equal("products: urea for the N left after DAP = 209.8 kg", 209.83m, Math.Round(dap["Urea"], 2));
		Check.Equal("products: MOP for 60 kg K2O = 100 kg", 100m, dap["MOP"]);
		Check.That("products: no SSP when DAP carries all the P", !dap.ContainsKey("SSP"));
		var nCheck = dap["DAP"] * 0.18m + dap["Urea"] * 0.46m;
		Check.Equal("products: DAP N + urea N = 120 kg", 120m, Math.Round(nCheck, 2));

		var lines = A.Lines(dap, null);
		Check.Equal("products: line order DAP, urea, MOP", "DAP,Urea,MOP", string.Join(",", lines.Select(l => l.Code)));
		Check.Equal("products: DAP 130 kg", 130m, Kg(lines, "DAP"));
		Check.Equal("products: urea 210 kg", 210m, Kg(lines, "Urea"));
		Check.Equal("products: urea bags of 45 kg = 4.7", 4.7m, lines.First(l => l.Code == "Urea").Bags);
		Check.Equal("products: DAP bags of 50 kg = 2.6", 2.6m, lines.First(l => l.Code == "DAP").Bags);
		Check.Equal("products: urea is SPIC Urea", "SPIC Urea", lines.First(l => l.Code == "Urea").SpicName);
		Check.That("products: MOP has no invented SPIC name", lines.First(l => l.Code == "MOP").SpicName is null);

		// More P than DAP can carry without too much N (soybean 20:80): DAP capped at the N, rest SSP.
		var capped = A.DapRoute(20m, 80m, 0m);
		Check.Equal("products: DAP capped by N (20 kg N -> 111.1 kg DAP)", 111.11m, Math.Round(capped["DAP"], 2));
		Check.Equal("products: the rest of P2O5 as SSP (180.6 kg)", 180.56m, Math.Round(capped["SSP"], 2));
		Check.That("products: no urea when DAP gives all the N", !capped.ContainsKey("Urea"));
		Check.That("products: no MOP for 0 K2O", !capped.ContainsKey("MOP"));

		var noN = A.DapRoute(0m, 35m, 110m);
		Check.That("products: P without N goes as SSP only", !noN.ContainsKey("DAP") && Math.Round(noN["SSP"], 2) == 218.75m);

		var ssp = A.SspRoute(120m, 60m, 60m);
		Check.Equal("products (SSP route): SSP 375 kg", 375m, ssp["SSP"]);
		Check.Equal("products (SSP route): urea 260.9 kg", 260.87m, Math.Round(ssp["Urea"], 2));

		var perPlant = A.Lines(new Dictionary<string, decimal> { ["Urea"] = 100m }, 1000);
		Check.Equal("products: grams per plant", 100m, perPlant[0].GramsPerPlant);
		var tiny = A.Lines(new Dictionary<string, decimal> { ["MOP"] = 0.3m }, null);
		Check.Equal("products: under 0.5 kg keeps one decimal", 0.3m, tiny[0].Kg);
	}

	private static void PhBands()
	{
		Check.Equal("pH 5.4 strongly acidic", A.StronglyAcidic, A.PhBandFor(5.4m, LabPh).Band);
		Check.Equal("pH 5.5 acidic", A.Acidic, A.PhBandFor(5.5m, LabPh).Band);
		Check.Equal("pH 6.49 acidic", A.Acidic, A.PhBandFor(6.49m, LabPh).Band);
		Check.Equal("pH 6.5 neutral (lab range)", A.Neutral, A.PhBandFor(6.5m, LabPh).Band);
		Check.Equal("pH 7.5 neutral (lab range)", A.Neutral, A.PhBandFor(7.5m, LabPh).Band);
		Check.Equal("pH 7.6 alkaline", A.Alkaline, A.PhBandFor(7.6m, LabPh).Band);
		Check.Equal("pH 8.5 alkaline", A.Alkaline, A.PhBandFor(8.5m, LabPh).Band);
		Check.Equal("pH 8.6 strongly alkaline", A.StronglyAlkaline, A.PhBandFor(8.6m, LabPh).Band);
		Check.Equal("pH: band label matches the lab's", "Acidic", A.PhBandFor(5.0m, LabPh).LevelLabel);
		Check.That("pH: acidic advice names the lime requirement test, no invented dose",
			A.PhBandFor(5.0m, LabPh).Advice.Any(a => a.Contains("lime requirement test")) && !A.PhBandFor(5.0m, LabPh).Advice.Any(a => a.Contains("t/acre")));
		Check.That("pH: sodic advice names the gypsum requirement test",
			A.PhBandFor(9.0m, LabPh).Advice.Any(a => a.Contains("gypsum requirement test")));
		Check.Equal("pH: neutral needs nothing", 1, A.PhBandFor(7.0m, LabPh).Advice.Count);
		Check.That("pH: alkaline advice does not claim the farmer's report advises gypsum",
			!A.PhBandFor(8.0m, LabPh).Advice.Any(a => a.StartsWith("Your SPIC soil report advises")));
	}

	private static void Seed()
	{
		var norms = FertilizerAdviceSeed.Norms;
		var splits = FertilizerAdviceSeed.Splits;
		Check.That("seed: norm ids unique", norms.Select(n => n.Id).Distinct().Count() == norms.Length);
		Check.That("seed: split ids unique", splits.Select(s => s.Id).Distinct().Count() == splits.Length);
		Check.That("seed: tip ids unique", FertilizerAdviceSeed.Tips.Select(t => t.Id).Distinct().Count() == FertilizerAdviceSeed.Tips.Length);
		foreach (var n in norms)
		{
			var own = splits.Where(s => s.NormId == n.Id).ToList();
			Check.That($"seed: {n.Crop} / {n.Variant} splits add up to {n.N}:{n.P2O5}:{n.K2O}",
				own.Count > 0 && own.Sum(s => s.N) == n.N && own.Sum(s => s.P2O5) == n.P2O5 && own.Sum(s => s.K2O) == n.K2O,
				$"{own.Sum(s => s.N)}:{own.Sum(s => s.P2O5)}:{own.Sum(s => s.K2O)}");
		}
		Check.That("seed: split Seq unique per norm", splits.GroupBy(s => s.NormId).All(g => g.Select(s => s.Seq).Distinct().Count() == g.Count()));
		Check.That("seed: every norm names a TNAU source", norms.All(n => n.Source.StartsWith("TNAU") && n.SourceUrl.StartsWith("https://agritech.tnau.ac.in/")));
		Check.That("seed: every tip names a TNAU source", FertilizerAdviceSeed.Tips.All(t => t.Source.StartsWith("TNAU") && t.SourceUrl.StartsWith("https://agritech.tnau.ac.in/")));
		Check.That("seed: tip categories are Water / Soil / Nutrient", FertilizerAdviceSeed.Tips.All(t => A.TipCategories.Contains(t.Category)));
		Check.That("seed: soil types are known", norms.All(n => n.SoilType is null || A.SoilTypes.Contains(n.SoilType))
			&& FertilizerAdviceSeed.Tips.All(t => t.SoilType is null || A.SoilTypes.Contains(t.SoilType)));
		Check.That("seed: tip crops are seeded crops", FertilizerAdviceSeed.Tips.All(t => t.Crop is null || norms.Any(n => n.Crop == t.Crop)));
		Check.That("seed: per-plant norms have a plant density", norms.Where(n => n.Basis == FertilizerDoseBasis.PerPlant).All(n => n.PlantsPerHectare > 0 && n.Spacing is not null));
		Check.That("seed: every tip that names lime is limited to acid soil (PhMax <= 6.5)",
			FertilizerAdviceSeed.Tips.Where(t => t.Text.Contains("lime", StringComparison.OrdinalIgnoreCase)).All(t => t.PhMax is { } m && m <= 6.5m));
		Check.That("seed: tip pH limits are sane", FertilizerAdviceSeed.Tips.All(t => t.PhMin is null || t.PhMax is null || t.PhMin < t.PhMax));
		Check.That("seed: a crop + variant + soil appears once", norms.GroupBy(n => (n.Crop, n.Variant, n.SoilType)).All(g => g.Count() == 1));

		FertilizerCropNorm N(string crop, string variantStart, string? soil = null) =>
			norms.Single(n => n.Crop == crop && n.Variant.StartsWith(variantStart) && n.SoilType == soil);
		string Dose(FertilizerCropNorm n) => $"{n.N:0.##}:{n.P2O5:0.##}:{n.K2O:0.##}";
		// The brief's examples, checked against the TNAU pages.
		Check.Equal("seed: paddy medium duration 150:50:50", "150:50:50", Dose(N("Paddy", "Medium")));
		Check.Equal("seed: cotton irrigated varieties 80:40:40", "80:40:40", Dose(N("Cotton", "Irrigated varieties")));
		Check.Equal("seed: cotton irrigated hybrids 120:60:60", "120:60:60", Dose(N("Cotton", "Irrigated hybrids")));
		Check.Equal("seed: rainfed cotton 40:20:40", "40:20:40", Dose(N("Cotton", "Rainfed")));
		Check.Equal("seed: groundnut rainfed 10:10:45", "10:10:45", Dose(N("Groundnut", "Rainfed")));
		Check.Equal("seed: maize hybrids 150:75:75 (TNAU nutrient page)", "150:75:75", Dose(N("Maize", "Irrigated hybrids")));
		Check.Equal("seed: rainfed maize on black soil 40:20:0", "40:20:0", Dose(N("Maize", "Rainfed", "Black Cotton Soil")));
		Check.Equal("seed: banana (Robusta) 110:35:330 g per plant", "110:35:330", Dose(N("Banana", "Garden land, varieties")));
		Check.Equal("seed: turmeric 150:60:108", "150:60:108", Dose(N("Turmeric", "Irrigated")));
		Check.That("seed: no sugarcane (TNAU pages disagree)", norms.All(n => n.Crop != "Sugarcane"));
		Check.That("seed: no irrigated groundnut (TNAU pages disagree)", norms.All(n => !(n.Crop == "Groundnut" && n.WaterRegime == FertilizerWaterRegime.Irrigated)));
		var cottonHybridSplits = splits.Where(s => s.NormId == N("Cotton", "Irrigated hybrids").Id).OrderBy(s => s.Seq).ToList();
		Check.Equal("seed: cotton hybrid N in three splits at basal, 40-45 and 60-65 days", "-:40-45:60-65",
			string.Join(":", cottonHybridSplits.Select(s => s.DayFrom is null ? "-" : $"{s.DayFrom}-{s.DayTo}")));
		var rice = splits.Where(s => s.NormId == N("Paddy", "Medium").Id).ToList();
		Check.That("seed: rice P all basal, N and K in four equal splits",
			rice.Count == 4 && rice.Single(s => s.Seq == 1).P2O5 == 50m && rice.All(s => s.N == 37.5m && s.K2O == 12.5m));
	}

	private static FertilizerCropNorm SeedNorm(int id)
	{
		var norm = FertilizerAdviceSeed.Norms.Single(n => n.Id == id);
		return norm;
	}

	private static FertilizerRecommendationDto Build(int normId, string soil, decimal acres, decimal n, decimal p, decimal k, decimal ph,
		LabFertilizerSchedule? schedule = null, int? plants = null, string? pBasis = null, string? kBasis = null)
	{
		schedule ??= new LabFertilizerSchedule();
		var norm = SeedNorm(normId);
		var request = new FertilizerRecommendRequest { NormId = normId, SoilType = soil, AreaAcres = acres, N = n, P = p, K = k, Ph = ph, PlantCount = plants, PBasis = pBasis, KBasis = kBasis };
		return A.Build(new A.Inputs(norm, FertilizerAdviceSeed.Splits.Where(s => s.NormId == normId).ToList(), FertilizerAdviceSeed.Tips,
			LabN, LabP, LabK, LabPh, (code, status) => schedule.FactorFor(code, status.ToString()), request));
	}

	private static void Recommendation()
	{
		// The sketch: cotton on black cotton soil, N 100, P 5, K 50 (all Low), irrigated hybrids, 1 acre.
		var r = Build(6, "Black Cotton Soil", 1m, 100m, 5m, 50m, 8.0m);
		Check.Equal("result: N, P and K are Low", "Low,Low,Low", string.Join(",", r.Nutrients.Select(x => x.Level)));
		Check.Equal("result: each +25 %", "+25 %,+25 %,+25 %", string.Join(",", r.Nutrients.Select(x => x.AdjustmentText)));
		Check.Equal("result: adjusted dose 150:75:75 kg/ha", "150:75:75", string.Join(":", r.Nutrients.Select(x => x.AdjustedDose.ToString("0.##"))));
		Check.Equal("result: need N 60.7 kg/acre", 60.7m, r.Nutrients[0].NeedKgPerAcre);
		Check.Equal("result: need P2O5 30.4 kg/acre", 30.4m, r.Nutrients[1].NeedKgPerAcre);
		Check.Equal("result: total N for 1 acre 60.7 kg", 60.7m, r.Nutrients[0].TotalKg);
		Check.Equal("result: three stages", 3, r.Stages.Count);
		var basal = r.Stages[0];
		Check.Equal("result: basal N 20.2 kg", 20.2m, basal.NKg);
		Check.Equal("result: basal P2O5 30.4 kg", 30.4m, basal.PKg);
		Check.Equal("result: basal DAP 66 kg", 66m, Kg(basal.Products, "DAP"));
		Check.Equal("result: basal urea 18 kg (DAP N counted)", 18m, Kg(basal.Products, "Urea"));
		Check.Equal("result: basal MOP 25 kg", 25m, Kg(basal.Products, "MOP"));
		Check.Equal("result: 2nd top dressing is urea only", "Urea", string.Join(",", r.Stages[2].Products.Select(x => x.Code)));
		Check.Equal("result: season urea total 106 kg (18 + 44 + 44)", 106m, Kg(r.Totals, "Urea"));
		Check.Equal("result: season DAP total 66 kg", 66m, Kg(r.Totals, "DAP"));
		Check.Equal("result: season MOP total 51 kg", 51m, Kg(r.Totals, "MOP"));
		Check.Equal("result: SSP route SSP 190 kg", 190m, Kg(r.SspTotals, "SSP"));
		Check.Equal("result: pH 8.0 is slightly alkaline", A.Alkaline, r.Ph.Band);
		Check.That("result: black soil and heavy-soil cotton water tips", r.WaterTips.Any(t => t.Text.StartsWith("Heavy (black) soil")) && r.SoilTips.Any(t => t.Text.StartsWith("Black soil")));
		Check.That("result: no light-soil tips on black soil", !r.WaterTips.Any(t => t.Text.StartsWith("Light soil")));
		Check.That("result: cotton zinc tip", r.NutrientTips.Any(t => t.Text.Contains("zinc sulphate 50 kg/ha")));
		Check.That("result: crop-specific tips come before soil tips", r.SoilTips.First().Text.StartsWith("Spread 12.5 t/ha"));
		Check.Equal("result: disclaimer", "General guidance from TNAU/ICAR norms. For exact advice take a SPIC soil test or ask your MDO.", r.Disclaimer);
		Check.That("result: summary names crop, soil, statuses and need", r.Summary.Contains("Cotton (Irrigated hybrids") && r.Summary.Contains("Black Cotton Soil")
			&& r.Summary.Contains("N Low, P Low, K Low") && r.Summary.Contains("60.7 : 30.4 : 30.4"));

		// Tips follow the pH: no lime at 8.6, no gypsum-for-alkalinity at 5.0 (red soil groundnut is the risky pair).
		var gnAlk = Build(12, "Red Soil", 1m, 300m, 15m, 150m, 8.6m);
		Check.That("tips: at pH 8.6 no tip mentions lime", gnAlk.SoilTips.Concat(gnAlk.NutrientTips).Concat(gnAlk.WaterTips).All(t => !t.Text.Contains("lime", StringComparison.OrdinalIgnoreCase)));
		Check.That("tips: at pH 8.6 the red soil crust tip is the gypsum one", gnAlk.SoilTips.Any(t => t.Text.Contains("gypsum at 2 t/ha with FYM")));
		Check.That("tips: at pH 8.6 the pH card does not say lime", gnAlk.Ph.Advice.All(a => !a.Contains("lime", StringComparison.OrdinalIgnoreCase)));
		var gnAcid = Build(12, "Red Soil", 1m, 300m, 15m, 150m, 5.0m);
		Check.That("tips: at pH 5.0 no gypsum-for-alkalinity tip", gnAcid.SoilTips.Concat(gnAcid.NutrientTips).Concat(gnAcid.Ph.Advice.Select(a => new FertilizerTipDto { Text = a }))
			.All(t => !t.Text.Contains("alkal", StringComparison.OrdinalIgnoreCase) && !t.Text.Contains("gypsum requirement", StringComparison.OrdinalIgnoreCase)));
		Check.That("tips: at pH 5.0 the lime crust tips show", gnAcid.SoilTips.Any(t => t.Text.Contains("lime or gypsum at 2 t/ha")) && gnAcid.SoilTips.Any(t => t.Text.StartsWith("If the surface crusts, lime")));
		Check.That("tips: at pH 6.5 (neutral) no lime tip", Build(12, "Red Soil", 1m, 300m, 15m, 150m, 6.5m).SoilTips.All(t => !t.Text.Contains("lime", StringComparison.OrdinalIgnoreCase)));
		Check.That("tips: pulses lime tip only below pH 6.0", Build(13, "Red Soil", 1m, 300m, 15m, 150m, 5.9m).SoilTips.Any(t => t.Text.StartsWith("Lime is recommended"))
			&& Build(13, "Red Soil", 1m, 300m, 15m, 150m, 6.0m).SoilTips.All(t => !t.Text.StartsWith("Lime is recommended")));

		// P entered as P2O5 / K as K2O: rated on P / K, shown as entered.
		var asOxide = Build(6, "Black Cotton Soil", 1m, 300m, 18m, 150m, 7m, pBasis: "P2O5", kBasis: "K2O");
		Check.Equal("basis: 18 kg P2O5 = 7.86 kg P, Low", "Low", asOxide.Nutrients[1].Level);
		Check.Equal("basis: the rating value is elemental P", 7.86m, asOxide.Nutrients[1].SoilValue);
		Check.Equal("basis: the entered value and basis are kept", "18 P2O5", $"{asOxide.Nutrients[1].SoilValueEntered:0} {asOxide.Nutrients[1].SoilBasis}");
		Check.Equal("basis: 150 kg K2O = 125 kg K, Medium", "Medium", asOxide.Nutrients[2].Level);
		var asP = Build(6, "Black Cotton Soil", 1m, 300m, 18m, 150m, 7m);
		Check.Equal("basis: 18 kg P (TN soil card Medium) is Medium", "Medium", asP.Nutrients[1].Level);
		Check.Equal("ui: soil line shows both bases", "Soil test 18 kg P2O5/ha (7.9 kg P/ha)", FertilizerUi.SoilLine(asOxide.Nutrients[1]));
		Check.Equal("ui: soil line for elemental P", "Soil test 18 kg P/ha", FertilizerUi.SoilLine(asP.Nutrients[1]));

		// Medium / High.
		var mixed = Build(5, "Red Soil", 2m, 300m, 30m, 300m, 6.0m);
		Check.Equal("result: Medium N, High P, High K", "Medium,High,High", string.Join(",", mixed.Nutrients.Select(x => x.Level)));
		Check.Equal("result: High P is -25 % (40 -> 30)", 30m, mixed.Nutrients[1].AdjustedDose);
		Check.Equal("result: Medium N unchanged (80)", 80m, mixed.Nutrients[0].AdjustedDose);
		Check.Equal("result: pH 6.0 acidic", A.Acidic, mixed.Ph.Band);
		Check.That("result: light soil cotton schedule on red soil", mixed.WaterTips.Any(t => t.Text.StartsWith("Light soil")));

		// Rainfed: no irrigation schedules.
		var rainfed = Build(7, "Black Cotton Soil", 1m, 100m, 5m, 50m, 7m);
		Check.That("result: rainfed cotton gets no irrigated water tips", rainfed.WaterTips.All(t => !t.Text.Contains("Irrigate right after sowing") && !t.Text.StartsWith("Heavy (black)")));
		Check.Equal("result: rainfed cotton one stage", 1, rainfed.Stages.Count);

		// Acidic laterite paddy, 0.5 acre.
		var acid = Build(1, "Laterite Soil", 0.5m, 200m, 10m, 100m, 5.2m);
		Check.Equal("result: pH 5.2 strongly acidic", A.StronglyAcidic, acid.Ph.Band);
		Check.That("result: laterite tips", acid.SoilTips.Any(t => t.Text.StartsWith("Laterite soils")));
		Check.Equal("result: paddy four stages", 4, acid.Stages.Count);
		Check.Equal("result: paddy stage days", "At planting|50-55 days after sowing|70-75 days after sowing|100-105 days after sowing",
			string.Join("|", acid.Stages.Select(s => s.When)));
	}

	private static void PerPlant()
	{
		var banana = Build(25, "Alluvial Soil", 1m, 300m, 30m, 150m, 7m);
		Check.Equal("per plant: 1 acre of Robusta = 1,246 plants at 1.8 x 1.8 m", 1246, banana.PlantCount);
		Check.That("per plant: counted from spacing", banana.PlantCountFromSpacing);
		Check.Equal("per plant: total N = 110 g x 1,246 = 137.1 kg", 137.1m, banana.Nutrients[0].TotalKg);
		Check.Equal("per plant: N need 137.1 kg/acre", 137.1m, banana.Nutrients[0].NeedKgPerAcre);
		Check.That("per plant: grams per plant on every product line", banana.Stages.All(s => s.Products.All(p => p.GramsPerPlant > 0)));
		Check.Equal("per plant: banana stages are named months", "3rd month after planting", banana.Stages[0].When);

		var own = Build(25, "Alluvial Soil", 1m, 300m, 30m, 150m, 7m, plants: 1000);
		Check.Equal("per plant: the farmer's own count wins", 1000, own.PlantCount);
		Check.Equal("per plant: N for 1000 plants = 110 kg", 110m, own.Nutrients[0].TotalKg);
		Check.Equal("per plant: K for 1000 plants = 330 kg, MOP 550 kg", 550m, Kg(own.Totals, "MOP"));

		var coconut = Build(27, "Coastal Saline", 2m, 300m, 30m, 150m, 7m);
		Check.Equal("per plant: 2 acres of coconut = 142 palms", 142, coconut.PlantCount);
		Check.Equal("per plant: coconut two splits", "June - July|December - January", string.Join("|", coconut.Stages.Select(s => s.When)));
	}

	private static void Validation()
	{
		var norm = SeedNorm(6);
		FertilizerRecommendRequest Ok() => new() { NormId = 6, SoilType = "Black Cotton Soil", AreaAcres = 1m, N = 100m, P = 5m, K = 50m, Ph = 7m };
		Check.Equal("validate: a good request", 0, A.Validate(Ok(), norm).Count);
		Check.That("validate: unknown norm", A.Validate(Ok(), null).ContainsKey("normId"));
		var r = Ok(); r.SoilType = "Moon dust";
		Check.That("validate: unknown soil type", A.Validate(r, norm).ContainsKey("soilType"));
		r = Ok(); r.AreaAcres = 0.05m;
		Check.That("validate: area below 0.1 acre", A.Validate(r, norm).ContainsKey("areaAcres"));
		r = Ok(); r.AreaAcres = 0.1m;
		Check.Equal("validate: 0.1 acre is fine", 0, A.Validate(r, norm).Count);
		r = Ok(); r.AreaAcres = 1001m;
		Check.That("validate: over 1000 acres", A.Validate(r, norm).ContainsKey("areaAcres"));
		r = Ok(); r.Ph = 2.9m;
		Check.That("validate: pH below 3", A.Validate(r, norm).ContainsKey("ph"));
		r = Ok(); r.N = -1m;
		Check.That("validate: negative N", A.Validate(r, norm).ContainsKey("n"));
		r = Ok(); r.PlantCount = 0;
		Check.That("validate: zero plants", A.Validate(r, norm).ContainsKey("plantCount"));
		var maizeBlack = SeedNorm(10);
		r = Ok(); r.NormId = 10; r.SoilType = "Red Soil";
		Check.That("validate: a soil-specific norm refuses another soil", A.Validate(r, maizeBlack).ContainsKey("soilType"));
		r = Ok(); r.PBasis = "K2O";
		Check.That("validate: a wrong P unit", A.Validate(r, norm).ContainsKey("pBasis"));
		r = Ok(); r.PBasis = "P2O5"; r.KBasis = "K2O";
		Check.Equal("validate: P2O5 and K2O are fine", 0, A.Validate(r, norm).Count);
		var inactive = new FertilizerCropNorm { Id = 99, IsActive = false };
		Check.That("validate: inactive norm", A.Validate(Ok(), inactive).ContainsKey("normId"));
	}

	private static void SoilReport()
	{
		var rows = new List<FarmerFertilizerController.SoilReportRow>
		{
			new("S-PH", SampleType.Soil, "pH", "7.0", "-"),
			new("W-PH", SampleType.Water, "pH", "8.2", "-"),
			new("S-N", SampleType.Soil, "Nitrogen", "120", "kg/acre"),
			new("S-P", SampleType.Soil, "Phosphorus", "10", "ppm"),
			new("S-K", SampleType.Soil, "Potassium", "abc", "kg/ha")
		};
		var notes = new List<string>();
		Check.Equal("report: soil pH, not water pH", 7.0, FarmerFertilizerController.ReadValue(rows, "S-PH", "pH", "pH", null, notes, out _));
		Check.Equal("report: N in kg/acre converted to kg/ha", 296.5, FarmerFertilizerController.ReadValue(rows, "S-N", "Nitrogen", "Nitrogen", "N", notes, out _));
		Check.Equal("report: P in ppm (Olsen P) converted to kg P/ha", 22.4, FarmerFertilizerController.ReadValue(rows, "S-P", "Phosphorus", "Phosphorus", "P", notes, out _));
		Check.That("report: an unreadable K is skipped with a note", FarmerFertilizerController.ReadValue(rows, "S-K", "Potassium", "Potassium", "K", notes, out _) is null
			&& notes.Any(n => n.StartsWith("Potassium")));
		Check.That("report: conversion notes for the farmer", notes.Any(n => n.Contains("converted from kg/acre")) && notes.Any(n => n.Contains("converted from ppm")));

		var v1 = new List<FarmerFertilizerController.SoilReportRow> { new(null, null, "Nitrogen", "250", "kg/ha") };
		Check.Equal("report: v1 rows without a master link match by name", 250.0, FarmerFertilizerController.ReadValue(v1, "S-N", "Nitrogen", "Nitrogen", "N", new List<string>(), out _));

		// The SPIC lab's own report: P 18 kg/ha is P2O5 -> 7.9 kg P/ha, shown on the P2O5 basis; a missing K is listed.
		var spic = new List<FarmerFertilizerController.SoilReportRow>
		{
			new("S-PH", SampleType.Soil, "pH", "8.1", "-"),
			new("S-N", SampleType.Soil, "Nitrogen", "180", "kg/ha"),
			new("S-P", SampleType.Soil, "Phosphorus", "18", "kg/ha")
		};
		var dto = new FertilizerSoilReportDto { Available = true };
		FarmerFertilizerController.Fill(dto, spic, "S-N", "S-P", "S-K");
		Check.Equal("report: SPIC P 18 kg/ha read as P2O5 = 7.86 kg P/ha", 7.86, dto.P);
		Check.Equal("report: the report's own P2O5 number is passed on (18)", 18.0, dto.PAsReported);
		Check.Equal("report: the P basis to show is P2O5", "P2O5", dto.ReportPBasis);
		Check.Equal("report: N stays 180", 180.0, dto.N);
		Check.That("report: K missing is listed, not invented", dto.K is null && dto.Missing.SequenceEqual(new[] { "K" }));
		Check.That("report: a note tells the farmer to enter K", dto.Notes.Any(n => n.StartsWith("Potassium is not in your report or could not be read")));
		Check.That("report: no 'converted' note for the lab's own P2O5 value", !dto.Notes.Any(n => n.StartsWith("Phosphorus")));
		Check.That("report: still available with three values", dto.Available);
		Check.That("ui: the strip names the missing value", FertilizerUi.ReportLine(new FertilizerSoilReportDto { ReportCode = "R1", Missing = new() { "K" } }).Contains("Your report has no usable potassium"));
		var empty = new FertilizerSoilReportDto { Available = true };
		FarmerFertilizerController.Fill(empty, new List<FarmerFertilizerController.SoilReportRow>(), "S-N", "S-P", "S-K");
		Check.That("report: nothing usable -> not available", !empty.Available && empty.Message is not null);

		// Slider ranges: the owner's sketch, with P2O5 / K2O views.
		var ranges = FarmerFertilizerController.Ranges();
		var rp = ranges.Single(x => x.Code == "P");
		var rk = ranges.Single(x => x.Code == "K");
		Check.Equal("range: P 0-60 kg P/ha, Low < 11, High > 22", "0-60 11/22", $"{rp.Min}-{rp.Max} {rp.LowBelow}/{rp.HighAbove}");
		Check.Equal("range: K 0-500 kg K/ha, Low < 118, High > 280", "0-500 118/280", $"{rk.Min}-{rk.Max} {rk.LowBelow}/{rk.HighAbove}");
		Check.Equal("range: N 0-800 default 100", "0-800 100", $"{ranges[0].Min}-{ranges[0].Max} {ranges[0].Default}");
		Check.Equal("range: pH default 7.0", 7.0, ranges[3].Default);
		Check.That("range: P switches to P2O5 x 2.29, K to K2O x 1.2", rp.AltBasis == "P2O5" && rp.AltFactor == 2.29 && rk.AltBasis == "K2O" && rk.AltFactor == 1.2);
		Check.That("range: never the lab master", ranges.All(x => !x.FromLabMaster));
		var p2o5 = FertilizerUi.Shown(rp, "P2O5");
		Check.Equal("ui: P2O5 view zones 25.19 / 50.38 (the server's), max 140", "25.19/50.38 140", $"{p2o5.LowBelow}/{p2o5.HighAbove} {p2o5.Max}");
		Check.Equal("ui: P2O5 hint is exact", "Low below 25.19, Medium 25.19 - 50.38, High above 50.38", FertilizerUi.Thresholds(p2o5));
		Check.Equal("ui: P2O5 view label and unit", "Available Phosphorus (P2O5) kg/ha", $"{p2o5.Label} {p2o5.Unit}");
		var k2o = FertilizerUi.Shown(rk, "K2O");
		Check.Equal("ui: K2O view zones 141.6 / 336, max 600", "141.6/336 600", $"{k2o.LowBelow}/{k2o.HighAbove} {k2o.Max}");
		Check.Equal("ui: P view label", "Available Phosphorus (P) kg/ha", $"{FertilizerUi.Shown(rp, "P").Label} {FertilizerUi.Shown(rp, "P").Unit}");
		Check.Equal("ui: switching 18 kg P2O5 to P gives 7.86", 7.86, FertilizerUi.Convert(18, rp, "P2O5", "P"));
		Check.Equal("ui: switching back gives 18 again", 18.0, FertilizerUi.Convert(FertilizerUi.Convert(18, rp, "P2O5", "P"), rp, "P", "P2O5"));
		Check.Equal("ui: K 125 to K2O = 150", 150.0, FertilizerUi.Convert(125, rk, "K", "K2O"));
		Check.Equal("ui: an old run without a basis shows its rating value", "Soil test 12.5 kg P/ha",
			FertilizerUi.SoilLine(new FertilizerNutrientResultDto { Code = "P", SoilValue = 12.5m }));
	}

	private static void UiAndMenu()
	{
		var maize = new FertilizerCropOptionDto
		{
			Crop = "Maize",
			Variants = FertilizerAdviceSeed.Norms.Where(n => n.Crop == "Maize")
				.Select(n => new FertilizerVariantOptionDto { NormId = n.Id, Variant = n.Variant, SoilType = n.SoilType }).ToList()
		};
		Check.Equal("ui: maize variant names once each", "Irrigated varieties,Irrigated hybrids,Rainfed", string.Join(",", FertilizerUi.VariantNames(maize)));
		Check.Equal("ui: rainfed maize on black soil resolves to the Vertisol norm", 10, FertilizerUi.Resolve(maize, "Rainfed", "Black Cotton Soil")?.NormId);
		Check.Equal("ui: rainfed maize on red soil resolves to the Alfisol norm", 11, FertilizerUi.Resolve(maize, "Rainfed", "Red Soil")?.NormId);
		Check.That("ui: rainfed maize on alluvial soil has no norm", FertilizerUi.Resolve(maize, "Rainfed", "Alluvial Soil") is null);
		Check.Equal("ui: irrigated maize fits every soil", 9, FertilizerUi.Resolve(maize, "Irrigated hybrids", "Alluvial Soil")?.NormId);
		Check.Equal("ui: soils of rainfed maize", "Black Cotton Soil or Red Soil", FertilizerUi.SoilsFor(maize, "Rainfed"));
		Check.Equal("ui: profile crop 'rice' picks Paddy", "Paddy", FertilizerUi.ProfileCrop(new[] { "Rice" }, new[] { new FertilizerCropOptionDto { Crop = "Paddy" } }));
		Check.Equal("ui: profile crop match ignores case", "Cotton", FertilizerUi.ProfileCrop(new[] { "Mango", "cotton" }, new[] { new FertilizerCropOptionDto { Crop = "Cotton" } }));
		Check.Equal("ui: bags text", "1.3 bags of 50 kg", FertilizerUi.Bags(1.3m, 50m));
		Check.Equal("ui: one bag", "1 bag of 45 kg", FertilizerUi.Bags(1m, 45m));
		Check.Equal("ui: level Low", FpLevel.Low, FertilizerUi.Level("Low"));
		Check.Equal("ui: history time in IST", "28 Nov 2026, 3:05 pm", FertilizerUi.When(new DateTime(2026, 11, 28, 9, 35, 0, DateTimeKind.Utc)));
		Check.Equal("ui: acres", "2.5 acres", FertilizerUi.Acres(2.5m));

		var farmer = TestLogin.As(AppRole.Farmer, "");
		Check.Equal("menu: the fertilizer tile comes first", "fertilizer", FarmerFeatureTiles.For(farmer).First().Id);
		Check.That("menu: the fertilizer tile opens for a farmer", RouteAccess.CanOpen(farmer, "/farmer/fertilizer", null));
		Check.That("menu: history opens for a farmer", RouteAccess.CanOpen(farmer, "/farmer/fertilizer/history", null)
			&& RouteAccess.CanOpen(farmer, "/farmer/fertilizer/history/12", null));
		var more = ShellNavigation.MoreItems(farmer).Select(t => t.Key).ToList();
		Check.That("menu: farmer More sheet offers Fertilizer advice", more.Contains(ShellNavigation.FarmerFertilizerKey));
		Check.That("menu: Fertilizer advice is not a phone tab", !ShellNavigation.GetPhoneTabs(farmer).Any(t => t.Key == ShellNavigation.FarmerFertilizerKey));
		var staff = TestLogin.As(AppRole.MDO, "");
		Check.That("menu: staff never get Fertilizer advice", !ShellNavigation.MoreItems(staff).Any(t => t.Key == ShellNavigation.FarmerFertilizerKey)
			&& !RouteAccess.CanOpen(staff, "/farmer/fertilizer", null));
	}
}
