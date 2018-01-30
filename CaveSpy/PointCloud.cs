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

        public double[] ExtractElevationMap(int gridWidth, int gridHeight, bool useBounds, double left, double top, double right, double bottom)
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

            double[] map = new double[gridWidth * gridHeight];

            double xScale = 0.0;
            double yScale = 0.0;
            double minX = 0.0;
            double minY = 0.0;
            if (useBounds)
            {
                xScale = (double)(gridWidth - 1) / (right - left);
                yScale = (double)(gridHeight - 1) / (bottom - top);
                minX = left;
                minY = top;
            }
            else
            {
                xScale = (double)(gridWidth - 1) / (_header.MaxX - _header.MinX);
                yScale = (double)(gridHeight - 1) / (_header.MaxY - _header.MinY);
                minX = _header.MinX;
                minY = _header.MinY;
            }

            for (int i = 0; i < _header.NumberOfPointRecords; i++)
            {
                var p = new PointDataRecordFormat();
                readFunc(p, _reader);

                double x = p.X * _header.XScaleFactor + _header.XOffset;
                double y = p.Y * _header.YScaleFactor + _header.YOffset;
                double z = p.Z * _header.ZScaleFactor + _header.ZOffset;

                // skip if not in area of interest
                if (useBounds && (x < left || x > right || y < top || y > bottom))
                    continue;

                int xi = (int)((x - minX) * xScale);
                int yi = gridHeight - (int)((y - minY) * yScale) - 1;
                map[yi * gridWidth + xi] = z;
            }

            return map;
        }

        public void FillMap(double[] map, int width, int height)
        {
            int i = 0;
            double lastValue = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, i++)
                {
                    double v = map[i];
                    if (v == 0)
                        map[i] = lastValue;
                    else
                        lastValue = v;
                }
            }
        }
    }
}
