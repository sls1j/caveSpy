using CoordinateSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class Image
    {
        private Dictionary<string, object> meta;
        private int height;
        private int width;
        private byte[] image;

        public Image(Map m)            
        {
            width = m.width;
            height = m.height;
            this.image = new byte[width * height * 4];
            meta = new Dictionary<string, object>();
            meta["zone"] = m.zone;
            meta["physicalLeft"] = m.physicalLeft;
            meta["physicalBottom"] = m.physicalBottom;
            meta["physicalRight"] = m.physicalRight;
            meta["physicalTop"] = m.physicalTop;
        }

        public void DrawHillshade(Map map, double heading, double step, double intensity, double opacity)
        {
            double headingRad = heading * Math.PI / 180;
            int vectorX = (int)(step * Math.Cos(headingRad));
            int vectorY = (int)(step * Math.Sin(headingRad));
            int vectorII = vectorY * width + vectorX;
            var elevations = map.elevations;
            for (int y = 0, i=0; y < height; y++)
            {
                for (int x = 0; x < width; x++, i++)
                {
                    int xx = x + vectorX;
                    int yy = y + vectorY;
                    int ii = i + vectorII;
                    // make sure they are actually within 
                    if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                        continue;

                    double m = (elevations[i] - elevations[ii]) / step;

                    int iii = ii * 4;
                    byte c = ConvertToByteColor(m * intensity + 0.5);
                    image[iii] = Mix(image[iii], c, opacity);
                    image[iii + 1] = Mix(image[iii+1], c, opacity);
                    image[iii + 2] = Mix(image[iii+2], c, opacity);
                    image[iii + 3] = 255;
                }
            }
        }

        internal void DrawClassification(Map map, int classification)
        {
            for (int i = 0, ii = 0; i < map.elevations.Length; i++, ii += 4)
            {
                var v = map.classifications[i];

                if (v == classification)
                {
                    image[ii] = 0;
                    image[ii + 1] = 0;
                    image[ii + 2] = 0;
                }
            }
        }

        public void DrawCaves(List<Cave> caves, double opacity)
        {
            for (int i = 0; i < caves.Count; i++)
            {
                var cave = caves[i];
                int ii = (int)(cave.X + cave.Y * width) * 4;
                image[ii] = Mix(image[ii], 0, opacity);
                image[ii + 1] = Mix(image[ii+1], 0, opacity);
                image[ii + 2] = Mix(image[ii+2], 255, opacity);
            }
        }

        public void DrawElevationColor(Image img, Map map, double spacing, double opacity)
        {            
            for (int i = 0, ii = 0; i < map.elevations.Length; i++, ii += 4)
            {
                double v = map.elevations[i];

                byte r, g, b;
                HueToRGB(v / spacing, 0.8, 0.8, out r, out g, out b);
                image[ii] = Mix(image[ii], b, opacity);
                image[ii + 1] = Mix(image[ii+1], g, opacity);
                image[ii + 2] = Mix(image[ii+2], r, opacity);
            }
        }

        public void DrawArrayInt(Image img, int[] array, double opacity)
        {
            if (img.width * img.height != array.Length)
                throw new InvalidOperationException("Array must be the same dimension of the image!");

            int max = int.MinValue;
            int min = int.MaxValue;
            for (int i = 0; i < array.Length; i++)
            {
                int v = array[i];
                if (v > max)
                    max = v;
                if (v < min)
                    min = v;
            }

            double logDiff = Math.Log10( max - min + 1 );

            for (int i = 0, ii=0; i < array.Length; i++, ii+=4)
            {
                int v = array[i];

                byte c = (byte)(Math.Log10(v - min + 1) * 256 / logDiff);

                image[ii] = Mix(image[ii], 0, opacity);
                image[ii+1] = Mix(image[ii+1], c, opacity);
                image[ii+2] = Mix(image[ii+2], 0, opacity);
            }
        }

        private static byte Mix(byte a, byte b, double opacity)
        {
            return ClipToByteColor(a * (1 - opacity) + b * (opacity));
        }

        private static byte ConvertToByteColor(double v)
        {
            v *= 256;
            if (v < 0)
                return 0;
            if (v > 255)
                return 255;
            else
                return (byte)v;
        }

        private static byte ClipToByteColor(double v)
        {
            if (v < 0)
                return 0;
            if (v > 255)
                return 255;
            else
                return (byte)v;
        }

        private static void HueToRGB(double hue, double saturation, double lightness, out byte r, out byte g, out byte b)
        {
            // convert to YIQ color space
            double Y = lightness;
            double I = Math.Cos(hue * 2 * Math.PI) * saturation;
            double Q = Math.Sin(hue * 2 * Math.PI) * saturation;

            // convert to RGB color space
            double cr = Y + 0.9563f * I + 0.6210f * Q;
            double cg = Y - 0.2721f * I - 0.6474f * Q;
            double cb = Y - 1.1070f * I + 1.7046f * Q;

            // make sure each value is in the range of 0-255 range
            r = ConvertToByteColor(cr);
            g = ConvertToByteColor(cg);
            b = ConvertToByteColor(cb);
        }

        public void Save(string path)
        {
            string ext = Path.GetExtension(path);
            switch (ext)
            {
                case ".bmp":
                    WriteImage(path);
                    break;
                case ".kml":
                    WriteKml(path);
                    break;
                case ".kmz":
                    string kmlDirName = WriteKml(path);
                    using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
                    {
                        foreach (var file in Directory.EnumerateFiles(kmlDirName))
                        {
                            zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                        }
                    }
                    break;
            }
        }


        private void WriteImage(string path)
        {
            unsafe
            {
                fixed (byte* ptr = this.image)
                {
                    using (Bitmap bitmap = new Bitmap(width, height, width * 4,
                                PixelFormat.Format32bppRgb, new IntPtr(ptr)))
                    {
                        bitmap.Save(path);
                    }
                }
            }
        }

        public void SetDouble(string name, double value)
        {
            meta[name] = value;
        }

        public void SetString(string name, string value)
        {
            meta[name] = value;
        }

        private double GetDouble(string name)
        {
            return (double)meta[name];
        }

        private string GetString(string name)
        {
            return (string)meta[name];
        }

        private string WriteKml(string path)
        {
            string fileName = Path.GetFileName(path);
            string dirName = Path.GetFileNameWithoutExtension(fileName);

            string packagePath = Path.Combine(Path.GetDirectoryName(path), dirName);
            Directory.CreateDirectory(packagePath);
            string kmlPath = Path.Combine(packagePath, fileName);
            string imagePath = Path.Combine(packagePath, Path.ChangeExtension(fileName, ".bmp"));
            WriteImage(imagePath);
            string template = LoadResource("CaveSpy.kmlTemplate.xml");

            // north and west
            Tuple<string, int> zone = GetZone(GetString("zone"));
            var utm = new UniversalTransverseMercator(zone.Item1, zone.Item2, GetDouble("physicalLeft"), GetDouble("physicalBottom"));
            var nw = UniversalTransverseMercator.ConvertUTMtoLatLong(utm);

            // south and east            
            var se = CoordinateSharp.UniversalTransverseMercator.ConvertUTMtoLatLong(
                new CoordinateSharp.UniversalTransverseMercator(zone.Item1, zone.Item2, GetDouble("physicalRight"), GetDouble("physicalTop")));

            string body = template.Replace("**OverlayName**", dirName);
            body = body.Replace("**Description**", "Create by CaveSpy");
            body = body.Replace("**Ref**", Path.GetFileName(imagePath));
            body = body.Replace("**North**", nw.Latitude.DecimalDegree.ToString());
            body = body.Replace("**West**", nw.Longitude.DecimalDegree.ToString());
            body = body.Replace("**South**", se.Latitude.DecimalDegree.ToString());
            body = body.Replace("**East**", se.Longitude.DecimalDegree.ToString());
            body = body.Replace("**Rotation**", "0");
            File.WriteAllText(kmlPath, body);

            return dirName;
        }

        private static Tuple<string, int> GetZone(string zone)
        {

            string latZ = zone.Substring(2, 1);
            int longZ = int.Parse(zone.Substring(0, 2));
            return Tuple.Create(latZ, longZ);
        }

        public static string LoadResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

       
    }
}
