using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 1 (docs/farmer-portal.md): phone normalisation, the OTP rules
/// (hash, constant-time compare, attempt / expiry, cooldown and hourly limits, client IP) and the
/// profile validation. All pure: the controllers only load rows and apply these decisions.
/// </summary>
internal static class FarmerPortalAuthTests
{
	private static readonly DateTime Now = new(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

	public static void Run()
	{
		Console.WriteLine("Farmer portal: phone, OTP and profile rules");
		Phone();
		Codes();
		Verify();
		SendLimits();
		ClientIp();
		Profile();
	}

	private static void Phone()
	{
		Check.Equal("phone: plain 10 digits kept", "9876543210", FarmerPhone.Normalise("9876543210"));
		Check.Equal("phone: +91 and spaces stripped", "9876543210", FarmerPhone.Normalise(" +91 98765 43210 "));
		Check.Equal("phone: 91 prefix on 12 digits stripped", "9876543210", FarmerPhone.Normalise("919876543210"));
		Check.Equal("phone: leading 0 stripped", "9876543210", FarmerPhone.Normalise("09876543210"));
		Check.Equal("phone: dashes and brackets stripped", "9876543210", FarmerPhone.Normalise("(+91) 98765-43210"));
		Check.Equal("phone: 0091 prefix stripped", "9876543210", FarmerPhone.Normalise("00919876543210"));
		Check.Equal("phone: a 10-digit number starting 91 is NOT cut", "9198765432", FarmerPhone.Normalise("9198765432"));
		Check.Equal("phone: null is empty", "", FarmerPhone.Normalise(null));

		Check.That("phone: 6-9 start is valid", FarmerPhone.IsValid("6000000000") && FarmerPhone.IsValid("9999999999"));
		Check.That("phone: 5 start is invalid", !FarmerPhone.IsValid("5876543210"));
		Check.That("phone: 9 digits invalid", !FarmerPhone.IsValid("987654321"));
		Check.That("phone: 11 digits invalid", !FarmerPhone.IsValid("98765432101"));
		Check.That("phone: letters invalid", !FarmerPhone.IsValid("98765abc10"));
		Check.That("phone: empty invalid", !FarmerPhone.IsValid(""));

		Check.Equal("phone: masked for logs", "98xxxxxx21", FarmerPhone.Mask("9876543221"));
		Check.Equal("phone: user name", "f9876543210", FarmerPhone.UserNameFor("9876543210"));
		Check.That("phone: stored variants include +91 form", FarmerPhone.StoredVariants("9876543210").Contains("+919876543210"));
	}

	private static void Codes()
	{
		var codes = Enumerable.Range(0, 200).Select(_ => FarmerOtpRules.GenerateCode()).ToList();
		Check.That("otp: codes are always 6 digits", codes.All(FarmerOtpRules.IsWellFormedCode));
		Check.That("otp: codes vary", codes.Distinct().Count() > 150);
		Check.That("otp: 5 digits is not a code", !FarmerOtpRules.IsWellFormedCode("12345"));
		Check.That("otp: letters are not a code", !FarmerOtpRules.IsWellFormedCode("12a456"));

		var hash = FarmerOtpRules.HashCode("123456", "9876543210", "pepper");
		Check.Equal("otp: hash is hex SHA-256", 64, hash.Length);
		Check.Equal("otp: hash is stable", hash, FarmerOtpRules.HashCode("123456", "9876543210", "pepper"));
		Check.That("otp: hash depends on the phone", hash != FarmerOtpRules.HashCode("123456", "9876543211", "pepper"));
		Check.That("otp: hash depends on the pepper", hash != FarmerOtpRules.HashCode("123456", "9876543210", "other"));
		Check.That("otp: hash depends on the code", hash != FarmerOtpRules.HashCode("123457", "9876543210", "pepper"));
		Check.That("otp: equal hashes match", FarmerOtpRules.HashesMatch(hash, FarmerOtpRules.HashCode("123456", "9876543210", "pepper")));
		Check.That("otp: different hashes do not match", !FarmerOtpRules.HashesMatch(hash, FarmerOtpRules.HashCode("000000", "9876543210", "pepper")));
		Check.That("otp: different length does not match", !FarmerOtpRules.HashesMatch(hash, hash[..10]));
	}

	private static void Verify()
	{
		var stored = FarmerOtpRules.HashCode("123456", "9876543210", "p");
		var right = stored;
		var wrong = FarmerOtpRules.HashCode("654321", "9876543210", "p");
		var expires = Now.AddMinutes(5);

		Check.Equal("verify: right code in time is Ok", OtpVerifyOutcome.Ok,
			FarmerOtpRules.DecideVerify(Now, expires, null, 0, stored, right));
		Check.Equal("verify: wrong code", OtpVerifyOutcome.WrongCode,
			FarmerOtpRules.DecideVerify(Now, expires, null, 0, stored, wrong));
		Check.Equal("verify: right code on the 5th try still Ok", OtpVerifyOutcome.Ok,
			FarmerOtpRules.DecideVerify(Now, expires, null, 4, stored, right));
		Check.Equal("verify: after 5 wrong tries even the right code is refused", OtpVerifyOutcome.TooManyAttempts,
			FarmerOtpRules.DecideVerify(Now, expires, null, 5, stored, right));
		Check.Equal("verify: expired at exactly 5 minutes", OtpVerifyOutcome.NoChallenge,
			FarmerOtpRules.DecideVerify(expires, expires, null, 0, stored, right));
		Check.Equal("verify: consumed code cannot be reused", OtpVerifyOutcome.NoChallenge,
			FarmerOtpRules.DecideVerify(Now, expires, Now.AddMinutes(-1), 0, stored, right));
		Check.Equal("verify: no challenge", OtpVerifyOutcome.NoChallenge,
			FarmerOtpRules.DecideVerify(Now, null, null, 0, null, right));

		Check.Equal("verify: 4 tries left after the first wrong code", 4, FarmerOtpRules.TriesLeftAfterWrong(0));
		Check.Equal("verify: none left after the fifth wrong code", 0, FarmerOtpRules.TriesLeftAfterWrong(4));
		Check.Equal("verify: code lives 5 minutes", TimeSpan.FromMinutes(5), FarmerOtpRules.CodeLifetime);
	}

	private static void SendLimits()
	{
		var none = Array.Empty<DateTime>();
		Check.Equal("send: first code allowed", OtpSendOutcome.Allowed, FarmerOtpRules.DecideSend(Now, none, 0).Outcome);

		var cooldown = FarmerOtpRules.DecideSend(Now, new[] { Now.AddSeconds(-10) }, 1);
		Check.Equal("send: 10 s after the last code is cooldown", OtpSendOutcome.Cooldown, cooldown.Outcome);
		Check.Equal("send: cooldown says 20 s left", 20, cooldown.RetryAfterSeconds);
		Check.Equal("send: 30 s after the last code is allowed", OtpSendOutcome.Allowed,
			FarmerOtpRules.DecideSend(Now, new[] { Now.AddSeconds(-30) }, 1).Outcome);

		var fourInHour = Enumerable.Range(1, 4).Select(i => Now.AddMinutes(-10 * i)).ToList();
		Check.Equal("send: 5th code in the hour allowed", OtpSendOutcome.Allowed, FarmerOtpRules.DecideSend(Now, fourInHour, 4).Outcome);

		var fiveInHour = Enumerable.Range(1, 5).Select(i => Now.AddMinutes(-10 * i)).ToList(); // -10 .. -50 min
		var limited = FarmerOtpRules.DecideSend(Now, fiveInHour, 5);
		Check.Equal("send: 6th code in the hour refused", OtpSendOutcome.PhoneHourLimit, limited.Outcome);
		Check.Equal("send: free again when the oldest send leaves the hour (10 min)", 600, limited.RetryAfterSeconds);

		var oldOnes = Enumerable.Range(1, 5).Select(i => Now.AddMinutes(-61 - i)).ToList();
		Check.Equal("send: sends older than an hour do not count", OtpSendOutcome.Allowed, FarmerOtpRules.DecideSend(Now, oldOnes, 0).Outcome);

		Check.Equal("send: 20 from one IP in the hour refused", OtpSendOutcome.IpHourLimit, FarmerOtpRules.DecideSend(Now, none, 20).Outcome);
		Check.Equal("send: 19 from one IP allowed", OtpSendOutcome.Allowed, FarmerOtpRules.DecideSend(Now, none, 19).Outcome);
		Check.Equal("send: cooldown wins over the hour limit", OtpSendOutcome.Cooldown,
			FarmerOtpRules.DecideSend(Now, fiveInHour.Append(Now.AddSeconds(-5)), 20).Outcome);
	}

	private static void ClientIp()
	{
		Check.Equal("ip: socket address without a proxy", "10.0.0.5", FarmerOtpRules.ClientIp(null, "10.0.0.5"));
		Check.Equal("ip: the proxy-appended (last) forwarded entry wins", "203.0.113.9",
			FarmerOtpRules.ClientIp("1.2.3.4, 203.0.113.9", "10.0.0.5"));
		Check.Equal("ip: single forwarded entry", "203.0.113.9", FarmerOtpRules.ClientIp("203.0.113.9", "10.0.0.5"));
		Check.Equal("ip: nothing known", null, FarmerOtpRules.ClientIp("", null));
	}

	private static void Profile()
	{
		static FarmerProfileUpsertDto Valid() => new()
		{
			Name = "Murugan K",
			StateId = 1,
			DistrictId = 2,
			Village = "Kovilpatti",
			PinCode = "628501",
			PrimaryCrops = new() { "Paddy", " banana ", "paddy", "", "Chilli, red" },
			LandAcres = 2.5m,
			IrrigationType = FarmerIrrigationType.Borewell,
			PreferredLanguage = "ta"
		};

		Check.Equal("profile: a valid form has no errors", 0, FarmerProfileRules.Validate(Valid()).Count);

		var noEmail = Valid(); noEmail.Email = "  ";
		Check.Equal("profile: email is optional", 0, FarmerProfileRules.Validate(noEmail).Count);

		Expect("profile: short name refused", d => d.Name = "A", "name");
		Expect("profile: 101-letter name refused", d => d.Name = new string('a', 101), "name");
		Expect("profile: bad email refused", d => d.Email = "murugan@", "email");
		Expect("profile: email with space refused", d => d.Email = "mur ugan@x.in", "email");
		Expect("profile: missing state refused", d => d.StateId = 0, "stateId");
		Expect("profile: missing district refused", d => d.DistrictId = 0, "districtId");
		Expect("profile: missing village refused", d => d.Village = " ", "village");
		Expect("profile: 5-digit pincode refused", d => d.PinCode = "62850", "pinCode");
		Expect("profile: letters in pincode refused", d => d.PinCode = "62850a", "pinCode");
		Expect("profile: negative land refused", d => d.LandAcres = -1, "landAcres");
		Expect("profile: land above 10000 acres refused", d => d.LandAcres = 10000.5m, "landAcres");
		Expect("profile: latitude without longitude refused", d => d.Latitude = 9.1, "latitude");
		Expect("profile: latitude above 90 refused", d => { d.Latitude = 91; d.Longitude = 78; }, "latitude");
		Expect("profile: longitude below -180 refused", d => { d.Latitude = 9; d.Longitude = -181; }, "latitude");
		Expect("profile: unknown language refused", d => d.PreferredLanguage = "fr", "preferredLanguage");
		Expect("profile: undefined irrigation value refused", d => d.IrrigationType = (FarmerIrrigationType)42, "irrigationType");
		Expect("profile: more than 15 crops refused", d => d.PrimaryCrops = Enumerable.Range(0, 16).Select(i => "Crop" + i).ToList(), "primaryCrops");

		var located = Valid(); located.Latitude = 9.17; located.Longitude = 77.87;
		Check.Equal("profile: a real location is fine", 0, FarmerProfileRules.Validate(located).Count);

		Check.Equal("profile: crops trimmed, de-duplicated, commas removed", "Paddy,banana,Chilli  red",
			FarmerProfileRules.JoinCrops(Valid().PrimaryCrops));
		Check.Equal("profile: no crops stored as null", null, FarmerProfileRules.JoinCrops(new[] { " ", "" }));
		Check.Equal("profile: stored crops split back", 2, FarmerProfileRules.SplitCrops("Paddy,Banana").Count);

		Check.That("profile: complete with name, state, district, village, pincode",
			FarmerProfileRules.IsComplete("Murugan", 1, 2, "Kovilpatti", "628501"));
		Check.That("profile: incomplete without a village", !FarmerProfileRules.IsComplete("Murugan", 1, 2, "", "628501"));
		Check.That("profile: incomplete without a district", !FarmerProfileRules.IsComplete("Murugan", 1, 0, "Kovilpatti", "628501"));
		Check.That("profile: seven languages offered", FarmerProfileRules.Languages.Count == 7 && FarmerProfileRules.IsKnownLanguage("ml"));
	}

	private static void Expect(string name, Action<FarmerProfileUpsertDto> change, string field)
	{
		var dto = new FarmerProfileUpsertDto
		{
			Name = "Murugan K", StateId = 1, DistrictId = 2, Village = "Kovilpatti", PinCode = "628501"
		};
		change(dto);
		var errors = FarmerProfileRules.Validate(dto);
		Check.That(name, errors.ContainsKey(field) && errors.Count == 1,
			$"errors were [{string.Join(", ", errors.Keys)}]");
	}
}
