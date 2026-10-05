namespace SPIC.Ifms.Relay
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
		}

		protected override Window CreateWindow(IActivationState? activationState) =>
			new(new AppShell()) { Title = "SPIC IFMS Relay" };
	}
}
