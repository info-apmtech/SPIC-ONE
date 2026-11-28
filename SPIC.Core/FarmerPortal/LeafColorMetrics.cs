namespace SPIC.Core.FarmerPortal;

/// <summary>What one pixel of a leaf photo is, by its colour.</summary>
public enum LeafPixelClass
{
    /// <summary>Not leaf: white / grey / black (low saturation), too dark, or a hue no leaf has (blue, purple, bright red).</summary>
    Background = 0,
    /// <summary>Hue 70-170 deg.</summary>
    Green = 1,
    /// <summary>Hue 40-70 deg: chlorosis.</summary>
    Yellow = 2,
    /// <summary>Hue below 40 deg (orange-brown, dark red-brown): dry, necrotic tissue.</summary>
    Brown = 3
}

/// <summary>
/// The colour measurements of one leaf photo (farmer portal Task 6). Shares are 0-1 of the LEAF pixels, except
/// <see cref="LeafShare"/> (of all pixels). Stored as JSON on the check and sent to Claude as a hint.
/// </summary>
public sealed class LeafMetrics
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int LeafPixels { get; set; }
    /// <summary>Leaf pixels / all pixels.</summary>
    public double LeafShare { get; set; }
    /// <summary>Hue of the mean leaf colour, degrees (yellow 60, green 120).</summary>
    public double MeanHue { get; set; }
    /// <summary>Mean G / (R + G + B) of the leaf pixels.</summary>
    public double GreenRatio { get; set; }
    /// <summary>Brightness (HSV value) of the mean leaf colour, 0-1.</summary>
    public double MeanValue { get; set; }
    public double GreenShare { get; set; }
    public double YellowShare { get; set; }
    public double BrownShare { get; set; }
    /// <summary>Brown share of the leaf's edge band (pixels near the leaf's own boundary, not the photo edge).</summary>
    public double MarginBrownShare { get; set; }
    /// <summary>Brown share of the leaf inside the edge band.</summary>
    public double InteriorBrownShare { get; set; }
    /// <summary>Of the small blocks with yellow in them, the share that also hold green (green veins with yellow
    /// between them give a high value; a leaf yellow all over, or yellow in one big patch, a low one).</summary>
    public double MixedBlockShare { get; set; }
    /// <summary>1 - MixedBlockShare when the leaf has yellow; 1 when it has none.</summary>
    public double Uniformity { get; set; }
    /// <summary>Leaf colour chart style score 1-6 (darker green = higher), from the mean leaf colour.</summary>
    public int LccScore { get; set; }
    /// <summary>none | uniform | interveinal | patchy | margin | spots (<see cref="LeafColorAnalyzer.PatternOf"/>).</summary>
    public string Pattern { get; set; } = "none";
}

/// <summary>
/// Pure colour analysis of a small RGB bitmap (farmer portal Task 6; the API decodes and shrinks the photo to at most
/// 256 px first). Background pixels are left out (saturation below 0.18, value below 0.12, or a non-leaf hue), so a
/// leaf on a plain sheet or hand is measured on its own. Pinned by the harness on synthetic bitmaps.
/// </summary>
public static class LeafColorAnalyzer
{
    public const double MinSaturation = 0.18;
    public const double MinValue = 0.12;
    public const double YellowHueFrom = 40;
    public const double GreenHueFrom = 70;
    public const double GreenHueTo = 170;

    /// <summary>A photo with less leaf than this (of all pixels) is "not a leaf / too far away".</summary>
    public const double MinLeafShare = 0.15;
    /// <summary>Below this the API does not even ask Claude (nothing plant-coloured in the photo).</summary>
    public const double NoLeafShare = 0.05;

    /// <summary>HSV of an 8-bit RGB colour: hue 0-360 deg, saturation and value 0-1.</summary>
    public static (double H, double S, double V) Hsv(byte r, byte g, byte b)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var d = max - min;
        double h = 0;
        if (d > 1e-9)
        {
            if (max == rf) h = 60 * (((gf - bf) / d) % 6);
            else if (max == gf) h = 60 * (((bf - rf) / d) + 2);
            else h = 60 * (((rf - gf) / d) + 4);
        }
        if (h < 0) h += 360;
        return (h, max <= 0 ? 0 : d / max, max);
    }

    public static LeafPixelClass Classify(byte r, byte g, byte b)
    {
        var (h, s, v) = Hsv(r, g, b);
        if (v < MinValue || s < MinSaturation) return LeafPixelClass.Background;
        if (h >= GreenHueFrom && h <= GreenHueTo) return LeafPixelClass.Green;
        if (h >= YellowHueFrom && h < GreenHueFrom) return LeafPixelClass.Yellow;
        // orange-brown, or a dark red-brown; a bright red / pink (flower, cloth) is not leaf
        if (h >= 10 && h < YellowHueFrom) return LeafPixelClass.Brown;
        if ((h < 10 || h >= 345) && v < 0.6) return LeafPixelClass.Brown;
        return LeafPixelClass.Background;
    }

    /// <summary>
    /// Leaf colour chart style score 1-6 from the mean leaf colour (the IRRI LCC idea: darker green = more nitrogen).
    /// Mean hue below 55 deg = 1; otherwise by brightness: 0.78+ = 1, 0.68+ = 2, 0.58+ = 3, 0.48+ = 4, 0.38+ = 5, darker = 6;
    /// a yellow-green mean (hue below 75 deg) is at most 2. Not calibrated against a real chart: daylight and the camera
    /// change brightness, so it is a hint.
    /// </summary>
    public static int LccScore(double meanHue, double meanValue)
    {
        if (meanHue < 55) return 1;
        var score = meanValue >= 0.78 ? 1
            : meanValue >= 0.68 ? 2
            : meanValue >= 0.58 ? 3
            : meanValue >= 0.48 ? 4
            : meanValue >= 0.38 ? 5
            : 6;
        return meanHue < 75 ? Math.Min(score, 2) : score;
    }

    /// <summary>
    /// The chlorosis pattern hint. none: yellow under 8 % and brown under 5 %. margin: brown at least 5 %, the edge band at
    /// least 25 % brown and at least twice the inside. Then with yellow at least 8 %: interveinal when at least 45 % of the
    /// yellow blocks also hold green, uniform when the leaf is at least 60 % yellow, else patchy. Brown only: spots.
    /// </summary>
    public static string PatternOf(LeafMetrics m)
    {
        if (m.YellowShare < 0.08 && m.BrownShare < 0.05) return "none";
        if (m.BrownShare >= 0.05 && m.MarginBrownShare >= 0.25 && m.MarginBrownShare >= 2 * m.InteriorBrownShare) return "margin";
        if (m.YellowShare >= 0.08)
        {
            if (m.MixedBlockShare >= 0.45) return "interveinal";
            return m.YellowShare >= 0.6 ? "uniform" : "patchy";
        }
        return "spots";
    }

    /// <summary>
    /// Measures an RGB bitmap (3 bytes per pixel, row by row). Throws for a buffer that does not match the size.
    /// </summary>
    public static LeafMetrics Analyze(byte[] rgb, int width, int height)
    {
        if (width <= 0 || height <= 0 || rgb is null || rgb.Length < width * height * 3)
            throw new ArgumentException("The pixel buffer does not match the size.");

        var n = width * height;
        var cls = new LeafPixelClass[n];
        int leaf = 0, green = 0, yellow = 0, brown = 0;
        double sumR = 0, sumG = 0, sumB = 0, sumRatio = 0;
        for (var i = 0; i < n; i++)
        {
            byte r = rgb[i * 3], g = rgb[i * 3 + 1], b = rgb[i * 3 + 2];
            var c = Classify(r, g, b);
            cls[i] = c;
            if (c == LeafPixelClass.Background) continue;
            leaf++;
            sumR += r; sumG += g; sumB += b;
            var total = r + g + b;
            sumRatio += total == 0 ? 0 : (double)g / total;
            if (c == LeafPixelClass.Green) green++;
            else if (c == LeafPixelClass.Yellow) yellow++;
            else brown++;
        }

        var m = new LeafMetrics { Width = width, Height = height, LeafPixels = leaf, LeafShare = Round((double)leaf / n) };
        if (leaf == 0)
        {
            m.Uniformity = 1;
            m.LccScore = 1;
            m.Pattern = "none";
            return m;
        }

        var mean = Hsv((byte)Math.Round(sumR / leaf), (byte)Math.Round(sumG / leaf), (byte)Math.Round(sumB / leaf));
        m.MeanHue = Math.Round(mean.H, 1);
        m.MeanValue = Round(mean.V);
        m.GreenRatio = Round(sumRatio / leaf);
        m.GreenShare = Round((double)green / leaf);
        m.YellowShare = Round((double)yellow / leaf);
        m.BrownShare = Round((double)brown / leaf);
        m.LccScore = LccScore(mean.H, mean.V);

        // ---- edge band: city-block distance to the nearest background pixel (the photo border does not count,
        //      so a leaf that fills the frame has no edge band) ----
        var dist = Distances(cls, width, height);
        var band = Math.Max(2, (int)Math.Round(0.06 * Math.Sqrt(leaf)));
        int bandPx = 0, bandBrown = 0, inPx = 0, inBrown = 0;
        for (var i = 0; i < n; i++)
        {
            if (cls[i] == LeafPixelClass.Background) continue;
            var isBrown = cls[i] == LeafPixelClass.Brown;
            if (dist[i] <= band) { bandPx++; if (isBrown) bandBrown++; }
            else { inPx++; if (isBrown) inBrown++; }
        }
        m.MarginBrownShare = bandPx == 0 ? 0 : Round((double)bandBrown / bandPx);
        m.InteriorBrownShare = inPx == 0 ? 0 : Round((double)inBrown / inPx);

        // ---- blocks: green + yellow together in one small block = green veins with yellow between ----
        var bs = Math.Max(4, Math.Min(width, height) / 32);
        int yellowBlocks = 0, mixedBlocks = 0;
        for (var by = 0; by < height; by += bs)
        for (var bx = 0; bx < width; bx += bs)
        {
            int bl = 0, bg = 0, byl = 0, all = 0;
            for (var y = by; y < Math.Min(by + bs, height); y++)
            for (var x = bx; x < Math.Min(bx + bs, width); x++)
            {
                all++;
                var c = cls[y * width + x];
                if (c == LeafPixelClass.Background) continue;
                bl++;
                if (c == LeafPixelClass.Green) bg++;
                else if (c == LeafPixelClass.Yellow) byl++;
            }
            if (bl * 2 < all || byl < 0.15 * bl) continue;
            yellowBlocks++;
            if (bg >= 0.2 * bl && byl >= 0.2 * bl) mixedBlocks++;
        }
        m.MixedBlockShare = yellowBlocks == 0 ? 0 : Round((double)mixedBlocks / yellowBlocks);
        m.Uniformity = m.YellowShare < 0.08 || yellowBlocks == 0 ? 1 : Round(1 - m.MixedBlockShare);
        m.Pattern = PatternOf(m);
        return m;
    }

    /// <summary>Two-pass city-block distance of every leaf pixel to the nearest background pixel (photo border = far).</summary>
    private static int[] Distances(LeafPixelClass[] cls, int w, int h)
    {
        const int Far = int.MaxValue / 4;
        var d = new int[cls.Length];
        for (var i = 0; i < d.Length; i++) d[i] = cls[i] == LeafPixelClass.Background ? 0 : Far;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = y * w + x;
            if (d[i] == 0) continue;
            if (x > 0) d[i] = Math.Min(d[i], d[i - 1] + 1);
            if (y > 0) d[i] = Math.Min(d[i], d[i - w] + 1);
        }
        for (var y = h - 1; y >= 0; y--)
        for (var x = w - 1; x >= 0; x--)
        {
            var i = y * w + x;
            if (d[i] == 0) continue;
            if (x < w - 1) d[i] = Math.Min(d[i], d[i + 1] + 1);
            if (y < h - 1) d[i] = Math.Min(d[i], d[i + w] + 1);
        }
        return d;
    }

    private static double Round(double v) => Math.Round(v, 3);
}
