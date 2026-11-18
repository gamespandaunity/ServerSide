#if !UNITY_SERVER || UNITY_EDITOR   // BouncyCastle (Mirror encryption) is left out of dedicated server builds
using System;
using Mirror.BouncyCastle.Crypto;

namespace Mirror.Transports.Encryption
{
    public struct PubKeyInfo
    {
        public string Fingerprint;
        public ArraySegment<byte> Serialized;
        public AsymmetricKeyParameter Key;
    }
}
#endif
