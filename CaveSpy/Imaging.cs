using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using CoordinateSharp;

namespace CaveSpy
{
    class Imaging
    {
        public void MakeImage(Map map)
        {
            var image = new byte[map.elevations.Length * 4];
            map.SetProperty("image", image);
        }

        public void AddElevation(Map map, bool color, double opacity)
        {
            byte[] image = map.GetProperty<byte[]>("image");
            for (int i = 0, ii = 0; i < map.elevations.Length; i++, ii+=4)
            {
                double v = map.elevations[i];

                byte r, g, b;
                HueToRGB(v / 40.0, 0.8, 0.8, out r, out g, out b);
                image[ii] = Confine((1 - opacity) * image[ii] + b * opacity);
                image[ii + 1] = Confine((1 - opacity) * image[ii+1] + g * opacity);
                image[ii + 2] = Confine((1 - opacity) * image[ii+2] + r * opacity);
                
            }
        }

        public void RenderElevationImage(Map map, bool addShading, bool addElevationLightness)
        {
            byte[] image = map.GetProperty<byte[]>("image");

            double max = map.elevations.Max();
            double min = map.elevations.Min();

            double scale = 256 / (max - min);

            for (int i = 0, ii = 0; i < map.elevations.Length; i++)
            {
                double v = 0;
                if (addElevationLightness)
                    v = map.elevations[i];
                else
                    v = (min + max) / 2;

                double dv = 0;
                if (addShading)
                {
                    if (i > 5)
                        dv = (map.elevations[i - 5] - map.elevations[i]) * 25;
                    else
                        dv = 0;
                }

                double scaledValue = (v - min) * scale + dv;
                byte color = (byte)Math.Max(Math.Min(scaledValue, 255), 0);
                image[ii++] = color;
                image[ii++] = color;
                image[ii++] = color;
                image[ii++] = 255;
            }

            map.SetProperty("image", image);
        }

        public void AddCaves(Map map, List<Cave> caves, int red, int green, int blue)
        {
            byte[] image = map.GetProperty<byte[]>("image");

            for (int i = 0; i < caves.Count; i++)
            {
                var c = caves[i];
                if (c.X < 0 || c.X >= map.width || c.Y < 0 || c.Y >= map.height)
                    continue;

                var ii = (int)(c.X + c.Y * map.width) * 4;
                image[ii] = (byte)0;
                image[ii + 1] = (byte)0;
                image[ii + 2] = (byte)255;
            }
        }

        public void Save(Map map, string fileName)
        {
            string ext = Path.GetExtension(fileName);
            switch (ext)
            {
                case ".bmp":
                    WriteImage(map, fileName);
                    break;
                case ".kml":
                    WriteKml(map, fileName);
                    break;
                case ".map":
                    map.Save(fileName);
                    break;
            }
        }

        private static void WriteKml(Map map, string path)
        {
            string fileName = Path.GetFileName(path);
            string dirName = Path.GetFileNameWithoutExtension(fileName);

            string packagePath = Path.Combine(Path.GetDirectoryName(path), dirName);
            Directory.CreateDirectory(packagePath);
            string kmlPath = Path.Combine(packagePath, fileName);
            string imagePath = Path.Combine(packagePath, Path.ChangeExtension(fileName, ".bmp"));
            WriteImage(map, imagePath);
            string template = LoadResource("CaveSpy.kmlTemplate.xml");

            // north and west
            Tuple<string, int> zone = GetZone(map.zone);
            var utm = new UniversalTransverseMercator(zone.Item1, zone.Item2, map.physicalLeft, map.physicalBottom);
            var nw = UniversalTransverseMercator.ConvertUTMtoLatLong( utm  );

            // south and east            
            var se = CoordinateSharp.UniversalTransverseMercator.ConvertUTMtoLatLong(
                new CoordinateSharp.UniversalTransverseMercator(zone.Item1, zone.Item2, map.physicalRight, map.physicalTop));

            string body = template.Replace("**OverlayName**", dirName);
            body = body.Replace("**Description**", "Create by CaveSpy");
            body = body.Replace("**Ref**", Path.GetFileName(imagePath));
            body = body.Replace("**North**", nw.Latitude.DecimalDegree.ToString());
            body = body.Replace("**West**", nw.Longitude.DecimalDegree.ToString());
            body = body.Replace("**South**", se.Latitude.DecimalDegree.ToString());
            body = body.Replace("**East**", se.Longitude.DecimalDegree.ToString());
            body = body.Replace("**Rotation**", "0");
            File.WriteAllText(kmlPath, body);
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
            //var resourceName = "AssemblyName.MyFile.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static void WriteImage(Map map, string fileName)
        {
            byte[] image = map.GetProperty<byte[]>("image");

            unsafe
            {
                fixed (byte* ptr = image)
                {
                    using (Bitmap bitmap = new Bitmap(map.width, map.height, map.width * 4,
                                PixelFormat.Format32bppRgb, new IntPtr(ptr)))
                    {
                        bitmap.Save(fileName);
                    }
                }
            }
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
            r = Convert(cr);
            g = Convert(cg);
            b = Convert(cb);
        }

        private static byte Convert(double v)
        {
            double vv = v * 256;
            if (vv < 0)
                return 0;
            else if (vv > 255)
                return 255;
            else
                return (byte)vv;
        }

        private static byte Confine(double v)
        {
            if (v < 0)
                return 0;
            else if (v > 255)
                return 255;
            else
                return (byte)v;
        }
    }
}
