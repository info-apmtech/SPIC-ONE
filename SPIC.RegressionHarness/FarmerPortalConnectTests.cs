using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 2: haversine distances, the dealer locator's listable rule / radius / fallback
/// chain, the call / WhatsApp / directions links, "My SPIC team" resolution and the call-back request
/// validation. All pure, on in-memory inputs.
/// </summary>
internal static class FarmerPortalConnectTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal: dealer locator, SPIC team and call-back requests");
		Geo();
		Listable();
		Locator();
		Links();
		Team();
		Requests();
		RoutesAndMenu();
		UiText();
	}

	private static void RoutesAndMenu()
	{
		var farmer = TestLogin.As(AppRole.Farmer, "");
		foreach (var url in new[] { "/farmer/dealers", "/farmer/team", "/farmer/requests" })
			Check.That($"route: farmer opens {url}", RouteAccess.CanOpen(farmer, url, null));
		Check.That("route: MDO is sent back from /farmer/team", !RouteAccess.CanOpen(TestLogin.As(AppRole.MDO, "Dashboard.View"), "/farmer/team", null));

		foreach (var role in new[] { AppRole.JMDO, AppRole.MDO, AppRole.RM, AppRole.RMD })
		{
			var staff = TestLogin.As(role, "Dashboard.View");
			Check.That($"route: {role} opens /farmer-requests without a PagePermission", RouteAccess.CanOpen(staff, "/farmer-requests", null));
			Check.That($"menu: {role} sees Farmer Call-backs", ShellNavigation.ShowFarmerRequests(staff));
		}
		Check.That("route: Admin opens /farmer-requests", RouteAccess.CanOpen(TestLogin.As(AppRole.Admin, ""), "/farmer-requests", null));
		Check.That("route: SuperAdmin opens /farmer-requests", RouteAccess.CanOpen(TestLogin.As(AppRole.SuperAdmin, ""), "/farmer-requests", null));
		Check.That("menu: Admin sees Farmer Call-backs", ShellNavigation.ShowFarmerRequests(TestLogin.As(AppRole.Admin, "")));
		Check.Equal("route: MO is sent to its landing page", LoginState.DefaultLandingPath,
			RouteAccess.Decide(TestLogin.As(AppRole.MO, "Dashboard.View"), "/farmer-requests", null).RedirectTo);
		Check.That("route: CorporateAdmin cannot open /farmer-requests", !RouteAccess.CanOpen(TestLogin.As(AppRole.CorporateAdmin, "Dashboard.View"), "/farmer-requests", null));
		Check.Equal("route: a farmer is sent home from /farmer-requests", LoginState.FarmerLandingPath,
			RouteAccess.Decide(farmer, "/farmer-requests", null).RedirectTo);
		Check.Equal("route: a farmer with a designation is sent home too", LoginState.FarmerLandingPath,
			RouteAccess.Decide(TestLogin.As(AppRole.Farmer, "SampleCollection.View"), "/farmer-requests", null).RedirectTo);
		Check.Equal("route: a JMDO without a designation still sees only Welcome", "/Welcome",
			RouteAccess.Decide(TestLogin.As(AppRole.JMDO, ""), "/farmer-requests", null).RedirectTo);
		Check.That("menu: MO and Farmer do not see Farmer Call-backs",
			!ShellNavigation.ShowFarmerRequests(TestLogin.As(AppRole.MO, "Dashboard.View")) && !ShellNavigation.ShowFarmerRequests(farmer));
		var rm = TestLogin.As(AppRole.RM, "Dashboard.View");
		Check.That("menu: Farmer Call-backs is More-sheet only (never a phone tab or rail slot)",
			ShellNavigation.GetPhoneTabs(rm).All(t => t.Key != ShellNavigation.FarmerRequestsKey)
			&& ShellNavigation.GetRailItems(rm).All(t => t.Key != ShellNavigation.FarmerRequestsKey)
			&& ShellNavigation.MoreItems(rm).Any(t => t.Key == ShellNavigation.FarmerRequestsKey));
		Check.That("menu: JMDO's scoped More sheet still lists Farmer Call-backs",
			ShellNavigation.MoreItems(TestLogin.As(AppRole.JMDO, "Dashboard.View")).Any(t => t.Key == ShellNavigation.FarmerRequestsKey));
		Check.Equal("menu: /farmer-requests highlights its own entry", ShellNavigation.FarmerRequestsKey, ShellNavigation.ActiveKey("/farmer-requests"));
		var farmerMore = ShellNavigation.MoreItems(farmer).Select(t => t.Key).ToList();
		Check.That("menu: farmer More sheet has Dealers, My SPIC team and My requests",
			new[] { ShellNavigation.FarmerDealersKey, ShellNavigation.FarmerTeamKey, ShellNavigation.FarmerMyRequestsKey }.All(farmerMore.Contains), string.Join(",", farmerMore));
		Check.That("menu: those farmer entries open for a farmer",
			ShellNavigation.MoreItems(farmer).All(t => RouteAccess.CanOpen(farmer, t.Href, null)));
		Check.That("menu: staff and Admin never get the farmer Task 2 entries",
			new[] { TestLogin.As(AppRole.MDO, "Dashboard.View"), TestLogin.As(AppRole.Admin, "") }.All(l =>
				ShellNavigation.MoreItems(l).All(t => t.Key is not (ShellNavigation.FarmerDealersKey or ShellNavigation.FarmerTeamKey or ShellNavigation.FarmerMyRequestsKey))));
		Check.That("menu: farmer phone tabs unchanged by the new entries",
			ShellNavigation.GetPhoneTabs(farmer).All(t => t.Key is not (ShellNavigation.FarmerDealersKey or ShellNavigation.FarmerTeamKey or ShellNavigation.FarmerMyRequestsKey)));
	}

	private static void UiText()
	{
		var now = new DateTime(2026, 11, 28, 6, 0, 0, DateTimeKind.Utc);           // 11:30 IST
		Check.Equal("when: today in IST", "Today, 10:15 am", FarmerConnectUi.IstWhen(new DateTime(2026, 11, 28, 4, 45, 0, DateTimeKind.Utc), now));
		Check.Equal("when: 18:45 UTC is the next IST day", "Today, 12:15 am", FarmerConnectUi.IstWhen(new DateTime(2026, 11, 27, 18, 45, 0, DateTimeKind.Utc), now));
		Check.Equal("when: yesterday", "Yesterday, 5:30 pm", FarmerConnectUi.IstWhen(new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), now));
		Check.Equal("when: this year", "3 Nov, 2:00 pm", FarmerConnectUi.IstWhen(new DateTime(2026, 11, 3, 8, 30, 0, DateTimeKind.Utc), now));
		Check.Equal("when: another year", "3 Nov 2025, 2:00 pm", FarmerConnectUi.IstWhen(new DateTime(2025, 11, 3, 8, 30, 0, DateTimeKind.Utc), now));

		var nearby = new FarmerDealerListDto { Scope = FarmerDealerScope.Nearby, RadiusKm = 25, Total = 3, UsedLocation = true, DistrictName = "Thoothukudi" };
		Check.Equal("scope title: nearby", "3 dealers within 25 km", FarmerConnectUi.ScopeTitle(nearby));
		Check.Equal("scope note: none when nearby", (string?)null, FarmerConnectUi.ScopeNote(nearby));
		var district = new FarmerDealerListDto { Scope = FarmerDealerScope.District, RadiusKm = 25, Total = 1, UsedLocation = true, DistrictName = "Thoothukudi" };
		Check.Equal("scope title: district, one dealer", "1 dealer in Thoothukudi", FarmerConnectUi.ScopeTitle(district));
		Check.Equal("scope note: GPS fell back to the district", "No SPIC dealer within 25 km of you, so we show the dealers in Thoothukudi.", FarmerConnectUi.ScopeNote(district));
		district.UsedLocation = false;
		Check.Equal("scope note: no note for a plain district list", (string?)null, FarmerConnectUi.ScopeNote(district));
		var state = new FarmerDealerListDto { Scope = FarmerDealerScope.State, Total = 12, DistrictName = "Ariyalur", StateName = "Tamil Nadu" };
		Check.Equal("scope note: state fallback", "No SPIC dealer in Ariyalur yet, so we show the dealers in Tamil Nadu.", FarmerConnectUi.ScopeNote(state));

		Check.Equal("basis note: district has none", (string?)null, FarmerConnectUi.BasisNote(new FarmerTeamDto { Basis = FarmerTeamBasis.District }));
		Check.That("basis note: nearest dealer mentions the distance",
			FarmerConnectUi.BasisNote(new FarmerTeamDto { Basis = FarmerTeamBasis.NearestDealer, DistrictName = "Ariyalur", NearestDealerKm = 14.2 })!.Contains("14 km away"));
		FarmerContactDto P(string id, AppRole role) => new() { ContactId = id, Role = role, Name = id };
		var members = new[] { P("j1", AppRole.JMDO), P("j2", AppRole.JMDO), P("m1", AppRole.MDO), P("m2", AppRole.MDO), P("r1", AppRole.RM) };
		Check.Equal("home preview: one of each role first, in team order", "j1,m1,r1", string.Join(",", FarmerConnectUi.Preview(members).Select(m => m.ContactId)));
		Check.Equal("home preview: fills with the next people", "j1,j2,m1", string.Join(",", FarmerConnectUi.Preview(members.Take(3).ToList()).Select(m => m.ContactId)));
		Check.Equal("home preview: fewer than three", "j1", string.Join(",", FarmerConnectUi.Preview(members.Take(1).ToList()).Select(m => m.ContactId)));
		Check.Equal("status hue: open is gold", FpHue.Gold, FarmerConnectUi.StatusHue(FarmerContactRequestStatus.Open));
		Check.Equal("role hue: RM is gold", FpHue.Gold, FarmerConnectUi.RoleHue(AppRole.RM));
	}

	private static void Geo()
	{
		// Kovilpatti (9.1717, 77.8691) to Thoothukudi (8.7642, 78.1348): about 53.5 km.
		var km = FarmerGeo.HaversineKm(9.1717, 77.8691, 8.7642, 78.1348);
		Check.That("haversine: Kovilpatti to Thoothukudi is about 53.5 km", Math.Abs(km - 53.5) < 1.0, $"was {km:0.00}");
		Check.That("haversine: same point is 0", FarmerGeo.HaversineKm(9.17, 77.87, 9.17, 77.87) < 1e-9);
		Check.That("haversine: one degree of latitude is about 111.2 km", Math.Abs(FarmerGeo.HaversineKm(10, 78, 11, 78) - 111.2) < 0.3);
		Check.That("haversine: symmetric", Math.Abs(FarmerGeo.HaversineKm(9, 77, 13, 80) - FarmerGeo.HaversineKm(13, 80, 9, 77)) < 1e-9);
		Check.That("haversine: Chennai to Delhi is about 1,760 km", Math.Abs(FarmerGeo.HaversineKm(13.0827, 80.2707, 28.6139, 77.2090) - 1760) < 15);

		Check.That("gps: a normal point is valid", FarmerGeo.IsValid(9.17, 77.87));
		Check.That("gps: 0,0 is no fix", !FarmerGeo.IsValid(0, 0));
		Check.That("gps: missing longitude", !FarmerGeo.IsValid(9.17, null));
		Check.That("gps: latitude out of range", !FarmerGeo.IsValid(91, 77));
		Check.That("gps: NaN", !FarmerGeo.IsValid(double.NaN, 77));

		var box = FarmerGeo.BoundingBox(9.17, 77.87, 25);
		var edgeNorth = (Lat: 9.17 + 24.9 / 111.2, Lng: 77.87);
		Check.That("bounding box: contains a point 24.9 km north",
			edgeNorth.Lat <= box.MaxLat && edgeNorth.Lat >= box.MinLat);
		var eastLng = 77.87 + 24.9 / (111.2 * Math.Cos(9.17 * Math.PI / 180));
		Check.That("bounding box: contains a point 24.9 km east", eastLng <= box.MaxLng);
		Check.Equal("coarse: three decimals", 9.172, FarmerGeo.Coarse(9.17166));
	}

	private static DealerRegistration Dealer(Action<DealerRegistration>? edit = null)
	{
		var d = new DealerRegistration
		{
			FirmName = "Demo Agro Centre", IsDealer = true, DealerType = RegistrationDealerType.Dealer,
			Status = DealerStatus.Active, ShopNoORRoomNoOrBlockNo = "", Village = "", PinCode = "",
			OfficialContactNumber = "", WhatsAppNumber = "", AccountHolderName = "", AccountNumber = "",
			Branch = "", IFSC = "", CreatedBy = "t", UpdatedBy = "t"
		};
		edit?.Invoke(d);
		return d;
	}

	private static void Listable()
	{
		Check.That("listable: an active existing dealer", FarmerDealerRules.IsListable(Dealer()));
		Check.That("listable: a bulk-loaded dealer (no type, no approval flags)",
			FarmerDealerRules.IsListable(Dealer(d => { d.DealerType = null; d.AVPApproved = null; })));
		Check.That("listable: not InActive", !FarmerDealerRules.IsListable(Dealer(d => d.Status = DealerStatus.InActive)));
		Check.That("listable: not Terminated", !FarmerDealerRules.IsListable(Dealer(d => d.Status = DealerStatus.Terminated)));
		Check.That("listable: not marked to be terminated",
			!FarmerDealerRules.IsListable(Dealer(d => d.InactiveProposal = FutureBusinessProposal.Terminated)));
		Check.That("listable: not marked not traceable",
			!FarmerDealerRules.IsListable(Dealer(d => d.InactiveProposal = FutureBusinessProposal.NotTraceable)));
		Check.That("listable: a future-business proposal alone does not hide an active dealer",
			FarmerDealerRules.IsListable(Dealer(d => d.InactiveProposal = FutureBusinessProposal.FutureBusiness)));
		Check.That("listable: not a Department registration",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsDealer = false; d.DealerType = RegistrationDealerType.Department; })));
		Check.That("listable: not an Institution",
			!FarmerDealerRules.IsListable(Dealer(d => d.DealerType = RegistrationDealerType.Institution)));
		Check.That("listable: a new dealer awaiting final approval is hidden (default Status is Active)",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.RMApproved = true; d.SMApproved = true; })));
		Check.That("listable: a new dealer rejected at AVP is hidden",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.AVPApproved = false; d.DealerCode = "D1"; })));
		Check.That("listable: a new dealer with final approval and a code is shown",
			FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.AVPApproved = true; d.DealerCode = "1000123"; })));
		Check.That("listable: approved but no dealer code yet is hidden",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.AVPApproved = true; d.DealerCode = ""; })));
		Check.That("listable: no firm name is hidden", !FarmerDealerRules.IsListable(Dealer(d => d.FirmName = "")));
		// Fix round 1 (I3): credit-limit maintenance resets AVPApproved to null on an approved new dealer.
		Check.That("listable: an approved new dealer in a maintenance cycle (AVPApproved reset, FinalApprovalDate kept) stays listed",
			FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.AVPApproved = null; d.FinalApprovalDate = new DateTime(2026, 6, 1); d.DealerCode = "1000123"; })));
		Check.That("listable: FinalApprovalDate without a dealer code is still hidden",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.FinalApprovalDate = new DateTime(2026, 6, 1); d.DealerCode = null; })));
		Check.That("listable: FinalApprovalDate does not bring back a terminated dealer",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.FinalApprovalDate = new DateTime(2026, 6, 1); d.DealerCode = "1000123"; d.Status = DealerStatus.Terminated; })));
		Check.That("listable: FinalApprovalDate does not bring back a to-be-terminated dealer",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.FinalApprovalDate = new DateTime(2026, 6, 1); d.DealerCode = "1000123"; d.Status = DealerStatus.InActive; d.InactiveProposal = FutureBusinessProposal.Terminated; })));
		Check.That("listable: FinalApprovalDate does not bring back a not-traceable dealer",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.FinalApprovalDate = new DateTime(2026, 6, 1); d.DealerCode = "1000123"; d.InactiveProposal = FutureBusinessProposal.NotTraceable; })));
		Check.That("listable: FinalApprovalDate does not bring back an Institution",
			!FarmerDealerRules.IsListable(Dealer(d => { d.IsNewDealerRegistration = true; d.FinalApprovalDate = new DateTime(2026, 6, 1); d.DealerCode = "1000123"; d.DealerType = RegistrationDealerType.Institution; })));
	}

	private static void Locator()
	{
		Check.Equal("radius: default 25", 25, FarmerDealerRules.ClampRadius(null));
		Check.Equal("radius: max 100", 100, FarmerDealerRules.ClampRadius(500));
		Check.Equal("radius: min 1", 1, FarmerDealerRules.ClampRadius(0));
		Check.Equal("radius: 50 kept", 50, FarmerDealerRules.ClampRadius(50));
		Check.Equal("paging: defaults", (1, 20), FarmerDealerRules.ClampPaging(null, null));
		Check.Equal("paging: page 0 is 1, size capped at 50", (1, 50), FarmerDealerRules.ClampPaging(0, 500));
		Check.Equal("search: blank is none", (string?)null, FarmerDealerRules.CleanSearch("   "));
		Check.Equal("search: trimmed and cut to 60", 60, FarmerDealerRules.CleanSearch(" " + new string('a', 90))!.Length);

		// Farmer at Kovilpatti (9.17, 77.87), district 1, state 1.
		var near = new FarmerDealerPoint(1, "Near Agro", 1, 1, 9.20, 77.88);       // ~3.5 km
		var mid = new FarmerDealerPoint(2, "Mid Agro", 1, 1, 9.35, 77.87);         // ~20 km
		var far = new FarmerDealerPoint(3, "Far Agro", 2, 1, 8.73, 77.70);         // ~52 km, district 2
		var noGps = new FarmerDealerPoint(4, "Aadhi Agro", 1, 1, null, null);      // district 1, no GPS
		var otherState = new FarmerDealerPoint(5, "Kerala Agro", 9, 2, 9.18, 77.86); // 1.4 km but another state
		var all = new[] { far, noGps, mid, near, otherState };

		var nearby = FarmerDealerRules.Nearby(all, 9.17, 77.87, 25);
		Check.Equal("nearby: within 25 km, nearest first (any state)", "5,1,2", string.Join(",", nearby.Select(h => h.Dealer.Id)));
		Check.That("nearby: the distance is filled", nearby.All(h => h.DistanceKm is > 0 and < 25));
		Check.Equal("nearby: a 100 km radius also finds the far one", 4, FarmerDealerRules.Nearby(all, 9.17, 77.87, 100).Count);

		var (s1, h1) = FarmerDealerRules.Select(all, 9.17, 77.87, 25, 1, 1);
		Check.Equal("select: GPS with dealers in range -> Nearby", FarmerDealerScope.Nearby, s1);
		Check.Equal("select: Nearby list", "5,1,2", string.Join(",", h1.Select(h => h.Dealer.Id)));

		// GPS far from every dealer (Chennai) -> district fallback, sorted by distance, no-GPS dealer last.
		var (s2, h2) = FarmerDealerRules.Select(all, 13.08, 80.27, 25, 1, 1);
		Check.Equal("select: nobody in range -> District", FarmerDealerScope.District, s2);
		Check.Equal("select: district list by distance, no-GPS dealer last", "2,1,4", string.Join(",", h2.Select(h => h.Dealer.Id)));

		// No GPS -> district, by name.
		var (s3, h3) = FarmerDealerRules.Select(all, null, null, 25, 1, 1);
		Check.Equal("select: no GPS -> District", FarmerDealerScope.District, s3);
		Check.Equal("select: district list by name without GPS", "4,2,1", string.Join(",", h3.Select(h => h.Dealer.Id)));
		Check.That("select: no distances without GPS", h3.All(h => h.DistanceKm is null));

		// District with no dealers -> state.
		var (s4, h4) = FarmerDealerRules.Select(all, null, null, 25, 7, 1);
		Check.Equal("select: empty district -> State", FarmerDealerScope.State, s4);
		Check.Equal("select: state list", "4,3,2,1", string.Join(",", h4.Select(h => h.Dealer.Id)));

		var (s5, h5) = FarmerDealerRules.Select(all, null, null, 25, 7, 5);
		Check.Equal("select: nothing anywhere -> None", FarmerDealerScope.None, s5);
		Check.Equal("select: None has no rows", 0, h5.Count);

		Check.Equal("scope: GPS but nothing near, nothing anywhere", FarmerDealerScope.None, FarmerDealerRules.ChooseScope(true, 0, 0, 0));
		Check.Equal("scope: nearby ignored without GPS", FarmerDealerScope.State, FarmerDealerRules.ChooseScope(false, 3, 0, 2));

		Check.Equal("distance text: metres", "850 m", FarmerDealerRules.DistanceText(0.83));
		Check.Equal("distance text: one decimal under 10 km", "4.2 km", FarmerDealerRules.DistanceText(4.24));
		Check.Equal("distance text: whole km above", "27 km", FarmerDealerRules.DistanceText(26.6));
		Check.Equal("address join skips blanks", "12, Main Road", FarmerDealerRules.JoinParts("12", " ", null, "Main Road"));
		Check.Equal("address: village = taluk only once", "North Colony, Kovilpatti", FarmerDealerRules.JoinDistinct("North Colony", "Kovilpatti", "kovilpatti"));
		Check.Equal("card address drops the village / district shown above it and repeats", "12, Main Bazaar Road, 628501",
			FarmerDealerRules.CardAddress("12, Main Bazaar Road", "Kovilpatti, Kovilpatti", "628501", "Kovilpatti", "Thoothukudi"));
		Check.Equal("card address: nothing but the pincode", "628952", FarmerDealerRules.CardAddress(null, "Kayathar", "628952", "Kayathar", "Thoothukudi"));
	}

	private static void Links()
	{
		Check.Equal("dial: mobile with +91 and spaces", "9876543210", FarmerContactLinks.DialNumber("+91 98765 43210"));
		Check.Equal("dial: landline with STD code", "04612345678", FarmerContactLinks.DialNumber("0461-2345678"));
		Check.Equal("dial: first of two numbers", "9876543210", FarmerContactLinks.DialNumber("98765 43210 / 0461 2345678"));
		Check.Equal("dial: junk gives no number", (string?)null, FarmerContactLinks.DialNumber("NA"));
		Check.Equal("dial: too short", (string?)null, FarmerContactLinks.DialNumber("12345"));
		Check.Equal("tel: mobile gets +91", "tel:+919876543210", FarmerContactLinks.TelHref("9876543210"));
		Check.Equal("tel: landline as dialled", "tel:04612345678", FarmerContactLinks.TelHref("04612345678"));
		Check.Equal("whatsapp: mobile", "https://wa.me/919876543210", FarmerContactLinks.WhatsAppHref("098765 43210"));
		Check.Equal("whatsapp: landline has none", (string?)null, FarmerContactLinks.WhatsAppHref("0461 2345678"));
		Check.Equal("map url: https kept", "https://maps.app.goo.gl/abc", FarmerContactLinks.SafeMapUrl(" https://maps.app.goo.gl/abc "));
		Check.Equal("map url: javascript refused", (string?)null, FarmerContactLinks.SafeMapUrl("javascript:alert(1)"));
		Check.Equal("map url: relative refused", (string?)null, FarmerContactLinks.SafeMapUrl("/maps/x"));
		Check.Equal("directions: GPS first", "https://www.google.com/maps/dir/?api=1&destination=9.2,77.88",
			FarmerContactLinks.DirectionsUrl(9.2, 77.88, "https://maps.app.goo.gl/abc", "x"));
		Check.Equal("directions: map link without GPS", "https://maps.app.goo.gl/abc",
			FarmerContactLinks.DirectionsUrl(null, null, "https://maps.app.goo.gl/abc", "x"));
		Check.Equal("directions: search when nothing else", "https://www.google.com/maps/search/?api=1&query=Demo%20Agro%2C%20Kovilpatti",
			FarmerContactLinks.DirectionsUrl(null, null, "javascript:x", "Demo Agro, Kovilpatti"));
		Check.Equal("display: mobile grouped", "98765 43210", FarmerContactLinks.Display("9876543210"));
	}

	private static FarmerTeamStaff Staff(string id, AppRole role, int region, int hq, string name, int state = 1, int[]? regions = null, int[]? hqs = null) =>
		new(id, role, state, region, hq, regions, hqs, name);

	private static void Team()
	{
		// District 1 has dealers at HQ 10 (two dealers) and HQ 11 (one), region 5. District 2 has none.
		var dealers = new[]
		{
			new FarmerTeamDealer(1, 1, 1, 5, 10, 9.20, 77.88),
			new FarmerTeamDealer(2, 1, 1, 5, 10, null, null),
			new FarmerTeamDealer(3, 1, 1, 5, 11, null, null),
			new FarmerTeamDealer(4, 3, 1, 6, 20, 8.50, 77.50),   // district 3, ~58 km from (9.0, 77.9)? see below
		};
		var staff = new[]
		{
			Staff("u-jmdo-10", AppRole.JMDO, 5, 10, "Priya"),
			Staff("u-jmdo-11", AppRole.JMDO, 5, 11, "Arun"),
			Staff("u-mdo-multi", AppRole.MDO, 5, 99, "Karthik", hqs: new[] { 99, 11 }),
			Staff("u-mdo-other", AppRole.MDO, 6, 20, "Meena"),
			Staff("u-rm-5", AppRole.RM, 5, 0, "Ravi"),
			Staff("u-rmd-multi", AppRole.RMD, 7, 0, "Lakshmi", regions: new[] { 7, 5 }),
			Staff("u-rm-6", AppRole.RM, 6, 0, "Suresh"),
			Staff("u-mo", AppRole.MO, 5, 10, "Not a team role"),
			Staff("u-rm-state2", AppRole.RM, 8, 0, "Other state RM", state: 2),
		};

		var r = FarmerTeamRules.Resolve(1, 1, null, null, dealers, staff);
		Check.Equal("team: district basis", FarmerTeamBasis.District, r.Basis);
		Check.Equal("team: JMDO / MDO by headquarter, RM / RMD by region, busiest HQ first",
			"u-jmdo-10,u-jmdo-11,u-mdo-multi,u-rm-5,u-rmd-multi", string.Join(",", r.Members.Select(m => m.Staff.UserId)));
		Check.Equal("team: JMDO area is its headquarter", 10, r.Members[0].AreaHqId);
		Check.Equal("team: multi-HQ MDO matched through HeadquartersIds", 11, r.Members[2].AreaHqId);
		Check.Equal("team: RMD matched through RegionIds", 5, r.Members[4].AreaRegionId);
		Check.That("team: MO is never on the farmer team", r.Members.All(m => m.Staff.Role != AppRole.MO));

		// District 2: no dealer. Farmer GPS near dealer 4 (district 3): nearest-dealer basis.
		var r2 = FarmerTeamRules.Resolve(2, 1, 8.55, 77.52, dealers, staff);
		Check.Equal("team: no dealer in district -> nearest dealer", FarmerTeamBasis.NearestDealer, r2.Basis);
		Check.Equal("team: nearest dealer's HQ / region staff", "u-mdo-other,u-rm-6", string.Join(",", r2.Members.Select(m => m.Staff.UserId)));
		Check.That("team: nearest dealer distance is reported", r2.NearestDealerKm is > 0 and < 10);

		// No dealer in district and no GPS -> the state's RMs / RMDs.
		var r3 = FarmerTeamRules.Resolve(2, 1, null, null, dealers, staff);
		Check.Equal("team: no dealer, no GPS -> state RMs", FarmerTeamBasis.State, r3.Basis);
		Check.Equal("team: state RMs only (no JMDO / MDO, no other state)", "u-rm-5,u-rm-6,u-rmd-multi",
			string.Join(",", r3.Members.Select(m => m.Staff.UserId)));

		// Nearest dealer more than 100 km away is not used.
		var r4 = FarmerTeamRules.Resolve(2, 1, 13.08, 80.27, dealers, staff);
		Check.Equal("team: nearest dealer over 100 km -> state", FarmerTeamBasis.State, r4.Basis);

		// Dealers in the district but no staff mapped to their HQ / region -> state RMs.
		var r5 = FarmerTeamRules.Resolve(1, 1, null, null, dealers, new[] { Staff("u-rm-6", AppRole.RM, 6, 0, "Suresh") });
		Check.Equal("team: dealers but nobody mapped -> state RMs", "State:u-rm-6", r5.Basis + ":" + string.Join(",", r5.Members.Select(m => m.Staff.UserId)));

		var r6 = FarmerTeamRules.Resolve(2, 3, null, null, dealers, staff);
		Check.Equal("team: nobody at all -> None", FarmerTeamBasis.None, r6.Basis);

		// One card per person; at most 8 people.
		var twoLogins = new[] { Staff("same", AppRole.JMDO, 5, 10, "Priya"), Staff("same", AppRole.MDO, 5, 10, "Priya") };
		Check.Equal("team: a person with two logins appears once", 1, FarmerTeamRules.Resolve(1, 1, null, null, dealers, twoLogins).Members.Count);
		var many = Enumerable.Range(1, 12).Select(i => Staff("j" + i, AppRole.JMDO, 5, 10, "JMDO " + i.ToString("00"))).ToArray();
		Check.Equal("team: capped at 8", FarmerTeamRules.MaxMembers, FarmerTeamRules.Resolve(1, 1, null, null, dealers, many).Members.Count);

		// Fix round 1 (I1): the login row's role must be the account role the token / staff page use.
		Check.That("team login: MDO login on an MDO account", FarmerTeamRules.IsTeamLogin(AppRole.MDO, AppRole.MDO, true, true));
		Check.That("team login: an MDO login row on an MO account is left out (its staff page would be 403)",
			!FarmerTeamRules.IsTeamLogin(AppRole.MDO, AppRole.MO, true, true));
		Check.That("team login: a JMDO login on an Admin account is left out", !FarmerTeamRules.IsTeamLogin(AppRole.JMDO, AppRole.Admin, true, true));
		Check.That("team login: MO login on an MO account is not a team role", !FarmerTeamRules.IsTeamLogin(AppRole.MO, AppRole.MO, true, true));
		Check.That("team login: inactive login or account left out",
			!FarmerTeamRules.IsTeamLogin(AppRole.RM, AppRole.RM, false, true) && !FarmerTeamRules.IsTeamLogin(AppRole.RM, AppRole.RM, true, false));
		Check.That("team login: every team-login role can open the staff page",
			FarmerTeamRules.TeamRoles.All(r => !FarmerTeamRules.IsTeamLogin(r, r, true, true) || FarmerContactRequestRules.CanUseStaffPage(r)));
		Check.Equal("role label: JMDO", "Junior Market Development Officer", FarmerTeamRules.RoleLabel(AppRole.JMDO));
		Check.Equal("role label: MDO", "Market Development Officer", FarmerTeamRules.RoleLabel(AppRole.MDO));
		Check.Equal("role label: RM", "Regional Manager", FarmerTeamRules.RoleLabel(AppRole.RM));
	}

	private static void Requests()
	{
		Check.Equal("request: a topic alone is enough", 0, FarmerContactRequestRules.Validate(FarmerContactTopic.Fertilizer, null, null).Count);
		Check.That("request: topic is required", FarmerContactRequestRules.Validate(null, "hi", null).ContainsKey("topic"));
		Check.That("request: an undefined topic is refused", FarmerContactRequestRules.Validate((FarmerContactTopic)42, "hi", null).ContainsKey("topic"));
		Check.That("request: Other needs a message", FarmerContactRequestRules.Validate(FarmerContactTopic.Other, "   ", null).ContainsKey("message"));
		Check.Equal("request: 500 characters is fine", 0,
			FarmerContactRequestRules.Validate(FarmerContactTopic.CropProblem, new string('a', 500), null).Count);
		Check.That("request: 501 characters is too long",
			FarmerContactRequestRules.Validate(FarmerContactTopic.CropProblem, new string('a', 501), null).ContainsKey("message"));
		Check.That("request: the message is trimmed before the limit",
			FarmerContactRequestRules.Validate(FarmerContactTopic.CropProblem, "  " + new string('a', 500) + "  ", null).Count == 0);
		Check.Equal("request: a listed preferred time", 0, FarmerContactRequestRules.Validate(FarmerContactTopic.SoilTest, null, "Morning").Count);
		Check.That("request: an unknown preferred time is refused",
			FarmerContactRequestRules.Validate(FarmerContactTopic.SoilTest, null, "Midnight").ContainsKey("preferredTime"));
		Check.That("request: 2 open -> may open another", FarmerContactRequestRules.CanOpenAnother(2));
		Check.That("request: 3 open -> no more", !FarmerContactRequestRules.CanOpenAnother(3));
		Check.Equal("topic labels", "Fertilizer|Crop problem|Soil test|Product|Dealer|Other",
			string.Join("|", Enum.GetValues<FarmerContactTopic>().Select(FarmerContactRequestRules.TopicLabel)));
		Check.Equal("preferred time label", "Any time", FarmerContactRequestRules.PreferredTimeLabel("Anytime"));

		Check.Equal("update: Contacted with a note", 0, FarmerContactRequestRules.ValidateUpdate(FarmerContactRequestStatus.Contacted, "Called, will visit").Count);
		Check.That("update: status required", FarmerContactRequestRules.ValidateUpdate(null, null).ContainsKey("status"));
		Check.That("update: note over 500", FarmerContactRequestRules.ValidateUpdate(FarmerContactRequestStatus.Closed, new string('n', 501)).ContainsKey("note"));

		Check.That("staff page: JMDO / MDO / RM / RMD / Admin / SuperAdmin",
			new[] { AppRole.JMDO, AppRole.MDO, AppRole.RM, AppRole.RMD, AppRole.Admin, AppRole.SuperAdmin }.All(r => FarmerContactRequestRules.CanUseStaffPage(r)));
		Check.That("staff page: not Farmer, MO, Dealer, CorporateAdmin",
			new[] { AppRole.Farmer, AppRole.MO, AppRole.Dealer, AppRole.CorporateAdmin }.All(r => !FarmerContactRequestRules.CanUseStaffPage(r)));
		// Fix round 1 (I2): the full phone only for the addressed officer.
		Check.Equal("staff phone: the addressed officer sees the full number", "9876543221", FarmerContactRequestRules.PhoneForStaffView("9876543221", true));
		Check.Equal("staff phone: anyone else gets it masked", "98xxxxxx21", FarmerContactRequestRules.PhoneForStaffView("9876543221", false));
		Check.That("staff phone: a masked number has no call link", FarmerContactLinks.TelHref("98xxxxxx21") is null);
		Check.Equal("staff phone: none stays none", (string?)null, FarmerContactRequestRules.PhoneForStaffView(" ", false));
		Check.That("staff page: Admin is a read-only viewer", FarmerContactRequestRules.IsAdminViewer(AppRole.Admin) && !FarmerContactRequestRules.IsAdminViewer(AppRole.MDO));
	}
}
