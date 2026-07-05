using System;

namespace DACDT_2026
{
    public static class PlcFloatWordCodec
    {
        public static int ToInt32Bits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        public static float FromWords(int lowWord, int highWord)
        {
            byte[] bytes = BitConverter.GetBytes((highWord << 16) | (lowWord & 0xFFFF));
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
