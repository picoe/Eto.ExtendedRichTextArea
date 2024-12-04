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
		public ExtendedRichTextArea RichTextArea { get; }

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

					var middle = new Size(width / 2, height / 2);
					var point1 = new PointF(random.Next(width), random.Next(height)) - middle;
					var point2 = new PointF(random.Next(width), random.Next(height)) - middle;
					var size = new SizeF(random.Next(width), random.Next(height));
					switch (random.Next(3))
					{
						case 0:
							graphics.FillRectangle(brush, new RectangleF(point1, size));
							break;
						case 1:
							graphics.FillEllipse(brush, new RectangleF(point1, size));
							break;
						case 2:
							graphics.DrawLine(pen, point1, point2);
							break;
					}
				}
			}

			return bitmap;
		}

		Control AttributeControls()
		{
			var attributesBinding = Binding.Property((ExtendedRichTextArea r) => r.SelectionAttributes);
			
			// Family
			var familyDropDown = new DropDown();
			familyDropDown.DropDownClosed += (sender, e) => RichTextArea.Focus();
			var families = Fonts.AvailableFontFamilies.ToList();
			/*
			families.Insert(1, Fonts.Monospace(10).Family);
			families.Insert(1, Fonts.Serif(10).Family);
			families.Insert(1, Fonts.Sans(10).Family);
			families.Insert(1, Fonts.Cursive(10).Family);
			*/
			families.Insert(0, null);
			familyDropDown.DataStore = families;
			familyDropDown.SelectedValueBinding.Bind(RichTextArea, 
				attributesBinding.Child<object>(a => a.Family).Convert(
					r => r,
					r => r
				));

			// Typeface
			var typefaceDropDown = new DropDown();
			typefaceDropDown.DropDownClosed += (sender, e) => RichTextArea.Focus();
			var dataStoreBinding = typefaceDropDown.Bind(c => c.DataStore,
				RichTextArea,
				attributesBinding.Child(a => a.Family).Convert(
					f => f?.Typefaces.Cast<object>(), null));
			var selectedBinding = typefaceDropDown.SelectedValueBinding.Bind(RichTextArea, 
				attributesBinding.Child<object>(a => a.Typeface).Convert(
					t => t, 
					t => t));
			dataStoreBinding.Changing += (sender, e) => selectedBinding.Mode = DualBindingMode.Manual;
			dataStoreBinding.Changed += (sender, e) => selectedBinding.Mode = DualBindingMode.TwoWay;

			// Size
			var sizeDropDown = new ComboBox();
			sizeDropDown.AutoComplete = true;
			sizeDropDown.TextInput += (sender, e) =>
			{
				if (!e.Text.All(char.IsDigit))
				{
					e.Cancel = true;
				}
			};
			sizeDropDown.DataStore = new List<object> { 8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };
			sizeDropDown.DropDownClosed += (sender, e) => RichTextArea.Focus();
			sizeDropDown.KeyDown += (sender, e) =>
			{
				if (e.KeyData == Keys.Enter)
				{
					RichTextArea.Focus();
					e.Handled = true;
				}
			};
			sizeDropDown.Bind(c => c.Text, RichTextArea,
				attributesBinding
				.Child(c => c.Size)
				.Convert(size => size.ToString(), s => float.TryParse(s, out var v) ? v : null));

			// Color
			var foregroundSelector = new ColorPicker { AllowAlpha = true };
			// TODO: This doesn't work on Wpf to set the focus to the richTextArea so you have to click first.
			foregroundSelector.ValueChanged += (sender, e) => Application.Instance.AsyncInvoke(RichTextArea.Focus);
			foregroundSelector.ValueBinding.Bind(RichTextArea,
				attributesBinding.Child(a => a.Foreground)
				.Convert(r => r is SolidBrush brush ? brush.Color : Colors.Transparent, r => r.A <= 0 ? null : new SolidBrush(r)));

			var backgroundSelector = new ColorPicker { AllowAlpha = true };
			// TODO: This doesn't work on Wpf to set the focus to the richTextArea so you have to click first.
			backgroundSelector.ValueChanged += (sender, e) => Application.Instance.AsyncInvoke(RichTextArea.Focus);
			backgroundSelector.ValueBinding.Bind(RichTextArea,
				attributesBinding.Child(a => a.Background)
				.Convert(r => r is SolidBrush brush ? brush.Color : Colors.Transparent, r => r.A <= 0 ? null : new SolidBrush(r)));

			// Layout
			return new TableLayout {
				Spacing = new Size(4, 4),
				Rows = {
					new TableRow("Family", familyDropDown, "Typeface", typefaceDropDown, "Size", sizeDropDown, "Fg", foregroundSelector, "Bg", backgroundSelector),
				},
			};
		}
		
		public MainForm()
		{
			Title = "My Eto Form";
			MinimumSize = new Size(200, 200);

#if UseDefaultRichTextArea
			var richTextArea = new RichTextArea { Size = new Size(700, 600) };
#else
			RichTextArea = new ExtendedRichTextArea { Size = new Size(700, 600) };
#endif

			var insertRandomTextButton = new Button { Text = "Insert Random Text" };
			insertRandomTextButton.Click += (sender, e) =>
			{
				RichTextArea.InsertText(LoremGenerator.GenerateLines(20, 20));
				RichTextArea.Focus();
			};

			var insertImageButton = new Button { Text = "Insert Image" };
			insertImageButton.Click += (sender, e) =>
			{
				RichTextArea.Insert(new ImageElement { Image = CreateRandomBitmap(200, 40) });
				RichTextArea.Focus();
			};


			var structure = new TreeGridView
			{
				Size = new Size(200, 300),
				ShowHeader = false,
				Columns = {
					new GridColumn { DataCell = new TextBoxCell(0) }
				}
			};

			var lines = new TreeGridView
			{
				Size = new Size(200, 300),
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
				foreach (var paragraph in RichTextArea.Document)
				{
					var paragraphItem = new TreeGridItem { Expanded = true };
					paragraphItem.Values = new object[] { $"Paragraph: {paragraph.DocumentStart}:{paragraph.Length}" };
					foreach (var inline in paragraph)
					{
						var inlineItem = new TreeGridItem();
						inlineItem.Values = new object[] { $"{inline.GetType().Name}: {inline.DocumentStart}:{inline.Text}" };
						paragraphItem.Children.Add(inlineItem);
					}
					items.Add(paragraphItem);
				}
				structure.DataStore = items;
				
				var linesItems = new TreeGridItemCollection();
				foreach (var line in RichTextArea.Document.EnumerateLines(0))
				{
					var lineItem = new TreeGridItem();
					lineItem.Values = new object[] { $"Line: {line.DocumentStart}:{line.Length}" };
					linesItems.Add(lineItem);
				}
				lines.DataStore = linesItems;

			};

			RichTextArea.Document.Changed += (sender, e) =>
			{
				changeTimer.Start();
			};

			RichTextArea.Document.DefaultFont = new Font("Arial", SystemFonts.Default().Size);
			// richTextArea.Document.Text = "Hello\nWorld";
			// richTextArea.Document.Text = LoremGenerator.GenerateLines(200, 20);

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

#endif

			var layout = new DynamicLayout { Padding = new Padding(10), DefaultSpacing = new Size(4, 4) };

			layout.AddSeparateRow(insertRandomTextButton, insertImageButton, null);
			layout.AddSeparateRow(AttributeControls(), null);
			layout.BeginVertical();
			layout.BeginHorizontal();
			layout.Add(RichTextArea, xscale: true);
			layout.BeginVertical();
			layout.Add(structure, yscale: true);
			layout.Add(lines, yscale: true);
			layout.EndVertical();
			layout.EndHorizontal();
			layout.EndVertical();

			Content = layout;
			Shown += (sender, e) => RichTextArea.Focus();

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
