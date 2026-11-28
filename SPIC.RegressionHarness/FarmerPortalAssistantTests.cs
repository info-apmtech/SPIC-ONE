using System.Text.Json;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.Core.Interfaces;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Services;
using Spic.Infrastructure.Services.Assistant;
using R = SPIC.Core.FarmerPortal.FarmerAssistantRules;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 5: SPIC AI for farmers. The daily cap on the IST day from a UTC clock, question
/// validation, the system prompt and farmer context (language, crops, district, soil ratings, fertilizer
/// summary, nothing personal), the Anthropic request built from them, the history window, the basic
/// (keyword) answer and its label, starter questions, the chat screen helpers and the menu.
/// </summary>
internal static class FarmerPortalAssistantTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal: SPIC AI for farmers");
		DailyCap();
		Questions();
		Prompt();
		Request();
		History();
		Fallback();
		Starters();
		UiAndMenu();
		Budget();
	}

	// Fix round 1 (I1): the daily AI budget across all farmers, the kill switch, and the review's small fixes.
	private static void Budget()
	{
		var D = FarmerAiDecision.Allowed;
		Check.Equal("budget: under the limit -> AI", D, FarmerAiBudgetRules.Decide(true, true, 1999, 2000, 0, null));
		Check.Equal("budget: at the limit -> basic answer", FarmerAiDecision.GlobalLimit, FarmerAiBudgetRules.Decide(true, true, 2000, 2000, 0, null));
		Check.Equal("budget: over the limit -> basic answer", FarmerAiDecision.GlobalLimit, FarmerAiBudgetRules.Decide(true, true, 2500, 2000, 0, null));
		Check.Equal("budget: a limit of 0 allows no AI answer", FarmerAiDecision.GlobalLimit, FarmerAiBudgetRules.Decide(true, true, 0, 0, 0, null));
		Check.Equal("budget: kill switch -> basic answer even with budget left", FarmerAiDecision.Disabled, FarmerAiBudgetRules.Decide(false, true, 0, 2000, 0, null));
		Check.Equal("budget: no key -> basic answer (nothing counted)", FarmerAiDecision.NoKey, FarmerAiBudgetRules.Decide(true, false, 0, 2000, 0, null));
		Check.Equal("budget: a kind's sub-limit (leaf check) applies", FarmerAiDecision.KindLimit, FarmerAiBudgetRules.Decide(true, true, 100, 2000, 300, 300));
		Check.Equal("budget: under the sub-limit -> AI", D, FarmerAiBudgetRules.Decide(true, true, 100, 2000, 299, 300));
		Check.Equal("budget: the global limit wins over a sub-limit", FarmerAiDecision.GlobalLimit, FarmerAiBudgetRules.Decide(true, true, 2000, 2000, 0, 300));

		Check.That("budget: enabled by default (empty)", FarmerAiBudgetRules.Enabled(null) && FarmerAiBudgetRules.Enabled("") && FarmerAiBudgetRules.Enabled("true"));
		Check.That("budget: false / 0 / off / no switch it off", new[] { "false", "False", "0", "off", " no " }.All(v => !FarmerAiBudgetRules.Enabled(v)));
		Check.Equal("budget: default global limit 2000", 2000, FarmerAiBudgetRules.GlobalLimit(""));
		Check.Equal("budget: configured global limit", 3, FarmerAiBudgetRules.GlobalLimit("3"));
		Check.Equal("budget: junk / negative = 2000", 2000, FarmerAiBudgetRules.GlobalLimit("-1"));
		Check.Equal("budget: 0 is a real limit (no AI)", 0, FarmerAiBudgetRules.GlobalLimit("0"));
		Check.That("budget: no sub-limit unless configured", FarmerAiBudgetRules.KindLimit("") is null && FarmerAiBudgetRules.KindLimit("50") == 50);

		// IST day rollover from a UTC clock: yesterday evening's (IST) calls do not count after 18:30 UTC.
		var calls = new[] { Utc(2026, 11, 28, 18, 0), Utc(2026, 11, 28, 18, 29, 59), Utc(2026, 11, 28, 18, 30), Utc(2026, 11, 29, 5, 0) };
		Check.Equal("budget: before IST midnight all of that IST day count", 2, FarmerAiBudgetRules.UsedToday(calls, Utc(2026, 11, 28, 18, 29, 59)));
		Check.Equal("budget: after IST midnight (18:30 UTC) only the new day's calls count", 2, FarmerAiBudgetRules.UsedToday(calls, Utc(2026, 11, 29, 6, 0)));
		Check.Equal("budget: at 18:30:00 UTC the new day has one call", 1, FarmerAiBudgetRules.UsedToday(calls.Take(3), Utc(2026, 11, 28, 18, 30)));

		Check.That("budget: gentle notice at the limit", FarmerAiBudgetRules.Notice(FarmerAiDecision.GlobalLimit)!.Contains("basic answer") && FarmerAiBudgetRules.Notice(FarmerAiDecision.GlobalLimit)!.Contains("12 midnight"));
		Check.That("budget: paused notice for the kill switch", FarmerAiBudgetRules.Notice(FarmerAiDecision.Disabled)!.Contains("paused"));
		Check.That("budget: no notice when the AI answers or there is no key", FarmerAiBudgetRules.Notice(FarmerAiDecision.Allowed) is null && FarmerAiBudgetRules.Notice(FarmerAiDecision.NoKey) is null);

		// M7: farmer answers ask for low effort; the library request sends no output_config.
		var farmer = AnthropicAssistantProvider.BuildRequest(new AssistantContext { Mode = AssistantMode.Farmer, Question = "q", Effort = "low", MaxTokens = 1500 }, new AssistantOptions());
		Check.That("effort: farmer request sends output_config.effort low", AnthropicMessagesClient.Serialize(farmer, "claude-sonnet-5").Contains("\"output_config\":{\"effort\":\"low\"}"));
		var library = AnthropicAssistantProvider.BuildRequest(new AssistantContext { Question = "q" }, new AssistantOptions { MaxTokens = 800 });
		Check.That("effort: the library request has no output_config", !AnthropicMessagesClient.Serialize(library, "claude-sonnet-5").Contains("output_config"));
		Check.Equal("effort: default for farmers is low", "low", new AssistantOptions().FarmerEffort);

		// M5: the endpoint override only for https or this machine.
		Check.Equal("endpoint: empty = Anthropic", AnthropicMessagesClient.Endpoint, AnthropicMessagesClient.ResolveEndpoint("").Url);
		Check.That("endpoint: https gateway is used", AnthropicMessagesClient.ResolveEndpoint("https://gw.example.org/v1/messages").Overridden);
		Check.That("endpoint: http to 127.0.0.1 (local stub) is used", AnthropicMessagesClient.ResolveEndpoint("http://127.0.0.1:5999/v1/messages").Overridden
			&& AnthropicMessagesClient.ResolveEndpoint("http://localhost:5999/v1/messages").Overridden);
		var remote = AnthropicMessagesClient.ResolveEndpoint("http://gw.example.org/v1/messages");
		Check.That("endpoint: plain http to another host is ignored (key never sent in clear)", !remote.Overridden && remote.Url == AnthropicMessagesClient.Endpoint && remote.Ignored is not null);
		Check.That("endpoint: junk is ignored", AnthropicMessagesClient.ResolveEndpoint("not a url").Url == AnthropicMessagesClient.Endpoint);
	}

	private static DateTime Utc(int y, int mo, int d, int h, int mi, int s = 0) => new(y, mo, d, h, mi, s, DateTimeKind.Utc);

	private static void DailyCap()
	{
		// IST midnight of 28 Nov 2026 is 27 Nov 18:30 UTC.
		Check.Equal("cap: the IST day of 28 Nov 09:00 UTC starts 27 Nov 18:30 UTC", Utc(2026, 11, 27, 18, 30), R.IstDayStartUtc(Utc(2026, 11, 28, 9, 0)));
		Check.Equal("cap: 18:29:59 UTC is still the previous IST day", Utc(2026, 11, 26, 18, 30), R.IstDayStartUtc(Utc(2026, 11, 27, 18, 29, 59)));
		Check.Equal("cap: 18:30:00 UTC is a new IST day", Utc(2026, 11, 27, 18, 30), R.IstDayStartUtc(Utc(2026, 11, 27, 18, 30, 0)));
		Check.Equal("cap: the day start is a UTC instant", DateTimeKind.Utc, R.IstDayStartUtc(Utc(2026, 11, 28, 9, 0)).Kind);
		Check.Equal("cap: a clock just after UTC midnight is mid-morning IST of the same date", Utc(2026, 11, 27, 18, 30), R.IstDayStartUtc(Utc(2026, 11, 28, 0, 5)));
		Check.Equal("cap: resets at the next IST midnight", Utc(2026, 11, 28, 18, 30), R.NextResetUtc(Utc(2026, 11, 28, 9, 0)));
		Check.Equal("cap: one second before the reset, 1 second to wait", 1, R.SecondsUntilReset(Utc(2026, 11, 28, 18, 29, 59)));

		// A question at 23:59 IST does not count the next morning; one at 00:00 IST does.
		var morning = Utc(2026, 11, 28, 3, 0); // 08:30 IST
		Check.That("cap: asked 23:59 IST yesterday does not count today", !R.CountsToday(Utc(2026, 11, 27, 18, 29), morning));
		Check.That("cap: asked 00:00 IST today counts", R.CountsToday(Utc(2026, 11, 27, 18, 30), morning));
		Check.That("cap: asked late tonight (23:59 IST) still counts today", R.CountsToday(Utc(2026, 11, 28, 18, 29), morning));

		Check.Equal("cap: default limit is 30", 30, R.Limit(null));
		Check.Equal("cap: empty config = 30", 30, R.Limit(""));
		Check.Equal("cap: configured 5", 5, R.Limit("5"));
		Check.Equal("cap: zero = 30", 30, R.Limit("0"));
		Check.Equal("cap: junk = 30", 30, R.Limit("x"));
		Check.That("cap: 29 used can ask", !R.IsCapped(29, 30));
		Check.That("cap: 30 used is capped", R.IsCapped(30, 30));
		Check.Equal("cap: left after 28", 2, R.Left(28, 30));
		Check.Equal("cap: never below zero", 0, R.Left(31, 30));
		var msg = R.CapMessage(30);
		Check.That("cap: friendly message names the limit, midnight and the SPIC team",
			msg.Contains("30 questions today") && msg.Contains("midnight") && msg.Contains("SPIC team"), msg);
	}

	private static void Questions()
	{
		Check.That("question: empty is refused", R.Validate("   ") is not null);
		Check.That("question: null is refused", R.Validate(null) is not null);
		Check.That("question: 1000 letters is fine", R.Validate(new string('a', 1000)) is null);
		Check.That("question: 1001 letters is refused", R.Validate(new string('a', 1001)) is not null);
		Check.That("question: surrounding spaces are not counted", R.Validate("  " + new string('a', 1000) + "  ") is null);
		Check.That("question: Tamil text is fine", R.Validate("என் நெல் இலைகள் ஏன் மஞ்சளாகின்றன?") is null);
		Check.Equal("title: short question kept", "When should I give urea?", R.MakeTitle("  When should I   give urea? "));
		var title = R.MakeTitle(new string('b', 80));
		Check.That("title: long question cut to 60 + ellipsis", title.Length == 61 && title.EndsWith("…"), title);
	}

	private static readonly FarmerAssistantFacts Facts = new()
	{
		Crops = new[] { "Paddy", "Banana", "paddy" },
		District = "Thoothukudi",
		State = "Tamil Nadu",
		LandAcres = 2.5m,
		Irrigation = "Borewell",
		SoilReportDate = new DateTime(2026, 11, 20),
		SoilRatings = new[] { ("Available Nitrogen", "Low"), ("Available Phosphorus", "Low"), ("Available Potassium", "Medium"), ("pH", "Alkaline") },
		FertilizerSummary = "Cotton (Irrigated hybrids) on Black Cotton Soil, 1 acre: N Low, P Low, K Low; pH 7.0 Neutral. Need N : P2O5 : K2O = 60.7 : 30.4 : 30.4 kg/acre.",
		FertilizerDateUtc = Utc(2026, 11, 28, 9, 35)
	};

	private static void Prompt()
	{
		var ta = R.SystemPrompt("ta");
		Check.That("prompt: Tamil profile asks for Tamil in Tamil script", ta.Contains("in Tamil (Tamil script)"), ta);
		Check.That("prompt: suggestions also in Tamil", ta.Contains("written in Tamil"));
		Check.That("prompt: Hindi profile asks for Hindi", R.SystemPrompt("hi").Contains("in Hindi (Devanagari script)"));
		Check.That("prompt: unknown language falls back to English", R.SystemPrompt("xx").Contains("whole answer in English,"));
		Check.That("prompt: simple words, short steps, metric units",
			ta.Contains("simple, everyday words") && ta.Contains("numbered steps") && ta.Contains("Use metric units"));
		Check.That("prompt: SPIC products only from the library content", ta.Contains("only when it appears in the SPIC library content"));
		Check.That("prompt: never invent prices or doses", ta.Contains("Never invent prices") && ta.Contains("Give a dose only when"));
		Check.That("prompt: pesticide label and safety", ta.Contains("follow the label dose exactly") && ta.Contains("gloves"));
		Check.That("prompt: suggests the MDO through My SPIC team", ta.Contains("SPIC officer (MDO)") && ta.Contains("My SPIC team"));
		Check.That("prompt: keeps the SUGGESTIONS line the parser reads", ta.Contains("SUGGESTIONS:"));

		var block = R.ContextBlock(Facts);
		Check.That("context: crops once each", block.Contains("- Crops: Paddy, Banana") && !block.Contains("Banana, paddy"), block);
		Check.That("context: district and state", block.Contains("Thoothukudi district, Tamil Nadu"));
		Check.That("context: land and water", block.Contains("2.5 acres, water: Borewell"));
		Check.That("context: soil ratings only, with the report date", block.Contains("ratings only, 20 Nov 2026): Available Nitrogen Low, Available Phosphorus Low, Available Potassium Medium, pH Alkaline"), block);
		Check.That("context: latest fertilizer advice summary with its IST date", block.Contains("Last fertilizer advice in the app (28 Nov 2026): Cotton (Irrigated hybrids)"));
		var empty = R.ContextBlock(new FarmerAssistantFacts());
		Check.That("context: nothing on record says so", empty.Contains("Crops: not given") && empty.Contains("soil test: none on record") && !empty.Contains("fertilizer advice"), empty);
		Check.That("context: the facts type has no personal fields (name, phone, village, GPS)",
			typeof(FarmerAssistantFacts).GetProperties().All(p => p.Name is not ("Name" or "Phone" or "Village" or "Latitude" or "Longitude" or "UserId")));

		Check.Equal("soil rating: the lab's label wins", "Alkaline", R.SoilRating("Alkaline", LabResultStatus.Excess));
		Check.Equal("soil rating: Deficient = Low", "Low", R.SoilRating(null, LabResultStatus.Deficient));
		Check.Equal("soil rating: Normal = Medium", "Medium", R.SoilRating(" ", LabResultStatus.Normal));
		Check.Equal("soil rating: Excess = High", "High", R.SoilRating(null, LabResultStatus.Excess));
		Check.Equal("language: ta = Tamil", "Tamil", R.LanguageName("ta"));
		Check.Equal("language: code kept when known", "ml", R.LanguageCode("ML"));
		Check.Equal("language: unknown code = en", "en", R.LanguageCode("fr"));
	}

	private static void Request()
	{
		var ctx = new AssistantContext
		{
			Mode = AssistantMode.Farmer,
			Question = "Why are my paddy leaves yellow?",
			History = R.History(new[]
			{
				new AssistantTurn { Role = "user", Text = "Hello" },
				new AssistantTurn { Role = "assistant", Text = "Vanakkam!" }
			}),
			Items = new List<AssistantLibraryItem> { new() { ContentId = 7, Title = "SPIC Gromor Urea (46-0-0)", Kind = LibraryContentKind.Product, Overview = "Urea supplies 46% nitrogen." } },
			SystemPrompt = R.SystemPrompt("ta"),
			ContextBlock = R.ContextBlock(Facts),
			Language = "ta",
			MaxTokens = 1500
		};
		var request = AnthropicAssistantProvider.BuildRequest(ctx, new AssistantOptions { Model = "claude-sonnet-5", MaxTokens = 800 });
		Check.That("request: farmer system prompt used (Tamil)", request.System.Contains("in Tamil (Tamil script)"));
		Check.Equal("request: farmer answer length", 1500, request.MaxTokens);
		Check.Equal("request: history then the question (3 messages)", 3, request.Messages.Count);
		var json = AnthropicMessagesClient.Serialize(request, "claude-sonnet-5");
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;
		Check.Equal("request: model from the options", "claude-sonnet-5", root.GetProperty("model").GetString());
		Check.Equal("request: first message is the user's", "user", root.GetProperty("messages")[0].GetProperty("role").GetString());
		var last = root.GetProperty("messages")[2].GetProperty("content")[0].GetProperty("text").GetString() ?? "";
		Check.That("request: the question block carries the farmer context first", last.StartsWith("About this farmer") && last.Contains("Thoothukudi district"), last);
		Check.That("request: then the library content and the question", last.Contains("### SPIC Gromor Urea (46-0-0)") && last.EndsWith("Why are my paddy leaves yellow?"));
		Check.That("request: no thinking / sampling fields are sent", !json.Contains("\"thinking\"") && !json.Contains("temperature"));

		var library = AnthropicAssistantProvider.BuildRequest(new AssistantContext { Question = "DAP?" }, new AssistantOptions { MaxTokens = 800 });
		Check.Equal("request: the library assistant keeps its own prompt", AnthropicAssistantProvider.SystemPrompt, library.System);
		Check.Equal("request: the library assistant keeps 800 tokens", 800, library.MaxTokens);
		Check.That("request: the library question block has no farmer context",
			AnthropicAssistantProvider.BuildQuestionBlock(new AssistantContext { Question = "DAP?" }).StartsWith("Library content available to you:"));

		var image = AnthropicContent.ImageBase64("image/jpeg", "AAAA");
		var imageJson = JsonSerializer.Serialize(image);
		Check.That("request: image blocks (for the leaf check) use the base64 source shape",
			imageJson.Contains("\"type\":\"image\"") && imageJson.Contains("\"media_type\":\"image/jpeg\""), imageJson);

		var (text, stop) = AnthropicMessagesClient.Parse("{\"content\":[{\"type\":\"thinking\",\"thinking\":\"\"},{\"type\":\"text\",\"text\":\"Hello\"}],\"stop_reason\":\"end_turn\"}");
		Check.That("response: thinking blocks are skipped, text kept", text == "Hello" && stop == "end_turn");
		var (none, refusal) = AnthropicMessagesClient.Parse("{\"content\":[],\"stop_reason\":\"refusal\"}");
		Check.That("response: a refusal has no text", none is null && refusal == "refusal");
		var (answer, suggestions) = AnthropicAssistantProvider.SplitSuggestions("Give urea in 3 splits.\nSUGGESTIONS: ஒன்று | இரண்டு | மூன்று");
		Check.That("response: Tamil suggestions are split off", answer == "Give urea in 3 splits." && suggestions.Count == 3 && suggestions[0] == "ஒன்று");
	}

	private static void History()
	{
		var turns = Enumerable.Range(1, 14).Select(i => new AssistantTurn { Role = i % 2 == 1 ? "user" : "assistant", Text = $"m{i}" }).ToList();
		var window = R.History(turns);
		Check.Equal("history: at most 10 messages", 10, window.Count);
		Check.Equal("history: the newest kept, starting with a question", "m5", window[0].Text);
		var orphan = R.History(new[]
		{
			new AssistantTurn { Role = "user", Text = "q1" },
			new AssistantTurn { Role = "user", Text = "q2" },
			new AssistantTurn { Role = "assistant", Text = "a2" }
		});
		Check.That("history: an unanswered question is joined to the next", orphan.Count == 2 && orphan[0].Text == "q1\n\nq2");
		var leading = R.History(new[] { new AssistantTurn { Role = "assistant", Text = "a" }, new AssistantTurn { Role = "user", Text = "q" } });
		Check.That("history: never starts with an answer", leading.Count == 1 && leading[0].Role == "user");
	}

	private static void Fallback()
	{
		var items = new List<AssistantLibraryItem>
		{
			new() { ContentId = 5, Title = "Gromor DAP application in paddy", Kind = LibraryContentKind.Video, Overview = "This video shows the basal application of DAP. Apply 50 kg per acre. Mix well." }
		};
		var (text, suggestions) = R.FallbackAnswer("How much DAP for paddy?", items.Concat(new[] { new AssistantLibraryItem { ContentId = 1, Title = "Gromor DAP application in paddy", Overview = "Same video." } }).ToList(), "en", new[] { "Paddy" });
		Check.That("fallback: an item uploaded twice is quoted once", !text.Contains("Same video."), text);
		Check.That("fallback: starts with the basic-answer label", text.StartsWith(R.FallbackLabel + ":"), text);
		Check.That("fallback: says it is from the SPIC library", text.Contains("what the SPIC library says"));
		Check.That("fallback: quotes the item in short (two sentences)", text.Contains("Gromor DAP application in paddy: This video shows the basal application of DAP. Apply 50 kg per acre.") && !text.Contains("Mix well"), text);
		Check.That("fallback: points to the SPIC team", text.Contains("My SPIC team"));
		Check.That("fallback: no safety line for a fertilizer question", !text.Contains("gloves"));
		Check.That("fallback: paddy starters as follow-ups", suggestions.Count == 3 && suggestions[0].Contains("paddy"), string.Join(" | ", suggestions));

		var (tamil, _) = R.FallbackAnswer("Which pesticide for bollworm spray?", items, "ta");
		Check.That("fallback: a pesticide question adds the label and safety line", tamil.Contains(R.SafetyLine));
		Check.That("fallback: a Tamil farmer is told basic answers are English", tamil.Contains("English only") && tamil.Contains("Tamil"));

		var (nothing, _) = R.FallbackAnswer("Tell me about drones", Array.Empty<AssistantLibraryItem>(), "en");
		Check.That("fallback: nothing in the library says so and points to people",
			nothing.StartsWith(R.FallbackLabel) && nothing.Contains("nothing on this question") && nothing.Contains("Community near me"), nothing);

		// The keyword provider in farmer mode uses the labelled answer (no database needed for it).
		var provider = new KeywordAssistantProvider(null!);
		var answer = provider.AnswerAsync(new AssistantContext { Mode = AssistantMode.Farmer, Question = "How much DAP?", Items = items, Language = "hi", Crops = new[] { "Cotton" } }).GetAwaiter().GetResult();
		Check.Equal("fallback: provider is keyword", "keyword", answer.Provider);
		Check.That("fallback: provider answer is labelled", answer.Text.StartsWith(R.FallbackLabel));
		Check.That("fallback: sources are the library items", answer.Sources.Count == 1 && answer.Sources[0].ContentId == 5);
		Check.That("fallback: cotton follow-ups", answer.Suggestions.Any(s => s.Contains("cotton")), string.Join(" | ", answer.Suggestions));
		Check.That("ui: keyword answers are shown as basic", FarmerAssistantUi.IsBasic("keyword") && !FarmerAssistantUi.IsBasic("anthropic"));
	}

	private static void Starters()
	{
		var paddy = R.Starters(new[] { "Paddy", "Banana" });
		Check.Equal("starters: four", 4, paddy.Count);
		Check.That("starters: two for each of the first two crops", paddy[0].Contains("paddy") && paddy[2].Contains("banana"), string.Join(" | ", paddy));
		var none = R.Starters(Array.Empty<string>());
		Check.That("starters: general questions without crops", none.Count == 4 && none.Contains("How do I read my soil test report?"));
		var other = R.Starters(new[] { "Jasmine" });
		Check.Equal("starters: an unknown crop gets a fertilizer question", "What fertilizer does my jasmine crop need?", other[0]);
		Check.That("starters: crops with pests ask for safe control", R.Starters(new[] { "Cotton" })[0].Contains("safely"));
	}

	private static void UiAndMenu()
	{
		var now = Utc(2026, 11, 28, 10, 0);
		Check.Equal("ui: time in IST", "3:05 pm", FarmerAssistantUi.Time(Utc(2026, 11, 28, 9, 35)));
		Check.Equal("ui: today by the IST calendar", "Today", FarmerAssistantUi.Group(Utc(2026, 11, 27, 19, 0), now));
		Check.Equal("ui: yesterday by the IST calendar", "Yesterday", FarmerAssistantUi.Group(Utc(2026, 11, 27, 18, 0), now));
		Check.Equal("ui: earlier", "Earlier", FarmerAssistantUi.Group(Utc(2026, 11, 20, 8, 0), now));
		Check.Equal("ui: list line", "3 questions · Today, 3:05 pm", FarmerAssistantUi.ListLine(3, Utc(2026, 11, 28, 9, 35), now));
		Check.Equal("ui: left text", "28 of 30 questions left today", FarmerAssistantUi.LeftText(28, 30));
		Check.Equal("ui: full allowance", "30 questions a day", FarmerAssistantUi.LeftText(30, 30));
		Check.Equal("ui: none left", "No questions left today", FarmerAssistantUi.LeftText(0, 30));
		var paragraphs = FarmerAssistantUi.Paragraphs("**Give urea** in splits.\n\n- First at 15 days\n- Then at 45 days\r\n\r\n## Note\nKeep water.");
		Check.That("ui: answer paragraphs without markdown marks", paragraphs.Count == 3 && paragraphs[0] == "Give urea in splits." && paragraphs[1].StartsWith("• First") && paragraphs[2] == "Note\nKeep water.", string.Join(" / ", paragraphs));
		Check.Equal("ui: source link", "/DigitalLibrary/view/7", FarmerAssistantUi.SourceHref(7));

		var farmer = TestLogin.As(AppRole.Farmer, "");
		Check.Equal("menu: the SPIC AI tile comes second", "assistant", FarmerFeatureTiles.For(farmer).Skip(1).First().Id);
		Check.That("menu: /farmer/assistant opens for a farmer", RouteAccess.CanOpen(farmer, "/farmer/assistant", null)
			&& RouteAccess.CanOpen(farmer, "/farmer/assistant/chats", null) && RouteAccess.CanOpen(farmer, "/farmer/assistant/12", null));
		Check.That("menu: a farmer can open a source's library page", RouteAccess.CanOpen(farmer, "/DigitalLibrary/view/7", null));
		Check.That("menu: other library pages stay closed to a farmer without a designation",
			!RouteAccess.CanOpen(farmer, "/DigitalLibrary/chat", null) && !RouteAccess.CanOpen(farmer, "/DigitalLibrary/add/product", null));
		var more = ShellNavigation.MoreItems(farmer).Select(t => t.Key).ToList();
		Check.That("menu: farmer More sheet offers Ask SPIC AI", more.Contains(ShellNavigation.FarmerAssistantKey), string.Join(",", more));
		Check.That("menu: Ask SPIC AI is not a phone tab", !ShellNavigation.GetPhoneTabs(farmer).Any(t => t.Key == ShellNavigation.FarmerAssistantKey));
		var staff = TestLogin.As(AppRole.MDO, "");
		Check.That("menu: staff never get Ask SPIC AI", !ShellNavigation.MoreItems(staff).Any(t => t.Key == ShellNavigation.FarmerAssistantKey)
			&& !RouteAccess.CanOpen(staff, "/farmer/assistant", null));
		Check.That("menu: Admin may open the page (support view)", RouteAccess.CanOpen(TestLogin.As(AppRole.Admin, ""), "/farmer/assistant", null));
		Check.Equal("menu: the farmer chat highlights the farmer home", ShellNavigation.FarmerHomeKey, ShellNavigation.ActiveKey("/farmer/assistant/5"));
	}
}
