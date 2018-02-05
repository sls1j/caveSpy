using Bee.Eee.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CaveSpy
{
    [Serializable]
    class Map
    {
        private Dictionary<string, object> _properties;
        public Map()
        {
            _properties = new Dictionary<string, object>();
        }       

        public double[] elevations;
        public byte[] classifications;
        public Color[] colors;
        public string zone;
        public int height;
        public int width;
        public double physicalLeft;
        public double physicalTop;
        public double physicalRight;
        public double physicalBottom;
        public double physicalWidth;
        public double physicalHeight;
        public double physicalHigh;
        public double physicalLow;

        public void SetProperty<T>(string name, T value)
        {
            _properties[name] = (object)value;
        }

        public T GetProperty<T>(string name)
        {
            object value;
            value = _properties[name];
            return (T)value;
        }

        public void Save(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                var writer = new BinaryFormatter();
                writer.Serialize(stream, this);
            }
        }

        public static Map Load(string filePath, ILogger logger)
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var reader = new BinaryFormatter();
                var map = (Map)reader.Deserialize(stream);
                return map;
            }
        }

        
    }

    [Serializable]
    class Color
    {
        public ushort red;
        public ushort green;
        public ushort blue;

        public Color(ushort red, ushort green, ushort blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }
    }

    class MapAlgorithms
    {
        private ILogger _logger;

        public MapAlgorithms(ILogger logger)
        {
            _logger = (logger != null) ? logger.CreateSub("MapAlgorithms") : throw new ArgumentNullException("logger");
        }
        public void ReadCloudIntoMap(Map map, int mapWidth, PointCloud cloud)
        {
            var header = cloud.Header;

            map.physicalLeft = header.MinX;
            map.physicalTop = header.MinY;
            map.physicalRight = header.MaxX;
            map.physicalBottom = header.MaxX;
            map.physicalWidth = header.MaxX - header.MinX;
            map.physicalHeight = header.MaxY - header.MinY;
            map.physicalHigh = cloud.Header.MaxZ;
            map.physicalLow = cloud.Header.MinZ;
            map.zone = cloud.Zone;

            map.width = mapWidth;
            map.height = (int)(int)(mapWidth / map.physicalHeight * map.physicalWidth);

            double xScale = (double)(map.width - 1) / map.physicalWidth;
            double yScale = (double)(map.height - 1) / map.physicalHeight;
            double minX = map.physicalLeft;
            double minY = map.physicalTop;

            map.elevations = new double[map.width * map.height];
            map.classifications = new byte[map.width * map.height];
            map.colors = new Color[map.width * map.height];


            for (int i = 0; i < cloud.Header.NumberOfPointRecords; i++)
            {
                if (i % 1000000 == 0)
                    _logger.Log(Level.Debug, $"{i:0,000}/{header.NumberOfPointRecords:0,000} {(double)i / (double)header.NumberOfPointRecords * 100: 0.00}%");

                var p = cloud.Points[i];

                double x = p.X * header.XScaleFactor + header.XOffset;
                double y = p.Y * header.YScaleFactor + header.YOffset;
                double z = p.Z * header.ZScaleFactor + header.ZOffset;

                // skip if not in area of interest
                if (x < map.physicalLeft || x > map.physicalRight || y < map.physicalTop || y > map.physicalBottom)
                    continue;

                int xi = (int)((x - minX) * xScale);
                int yi = map.height - (int)((y - minY) * yScale) - 1;
                int ii = yi * map.width + xi;
                map.elevations[ii] = z;
                map.classifications[ii] = p.Classification;
                if (header.PointDataFormat == 2 || header.PointDataFormat == 3)
                    map.colors[ii] = new Color(p.Red, p.Green, p.Blue);
            }
        }

        public void LinearFillMap(Map map)
        {
            int i = 0;
            double lastValue = 0;
            for (int y = 0; y < map.height; y++)
            {
                for (int x = 0; x < map.width; x++, i++)
                {
                    double v = map.elevations[i];
                    if (v == 0)
                        map.elevations[i] = lastValue;
                    else
                        lastValue = v;
                }
            }
        }
    }
}
