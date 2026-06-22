using System;
using System.Collections.Generic;
using System.Threading;

#if __MACOS__
using AppKit;
using Foundation;
#else
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif

using Eto.Forms;
using Eto.ExtendedRichTextArea.SpellCheck;

namespace Eto.ExtendedRichTextArea.Mac;

/// <summary>
/// An <see cref="ITextChecker"/> backed by macOS's <c>NSSpellChecker</c>, the same engine that
/// powers system-wide spelling and grammar checking. Provides spelling and grammar squiggles,
/// suggestions, and "Learn" (add to dictionary) using the user's configured languages.
/// </summary>
/// <remarks>
/// Offsets from <c>NSSpellChecker</c> are UTF-16 code-unit ranges, which line up exactly with
/// .NET string indices (and therefore the document offsets used by the control), so no conversion
/// is needed. <see cref="Check"/> is invoked on a background thread by the controller, but
/// <c>NSSpellChecker</c> is AppKit and must run on the UI thread, so the native calls are marshalled
/// there via <see cref="Application.Invoke(Action)"/>.
/// </remarks>
public sealed class MacSpellChecker : ITextChecker
{
	readonly string? _language;
	readonly nint _tag;
	TextCheckTypes _checkTypes = TextCheckTypes.All;
	bool _checkUppercaseWords;
	NSSpellChecker _checker;

	public event EventHandler? DictionaryChanged;

	/// <inheritdoc/>
	public TextCheckTypes CheckTypes
	{
		get => _checkTypes;
		set
		{
			if (_checkTypes == value)
				return;
			_checkTypes = value;
			DictionaryChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <inheritdoc/>
	public bool CheckUppercaseWords
	{
		get => _checkUppercaseWords;
		set
		{
			if (_checkUppercaseWords == value)
				return;
			_checkUppercaseWords = value;
			DictionaryChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <param name="language">
	/// BCP-47 language tag to check against (e.g. "en_US"). When null, NSSpellChecker uses the
	/// user's automatic/selected language.
	/// </param>
	public MacSpellChecker(string? language = null)
	{
		_language = language;
		_tag = (nint)NSSpellChecker.UniqueSpellDocumentTag;
		_checker = new NSSpellChecker();
		if (!string.IsNullOrEmpty(_language))
			_checker.Language = _language;
	}

	public IReadOnlyList<TextProblem> Check(string text, CancellationToken token)
	{
		if (string.IsNullOrEmpty(text))
			return Array.Empty<TextProblem>();

		var checkTypes = _checkTypes;
		if (checkTypes == TextCheckTypes.None)
			return Array.Empty<TextProblem>();

		// Only ask NSSpellChecker for the categories the caller selected.
		NSTextCheckingType nativeTypes = 0;
		if ((checkTypes & TextCheckTypes.Spelling) != 0)
			nativeTypes |= NSTextCheckingType.Spelling;
		if ((checkTypes & TextCheckTypes.Grammar) != 0)
			nativeTypes |= NSTextCheckingType.Grammar;
		var types = (NSTextCheckingTypes)nativeTypes;
		var results = _checker.CheckString(text, new NSRange(0, text.Length), types, (NSDictionary?)null, _tag, out _, out _);

		var problems = new List<TextProblem>();
		if (results != null)
		{
			foreach (var result in results)
			{
				if (token.IsCancellationRequested)
					break;
				var range = result.Range;
				if (range.Length == 0)
					continue;
				if (result.ResultType == NSTextCheckingType.Grammar)
				{
					// Grammar corrections apply to the whole flagged span; carry the platform's suggested
					// replacement so the context menu can offer it (spelling guesses don't apply to a phrase).
					var replacement = result.ReplacementString;
					IReadOnlyList<string>? suggestions = !string.IsNullOrEmpty(replacement) ? new[] { replacement! } : null;
					problems.Add(new TextProblem((int)range.Location, (int)range.Length, TextProblemKind.Grammar, null, suggestions));
				}
				else
				{
					// Spelling. When uppercase-checking is off, drop all-caps spans so behaviour matches
					// the "acronyms are skipped" contract (NSSpellChecker still flags a few common all-caps
					// typos such as "TEH").
					if (!_checkUppercaseWords && SpellCheckText.IsUppercaseWord(text.Substring((int)range.Location, (int)range.Length)))
						continue;
					problems.Add(new TextProblem((int)range.Location, (int)range.Length, TextProblemKind.Spelling));
				}
			}
		}

		// NSSpellChecker skips all-caps tokens as presumed acronyms. When the caller opts in, re-check
		// each by spelling its lowercased form (verified: CheckSpelling flags "helllo" but not "nasa").
		if (_checkUppercaseWords && (checkTypes & TextCheckTypes.Spelling) != 0)
			AddUppercaseSpellingProblems(_checker, text, problems, token);

		return problems.Count > 0 ? problems : (IReadOnlyList<TextProblem>)Array.Empty<TextProblem>();
	}

	void AddUppercaseSpellingProblems(NSSpellChecker checker, string text, List<TextProblem> problems, CancellationToken token)
	{
		HashSet<int>? coveredStarts = null;
		foreach (var match in SpellCheckText.EnumerateWords(text))
		{
			if (token.IsCancellationRequested)
				break;
			var word = match.Value;
			if (!SpellCheckText.IsUppercaseWord(word))
				continue;
			// Skip tokens NSSpellChecker already flagged (e.g. "TEH") so we don't double-squiggle.
			coveredStarts ??= BuildCoveredStarts(problems);
			if (coveredStarts.Contains(match.Index))
				continue;
			var range = checker.CheckSpelling(word.ToLowerInvariant(), 0);
			if (range.Length > 0)
				problems.Add(new TextProblem(match.Index, word.Length, TextProblemKind.Spelling));
		}
	}

	static HashSet<int> BuildCoveredStarts(List<TextProblem> problems)
	{
		var starts = new HashSet<int>();
		for (int i = 0; i < problems.Count; i++)
			starts.Add(problems[i].Start);
		return starts;
	}

	public IReadOnlyList<string> GetSuggestions(string word)
	{
		if (string.IsNullOrEmpty(word))
			return Array.Empty<string>();

		var language = _language ?? _checker.Language;
		var guesses = _checker.GuessesForWordRange(new NSRange(0, word.Length), word, language, _tag);

		// All-caps words yield no guesses from NSSpellChecker (it treats them as acronyms); fall back to
		// guessing the lowercased form and re-uppercasing, so the menu still offers corrections.
		if ((guesses == null || guesses.Length == 0) && SpellCheckText.IsUppercaseWord(word))
		{
			var lower = word.ToLowerInvariant();
			var lowerGuesses = _checker.GuessesForWordRange(new NSRange(0, lower.Length), lower, language, _tag);
			if (lowerGuesses != null && lowerGuesses.Length > 0)
			{
				var mapped = new string[lowerGuesses.Length];
				for (int i = 0; i < lowerGuesses.Length; i++)
					mapped[i] = lowerGuesses[i].ToUpperInvariant();
				return mapped;
			}
		}
		return guesses ?? Array.Empty<string>();
	}

	public void AddToDictionary(string word)
	{
		if (string.IsNullOrEmpty(word))
			return;
		_checker.LearnWord(word);
		DictionaryChanged?.Invoke(this, EventArgs.Empty);
	}

	public void RemoveFromDictionary(string word)
	{
		if (string.IsNullOrEmpty(word))
			return;
		_checker.UnlearnWord(word);
		DictionaryChanged?.Invoke(this, EventArgs.Empty);
	}

	public bool IsWordLearned(string word)
		=> !string.IsNullOrEmpty(word) && _checker.HasLearnedWord(word);
}
