using UnityEngine;
public static class AudioUtility
{
    public static byte[] AudioClipToByteArray(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        // Convert float samples to bytes (assuming 16-bit PCM)
        byte[] byteArray = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short value = (short)(samples[i] * 32767);
            byteArray[i * 2] = (byte)(value & 0xff);
            byteArray[i * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return byteArray;
    }
}
