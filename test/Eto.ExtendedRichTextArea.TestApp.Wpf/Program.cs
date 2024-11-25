using System;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.TestApp.Wpf
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var app = new Application(Eto.Platforms.Wpf);

			app.Run(new MainForm());
		}
	}
}
