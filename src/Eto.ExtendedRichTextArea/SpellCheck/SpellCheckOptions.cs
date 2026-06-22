using Eto.Drawing;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.SpellCheck;

/// <summary>
/// Rendering and context-menu options for spell/grammar checking. Instantiate one, set the
/// properties you care about, and assign it to <see cref="ExtendedRichTextArea.SpellCheckOptions"/>
/// (independently of, and in any order relative to, <see cref="ExtendedRichTextArea.SpellChecker"/>).
/// Properties may also be changed on the assigned instance at runtime; changes take effect on the
/// next check/paint.
/// </summary>
public sealed class SpellCheckOptions
{
	/// <summary>Idle time (seconds) after a change before a re-check runs. Defaults to 0.3s.</summary>
	public double DebounceInterval { get; set; } = 0.3;

	/// <summary>Pen used for spelling squiggles. Defaults to a thin red pen.</summary>
	public Pen SpellingPen { get; set; } = new Pen(Colors.Red, 1f);

	/// <summary>Pen used for grammar squiggles. Defaults to a thin green pen.</summary>
	public Pen GrammarPen { get; set; } = new Pen(Color.FromRgb(0x1A8A1A), 1f);

	/// <summary>Peak-to-trough height of the squiggle, in points.</summary>
	public float Amplitude { get; set; } = 2f;

	/// <summary>Horizontal distance between squiggle peaks, in points.</summary>
	public float Wavelength { get; set; } = 4f;

	/// <summary>Maximum number of replacement suggestions to show in the context menu. Defaults to 10.</summary>
	public int MaxSuggestions { get; set; } = 10;

	/// <summary>Text for the "add to dictionary" menu item. Set to null to omit it.</summary>
	public string? AddToDictionaryText { get; set; } =  Application.Instance.Localize(typeof(SpellCheckOptions), "Add to Dictionary");

	/// <summary>
	/// Text for the "remove from dictionary" menu item, shown when right-clicking a word the user
	/// previously added to the dictionary. Set to null to omit it.
	/// </summary>
	public string? RemoveFromDictionaryText { get; set; } = Application.Instance.Localize(typeof(SpellCheckOptions), "Remove from Dictionary");

	/// <summary>Text shown (disabled) when a flagged word/phrase has no suggestions. Set to null to omit it.</summary>
	public string? NoSuggestionsText { get; set; } = Application.Instance.Localize(typeof(SpellCheckOptions), "(no suggestions)");

	/// <summary>Label for a grammar suggestion that removes the span (e.g. a repeated word).</summary>
	public string DeleteSuggestionText { get; set; } = Application.Instance.Localize(typeof(SpellCheckOptions), "Delete repeated word");
}
