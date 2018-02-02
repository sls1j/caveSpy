using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    class PointCloud : IDisposable
    {
        FileStream _fin;
        BinaryReader _reader;
        PointCloudHeader _header;
        ProjectionData _projection;

        public PointCloud(string path)
        {
            _fin = new FileStream(path, FileMode.Open);
            _reader = new BinaryReader(_fin);
            _header = new PointCloudHeader(_reader);
            var rawRrojectionData = _header.VarHeaders.FirstOrDefault(h => h.Description == "OGR variant of OpenGIS WKT SRS");
            if (null == rawRrojectionData)
                _projection = new ProjectionData();
            else
                _projection = new ProjectionData(rawRrojectionData.Data);
        }

        public PointCloudHeader Header { get { return _header; } }
        public ProjectionData Projection { get { return _projection; } }

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
            var map = new Map(gridHeight, gridWidth, GetZone(), physicalX, physicalY, physicalWidth, physicalHeight, _header.MaxZ, _header.MinZ);
            return map;
        }

        public Map MakeMap(int gridWidth)
        {
            double width = _header.MaxX - _header.MinX;
            double height = _header.MaxY - _header.MinY;
            int gridHeight = (int)(gridWidth / height * width);            
            var map = new Map(gridHeight, gridWidth, GetZone(), _header.MinX, _header.MinY, width, height, _header.MaxZ, _header.MinZ);
            return map;
        }

        private string GetZone()
        {
            if (_projection == null)
                return "12T";
            else
            {
                var zone = _projection.Head.properties.FirstOrDefault(p => p.Contains("zone"));
                if (null == zone)
                    return "12T";
                else
                {
                    int index = zone.IndexOf("zone ");
                    zone = zone.Substring(index + 5, zone.Length - index - 5);
                    return zone;
                }
            }
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
                if (i % 1000000 == 0)
                    Console.WriteLine($"{i:0,000}/{_header.NumberOfPointRecords:0,000} {(double)i / (double)_header.NumberOfPointRecords * 100: 0.00}");

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

    public class ProjectionData
    {
        private string _rawData;
        private ProjNode _head;
        public ProjectionData()
        {

        }

        public ProjectionData(byte[] data)
        {
            _rawData = Encoding.UTF8.GetString(data);
            ParseData();
        }

        public ProjNode Head { get { return _head; } }

        private void ParseData()
        {
            int state = 0;
            var sb = new StringBuilder();
            var stack = new Stack<ProjNode>();
            var currNode = _head = new ProjNode();
            for (int i = 0; i < _rawData.Length; i++)
            {
                char c = _rawData[i];
                switch (state)
                {
                    case 0: // slurp node name
                        {
                            if (char.IsLetter(c))
                            {
                                sb.Append(c);
                            }
                            else if (c == '[')
                            {                                
                                currNode.name = sb.ToString();
                                sb = new StringBuilder();
                                state = 1;
                            }
                        }
                        break;
                    case 1: // read node contents
                        if (c == '\"')
                            state = 2;
                        else if (char.IsLetter(c))
                        {
                            stack.Push(currNode);
                            var child = new ProjNode();
                            currNode.children.Add(child);
                            currNode = child;
                            state = 0;
                        }
                        else if (c == ']')
                        {
                            currNode = stack.Pop();
                        }
                        else if (c == ',')
                        {
                        }
                        break;
                    case 2: // read property
                        if (c == '"')
                        {
                            currNode.properties.Add(sb.ToString());
                            sb = new StringBuilder();
                            state = 1;
                        }
                        else
                            sb.Append(c);
                        break;
                }
            }
        }
    }


    public class ProjNode
    {
        public string name;
        public List<string> properties;
        public List<ProjNode> children;

        public ProjNode()
        {
            properties = new List<string>();
            children = new List<ProjNode>();
        }

        public ProjNode(string name)
            : this()
        {

        }

        public override string ToString()
        {
            var props = properties.Select(p => $"\"{p}\"");
            var cs = children.Select(c => c.ToString());
            var combined = cs.Union(props);
            string guts = string.Join(",", combined);
            
            return $"{name}[{guts}]";
        }
    }

    [Serializable]
    class Map
    {
        private Dictionary<string, object> _properties;
        public Map()
        {
            _properties = new Dictionary<string, object>();
        }

        public Map(int width, int height, string zone, double physicalX, double physicalY, double physicalWidth, double physicalHeight, double maxElevation, double minElevation)
            : this()
        {
            this.width = width;
            this.height = height;
            this.elevations = new double[this.width * this.height];
            this.zone = zone;
            this.physicalWidth = physicalWidth;
            this.physicalHeight = physicalHeight;
            this.physicalLeft = physicalX;
            this.physicalTop = physicalY;
            this.physicalRight = physicalX + physicalWidth;
            this.physicalBottom = physicalTop + physicalHeight;
            this.maxElevation = maxElevation;
            this.minElevation = minElevation;
        }

        public double[] elevations;
        public string zone;
        public int height;
        public int width;
        public double physicalLeft;
        public double physicalTop;
        public double physicalRight;
        public double physicalBottom;
        public double physicalWidth;
        public double physicalHeight;
        public double maxElevation;
        public double minElevation;

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

        public static Map Load(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var reader = new BinaryFormatter();
                return (Map)reader.Deserialize(stream);
            }
        }
    }
}
