﻿using System;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.TestApp.Wpf
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Wpf).Run(new MainForm());
		}
	}
}