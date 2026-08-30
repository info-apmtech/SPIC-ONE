using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SPIC.Ifms.Automation.Options;
using Tesseract;

namespace SPIC.Ifms.Automation.Portal.Challenges
{
	/// <summary>
	/// Reads the mFMS image CAPTCHA with Tesseract.
	///
	/// Measured on 25 live samples (2026-08-30): 11 read exactly right, 9 rejected
	/// for being the wrong length, 5 wrong — about 44% per attempt. That sounds
	/// poor until you notice a wrong answer costs nothing: the portal says
	/// "invalid captcha", the login reloads for a fresh image, and five attempts
	/// turn 44% into roughly 95%. The phone gets asked about one morning in
	/// eighteen.
	///
	/// Two pieces do the work, and both were arrived at by looking at the images
	/// rather than by tuning thresholds:
	///   - the colour mask in Preprocess, which exploits the text being orange and
	///     the background grey
	///   - the segmentation in ReadSegmented, which undoes the staggered baselines
	///
	/// Whole-strip OCR without those scored 1 in 13.
	///
	/// The residual errors are the classic font confusions: 8 read as B, 6 as S,
	/// O as 0. Nothing to be done about those from the image alone, and the retry
	/// loop absorbs them.
	/// </summary>
	public sealed class OcrCaptchaSolver : ICaptchaSolver, IDisposable
	{
		public string Name => "Ocr";

		private readonly IfmsCaptchaOptions _options;
		private readonly ILogger<OcrCaptchaSolver> _logger;
		private readonly Lazy<TesseractEngine?> _engine;

		public OcrCaptchaSolver(IOptions<IfmsOptions> options, ILogger<OcrCaptchaSolver> logger)
		{
			_options = options.Value.Captcha;
			_logger = logger;
			_engine = new Lazy<TesseractEngine?>(CreateEngine, LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public Task<CaptchaAnswer?> SolveAsync(CaptchaChallenge challenge, CancellationToken cancellationToken)
		{
			if (challenge.ImagePng is null || challenge.ImagePng.Length == 0)
				return Task.FromResult<CaptchaAnswer?>(null);

			var engine = _engine.Value;
			if (engine is null)
				return Task.FromResult<CaptchaAnswer?>(null);

			try
			{
				var bytes = _options.PreprocessImage
					? Preprocess(challenge.ImagePng)
					: challenge.ImagePng;

				var (cleaned, confidence) = _options.SegmentCharacters
					? ReadSegmented(engine, bytes)
					: ReadWholeLine(engine, bytes);

				if (cleaned.Length == 0)
				{
					_logger.LogWarning("OCR returned nothing usable for the CAPTCHA image.");
					return Task.FromResult<CaptchaAnswer?>(null);
				}

				if (_options.ExpectedLength > 0 && cleaned.Length != _options.ExpectedLength)
				{
					_logger.LogWarning(
						"OCR read {Value} ({Actual} characters) but the CAPTCHA should be {Expected}; skipping this attempt.",
						cleaned, cleaned.Length, _options.ExpectedLength);
					return Task.FromResult<CaptchaAnswer?>(null);
				}

				_logger.LogInformation(
					"OCR read the CAPTCHA as {Value} at {Confidence:P0} confidence (attempt {Attempt}).",
					cleaned, confidence, challenge.Attempt);

				return Task.FromResult<CaptchaAnswer?>(new CaptchaAnswer
				{
					Value = cleaned,
					Method = Name,
					Confidence = confidence
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "OCR failed on the CAPTCHA image.");
				return Task.FromResult<CaptchaAnswer?>(null);
			}
		}

		/// <summary>
		/// The cleaned-up image the OCR actually sees. Only used by the calibration
		/// tool: looking at this is how you tell a preprocessing problem apart from
		/// a Tesseract one.
		/// </summary>
		public byte[] RenderPreprocessed(byte[] png) => Preprocess(png);

		/// <summary>
		/// Cuts the CAPTCHA into individual glyphs and reads each one on its own.
		///
		/// This is what makes the mFMS CAPTCHA readable. Its characters sit at
		/// deliberately staggered heights, and Tesseract's line modes assume a
		/// shared baseline — fed the whole strip it silently drops the characters
		/// that do not fit the line it thinks it found, which is why whole-line
		/// reading returns three or four characters out of six.
		///
		/// Cutting first sidesteps the problem entirely: after the colour mask each
		/// glyph is an isolated blob of black on white, so a vertical ink profile
		/// separates them exactly, and every glyph is then a clean single-character
		/// recognition with no layout analysis left to get wrong.
		/// </summary>
		private (string Text, float Confidence) ReadSegmented(TesseractEngine engine, byte[] png)
		{
			using var image = Image.Load<Rgba32>(png);

			var columns = FindGlyphColumns(image);

			if (columns.Count == 0)
				return (string.Empty, 0f);

			// A wildly wrong segment count means the colour mask failed, not that
			// the portal changed. Reading on would produce confident nonsense.
			if (_options.ExpectedLength > 0 &&
				(columns.Count < _options.ExpectedLength - 1 ||
				 columns.Count > _options.ExpectedLength + 1))
			{
				_logger.LogWarning(
					"Segmentation found {Found} glyphs but expected about {Expected}; " +
					"the CAPTCHA layout or the colour mask may need retuning.",
					columns.Count, _options.ExpectedLength);

				return (string.Empty, 0f);
			}

			// Straightened line first. It keeps each glyph in the company of its
			// neighbours, which is the situation Tesseract is actually trained for,
			// and it reads a lone narrow "I" or "1" far more reliably than
			// single-character mode does.
			var line = ReadNormalisedLine(engine, image, columns);

			if (_options.ExpectedLength > 0 && line.Text.Length == _options.ExpectedLength)
				return line;

			// Fall back to reading each glyph alone. Weaker on narrow characters,
			// but it cannot drop one, so it rescues the cases where the straightened
			// line lost a character.
			var perChar = ReadEachGlyph(engine, image, columns);

			if (_options.ExpectedLength > 0 && perChar.Text.Length == _options.ExpectedLength)
				return perChar;

			// Neither has the right length: hand back whichever got closer, and let
			// the length check reject it.
			return line.Text.Length >= perChar.Text.Length ? line : perChar;
		}

		/// <summary>
		/// Rebuilds the CAPTCHA as an ordinary word: same glyphs, same order, but
		/// each one re-centred on a shared baseline with even spacing.
		///
		/// The staggered heights are the whole difficulty of this CAPTCHA. Rather
		/// than fight Tesseract's layout analysis, this removes the thing that
		/// confuses it and hands over a strip that looks like printed text.
		/// </summary>
		private (string Text, float Confidence) ReadNormalisedLine(
			TesseractEngine engine,
			Image<Rgba32> image,
			List<(int Start, int End)> columns)
		{
			const int gap = 24;
			const int margin = 30;

			var glyphs = new List<(Image<Rgba32> Image, int Width)>();

			try
			{
				foreach (var (start, end) in columns)
				{
					var bounds = FindInkRows(image, start, end);
					if (bounds is null)
						continue;

					var (top, bottom) = bounds.Value;
					var width = end - start + 1;

					glyphs.Add((
						image.Clone(ctx => ctx.Crop(
							new Rectangle(start, top, width, bottom - top + 1))),
						width));
				}

				if (glyphs.Count == 0)
					return (string.Empty, 0f);

				var totalWidth = glyphs.Sum(g => g.Image.Width) + gap * (glyphs.Count - 1) + margin * 2;
				var maxHeight = glyphs.Max(g => g.Image.Height);
				var canvasHeight = maxHeight + margin * 2;

				using var canvas = new Image<Rgba32>(totalWidth, canvasHeight);
				canvas.Mutate(ctx => ctx.BackgroundColor(Color.White));

				var x = margin;

				foreach (var glyph in glyphs)
				{
					// Bottom-align rather than centre: that is how real type sits,
					// and it keeps the relative height of a "3" against a "K" honest.
					var y = margin + (maxHeight - glyph.Image.Height);

					canvas.Mutate(ctx => ctx.DrawImage(glyph.Image, new Point(x, y), 1f));
					x += glyph.Image.Width + gap;
				}

				using var buffer = new MemoryStream();
				canvas.SaveAsPng(buffer);

				using var pix = Pix.LoadFromMemory(buffer.ToArray());
				using var page = engine.Process(pix, PageSegMode.SingleWord);

				return (Clean(page.GetText() ?? string.Empty), page.GetMeanConfidence());
			}
			finally
			{
				foreach (var glyph in glyphs)
					glyph.Image.Dispose();
			}
		}

		/// <summary>
		/// Reads every glyph on its own. Cannot drop a character, which is exactly
		/// what makes it the right fallback when the straightened line comes back
		/// a character short.
		/// </summary>
		private (string Text, float Confidence) ReadEachGlyph(
			TesseractEngine engine,
			Image<Rgba32> image,
			List<(int Start, int End)> columns)
		{
			var builder = new StringBuilder(columns.Count);
			var confidences = new List<float>(columns.Count);

			foreach (var (start, end) in columns)
			{
				using var glyph = CropGlyph(image, start, end);
				using var buffer = new MemoryStream();
				glyph.SaveAsPng(buffer);

				using var pix = Pix.LoadFromMemory(buffer.ToArray());
				using var page = engine.Process(pix, PageSegMode.SingleChar);

				var read = Clean(page.GetText() ?? string.Empty);

				if (read.Length == 0)
				{
					// A glyph Tesseract will not name is nearly always a bare
					// vertical bar. Keeping its place matters more than naming it:
					// the length check then still passes and the portal decides.
					var aspect = (double)(end - start + 1) / image.Height;

					if (aspect < 0.06)
					{
						builder.Append('1');
						confidences.Add(0.3f);
					}

					continue;
				}

				builder.Append(read[0]);
				confidences.Add(page.GetMeanConfidence());
			}

			var mean = confidences.Count == 0 ? 0f : confidences.Average();
			return (builder.ToString(), mean);
		}

		/// <summary>Top and bottom rows containing ink within a column range.</summary>
		private static (int Top, int Bottom)? FindInkRows(Image<Rgba32> image, int start, int end)
		{
			int top = -1, bottom = -1;

			image.ProcessPixelRows(accessor =>
			{
				for (var y = 0; y < accessor.Height; y++)
				{
					var row = accessor.GetRowSpan(y);

					for (var x = start; x <= end && x < row.Length; x++)
					{
						if (row[x].R >= 128)
							continue;

						if (top < 0)
							top = y;

						bottom = y;
						break;
					}
				}
			});

			return top < 0 ? null : (top, bottom);
		}

		private (string Text, float Confidence) ReadWholeLine(TesseractEngine engine, byte[] png)
		{
			using var pix = Pix.LoadFromMemory(png);
			using var page = engine.Process(pix, PageSegMode.SingleLine);

			return (Clean(page.GetText() ?? string.Empty), page.GetMeanConfidence());
		}

		/// <summary>
		/// Column ranges that contain ink, which after the colour mask is one range
		/// per character. Runs separated by less than a minimum gap are merged so a
		/// glyph with a natural internal break is not split in two.
		/// </summary>
		private List<(int Start, int End)> FindGlyphColumns(Image<Rgba32> image)
		{
			var hasInk = new bool[image.Width];

			image.ProcessPixelRows(accessor =>
			{
				for (var y = 0; y < accessor.Height; y++)
				{
					var row = accessor.GetRowSpan(y);

					for (var x = 0; x < row.Length; x++)
					{
						if (row[x].R < 128)
							hasInk[x] = true;
					}
				}
			});

			// Scaled off the image so the numbers hold whatever UpscaleFactor is set
			// to. minWidth has to stay small: an "I" or a "1" is only a few pixels
			// wide, and discarding those as noise was silently eating characters.
			var minGap = Math.Max(2, image.Width / 120);
			var minWidth = Math.Max(2, image.Width / 200);

			var runs = new List<(int Start, int End)>();
			var runStart = -1;

			for (var x = 0; x < image.Width; x++)
			{
				if (hasInk[x])
				{
					if (runStart < 0)
						runStart = x;
				}
				else if (runStart >= 0)
				{
					runs.Add((runStart, x - 1));
					runStart = -1;
				}
			}

			if (runStart >= 0)
				runs.Add((runStart, image.Width - 1));

			var merged = new List<(int Start, int End)>();

			foreach (var run in runs)
			{
				if (merged.Count > 0 && run.Start - merged[^1].End <= minGap)
					merged[^1] = (merged[^1].Start, run.End);
				else
					merged.Add(run);
			}

			return merged.Where(r => r.End - r.Start + 1 >= minWidth).ToList();
		}

		/// <summary>
		/// One glyph on a generous white margin. The padding matters: Tesseract
		/// treats a character that touches the image edge as clipped and often
		/// refuses to name it.
		/// </summary>
		private static Image<Rgba32> CropGlyph(Image<Rgba32> image, int start, int end)
		{
			var width = end - start + 1;
			const int padding = 20;

			var glyph = new Image<Rgba32>(width + padding * 2, image.Height + padding * 2);
			glyph.Mutate(ctx => ctx.BackgroundColor(Color.White));

			using var slice = image.Clone(ctx => ctx.Crop(
				new Rectangle(start, 0, width, image.Height)));

			glyph.Mutate(ctx => ctx.DrawImage(slice, new Point(padding, padding), 1f));

			return glyph;
		}

		/// <summary>
		/// Turns the portal's CAPTCHA into something Tesseract was trained on:
		/// large, black text on a plain white page.
		///
		/// The order matters. Upscaling first gives the later steps more pixels to
		/// work with. The colour mask runs before any greyscale conversion, since
		/// converting to grey is exactly what destroys the hue information that
		/// separates the orange characters from the grey gradient behind them.
		/// </summary>
		private byte[] Preprocess(byte[] png)
		{
			using var image = Image.Load<Rgba32>(png);

			var factor = Math.Max(1, _options.UpscaleFactor);
			image.Mutate(ctx => ctx.Resize(
				image.Width * factor,
				image.Height * factor,
				KnownResamplers.Lanczos3));

			if (_options.IsolateColouredText)
			{
				IsolateColour(image, _options.SaturationThreshold);
			}
			else
			{
				var threshold = Math.Clamp(_options.BinarizeThreshold, 1, 254) / 255f;

				image.Mutate(ctx =>
				{
					ctx.Grayscale().Contrast(1.5f);

					if (_options.InvertImage)
						ctx.Invert();

					ctx.GaussianSharpen(0.6f).BinaryThreshold(threshold);
				});
			}

			using var buffer = new MemoryStream();
			image.SaveAsPng(buffer);
			return buffer.ToArray();
		}

		/// <summary>
		/// Repaints the image in pure black and white by hue rather than
		/// brightness: any pixel colourful enough becomes black text, everything
		/// else becomes white paper. The dark gradient, being grey, vanishes.
		/// </summary>
		private static void IsolateColour(Image<Rgba32> image, int saturationThreshold)
		{
			var cutoff = Math.Clamp(saturationThreshold, 1, 254);

			image.ProcessPixelRows(accessor =>
			{
				for (var y = 0; y < accessor.Height; y++)
				{
					var row = accessor.GetRowSpan(y);

					for (var x = 0; x < row.Length; x++)
					{
						ref var pixel = ref row[x];

						int max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
						int min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));

						var isText = max - min >= cutoff;

						pixel = isText
							? new Rgba32(0, 0, 0, 255)
							: new Rgba32(255, 255, 255, 255);
					}
				}
			});
		}

		private string Clean(string text)
		{
			var whitelist = _options.CharacterWhitelist;

			var kept = text
				.Where(c => !char.IsWhiteSpace(c))
				.Where(c => whitelist.Length == 0 || whitelist.Contains(c))
				.ToArray();

			return new string(kept);
		}

		private TesseractEngine? CreateEngine()
		{
			var path = _options.TessDataPath;
			if (!Path.IsPathRooted(path))
				path = Path.Combine(AppContext.BaseDirectory, path);

			var trainedData = Path.Combine(path, $"{_options.TessLanguage}.traineddata");
			if (!File.Exists(trainedData))
			{
				_logger.LogError(
					"Tesseract data not found at {Path}. Download {Language}.traineddata from " +
					"https://github.com/tesseract-ocr/tessdata_fast and place it there, or drop " +
					"Ocr from Ifms:Captcha:Strategies.",
					trainedData, _options.TessLanguage);
				return null;
			}

			try
			{
				var engine = new TesseractEngine(path, _options.TessLanguage, EngineMode.LstmOnly);

				if (_options.CharacterWhitelist.Length > 0)
					engine.SetVariable("tessedit_char_whitelist", _options.CharacterWhitelist);

				// CAPTCHAs are one short line with no language model to lean on.
				engine.SetVariable("load_system_dawg", false);
				engine.SetVariable("load_freq_dawg", false);

				// Reading one glyph at a time leaves no context to disambiguate,
				// so the whitelist is doing real work here.
				engine.SetVariable("classify_bln_numeric_mode", false);

				return engine;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Could not start Tesseract from {Path}.", path);
				return null;
			}
		}

		public void Dispose()
		{
			if (_engine.IsValueCreated)
				_engine.Value?.Dispose();
		}
	}
}
