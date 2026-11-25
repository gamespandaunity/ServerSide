using System;
using System.Globalization;

/// <summary>
/// Player ids are 10-character public IDs since the backend's "Player App API Changes" (25 Nov 2026): a letter A-Z then
/// 9 letters or digits, e.g. "K7Q2M9XA4B", in every field that names a player (user _id, first_player, second_player,
/// player_info_id, user_id, invited_players, winner, sender, receiver_id ...). "ai" stays "ai". The server trims and
/// upper-cases what the app sends. Ids are strings everywhere in the app: never parse, add, sort numerically or store
/// them as int. Compare with <see cref="Same"/>.
/// </summary>
public static class PlayerId
{
    public const string Ai = "ai";

    /// <summary>The id as the backend wants it: trimmed and upper-cased ("ai" stays lower-case). Null/empty → "".</summary>
    public static string Normalize(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        string t = id.Trim();
        return string.Equals(t, Ai, StringComparison.OrdinalIgnoreCase) ? Ai : t.ToUpperInvariant();
    }

    /// <summary>Any JSON value (string, number from an old answer, null) as an id string.</summary>
    public static string Of(object value)
    {
        if (value == null) return "";
        if (value is string s) return Normalize(s);
        if (value is IFormattable f) return Normalize(f.ToString(null, CultureInfo.InvariantCulture));
        return Normalize(value.ToString());
    }

    /// <summary>Same player? Case- and space-insensitive; two empty ids are never the same player.</summary>
    public static bool Same(string a, string b)
    {
        string x = Normalize(a), y = Normalize(b);
        return x.Length > 0 && string.Equals(x, y, StringComparison.Ordinal);
    }

    /// <summary>A well-formed public ID (letter, then 9 letters or digits), after normalising.</summary>
    public static bool IsPublicId(string id)
    {
        string t = Normalize(id);
        if (t.Length != 10 || t[0] < 'A' || t[0] > 'Z') return false;
        for (int i = 1; i < 10; i++)
        {
            char c = t[i];
            if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
        }
        return true;
    }

    /// <summary>The signed-in player's own public ID ("" when not signed in).</summary>
    public static string Me => Normalize(staticVariables.UserProfiledata?.user?._id);

    /// <summary>Is <paramref name="id"/> the signed-in player?</summary>
    public static bool IsMe(string id) => Same(id, Me);
}
