using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class PointCloud : IDisposable
    {
        FileStream _fin;
        BinaryReader _reader;
        PointCloudHeader _header;

        public PointCloud(string path)
        {
            _fin = new FileStream(path, FileMode.Open);
            _reader = new BinaryReader(_fin);
            _header = new PointCloudHeader(_reader);
        }

        public void Dispose()
        {
            try
            {
                if (_reader != null)
                    _reader.Dispose();

                if (_fin != null)
                    _fin.Dispose();

                _reader = null;
                _fin = null;
            }
            catch (Exception)
            {
            }

            GC.SuppressFinalize(this);
        }

        public Map MakeMap(int gridWidth, double physicalX, double physicalY, double physicalWidth, double physicalHeight)
        {
            int gridHeight = (int)(gridWidth / physicalHeight * physicalWidth);
            var map = new Map(gridHeight, gridWidth, physicalX, physicalY, physicalWidth, physicalHeight);
            return map;
        }

        public Map MakeMap(int gridWidth, int gridHeight)
        {
            var map = new Map(gridHeight, gridWidth, _header.XOffset, _header.YOffset, _header.MaxX - _header.MinX, _header.MaxY - _header.MinY);
            return map;
        }

        public Map MakeMap(int gridWidth)
        {
            double width = _header.MaxX - _header.MinX;
            double height = _header.MaxY - _header.MinY;
            int gridHeight = (int)(gridWidth / height * width);
            var map = new Map(gridHeight, gridWidth, _header.MinX, _header.MinY, width, height);
            return map;
        }

        public void ExtractElevationMap(Map map)
        {
            // reset the file position
            _fin.Seek(_header.OffsetToPointData, SeekOrigin.Begin);

            Action<PointDataRecordFormat, BinaryReader> readFunc;
            switch (_header.PointDataFormat)
            {
                case 1: readFunc = PointDataRecordFormat.ReadFormat1; break;
                case 2: readFunc = PointDataRecordFormat.ReadFormat2; break;
                case 3: readFunc = PointDataRecordFormat.ReadFormat3; break;
                default:
                    throw new NotImplementedException($"Format {_header.PointDataFormat} not supported.");
            }

            double[] elevations = map.elevations;

            double xScale = (double)(map.width - 1) / map.physicalWidth;
            double yScale = (double)(map.height - 1) / map.physicalHeight;
            double minX = map.physicalLeft;
            double minY = map.physicalTop;

            for (int i = 0; i < _header.NumberOfPointRecords; i++)
            {
                var p = new PointDataRecordFormat();
                readFunc(p, _reader);

                double x = p.X * _header.XScaleFactor + _header.XOffset;
                double y = p.Y * _header.YScaleFactor + _header.YOffset;
                double z = p.Z * _header.ZScaleFactor + _header.ZOffset;

                // skip if not in area of interest
                if (x < map.physicalLeft || x > map.physicalRight || y < map.physicalTop || y > map.physicalBottom)
                    continue;

                int xi = (int)((x - minX) * xScale);
                int yi = map.height - (int)((y - minY) * yScale) - 1;
                elevations[yi * map.width + xi] = z;
            }
        }

        public void FillMap(Map map)
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

    class Map
    {
        private Dictionary<string, object> _properties;
        public Map(int width, int height, double physicalX, double physicalY, double physicalWidth, double physicalHeight)
        {
            this.width = width;
            this.height = height;
            this.elevations = new double[this.width * this.height];
            this.physicalWidth = physicalWidth;
            this.physicalHeight = physicalHeight;
            this.physicalLeft = physicalX;
            this.physicalTop = physicalY;
            this.physicalRight = physicalX + physicalWidth;
            this.physicalBottom = physicalTop + physicalHeight;
            _properties = new Dictionary<string, object>();
        }

        public double[] elevations;
        public int height;
        public int width;
        public double physicalLeft;
        public double physicalTop;
        public double physicalRight;
        public double physicalBottom;

        public double physicalWidth;
        public double physicalHeight;

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
    }
}
