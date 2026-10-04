using UnityEngine;

// Reads the build timestamp stamped by BuildStampWriter at build time (yyyyMMddHHmm as a long).
// 0 means "no stamp" — editor runs, or a build made before the stamping hook existed. Callers
// must treat 0 as EXEMPT (never block): old-build discard only targets stamped builds, and the
// remote cutoff only moves forward once stamped builds are the only ones in circulation.
public static class BuildStamp
{
    private static long cached = -1;

    public static long Timestamp
    {
        get
        {
            if (cached >= 0)
                return cached;
            TextAsset asset = Resources.Load<TextAsset>("build_stamp");
            cached = (asset != null && long.TryParse(asset.text.Trim(), out long value)) ? value : 0;
            return cached;
        }
    }
}
