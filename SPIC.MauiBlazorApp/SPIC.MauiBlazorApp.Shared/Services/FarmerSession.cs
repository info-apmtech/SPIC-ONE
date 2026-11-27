using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The signed-in farmer's profile, loaded once per session from GET api/Farmer/profile and shared
/// by every /farmer page (Task 1 decision: no profile claims in the JWT, so the app also knows the
/// profile state after a plain session restore). Also feeds LoginState.FarmerSasLinked so the shell
/// shows My Samples / My Reports for a linked SAS farmer. Forgets everything when the signed-in
/// user changes. Registered with AddScoped in both hosts.
/// </summary>
public sealed class FarmerSession : IDisposable
{
    private readonly FarmerApi _api;
    private readonly LoginState _login;
    private string? _loadedFor;
    private Task<bool>? _loading;
    private string? _lastUserId;

    public FarmerSession(FarmerApi api, LoginState login)
    {
        _api = api;
        _login = login;
        _login.OnChange += OnLoginChanged;
    }

    /// <summary>The saved profile, or null when the farmer has not finished onboarding yet.</summary>
    public FarmerProfileDto? Profile { get; private set; }

    /// <summary>The verified mobile (known even before the profile exists).</summary>
    public string? Phone { get; private set; }

    public bool IsLoaded => _loadedFor is not null && _loadedFor == _login.UserId;

    public bool ProfileComplete => Profile?.ProfileComplete == true;

    /// <summary>The last load problem in plain words (null when fine).</summary>
    public string? LoadError { get; private set; }

    /// <summary>
    /// Loads the profile unless it is already loaded for this user (force: always). Returns false
    /// when it could not be loaded (offline, server error, or the caller is not a farmer).
    /// </summary>
    public Task<bool> EnsureLoadedAsync(bool force = false)
    {
        if (_login.UserRole != AppRole.Farmer) return Task.FromResult(false);
        if (!force && IsLoaded) return Task.FromResult(true);
        if (!force && _loading is { IsCompleted: false }) return _loading;
        return _loading = LoadAsync();
    }

    private async Task<bool> LoadAsync()
    {
        var userId = _login.UserId;
        var result = await _api.GetProfileAsync();
        if (userId != _login.UserId) return false; // signed out / switched user meanwhile

        if (!result.Ok)
        {
            LoadError = result.Message;
            return false;
        }

        LoadError = null;
        Profile = result.Profile;
        Phone = result.Phone ?? result.Profile?.Phone;
        _loadedFor = userId;
        _login.SetFarmerSasLinked(result.SasLinked);
        return true;
    }

    /// <summary>After a successful save: keep the returned profile without another round trip.</summary>
    public void Update(FarmerProfileDto profile)
    {
        Profile = profile;
        Phone = profile.Phone;
        _loadedFor = _login.UserId;
        LoadError = null;
        _login.SetFarmerSasLinked(profile.SasLinked);
    }

    /// <summary>Forgets the cached profile (after a new OTP sign-in, or when the signed-in user changes).</summary>
    public void Reset()
    {
        Profile = null;
        Phone = null;
        _loadedFor = null;
        _loading = null;
        LoadError = null;
    }

    private void OnLoginChanged()
    {
        if (_login.UserId == _lastUserId && _login.IsLoggedIn) return;
        _lastUserId = _login.IsLoggedIn ? _login.UserId : null;
        if (_loadedFor is not null && _loadedFor != _lastUserId) Reset();
    }

    public void Dispose() => _login.OnChange -= OnLoginChanged;
}
