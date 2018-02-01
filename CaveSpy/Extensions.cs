using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    public static class Extensions
    {
        public static void NeighborhoodLoop(int width, int height, int x, int y, Action<int, int, int> a)
        {
            for (int yi = -1; yi <= 1; yi++)
            {
                int yl = y + yi;
                if (yl < 0 || yl >= height)
                    continue;

                for (int xi = -1; xi <= 1; xi++)
                {
                    int xl = x + xi;
                    if (xl < 0 || xl >= width)
                        continue;

                    int ii = yl * width + xl;
                    a(ii, xl, yl);
                }
            }
        }

        public static string ReadString(this BinaryReader reader, int fieldLength)
        {
            char[] value = reader.ReadChars(fieldLength);
            int charLength = value.Length;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\0')
                {
                    charLength = i;
                    break;
                }
            }

            return new string(value, 0, charLength);
        }
    }
}
