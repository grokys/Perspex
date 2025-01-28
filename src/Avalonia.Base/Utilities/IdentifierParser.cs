using System;
using System.Globalization;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    static class IdentifierParser
    {
        public static ReadOnlySpan<char> ParseIdentifier(this
#if NET7SDK
            scoped
#endif
            ref CharacterReader r)
        {
            if (IsValidIdentifierStart(r.Peek))
            {
                return r.TakeWhile(c => IsValidIdentifierChar(c));
            }
            else
            {
                return ReadOnlySpan<char>.Empty;
            }
        }

        private static bool IsValidIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsValidIdentifierChar(char c)
        {
            if (IsValidIdentifierStart(c))
            {
                return true;
            }
            else
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                return cat == UnicodeCategory.NonSpacingMark ||
                       cat == UnicodeCategory.SpacingCombiningMark ||
                       cat == UnicodeCategory.ConnectorPunctuation ||
                       cat == UnicodeCategory.Format ||
                       cat == UnicodeCategory.DecimalDigitNumber;
            }
        }

        internal static ReadOnlySpan<char> ParseNumber(this ref CharacterReader r)
        {
            return r.TakeWhile(c => IsValidNumberChar(c));
        }

        private static bool IsValidNumberChar(char c)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            return cat == UnicodeCategory.DecimalDigitNumber;
        }
    }
}
