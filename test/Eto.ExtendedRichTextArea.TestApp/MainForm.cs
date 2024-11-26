//#define UseDefaultRichTextArea
using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Eto.ExtendedRichTextArea;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.TestApp
{
	public partial class MainForm : Form
	{
		public Bitmap CreateRandomBitmap(int width = 200, int height = 50)
		{
			var random = new Random();
			var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgba);

			using (var graphics = new Graphics(bitmap))
			{

				for (int i = 0; i < 10; i++)
				{
					var brush = new SolidBrush(Color.FromArgb(random.Next(256), random.Next(256), random.Next(256), random.Next(256)));
					var pen = new Pen(brush, random.Next(1, 5));

					switch (random.Next(3))
					{
						case 0:
							graphics.FillRectangle(brush, random.Next(width), random.Next(height), random.Next(width), random.Next(height));
							break;
						case 1:
							graphics.FillEllipse(brush, random.Next(width), random.Next(height), random.Next(width), random.Next(height));
							break;
						case 2:
							graphics.DrawLine(pen, random.Next(width), random.Next(height), random.Next(width), random.Next(height));
							break;
					}
				}
			}

			return bitmap;
		}
		
		public MainForm()
		{
			Title = "My Eto Form";
			MinimumSize = new Size(200, 200);

#if UseDefaultRichTextArea
			var richTextArea = new RichTextArea { Size = new Size(700, 600) };
#else
			var richTextArea = new ExtendedRichTextArea { Size = new Size(700, 600) };
#endif

			var initialText = LoremGenerator.GenerateLines(200, 20);

			var insertRandomTextButton = new Button { Text = "Insert Random Text" };
			insertRandomTextButton.Click += (sender, e) =>
			{
				richTextArea.InsertText(LoremGenerator.GenerateLines(20, 20));
			};

			var insertImageButton = new Button { Text = "Insert Image" };
			insertImageButton.Click += (sender, e) =>
			{
				richTextArea.Insert(new ImageElement { Image = CreateRandomBitmap(200, 40) });
			};

			var fontSelector = new FontPicker();
			fontSelector.ValueBinding.Bind(richTextArea, r => r.SelectionFont);
			fontSelector.GotFocus += (sender, e) => richTextArea.Focus();
			fontSelector.ValueChanged += (sender, e) => richTextArea.Focus();

			var colorSelector = new ColorPicker { AllowAlpha = true };
			// TODO: This doesn't work on Wpf to set the focus to the richTextArea so you have to click first.
			colorSelector.ValueChanged += (sender, e) => Application.Instance.AsyncInvoke(richTextArea.Focus);
			colorSelector.ValueBinding.Bind(richTextArea,
				Binding.Property((ExtendedRichTextArea r) => r.SelectionBrush)
				.Convert(r => r is SolidBrush brush ? brush.Color : Colors.Black, r => new SolidBrush(r)));

			var structure = new TreeGridView
			{
				Size = new Size(200, 600),
				ShowHeader = false,
				Columns = {
					new GridColumn { DataCell = new TextBoxCell(0) }
				}
			};

			var lines = new TreeGridView
			{
				Size = new Size(200, 600),
				ShowHeader = false,
				Columns = {
					new GridColumn { DataCell = new TextBoxCell(0) }
				}
			};

#if !UseDefaultRichTextArea
			var changeTimer = new UITimer { Interval = 1 };
			changeTimer.Elapsed += (sender, e) =>
			{
				changeTimer.Stop();
				var items = new TreeGridItemCollection();
				foreach (var paragraph in richTextArea.Document)
				{
					var paragraphItem = new TreeGridItem { Expanded = true };
					paragraphItem.Values = new object[] { $"Paragraph: {paragraph.DocumentStart}:{paragraph.Length}" };
					foreach (var run in paragraph)
					{
						var runItem = new TreeGridItem { Expanded = true };
						runItem.Values = new object[] { $"Run: {run.DocumentStart}:{run.Length}" };
						foreach (var span in run)
						{
							var spanItem = new TreeGridItem();
							spanItem.Values = new object[] { $"{span.GetType().Name}: {span.DocumentStart}:{span.Text}" };
							runItem.Children.Add(spanItem);
						}
						paragraphItem.Children.Add(runItem);
					}
					items.Add(paragraphItem);
				}
				structure.DataStore = items;
				
				var linesItems = new TreeGridItemCollection();
				foreach (var line in richTextArea.Document.EnumerateLines(0))
				{
					var lineItem = new TreeGridItem();
					lineItem.Values = new object[] { $"Line: {line.DocumentStart}:{line.Length}" };
					linesItems.Add(lineItem);
				}
				lines.DataStore = linesItems;

			};

			richTextArea.Document.Changed += (sender, e) =>
			{
				changeTimer.Start();
			};

			richTextArea.Document.DefaultFont = Fonts.Monospace(SystemFonts.Default().Size);
			richTextArea.Document.Text = "Hello\nWorld";
			// richTextArea.Document.Text = initialText;

			// var bmp = new Bitmap(200, 25, PixelFormat.Format32bppRgba);
			// using (var g = new Graphics(bmp))
			// {
			// 	g.DrawLine(Colors.Black, 0, 0, 200, 25);
			// 	g.DrawLine(Colors.Black, 0, 25, 200, 0);
			// 	g.DrawText(richTextArea.SelectionFont, new SolidBrush(SystemColors.ControlText), new PointF(0, 0), "Hello World");
			// }
			// var imageElement = new ImageElement { Image = CreateRandomBitmap(200, 40) };
			// var para = richTextArea.Document.Skip(2).FirstOrDefault();

			// para.FirstOrDefault()?.InsertAt(0, imageElement);
			// para.FirstOrDefault()?.InsertAt(0, new Span {  Text = "Image:"});

#else
			richTextArea.Text = initialText;

#endif

			var layout = new DynamicLayout { Padding = new Padding(10), DefaultSpacing = new Size(4, 4) };

			layout.AddSeparateRow(fontSelector, colorSelector, insertRandomTextButton, insertImageButton, null);

			layout.BeginVertical();
			layout.BeginHorizontal();
			layout.Add(richTextArea, xscale: true);
			layout.BeginVertical();
			layout.Add(structure, yscale: true);
			layout.Add(lines, yscale: true);
			layout.EndVertical();
			layout.EndHorizontal();
			layout.EndVertical();

			Content = layout;
			Shown += (sender, e) => richTextArea.Focus();

			CreateMenu();

		}

		private void CreateMenu()
		{
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
		}
	}
}
