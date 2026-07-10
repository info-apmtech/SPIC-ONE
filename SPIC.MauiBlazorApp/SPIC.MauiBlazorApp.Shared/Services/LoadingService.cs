namespace SPIC.MauiBlazorApp.Shared.Services;

public class LoadingService
{
	public bool IsLoading { get; private set; }

	public event Action? OnChange;

	public void Show()
	{
		if (!IsLoading)
		{
			IsLoading = true;
			OnChange?.Invoke();
		}
	}

	public void Hide()
	{
		if (IsLoading)
		{
			IsLoading = false;
			OnChange?.Invoke();
		}
	}
}
