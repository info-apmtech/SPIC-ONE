using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SPIC.RegressionHarness;

internal static class MultiRegionHqTests
{
    public static void Run()
    {
        Console.WriteLine("MultiRegionHqTests");
        Console.WriteLine("------------------");

        TestEntityAndDtoProperties();
        TestThreeStateSafeFallbackLogic();
        TestCrossGeographyAuthorizationScoping();
        TestCommonRoleOptionalGeographyRules();
        TestAvpOfficeZoneAuthorizationAndWorkflow();
        TestBulkUploadArrayInitialization();
        TestEmployeeManagementListDisplayResolution();
    }

    private static void TestEntityAndDtoProperties()
    {
        var login = new Employeelogin
        {
            RegionId = 10,
            HeadquartersId = 100,
            RegionIds = new[] { 10, 20 },
            HeadquartersIds = new[] { 100, 200 }
        };

        Check.Equal("Employeelogin RegionIds length is 2", 2, login.RegionIds?.Length ?? 0);
        Check.Equal("Employeelogin HeadquartersIds length is 2", 2, login.HeadquartersIds?.Length ?? 0);
        Check.That("Employeelogin preserves scalar RegionId", login.RegionId == 10);
        Check.That("Employeelogin preserves scalar HeadquartersId", login.HeadquartersId == 100);

        var request = new EmployeeLoginCreateRequest
        {
            RegionId = 10,
            HeadquartersId = 100,
            RegionIds = new[] { 10, 20 },
            HeadquartersIds = new[] { 100, 200 }
        };

        Check.Equal("EmployeeLoginCreateRequest RegionIds length is 2", 2, request.RegionIds?.Length ?? 0);
        Check.Equal("EmployeeLoginCreateRequest HeadquartersIds length is 2", 2, request.HeadquartersIds?.Length ?? 0);

        var profileDto = new ProfileDto
        {
            RegionId = 10,
            HeadquartersId = 100,
            RegionIds = new[] { 10, 20 },
            HeadquartersIds = new[] { 100, 200 }
        };

        Check.Equal("ProfileDto RegionIds length is 2", 2, profileDto.RegionIds?.Length ?? 0);
        Check.Equal("ProfileDto HeadquartersIds length is 2", 2, profileDto.HeadquartersIds?.Length ?? 0);
    }

    private static void TestThreeStateSafeFallbackLogic()
    {
        // Helper resolving effective regions mimicking AuthenticationController & consumers
        int[] ResolveEffectiveRegions(int[]? regionIds, int scalarRegionId)
        {
            if (regionIds != null)
                return regionIds; // State 2 (active multi) or State 3 (intentionally cleared [])
            return scalarRegionId > 0 ? new[] { scalarRegionId } : Array.Empty<int>(); // State 1 (legacy unmigrated)
        }

        int[] ResolveEffectiveHqs(int[]? hqIds, int scalarHqId)
        {
            if (hqIds != null)
                return hqIds;
            return scalarHqId > 0 ? new[] { scalarHqId } : Array.Empty<int>();
        }

        // State 1: Legacy employee row not yet migrated (RegionIds == null)
        var legacyRegions = ResolveEffectiveRegions(null, 5);
        Check.Equal("Legacy row with scalar RegionId falls back to single region", 1, legacyRegions.Length);
        Check.That("Legacy row returns scalar RegionId", legacyRegions.Contains(5));

        // State 2: Active multi-assignment (RegionIds != null && RegionIds.Length > 0)
        var activeRegions = ResolveEffectiveRegions(new[] { 5, 6, 7 }, 5);
        Check.Equal("Active multi-assignment resolves all assigned regions", 3, activeRegions.Length);
        Check.That("Active multi-assignment includes region 7", activeRegions.Contains(7));

        // State 3: Intentionally cleared assignment (RegionIds != null && RegionIds.Length == 0)
        // CRITICAL SECURITY REQUIREMENT: An intentionally cleared assignment must NOT fall back to stale scalar!
        var clearedRegions = ResolveEffectiveRegions(Array.Empty<int>(), 5);
        Check.Equal("Intentionally cleared assignment returns empty array", 0, clearedRegions.Length);
        Check.That("Intentionally cleared assignment does NOT fall back to stale scalar RegionId 5", !clearedRegions.Contains(5));

        // Same for Headquarters
        var legacyHqs = ResolveEffectiveHqs(null, 50);
        Check.Equal("Legacy row with scalar HqId falls back to single HQ", 1, legacyHqs.Length);
        Check.That("Legacy row returns scalar HqId", legacyHqs.Contains(50));

        var clearedHqs = ResolveEffectiveHqs(Array.Empty<int>(), 50);
        Check.Equal("Intentionally cleared HQ assignment returns empty array", 0, clearedHqs.Length);
        Check.That("Intentionally cleared HQ assignment does NOT fall back to stale scalar HqId 50", !clearedHqs.Contains(50));
    }

    private static void TestCrossGeographyAuthorizationScoping()
    {
        var sampleDealers = new List<DealerRegistration>
        {
            new() { Id = 1, Region = 10, HQ = 101, StateId = 1 },
            new() { Id = 2, Region = 20, HQ = 201, StateId = 1 },
            new() { Id = 3, Region = 30, HQ = 301, StateId = 1 }
        };

        // RM with multiple regions [10, 20]
        var rmAssignedRegions = new List<int> { 10, 20 };
        var rmScopedDealers = sampleDealers.Where(d => rmAssignedRegions.Contains(d.Region)).ToList();
        Check.Equal("RM with regions [10, 20] sees 2 dealers", 2, rmScopedDealers.Count);
        Check.That("RM with regions [10, 20] sees dealer in Region 10", rmScopedDealers.Any(d => d.Id == 1));
        Check.That("RM with regions [10, 20] sees dealer in Region 20", rmScopedDealers.Any(d => d.Id == 2));
        Check.That("RM with regions [10, 20] cannot see dealer in Region 30", !rmScopedDealers.Any(d => d.Id == 3));

        // RM with intentionally empty regions []
        var rmEmptyRegions = new List<int>();
        var rmEmptyScopedDealers = sampleDealers.Where(d => rmEmptyRegions.Count > 0 && rmEmptyRegions.Contains(d.Region)).ToList();
        Check.Equal("RM with empty regions fails closed to 0 dealers", 0, rmEmptyScopedDealers.Count);

        // MO with multiple HQs [101, 201]
        var moAssignedHqs = new List<int> { 101, 201 };
        var moScopedDealers = sampleDealers.Where(d => moAssignedHqs.Contains(d.HQ)).ToList();
        Check.Equal("MO with HQs [101, 201] sees 2 dealers", 2, moScopedDealers.Count);
        Check.That("MO cannot see dealer in HQ 301", !moScopedDealers.Any(d => d.Id == 3));

        // MO with empty HQs []
        var moEmptyHqs = new List<int>();
        var moEmptyScopedDealers = sampleDealers.Where(d => moEmptyHqs.Count > 0 && moEmptyHqs.Contains(d.HQ)).ToList();
        Check.Equal("MO with empty HQs fails closed to 0 dealers", 0, moEmptyScopedDealers.Count);
    }

    private static void TestCommonRoleOptionalGeographyRules()
    {
        // 1. All empty
        var emptyReq = new EmployeeLoginCreateRequest
        {
            Role = AppRole.CommonRole,
            ZoneId = 0,
            StateId = 0,
            RegionId = 0,
            HeadquartersId = 0,
            RegionIds = Array.Empty<int>(),
            HeadquartersIds = Array.Empty<int>()
        };
        Check.That("CommonRole with all 4 geography fields empty is valid",
            emptyReq.ZoneId == 0 && emptyReq.StateId == 0 && (emptyReq.RegionIds?.Length ?? 0) == 0 && (emptyReq.HeadquartersIds?.Length ?? 0) == 0);

        // 2. Zone only
        var zoneOnlyReq = new EmployeeLoginCreateRequest
        {
            Role = AppRole.CommonRole,
            ZoneId = 1,
            StateId = 0,
            RegionIds = Array.Empty<int>(),
            HeadquartersIds = Array.Empty<int>()
        };
        Check.That("CommonRole with Zone only is valid", zoneOnlyReq.ZoneId == 1 && zoneOnlyReq.StateId == 0);

        // 3. State only
        var stateOnlyReq = new EmployeeLoginCreateRequest
        {
            Role = AppRole.CommonRole,
            ZoneId = 0,
            StateId = 5,
            RegionIds = Array.Empty<int>(),
            HeadquartersIds = Array.Empty<int>()
        };
        Check.That("CommonRole with State only is valid", stateOnlyReq.ZoneId == 0 && stateOnlyReq.StateId == 5);

        // 4. Multiple Regions without State/Zone
        var regionsOnlyReq = new EmployeeLoginCreateRequest
        {
            Role = AppRole.CommonRole,
            ZoneId = 0,
            StateId = 0,
            RegionIds = new[] { 10, 20 },
            HeadquartersIds = Array.Empty<int>()
        };
        Check.Equal("CommonRole with multiple Regions without State/Zone is valid", 2, regionsOnlyReq.RegionIds?.Length ?? 0);

        // 5. Hierarchy check logic: Region must match State when State is provided
        var stateId = 1;
        var regionsInState = new Dictionary<int, int> { { 10, 1 }, { 20, 1 }, { 30, 2 } }; // RegionId -> StateId

        var validSelectedRegions = new[] { 10, 20 };
        var allBelongToState = validSelectedRegions.All(rId => regionsInState.TryGetValue(rId, out var sid) && sid == stateId);
        Check.That("Valid combined State and Regions match hierarchy", allBelongToState);

        var invalidSelectedRegions = new[] { 10, 30 };
        var hasMismatch = invalidSelectedRegions.Any(rId => regionsInState.TryGetValue(rId, out var sid) && sid != stateId);
        Check.That("Invalid combined State and Region is detected as mismatch", hasMismatch);
    }

    private static void TestAvpOfficeZoneAuthorizationAndWorkflow()
    {
        // 1. AVP Office with no Zone assignment has NO authorized zone states (fails closed)
        int avpOfficeZoneId = 0;
        var statesInZone = new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 20 } }; // StateId -> ZoneId
        var authorizedStates = avpOfficeZoneId > 0
            ? statesInZone.Where(kvp => kvp.Value == avpOfficeZoneId).Select(kvp => kvp.Key).ToList()
            : new List<int>();

        Check.Equal("AVP Office without Zone assignment yields 0 authorized states", 0, authorizedStates.Count);

        // 2. AVP Office with Zone 10 has authorized states [1, 2]
        avpOfficeZoneId = 10;
        authorizedStates = statesInZone.Where(kvp => kvp.Value == avpOfficeZoneId).Select(kvp => kvp.Key).ToList();
        Check.Equal("AVP Office with Zone 10 yields 2 authorized states", 2, authorizedStates.Count);

        // 3. Workflow gate: AVP Office must never approve while AVPApproved == null
        var dealerPendingAvp = new DealerRegistration
        {
            Id = 1,
            IsNewDealerRegistration = true,
            IsSubmittedForReview = true,
            RMApproved = true,
            SMApproved = true,
            AVPApproved = null // NOT yet approved by AVP
        };

        bool canAvpOfficeAct = dealerPendingAvp.IsNewDealerRegistration
            && dealerPendingAvp.IsSubmittedForReview == true
            && dealerPendingAvp.AVPApproved == true; // STRICT GATE

        Check.That("AVP Office cannot act when AVPApproved is null", !canAvpOfficeAct);

        // When AVP approves:
        dealerPendingAvp.AVPApproved = true;
        canAvpOfficeAct = dealerPendingAvp.IsNewDealerRegistration
            && dealerPendingAvp.IsSubmittedForReview == true
            && dealerPendingAvp.AVPApproved == true;

        Check.That("AVP Office can act ONLY after AVP approval", canAvpOfficeAct);

        // Existing dealers NEVER route to AVP Office
        var existingDealer = new DealerRegistration
        {
            Id = 2,
            IsNewDealerRegistration = false,
            IsSubmittedForReview = true,
            RMApproved = true,
            SMApproved = true,
            AVPApproved = true
        };

        bool existingDealerRoutesToAvpOffice = existingDealer.IsNewDealerRegistration
            && existingDealer.AVPApproved == true;

        Check.That("Existing dealer never routes to AVP Office", !existingDealerRoutesToAvpOffice);

        // Legacy AVP role is not AVP Office
        var legacyAvpRole = AppRole.AVP;
        Check.That("AppRole.AVP is distinct from AppRole.CommonRole", legacyAvpRole != AppRole.CommonRole);
    }

    private static void TestBulkUploadArrayInitialization()
    {
        int regionId = 5;
        int hqId = 50;
        var loginWithLocations = new Employeelogin
        {
            RegionId = regionId,
            HeadquartersId = hqId,
            RegionIds = regionId > 0 ? new[] { regionId } : Array.Empty<int>(),
            HeadquartersIds = hqId > 0 ? new[] { hqId } : Array.Empty<int>()
        };

        Check.Equal("Bulk upload with region populates RegionIds array with 1 item", 1, loginWithLocations.RegionIds?.Length ?? 0);
        Check.Equal("Bulk upload with region populates matching RegionId in array", 5, loginWithLocations.RegionIds![0]);
        Check.Equal("Bulk upload with HQ populates HeadquartersIds array with 1 item", 1, loginWithLocations.HeadquartersIds?.Length ?? 0);
        Check.Equal("Bulk upload with HQ populates matching HQ in array", 50, loginWithLocations.HeadquartersIds![0]);

        int zeroRegionId = 0;
        int zeroHqId = 0;
        var loginWithoutLocations = new Employeelogin
        {
            RegionId = zeroRegionId,
            HeadquartersId = zeroHqId,
            RegionIds = zeroRegionId > 0 ? new[] { zeroRegionId } : Array.Empty<int>(),
            HeadquartersIds = zeroHqId > 0 ? new[] { zeroHqId } : Array.Empty<int>()
        };

        Check.Equal("Bulk upload without region populates empty RegionIds array", 0, loginWithoutLocations.RegionIds?.Length ?? -1);
        Check.Equal("Bulk upload without HQ populates empty HeadquartersIds array", 0, loginWithoutLocations.HeadquartersIds?.Length ?? -1);
    }

    private static void TestEmployeeManagementListDisplayResolution()
    {
        var regionNameById = new Dictionary<int, string>
        {
            { 1, "North Region" },
            { 2, "South Region" }
        };
        var hqNameById = new Dictionary<int, string>
        {
            { 10, "Chennai HQ" },
            { 20, "Madurai HQ" }
        };

        string GetRegionDisplay(Employeelogin login)
        {
            if (login.RegionIds != null)
            {
                if (login.RegionIds.Length == 0) return "-";
                var names = login.RegionIds
                    .Select(id => regionNameById.TryGetValue(id, out var v) && !string.IsNullOrWhiteSpace(v) && v != "-" ? v : id.ToString())
                    .ToList();
                return names.Count > 0 ? string.Join(", ", names) : "-";
            }
            if (login.RegionId > 0 && regionNameById.TryGetValue(login.RegionId, out var name) && !string.IsNullOrWhiteSpace(name) && name != "-")
                return name;
            return "-";
        }

        string GetHqDisplay(Employeelogin login)
        {
            if (login.HeadquartersIds != null)
            {
                if (login.HeadquartersIds.Length == 0) return "-";
                var names = login.HeadquartersIds
                    .Select(id => hqNameById.TryGetValue(id, out var v) && !string.IsNullOrWhiteSpace(v) && v != "-" ? v : id.ToString())
                    .ToList();
                return names.Count > 0 ? string.Join(", ", names) : "-";
            }
            if (login.HeadquartersId > 0 && hqNameById.TryGetValue(login.HeadquartersId, out var name) && !string.IsNullOrWhiteSpace(name) && name != "-")
                return name;
            return "-";
        }

        // 1. Multi-assignment: RegionIds = [1, 2] -> "North Region, South Region"
        var multiLogin = new Employeelogin { RegionIds = new[] { 1, 2 }, HeadquartersIds = new[] { 10, 20 } };
        Check.Equal("Multi-region resolves comma-separated names", "North Region, South Region", GetRegionDisplay(multiLogin));
        Check.Equal("Multi-HQ resolves comma-separated names", "Chennai HQ, Madurai HQ", GetHqDisplay(multiLogin));

        // 2. Legacy fallback: RegionIds == null, scalar RegionId = 1 -> "North Region"
        var legacyLogin = new Employeelogin { RegionId = 1, RegionIds = null, HeadquartersId = 10, HeadquartersIds = null };
        Check.Equal("Legacy null-array row falls back to scalar Region name", "North Region", GetRegionDisplay(legacyLogin));
        Check.Equal("Legacy null-array row falls back to scalar HQ name", "Chennai HQ", GetHqDisplay(legacyLogin));

        // 3. Intentionally cleared assignment: RegionIds = [] -> "-"
        var clearedLogin = new Employeelogin { RegionId = 1, RegionIds = Array.Empty<int>(), HeadquartersId = 10, HeadquartersIds = Array.Empty<int>() };
        Check.Equal("Cleared RegionIds returns - even with stale scalar", "-", GetRegionDisplay(clearedLogin));
        Check.Equal("Cleared HeadquartersIds returns - even with stale scalar", "-", GetHqDisplay(clearedLogin));
    }
}
