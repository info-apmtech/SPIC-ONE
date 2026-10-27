using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// Regression checks for Admin GPS Settings implementation.
/// Pins entity defaults, DTO range constraints (1-100s), controller role authorization,
/// enum index preservation, and role-based access rules.
/// </summary>
internal static class GpsSettingsTests
{
    public static void Run()
    {
        Console.WriteLine("GpsSettings");

        EntityDefaultsAreValid();
        DtoRangeValidationEnforcesOneToOneHundred();
        ControllerAuthorizationProtectsAdminEndpoints();
        PagePermissionEnumPreservesIndices();
        AdminAndSuperAdminReachGpsSettings();
        NonAdminRolesCannotReachGpsSettingsWithoutGrant();
    }

    private static void EntityDefaultsAreValid()
    {
        var entity = new GpsSetting();
        Check.Equal("GpsSetting default interval is 20 seconds", 20, entity.IntervalSeconds);
        Check.That("GpsSetting default IsActive is true", entity.IsActive);
        Check.That("GpsSetting CreatedAt is initialized", entity.CreatedAt <= DateTime.UtcNow);
    }

    private static void DtoRangeValidationEnforcesOneToOneHundred()
    {
        var validDto20 = new UpdateGpsSettingDto { IntervalSeconds = 20 };
        var validDto1 = new UpdateGpsSettingDto { IntervalSeconds = 1 };
        var validDto100 = new UpdateGpsSettingDto { IntervalSeconds = 100 };
        var invalidDto0 = new UpdateGpsSettingDto { IntervalSeconds = 0 };
        var invalidDto101 = new UpdateGpsSettingDto { IntervalSeconds = 101 };
        var invalidDtoNeg = new UpdateGpsSettingDto { IntervalSeconds = -10 };

        Check.That("20 seconds is valid", ValidateDto(validDto20));
        Check.That("1 second is valid (minimum)", ValidateDto(validDto1));
        Check.That("100 seconds is valid (maximum)", ValidateDto(validDto100));
        Check.That("0 seconds is invalid", !ValidateDto(invalidDto0));
        Check.That("101 seconds is invalid", !ValidateDto(invalidDto101));
        Check.That("-10 seconds is invalid", !ValidateDto(invalidDtoNeg));
    }

    private static bool ValidateDto(object dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
    }

    private static void ControllerAuthorizationProtectsAdminEndpoints()
    {
        var controllerType = typeof(GpsSettingController);

        var saveMethod = controllerType.GetMethod(nameof(GpsSettingController.Save));
        Check.That("Save method exists", saveMethod != null);

        var saveAuth = saveMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Check.That("Save endpoint is protected with AuthorizeAttribute", saveAuth != null);
        Check.Equal("Save endpoint requires Admin,SuperAdmin roles", "Admin,SuperAdmin", saveAuth?.Roles);

        var allMethod = controllerType.GetMethod(nameof(GpsSettingController.GetAll));
        Check.That("GetAll method exists", allMethod != null);

        var allAuth = allMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Check.That("GetAll endpoint is protected with AuthorizeAttribute", allAuth != null);
        Check.Equal("GetAll endpoint requires Admin,SuperAdmin roles", "Admin,SuperAdmin", allAuth?.Roles);

        var activeMethod = controllerType.GetMethod(nameof(GpsSettingController.GetActive));
        Check.That("GetActive method exists", activeMethod != null);

        var activeAuth = activeMethod?.GetCustomAttribute<AuthorizeAttribute>() ?? controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Check.That("GetActive endpoint requires authenticated user", activeAuth != null);
    }

    private static void PagePermissionEnumPreservesIndices()
    {
        // GpsSettings must be the very last entry to preserve existing database seed IDs
        var values = Enum.GetValues<PagePermission>();
        var lastEnum = values[^1];

        Check.Equal("GpsSettings is the last PagePermission member", PagePermission.GpsSettings, lastEnum);

        // Verify preceding member PlantDealerNatureMaster index didn't shift
        var plantDealerIndex = (int)PagePermission.PlantDealerNatureMaster;
        var gpsSettingsIndex = (int)PagePermission.GpsSettings;

        Check.That("GpsSettings index is strictly after preceding members", gpsSettingsIndex > plantDealerIndex);
        Check.Equal("RoleAccessPermissions key for GpsSettings is 'GpsSettings'", "GpsSettings", RoleAccessPermissions.KeyFor(PagePermission.GpsSettings));
    }

    private static void AdminAndSuperAdminReachGpsSettings()
    {
        var admin = PageAuthorization.GetEffectivePagePermissions(AppRole.Admin, (IEnumerable<string>?)null);
        var superAdmin = PageAuthorization.GetEffectivePagePermissions(AppRole.SuperAdmin, (IEnumerable<string>?)null);

        Check.That("Admin can reach GpsSettings via role bypass", admin.CanReach(PagePermission.GpsSettings));
        Check.That("SuperAdmin can reach GpsSettings via role bypass", superAdmin.CanReach(PagePermission.GpsSettings));
    }

    private static void NonAdminRolesCannotReachGpsSettingsWithoutGrant()
    {
        var corpAdmin = PageAuthorization.GetEffectivePagePermissions(AppRole.CorporateAdmin, (IEnumerable<string>?)null);
        var director = PageAuthorization.GetEffectivePagePermissions(AppRole.Director, (IEnumerable<string>?)null);
        var dealer = PageAuthorization.GetEffectivePagePermissions(AppRole.Dealer, (IEnumerable<string>?)null);
        var commonRole = PageAuthorization.GetEffectivePagePermissions(AppRole.CommonRole, (IEnumerable<string>?)null);
        var specialAdmin = PageAuthorization.GetEffectivePagePermissions(AppRole.SpecialAdmin, (IEnumerable<string>?)null);

        Check.That("CorporateAdmin without grant cannot reach GpsSettings", !corpAdmin.CanReach(PagePermission.GpsSettings));
        Check.That("Director without grant cannot reach GpsSettings", !director.CanReach(PagePermission.GpsSettings));
        Check.That("Dealer without grant cannot reach GpsSettings", !dealer.CanReach(PagePermission.GpsSettings));
        Check.That("CommonRole without grant cannot reach GpsSettings", !commonRole.CanReach(PagePermission.GpsSettings));
        Check.That("SpecialAdmin without grant cannot reach GpsSettings", !specialAdmin.CanReach(PagePermission.GpsSettings));
    }
}
