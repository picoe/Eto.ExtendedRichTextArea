//#define UseDefaultRichTextArea
using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Eto.ExtendedRichTextArea;

namespace EtoTextDrawable
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			Title = "My Eto Form";
			MinimumSize = new Size(200, 200);


#if UseDefaultRichTextArea
			var richTextArea = new RichTextArea { Size = new Size(700, 600) };
#else
			var richTextArea = new NewRichTextArea { Size = new Size(700, 600) };
#endif

			var initialText = LoremGenerator.GenerateLines(10, 10);

			var fontSelector = new FontPicker();
			fontSelector.ValueBinding.Bind(richTextArea, r => r.SelectionFont);

			var structure = new TreeGridView
			{
				Size = new Size(200, 600),
				ShowHeader = false,
				Columns = {
					new GridColumn { DataCell = new TextBoxCell(0) }
				}
			};

			IDocumentElement element = new Run();
			element.Start = 0;
#if !UseDefaultRichTextArea
			var changeTimer = new UITimer { Interval = 1 };
			changeTimer.Elapsed += (sender, e) =>
			{
				changeTimer.Stop();
				var items = new TreeGridItemCollection();
				foreach (var paragraph in richTextArea.Document)
				{
					var paragraphItem = new TreeGridItem { Expanded = true };
					paragraphItem.Values = new object[] { $"Paragraph: {paragraph.Start}:{paragraph.Length}" };
					foreach (var run in paragraph)
					{
						var runItem = new TreeGridItem { Expanded = true };
						runItem.Values = new object[] { $"Run: {run.Start}:{run.Length}" };
						foreach (var span in run)
						{
							var spanItem = new TreeGridItem();
							spanItem.Values = new object[] { $"Span: {span.Start}:{span.Text}" };
							runItem.Children.Add(spanItem);
						}
						paragraphItem.Children.Add(runItem);
					}
					items.Add(paragraphItem);
				}
				structure.DataStore = items;

			};


			richTextArea.Document.Changed += (sender, e) =>
			{
				changeTimer.Start();
			};

			richTextArea.Document.Text = initialText;
#else
			richTextArea.Text = initialText;

#endif


			var layout = new DynamicLayout { Padding = new Padding(10), DefaultSpacing = new Size(4, 4) };

			layout.AddSeparateRow(fontSelector, null);

			layout.BeginVertical();
			layout.BeginHorizontal();
			layout.Add(richTextArea, xscale: true);
			layout.Add(structure);
			layout.EndHorizontal();
			layout.EndVertical();

			Content = layout;


			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();

			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

			// create menu
			Menu = new MenuBar
			{
				Items =
				{
					// File submenu
					// new SubMenuItem { Text = "&File", Items = { clickMe } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
				ApplicationItems =
				{
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
				},
				QuitItem = quitCommand,
				AboutItem = aboutCommand
			};


			Shown += (sender, e) => richTextArea.Focus();



		}
	}
}
