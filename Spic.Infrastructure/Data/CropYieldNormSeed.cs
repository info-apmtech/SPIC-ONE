using SPIC.Core.Entities;

namespace Spic.Infrastructure.Data;

/// <summary>
/// Seed of the harvest yield forecast (farmer portal Task 7, migration V41_YieldForecast). Read 2026-11-28 from:
///  - TNAU variety / research-station pages (variety average yields, the basis of "good management");
///  - DES Chennai, Statistical Hand Book of Tamil Nadu 2022-23, table 4.3 (state productivity 2022-23) and 4.20
///    (banana), for the state averages;
///  - FAO Irrigation Water Management training manual 3 (1986), table 5, for the seasonal crop water need, and FAO
///    Irrigation and Drainage Paper 33 (Doorenbos and Kassam 1979), table 1, for the yield response factor Ky.
///
/// The typical yield is a little below the variety averages from trials (farm fields reach less than research
/// plots) and above the state average (which includes poor fields and seasons). The rainfall band is the season
/// rainfall in which the typical yield holds (a model choice, see YieldEstimator.RainFactor), not an FAO figure.
/// Rice is not tabulated in FAO paper 33: Ky 1.10 is assumed (rice is about as drought sensitive as maize).
/// An agronomist should confirm the rows before go-live (docs/farmer-portal.md, Task 7).
/// </summary>
public static class CropYieldNormSeed
{
    private const string I = "Irrigated";
    private const string R = "Rainfed";

    /// <summary>Rows whose Ky is FAO paper 33's own value for the crop.</summary>
    private const string Fao = "FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)";
    /// <summary>Rice: FAO paper 33 has no Ky, so 1.10 is assumed (fix round 1: the farmer-visible source says so).</summary>
    private const string FaoKyAssumed = "FAO water need (Irrigation Water Management manual 3, table 5); Ky assumed (rice is not in FAO I&D paper 33)";
    /// <summary>Black gram and green gram: FAO's bean water need and Ky stand in for the pulses.</summary>
    private const string FaoBean = "FAO water need and Ky of FAO's bean as a pulses proxy (Irrigation Water Management manual 3, table 5; FAO I&D paper 33)";

    /// <summary>Crops whose Ky is assumed / a proxy (the harness checks their sources never claim FAO 33's own Ky).</summary>
    public static readonly IReadOnlySet<string> AssumedKyCrops = new HashSet<string> { "Paddy" };
    public static readonly IReadOnlySet<string> ProxyKyCrops = new HashSet<string> { "Black gram", "Green gram" };
    private const string Des = "DES Statistical Hand Book of Tamil Nadu 2022-23";

    public static readonly CropYieldNorm[] Rows =
    {
        Row(1, "Paddy", I, 5.8m, 4.5m, 7.0m, 5.2m, 800, 1300, 450, 700, 1.10m, "paddy grain",
            "State average 3,500 kg/ha is milled rice; at 67 % milling out-turn about 5.2 t/ha of paddy. Ky assumed (rice is not in FAO paper 33).",
            $"TNAU rice varieties: CO 51 6,623 kg/ha, ADT 45 5,900 kg/ha; {Des}: rice 3,500 kg/ha; {FaoKyAssumed}",
            "https://agritech.tnau.ac.in/agriculture/CO51.html", 10),
        Row(2, "Paddy", R, 3.6m, 2.5m, 4.5m, null, 800, 1300, 450, 700, 1.10m, "paddy grain",
            "Semi-dry / rainfed rice of the Ramanathapuram and Sivaganga tracts. Ky assumed.",
            $"TNAU ARS Paramakudi: ANNA (R) 4 3.7 t/ha, PMK (R) 3 3.2-3.5 t/ha; {FaoKyAssumed}",
            "https://tnau.ac.in/site/ars-paramakudi/?p=379", 11),
        Row(3, "Maize", I, 7.5m, 5.5m, 9.0m, 7.0m, 500, 900, 500, 800, 1.25m, "grain",
            "Hybrids. FAO: a good commercial irrigated grain yield is 6-9 t/ha.",
            $"TNAU maize hybrid COH(M) 8: 7,500-8,000 kg/ha irrigated; {Des}: maize 7,007 kg/ha; {Fao}",
            "https://agritech.tnau.ac.in/agriculture/agri_maize_Characteristics_hybrids%20COhm8.html", 20),
        Row(4, "Maize", R, 5.0m, 3.5m, 6.0m, null, 500, 900, 500, 800, 1.25m, "grain",
            null,
            $"TNAU maize hybrid COH(M) 8: 5,000-5,500 kg/ha rainfed; {Fao}",
            "https://agritech.tnau.ac.in/agriculture/agri_maize_Characteristics_hybrids%20COhm8.html", 21),
        Row(5, "Cotton", I, 2.0m, 1.4m, 2.6m, null, 600, 1100, 700, 1300, 0.85m, "seed cotton (kapas)",
            "The state average is lint (313 kg/ha in 2022-23, about 0.9 t/ha of kapas at 34 % ginning), so it is not shown.",
            $"TNAU cotton varieties (seed cotton, irrigated): CO 14 1,768, CCH 2623 1,800, SVPR 6 2,312 kg/ha; {Des}: cotton 313 kg lint/ha; {Fao}",
            "https://agritech.tnau.ac.in/agriculture/CT%20CO14.html", 30),
        Row(6, "Cotton", R, 1.2m, 0.7m, 1.8m, null, 600, 1100, 700, 1300, 0.85m, "seed cotton (kapas)",
            null,
            $"TNAU cotton: Sumangala 1,200 kg/ha seed cotton in rainfed tracts; {Fao}",
            "https://agritech.tnau.ac.in/CropProduction/Fibre%20crops/fibrecrops_cotton.html", 31),
        Row(7, "Groundnut", I, 2.6m, 1.8m, 3.2m, 2.6m, 500, 800, 500, 700, 0.70m, "dry pods",
            null,
            $"TNAU groundnut varieties (pods, irrigated): VRI 8 2,700 kg/ha, TMV 14 2,360 kg/ha; {Des}: groundnut 2,598 kg/ha pods; {Fao}",
            "https://agritech.tnau.ac.in/agriculture/grounnut%20VRI%208.html", 40),
        Row(8, "Groundnut", R, 2.0m, 1.2m, 2.6m, null, 500, 800, 500, 700, 0.70m, "dry pods",
            null,
            $"TNAU groundnut varieties (pods, rainfed): VRI 8 2,130 kg/ha, TMV 14 2,222 kg/ha; {Fao}",
            "https://agritech.tnau.ac.in/agriculture/grounnut%20VRI%208.html", 41),
        Row(9, "Sugarcane", I, 115m, 85m, 140m, 111m, 800, 1400, 1500, 2500, 1.20m, "cane",
            "Irrigated only (no rainfed row): the water need is far above the rainfall.",
            $"TNAU promising sugarcane varieties: Co 86032 110 t/ha, CoC 24 133 t/ha; {Des}: sugarcane 111 t/ha; {Fao}",
            "https://agritech.tnau.ac.in/crop_improvement/crop_imprv_prom_varieties_sugarcrops.html", 50),
        Row(10, "Banana", I, 50m, 35m, 70m, 39.4m, 800, 1400, 1200, 2200, 1.30m, "fruit bunches",
            "Irrigated only (no rainfed row). Ky: FAO gives 1.2-1.35.",
            $"ICAR-NRCB: Robusta 47.2 t/ha conventional planting, 65.9 t/ha paired rows; TNAU Grand Naine about 30 kg per plant; {Des} table 4.20: banana 39.4 t/ha; {Fao}",
            "https://nrcb.icar.gov.in/oldwebsite/crop-production.php", 60),
        Row(11, "Black gram", I, 0.9m, 0.6m, 1.1m, null, 450, 700, 300, 500, 1.15m, "grain",
            "Water need and Ky of FAO's bean (pulses).",
            $"TNAU Pulses Research Station Vamban: VBN 10 900 kg/ha irrigated, VBN 8 900 kg/ha; {FaoBean}",
            "https://tnau.ac.in/site/prs-vamban/?p=379", 70),
        Row(12, "Black gram", R, 0.75m, 0.45m, 0.9m, 0.68m, 450, 700, 300, 500, 1.15m, "grain",
            "Water need and Ky of FAO's bean (pulses).",
            $"TNAU Pulses Research Station Vamban: VBN 10 750 kg/ha rainfed; {Des}: black gram 675 kg/ha; {FaoBean}",
            "https://tnau.ac.in/site/prs-vamban/?p=379", 71),
        Row(13, "Green gram", I, 1.0m, 0.7m, 1.25m, null, 450, 700, 300, 500, 1.15m, "grain",
            "Water need and Ky of FAO's bean (pulses).",
            $"TNAU green gram VBN 4: 1,251 kg/ha irrigated (EJPB 2021); TNAU green gram 850-1,000 kg/ha; {FaoBean}",
            "https://agritech.tnau.ac.in/agriculture/CropProduction/Pulses/organic%20package%20greengram.html", 80),
        Row(14, "Green gram", R, 0.8m, 0.35m, 1.0m, 0.36m, 450, 700, 300, 500, 1.15m, "grain",
            "Water need and Ky of FAO's bean (pulses). The state average (361 kg/ha) is low: most green gram is a rice-fallow or rainfed catch crop.",
            $"TNAU green gram VBN 4: 936 kg/ha rainfed (EJPB 2021); {Des}: green gram 361 kg/ha; {FaoBean}",
            "https://agritech.tnau.ac.in/agriculture/CropProduction/Pulses/organic%20package%20greengram.html", 81)
    };

    private static CropYieldNorm Row(int id, string crop, string cls, decimal typical, decimal low, decimal high, decimal? stateAvg,
        int rainLow, int rainHigh, int needMin, int needMax, decimal ky, string product, string? notes, string source, string url, int sort) => new()
    {
        Id = id,
        Crop = crop,
        IrrigationClass = cls,
        TypicalTPerHa = typical,
        LowTPerHa = low,
        HighTPerHa = high,
        StateAverageTPerHa = stateAvg,
        RainLowMm = rainLow,
        RainHighMm = rainHigh,
        WaterNeedMinMm = needMin,
        WaterNeedMaxMm = needMax,
        YieldResponseFactor = ky,
        Product = product,
        Unit = "t",
        Notes = notes,
        Source = source,
        SourceUrl = url,
        SortOrder = sort,
        IsActive = true
    };
}
