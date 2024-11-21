using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace EtoTextDrawable
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			Title = "My Eto Form";
			MinimumSize = new Size(200, 200);

			var richTextArea = new NewRichTextArea { Size = new Size(400, 200) };
			
			var fontSelector = new FontPicker();
			fontSelector.ValueBinding.Bind(richTextArea, r => r.InsertionFont);

			var structure = new TreeGridView
			{
				Width = 200,
				ShowHeader = false,
				Columns = {
					new GridColumn { DataCell = new TextBoxCell(0) }
				}
			};
			
			richTextArea.Document.Changed += (sender, e) => {
				var items = new TreeGridItemCollection();
				foreach (var paragraph in richTextArea.Document)
				{
					var paragraphItem = new TreeGridItem { Expanded = true};
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


			var layout = new DynamicLayout { Padding = new Padding(10), DefaultSpacing = new Size(4, 4) };

			layout.AddSeparateRow(fontSelector, null);

			layout.BeginVertical();
			layout.BeginHorizontal();
			layout.Add(richTextArea, xscale: true);
			layout.Add(structure);
			layout.EndHorizontal();
			layout.EndVertical();

			Content = layout;

			// create a few commands that can be used for the menu and toolbar
			var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
			clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

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
					new SubMenuItem { Text = "&File", Items = { clickMe } },
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

			// create toolbar			
			ToolBar = new ToolBar { Items = { clickMe } };
			
			Shown += (sender, e) => richTextArea.Focus();
			
		}
	}
}
