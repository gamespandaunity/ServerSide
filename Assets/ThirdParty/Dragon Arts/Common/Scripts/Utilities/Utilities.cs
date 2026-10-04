using System.Linq;
using System;

namespace DragonArts.Common
{

    public static class Utilities {

        public static string GenerateUid (string chars, System.Random random, int length, string prefix = null) {
            string uidPart = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            return String.IsNullOrWhiteSpace(prefix) ? uidPart : $"{prefix}-{uidPart}";
        }
    }
}