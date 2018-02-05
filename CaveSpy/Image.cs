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
        }

        public void DrawHillshade(Map map, double heading, double step, double intensity)
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
                    image[iii] = c;
                    image[iii + 1] = c;
                    image[iii + 2] = c;
                    image[iii + 3] = 255;
                }
            }
        }

        public void DrawCaves(List<Cave> caves, double opacity)
        {
            for (int i = 0; i < caves.Count; i++)
            {
                var cave = caves[i];
                int ii = (int)(cave.X + cave.Y * width) * 4;
                image[ii] = 0;
                image[ii + 1] = 0;
                image[ii + 2] = 255;
            }
        }

        byte ConvertToByteColor(double v)
        {
            v *= 256;
            if (v < 0)
                return 0;
            if (v > 255)
                return 255;
            else
                return (byte)v;
        }

        byte ClipToByteColor(double v)
        {
            if (v < 0)
                return 0;
            if (v > 255)
                return 255;
            else
                return (byte)v;
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
