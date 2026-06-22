using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Eto.ExtendedRichTextArea.SpellCheck;

/// <summary>
/// Shared text helpers used by <see cref="ITextChecker"/> implementations (including the platform
/// companions) so word tokenization and the "all-caps word" rule are defined in exactly one place.
/// Kept internal; the platform companion projects compile this same source file via a linked
/// <c>&lt;Compile&gt;</c> item (the core assembly is strong-named, so InternalsVisibleTo to the
/// unsigned companions isn't an option).
/// </summary>
internal static class SpellCheckText
{
	// Words: runs of letters, optionally joined by apostrophes (don't, isn't).
	static readonly Regex WordPattern = new Regex(@"\p{L}+(?:'\p{L}+)*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>Enumerates the word tokens in <paramref name="text"/> as regex matches (letters/apostrophes).</summary>
	public static IEnumerable<Match> EnumerateWords(string text)
	{
		if (string.IsNullOrEmpty(text))
			yield break;
		foreach (Match match in WordPattern.Matches(text))
			yield return match;
	}

	/// <summary>
	/// Gets whether <paramref name="word"/> is an "all-caps" token: it contains at least one letter
	/// and none of its letters are lowercase (e.g. <c>NASA</c>, <c>ID</c>, <c>DON'T</c>). Such tokens
	/// are the ones platform spell-checkers skip by default as presumed acronyms.
	/// </summary>
	public static bool IsUppercaseWord(string word)
	{
		if (string.IsNullOrEmpty(word))
			return false;
		var hasLetter = false;
		for (int i = 0; i < word.Length; i++)
		{
			var c = word[i];
			if (char.IsLower(c))
				return false;
			if (char.IsLetter(c))
				hasLetter = true;
		}
		return hasLetter;
	}
}
