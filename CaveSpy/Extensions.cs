using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static (byte r, byte g, byte b) ToColor(this string color)
        {
            var matches = Regex.Matches(color, "^*([A-Fa-f0-9]{2})", RegexOptions.None);
            return (
                Convert.ToByte(matches[0].Value, 16),
                Convert.ToByte(matches[1].Value, 16),
                Convert.ToByte(matches[1].Value, 16));
        }

        public static (double r, double g, double b) ToColorDouble(this string color)
        {
            var matches = Regex.Matches(color, "^*([A-Fa-f0-9]{2})", RegexOptions.None);
            return (
                Convert.ToByte(matches[0].Value, 16) / 256.0,
                Convert.ToByte(matches[1].Value, 16) / 256.0,
                Convert.ToByte(matches[2].Value, 16) / 256.0);
        }

        public static void Save(this int[] array, string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteArray(array);
            }
        }

        public static int[] LoadArray(string path)
        {
            int[] array = null;
            using (var stream = new FileStream(path, FileMode.Open))
            using (var reader = new BinaryReader(stream))
            {
                array = reader.ReadIntArray();
            }

            return array;
        }

        public static void WriteArray<T>(this BinaryWriter writer, Action<BinaryWriter, T> writerFunc, T[] array)
        {
            long length = 0;
            if (null != array)
                length = array.LongLength;

            writer.Write(length);
            for (long i = 0; i < length; i++)
                writerFunc(writer, array[i]);
        }

        public static void WriteArray(this BinaryWriter writer, int[] array)
        {
            WriteArray<int>(writer, (w, i) => w.Write(i), array);
        }

        public static void WriteArray(this BinaryWriter writer, double[] array)
        {
            WriteArray<double>(writer, (w, i) => w.Write(i), array);
        }

        public static void WriteArray(this BinaryWriter writer, byte[] array)
        {
            WriteArray<byte>(writer, (w, i) => w.Write(i), array);
        }

        public static void WriteArray(this BinaryWriter writer, short[] array)
        {
            WriteArray<short>(writer, (w, i) => w.Write(i), array);
        }

        public static void WriteArray(this BinaryWriter writer, Color[] array)
        {
            WriteArray<Color>(writer, (w, i) => {
                if (null == i)
                    i = Color.Empty;

                w.Write(i.red); w.Write(i.green); w.Write(i.blue); }, array);
        }

        public static int[] ReadIntArray(this BinaryReader reader)
        {
            return ReadArray<int>(reader, r => r.ReadInt32());
        }

        public static double[] ReadDoubleArray(this BinaryReader reader)
        {
            return ReadArray<double>(reader, r => r.ReadDouble());
        }

        public static short[] ReadShortArray(this BinaryReader reader)
        {
            return ReadArray<short>(reader, r => r.ReadInt16());
        }

        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            return ReadArray<byte>(reader, r => r.ReadByte());
        }

        public static Color[] ReadColorArray(this BinaryReader reader)
        {
            return ReadArray<Color>(reader, r =>
            {
                Color c = new Color();
                c.red = r.ReadUInt16();
                c.green = r.ReadUInt16();
                c.blue = r.ReadUInt16();
                return c;
            });
        }

        private static T[] ReadArray<T>(BinaryReader reader, Func<BinaryReader, T> readItem)
        {
            long length = reader.ReadInt64();
            var result = new T[length];
            for (long i = 0; i < length; i++)
            {
                result[i] = readItem(reader);
            }
            return result;
        }
    }
}
