using System;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.TestApp.Gtk
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Gtk).Run(new MainForm());
		}
	}
}
