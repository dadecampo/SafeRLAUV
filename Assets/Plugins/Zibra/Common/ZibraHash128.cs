using System.Runtime.InteropServices;
using UnityEngine;

namespace com.zibra.common.Utilities
{
    // Basically CRC128
    [StructLayout(LayoutKind.Sequential)]
    internal class ZibraHash128
    {
        private ulong M0;
        private ulong M1;

        private const ulong POLYNOMIAL0 = 0x8000000000000000ul;
        private const ulong POLYNOMIAL1 = 0x0000000000000003ul;

        public void Init()
        {
            M0 = 0;
            M1 = 0;
        }

        public void Append(bool input)
        {
            ulong bit = (input ? 1ul : 0ul) ^ (M1 & 1ul);

            // right shifting 128 bit hash
            M1 >>= 1;
            M1 += (M0 & 1) * 0x8000000000000000ul;
            M0 >>= 1;

            M0 ^= bit * POLYNOMIAL0;
            M1 ^= bit * POLYNOMIAL1;
        }

        public void Append(int input)
        {
            for (int i = 0; i < sizeof(int) * 8; i++)
            {
                Append((input & 1) != 0);
                input >>= 1;
            }
        }

        public void Append(Color32[] input)
        {
            foreach (Color32 color in input)
            {
                Append(color.r);
                Append(color.g);
                Append(color.b);
                Append(color.a);
            }
        }

        public static bool operator ==(ZibraHash128 hash1, ZibraHash128 hash2)
        {
            if (hash1 is null && hash2 is null)
                return true;
            if (hash1 is null || hash2 is null)
                return false;
            return hash1.M0 == hash2.M0 && hash1.M1 == hash2.M1;
        }

        public static bool operator !=(ZibraHash128 hash1, ZibraHash128 hash2)
        {
            if (hash1 is null && hash2 is null)
                return false;
            if (hash1 is null || hash2 is null)
                return true;
            return hash1.M0 != hash2.M0 || hash1.M1 != hash2.M1;
        }

        public static bool operator<(ZibraHash128 hash1, ZibraHash128 hash2)
        {
            if (hash1 is null && hash2 is null)
                return false;
            if (hash1 is null || hash2 is null)
                return hash1 is null;
            return hash1.M0 < hash2.M0 || (hash1.M0 == hash2.M0 && hash1.M1 < hash2.M1);
        }

        public static bool operator>(ZibraHash128 hash1, ZibraHash128 hash2)
        {
            if (hash1 is null && hash2 is null)
                return false;
            if (hash1 is null || hash2 is null)
                return hash2 is null;
            return hash1.M0 > hash2.M0 || (hash1.M0 == hash2.M0 && hash1.M1 > hash2.M1);
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !typeof(ZibraHash128).Equals(obj.GetType()))
            {
                return false;
            }

            ZibraHash128 p = (ZibraHash128)obj;
            return this == p;
        }

        public override int GetHashCode()
        {
            const uint MAX_INT = 0xFFFFFFFF;

            ulong hash = ((M0 >> 16) & MAX_INT) ^ (M0 & MAX_INT) ^ ((M1 >> 16) & MAX_INT) ^ (M1 & MAX_INT);

            return (int)hash;
        }
    }
}