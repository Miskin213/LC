using System;
using System.Collections.Generic;
using System.Globalization;

namespace PalaCompany
{
    internal static class Loc
    {
        internal enum Language
        {
            English,
            Turkish
        }

        internal static Language Current { get; set; } = Language.English;

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "tip.spawn.header", "{0}" },
            { "tip.spawn.body", "{0}" },
            { "poll.open.header", "POLL: type !1 or !2" },
            { "poll.open.body", "1) {0}\n2) {1}" },
            { "poll.vote.summary", "1) {0} - {1} votes; 2) {2} - {3} votes" },
            { "poll.result.file.winner", "POLL RESULT - {0} won; {1}" },
            { "poll.result.file.skipped", "POLL RESULT - skipped (no votes)" },
            { "poll.result.tip.winner.header", "POLL: {0} won" },
            { "poll.result.tip.skipped.header", "POLL: skipped" },
            { "poll.result.tip.skipped.body", "No votes; poll skipped. {0}" },
            { "poll.spawn.failed", "\nCouldn't spawn {0}: {1}" },
            { "poll.file.open", "POLL - {0}s left\n1) {1} - {2} votes\n2) {3} - {4} votes" }
        };

        private static readonly Dictionary<string, string> Turkish = new Dictionary<string, string>();

        internal static string T(string key, params object[] args)
        {
            string value = null;
            if (Current == Language.Turkish)
                Turkish.TryGetValue(key, out value);
            if (value == null)
                English.TryGetValue(key, out value);
            if (value == null)
                value = key;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, value, args ?? Array.Empty<object>());
            }
            catch (FormatException)
            {
                return value;
            }
        }
    }
}
