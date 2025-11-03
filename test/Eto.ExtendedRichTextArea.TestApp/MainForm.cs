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

		readonly TreeGridView _structure = new();
		readonly TreeGridView _lines = new();

		(RectangleF bounds, Color color)? _highlightedBounds;

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
			var attributesBinding = Binding.Property((ExtendedRichTextArea r) => r.SelectionAttributes).Convert(
				r => r,
				r => r
			);
			
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


			var superscriptCheckBox = new CheckBox { Text = "Superscript" };
			superscriptCheckBox.CheckedBinding.Bind(RichTextArea,
				attributesBinding.Child(a => a.Superscript));
			superscriptCheckBox.CheckedChanged += (sender, e) => RichTextArea.Focus();

			var subscriptCheckBox = new CheckBox { Text = "Subscript" };
			subscriptCheckBox.CheckedBinding.Bind(RichTextArea,
				attributesBinding.Child(a => a.Subscript));
			subscriptCheckBox.CheckedChanged += (sender, e) => RichTextArea.Focus();

			var underlineCheckBox = new CheckBox { Text = "Underline" };
			underlineCheckBox.CheckedBinding.Bind(RichTextArea,
				attributesBinding.Child(a => a.Underline));
			underlineCheckBox.CheckedChanged += (sender, e) => RichTextArea.Focus();

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
				.Convert(
					size => size.ToString(),
					s => float.TryParse(s, out var v) ? v : null));

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
					new TableRow("Family", familyDropDown, "Typeface", typefaceDropDown, "Size", sizeDropDown, underlineCheckBox, superscriptCheckBox, subscriptCheckBox, "Fg", foregroundSelector, "Bg", backgroundSelector),
				},
			};
		}
		
		public MainForm()
		{
			Title = "ExtendedRichTextArea Test App";
			MinimumSize = new Size(200, 200);

#if UseDefaultRichTextArea
			var richTextArea = new RichTextArea { Size = new Size(700, 600) };
#else
			RichTextArea = new ExtendedRichTextArea { Size = new Size(700, 600) };

			var drawable = RichTextArea.FindChild<Drawable>();
			drawable.Paint += drawable_Paint;
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

			var insertListButton = new SegmentedButton();
			
			var insertListMenu = new MenuSegmentedItem { Text = "Insert List" };
			
			void InsertList(ListType type)
			{
				var list = new ListElement { Type = type };
				list.Add(new ListItemElement());
				RichTextArea.Insert(list);
				RichTextArea.Focus();
			}
			insertListMenu.Menu = new ContextMenu
			{
				Items =
				{
					new ButtonMenuItem { Text = "Unordered List", Command = new Command((s, e) => InsertList(ListType.Unordered)) },
					new ButtonMenuItem { Text = "Ordered List", Command = new Command((s, e) => InsertList(ListType.Ordered)) },
				}
			};
			insertListButton.Items.Add(insertListMenu);

			var setSelectedText = new Button { Text = "Set SelectedText" };
			setSelectedText.Click += (sender, e) =>
			{
				var dlg = new Dialog<bool>();
				var edit = new TextArea { Text = RichTextArea.SelectionText, Width = 300 };
				edit.SelectAll();
				dlg.Content = new TableLayout
				{
					Spacing = new Size(5, 5),
					Rows = { new TableRow("SelectedText:", edit) }
				};
				dlg.PositiveButtons.Add(dlg.DefaultButton = new Button((s, e) => dlg.Close(true)) { Text = "Ok" });
				dlg.NegativeButtons.Add(dlg.AbortButton = new Button((s, e) => dlg.Close(false)) { Text = "Cancel" });
				dlg.Shown += (s, e) => edit.Focus();
				if (dlg.ShowModal(setSelectedText))
				{
					RichTextArea.SelectionText = edit.Text;
				}
			};
			
			var clearButton = new Button { Text = "Clear" };
			clearButton.Click += (sender, e) =>
			{
				RichTextArea.Document.Clear();
				RichTextArea.Focus();
			};


			_structure.Size = new Size(200, 300);
			_structure.ShowHeader = false;
			_structure.Columns.Add(new GridColumn { DataCell = new TextBoxCell(0), AutoSize = true });
			_structure.CellDoubleClick += (sender, e) =>
			{
				if (e.Item is TreeGridItem item && item.Tag != null)
				{
					ShowProperties(item.Tag);
				}
			};



			_lines.Size = new Size(200, 300);
			_lines.ShowHeader = false;
			_lines.Columns.Add(new GridColumn { DataCell = new TextBoxCell(0), AutoSize = true });
			_lines.CellDoubleClick += (sender, e) =>
			{
				if (e.Item is TreeGridItem item && item.Tag != null)
				{
					ShowProperties(item.Tag);
				}
			};
			_lines.SelectedItemsChanged += (sender, e) =>
			{
				if (_highlightedBounds.HasValue)
				{
					var (bounds, color) = _highlightedBounds.Value;
					_highlightedBounds = null;
				}
				if (_lines.SelectedItem is TreeGridItem item && item.Tag is Line line)
				{
					_highlightedBounds = (line.Bounds, Colors.Orange);
				}
				else if (_lines.SelectedItem is TreeGridItem chunkItem && chunkItem.Tag is Chunk chunk)
				{
					_highlightedBounds = (chunk.Bounds, Colors.Green);
				}
				RichTextArea.Invalidate();
			};

#if !UseDefaultRichTextArea
			var changeTimer = new UITimer { Interval = 1 };
			changeTimer.Elapsed += (sender, e) =>
			{
				changeTimer.Stop();
				Application.Instance.AsyncInvoke(UpdateStructure);
				Application.Instance.AsyncInvoke(UpdateLines);
			};

			RichTextArea.Document.Changed += (sender, e) =>
			{
				changeTimer.Start();
				_highlightedBounds = null;
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

			var _status = new Label();
			var binding = _status.Bind(c => c.Text, RichTextArea, r => $"Document Length: {r.Document.Length}, Selection: {r.Selection.Start}-{r.Selection.End} ({r.Selection.Length}), Caret: {r.CaretIndex}");
			RichTextArea.SelectionChanged += (s, e) => binding.Update();

			var mainSplitter = new Splitter { Orientation = Orientation.Horizontal, FixedPanel = SplitterFixedPanel.Panel2 };
			mainSplitter.Panel1 = RichTextArea;
			mainSplitter.Panel2 = new Splitter
			{
				Orientation = Orientation.Vertical,
				FixedPanel = SplitterFixedPanel.None,
				Panel1 = _structure,
				Panel2 = _lines
			};

			// layout
			var layout = new DynamicLayout { Padding = new Padding(10), DefaultSpacing = new Size(4, 4) };
			layout.Styles.Add(null, (Label lbl) => lbl.VerticalAlignment = VerticalAlignment.Center);

			layout.AddSeparateRow(insertRandomTextButton, setSelectedText, insertImageButton, insertListButton, clearButton, null);
			layout.AddSeparateRow(AttributeControls(), null);
			{
				layout.BeginVertical();
				layout.Add(mainSplitter, yscale: true);
				layout.Add(_status);
				layout.EndVertical();
			}

			Content = layout;
			Shown += (sender, e) => RichTextArea.Focus();

			CreateMenu();

		}

		private void drawable_Paint(object sender, PaintEventArgs e)
		{
			var highlight = _highlightedBounds;
			if (highlight.HasValue)
			{
				var (bounds, color) = highlight.Value;
				e.Graphics.DrawRectangle(color, bounds);
			}
		}

		private void ShowProperties(object item)
		{
			var grid = new PropertyGrid { SelectedObject = item, Size = new Size(400, 300) };
			var dlg = new Dialog<bool>
			{
				Title = "Properties",
				Resizable = true,
				// MinimumSize = new Size(300, 400),
				Content = grid
			};
			dlg.PositiveButtons.Add(dlg.DefaultButton = new Button((s, ev) => dlg.Close(true)) { Text = "Ok" });
			dlg.ShowModal(this);
		}

		static string NiceName(object obj)
		{
			var name = obj.GetType().Name;
			if (name.EndsWith("Element"))
				name = name.Substring(0, name.Length - "Element".Length);
			return name;
		}

		private void UpdateStructure()
		{
			var items = new TreeGridItemCollection();
			static TreeGridItem CreateNode(IElement element)
			{
				var item = new TreeGridItem { Expanded = true, Tag = element };
				item.Values = [$"{NiceName(element)}: {element.DocumentStart}:{element.Length} - {element.Text?.Substring(0, Math.Min(element.Text.Length, 100)).Replace('\n', ' ').Replace('\x2028', ' ')}"];
				if (element is IList list)
				{
					foreach (var child in list.OfType<IElement>())
					{
						var childItem = CreateNode(child);
						item.Children.Add(childItem);
					}
				}
				return item;
			}
			
			_structure.DataStore = new TreeGridItem { Children = { CreateNode(RichTextArea.Document) } };
		}
		private void UpdateLines()
		{
			var linesItems = new TreeGridItemCollection();
			foreach (var line in RichTextArea.Document.EnumerateLines(0))
			{
				var lineItem = new TreeGridItem { Expanded = true };
				lineItem.Values = new object[] { $"Line: {line.Start}:{line.Length}" };
				foreach (var chunk in line)
				{
					var chunkItem = new TreeGridItem();
					chunkItem.Values = new object[] { $"Chunk ({NiceName(chunk.Element)}): {chunk.InlineStart}:{chunk.Length} - {chunk.Text.Substring(0, Math.Min(chunk.Text.Length, 100))}" };
					chunkItem.Tag = chunk;
					lineItem.Children.Add(chunkItem);
				}
				lineItem.Tag = line;
				linesItems.Add(lineItem);
			}
			_lines.DataStore = linesItems;
			_highlightedBounds = null;
			Invalidate();
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
