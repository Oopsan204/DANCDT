using System;

namespace WPF_Test_PLC20260124
{
    public static class PlcBitHelper
    {
        public static int[] BoolArrayToIntArray(bool[] bits)
        {
            if (bits == null) return Array.Empty<int>();

            int[] arr = new int[bits.Length];
            for (int i = 0; i < bits.Length; i++)
                arr[i] = bits[i] ? 1 : 0;

            return arr;
        }

        public static bool[] IntArrayToBoolArray(int[] arr)
        {
            bool[] bits = new bool[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                bits[i] = arr[i] != 0;
            return bits;
        }

        public static bool[] WordToBits(int word)
        {
            bool[] bits = new bool[16];
            for (int i = 0; i < 16; i++)
                bits[i] = ((word >> i) & 1) == 1;
            return bits;
        }

        public static int BitsToWord(bool[] bits)
        {
            if (bits == null) return 0;
            int word = 0;
            for (int i = 0; i < bits.Length && i < 16; i++)
                if (bits[i]) word |= (1 << i);
            return word;
        }

        public static string WordToBitString(int word)
        {
            word &= 0xFFFF;
            char[] bits = new char[16];
            for (int i = 0; i < 16; i++)
                bits[i] = ((word >> i) & 1) == 1 ? '1' : '0';
            return new string(bits);
        }

        public static int BitStringToWord(string bits)
        {
            if (string.IsNullOrEmpty(bits)) return 0;
            int word = 0;
            for (int i = 0; i < bits.Length && i < 16; i++)
                if (bits[i] == '1')
                    word |= (1 << i);
            return word;
        }

        public static bool GetBit(int word, int bitIndex)
        {
            return ((word >> bitIndex) & 1) == 1;
        }

        public static int SetBit(int word, int bitIndex, bool value)
        {
            if (value)
                return word | (1 << bitIndex);
            return word & ~(1 << bitIndex);
        }

        public static int GetCurrentPosition(int[] arr, int index)
        {
            return arr[index] | (arr[index + 1] << 16);
        }

        public static void SetCurrentPosition(int[] arr, int index, int value)
        {
            arr[index] = value & 0xFFFF;
            arr[index + 1] = (value >> 16) & 0xFFFF;
        }
    }
}
