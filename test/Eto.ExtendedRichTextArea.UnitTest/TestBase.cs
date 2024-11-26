using Eto.Forms;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class TestBase
{
	[SetUp]
	public void Setup()
	{
		if (Application.Instance == null)
		{
			Platform platform;
#if MAC
			if (EtoEnvironment.Platform.IsMac)
				platform = new Eto.Mac.Platform();
#endif
			// else if (EtoEnvironment.Platform.IsGtk)
			// 	platform = new Eto.GtkSharp.Platform();
			// else if (EtoEnvironment.Platform.IsWinForms)
			// 	platform = new Eto.WinForms.Platform();
#if WINDOWS
			if (EtoEnvironment.Platform.IsWindows)
				platform = new Eto.Wpf.Platform();
#endif
			else
				throw new NotSupportedException("Platform not supported");
			var app = new Application(platform);
			app.Attach();
		}
	}

	[TearDown]
	public void TearDown()
	{
	}	
}
