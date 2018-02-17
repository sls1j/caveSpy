using Bee.Eee.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    [Serializable]
    class PointCloud
    {
        string _path;
        Action<PointData, BinaryReader> _readFunc;
        PointCloudHeader _header;
        ProjectionData _projection;        
        ILogger _logger;

        public PointCloud(ILogger logger)
        {
            _logger = (logger != null) ? logger.CreateSub("PointCloud") : throw new ArgumentNullException("logger");
        }

        public PointCloudHeader Header { get { return _header; } }
        public ProjectionData Projection { get { return _projection; } }

        public void LoadFromLas(string path)
        {
            _path = path;
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
            {
                _header = new PointCloudHeader(reader);
                var rawRrojectionData = _header.VarHeaders.FirstOrDefault(h => h.Description == "OGR variant of OpenGIS WKT SRS");
                if (null == rawRrojectionData)
                    _projection = new ProjectionData();
                else
                    _projection = new ProjectionData(rawRrojectionData.Data);


                // reset the file position
                fileStream.Seek(_header.OffsetToPointData, SeekOrigin.Begin);
                
                switch (_header.PointDataFormat)
                {
                    case 0: _readFunc = PointData.ReadFormat0; break;
                    case 1: _readFunc = PointData.ReadFormat1; break;
                    case 2: _readFunc = PointData.ReadFormat2; break;
                    case 3: _readFunc = PointData.ReadFormat3; break;
                    default:
                        throw new NotImplementedException($"Format {_header.PointDataFormat} not supported.");
                }
            }
        }

        public void LogHeader()
        {
            _logger.Log($"");
        }

        public void HandlePoints(Action<int, PointData> handler)
        {
            using (FileStream fileStream = new FileStream(_path, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
            {
                // move to the point data
                fileStream.Seek(_header.OffsetToPointData, SeekOrigin.Begin);

                // start reading
                for (var i = 0; i < _header.NumberOfPointRecords; i++)
                {
                    var pd = new PointData();
                    _readFunc(pd, reader);
                    handler(i, pd);
                }
            }
        }
        
        public string Zone
        {
            get
            {
                if (_projection == null || _projection.Head == null)
                {
                    _logger.Log(Level.Warn, $"Zone not found in the Projection data. Assuming UTM zone of {DefaultZone}.  The kml will not likely be in the correct place!");
                    return DefaultZone;
                }
                else
                {
                    var zone = _projection.Head.properties.FirstOrDefault(p => p.Contains("zone"));
                    if (null == zone)
                        return DefaultZone;
                    else
                    {
                        int index = zone.IndexOf("zone ");
                        zone = zone.Substring(index + 5, zone.Length - index - 5);
                        return zone;
                    }
                }

            }
        }

        public string DefaultZone { get; set; }

        public void Save(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                var writer = new BinaryFormatter();
                writer.Serialize(stream, this);
            }
        }

        public static PointCloud Load(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var reader = new BinaryFormatter();
                return (PointCloud)reader.Deserialize(stream);
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

    [Serializable]
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

    // Define other methods and classes here
    [Serializable]
    class PointCloudHeader
    {
        public char[] FileSignature; // 4 bytes
        public ushort FileSourceID;
        public ushort GlobalEncoding;
        public byte[] ProjectID;
        public byte VersionMajor;
        public byte VersionMinor;
        public byte[] SystemIdentifier; // 32 bytes
        public char[] GeneratingSoftware; // 32 bytes;
        public ushort FileCreationDayOfYear;
        public ushort FileCreationYear;
        public ushort HeaderSize;
        public uint OffsetToPointData;
        public uint NumberOfVariableLengthRecords;
        public byte PointDataFormat;
        public ushort PointDataRecordLength;
        public uint NumberOfPointRecords;
        public uint[] NumberOfPointsByReturn; // size 5 -- 20 bytes
        public double XScaleFactor;
        public double YScaleFactor;
        public double ZScaleFactor;
        public double XOffset;
        public double YOffset;
        public double ZOffset;
        public double MaxX;
        public double MinX;
        public double MaxY;
        public double MinY;
        public double MaxZ;
        public double MinZ;
        public List<PointCloudVariableLengthHeader> VarHeaders;

        public PointCloudHeader()
        {
            VarHeaders = new List<PointCloudVariableLengthHeader>();
        }

        public PointCloudHeader(BinaryReader r)
            : this()
        {
            Read(r);
        }

        void Read(BinaryReader reader)
        {
            FileSignature = reader.ReadChars(4);
            FileSourceID = reader.ReadUInt16();
            GlobalEncoding = reader.ReadUInt16();
            ProjectID = reader.ReadBytes(16);
            VersionMajor = reader.ReadByte();
            VersionMinor = reader.ReadByte();
            SystemIdentifier = reader.ReadBytes(32);
            GeneratingSoftware = reader.ReadChars(32);
            FileCreationDayOfYear = reader.ReadUInt16();
            FileCreationYear = reader.ReadUInt16();
            HeaderSize = reader.ReadUInt16();
            OffsetToPointData = reader.ReadUInt32();
            NumberOfVariableLengthRecords = reader.ReadUInt32();
            PointDataFormat = reader.ReadByte();
            PointDataRecordLength = reader.ReadUInt16();
            NumberOfPointRecords = reader.ReadUInt32();
            NumberOfPointsByReturn = new uint[5];
            for (int i = 0; i < 5; i++)
            {
                NumberOfPointsByReturn[i] = reader.ReadUInt32();
            }
            XScaleFactor = reader.ReadDouble();
            YScaleFactor = reader.ReadDouble();
            ZScaleFactor = reader.ReadDouble();
            XOffset = reader.ReadDouble();
            YOffset = reader.ReadDouble();
            ZOffset = reader.ReadDouble();
            MaxX = reader.ReadDouble();
            MinX = reader.ReadDouble();
            MaxY = reader.ReadDouble();
            MinY = reader.ReadDouble();
            MaxZ = reader.ReadDouble();
            MinZ = reader.ReadDouble();
            for (int i = 0; i < this.NumberOfVariableLengthRecords; i++)
            {
                var vh = new PointCloudVariableLengthHeader(reader);
                VarHeaders.Add(vh);
            }
        }
    }

    [Serializable]
    class PointData
    {
        public int X;
        public int Y;
        public int Z;
        public ushort Intensity;
        public byte ReturnBits; // 0-2 ReturnNumber, 3-5 NumberOfReturns, 6 Scan Direction Flag, 7 Edige of Flight Line
        public byte Classification;
        public byte ScanAngleRank;
        public byte UserData;
        public ushort PointSourceID;
        public double GPSTime;
        public ushort Red;
        public ushort Green;
        public ushort Blue;

        public static void ReadFormat0(PointData p, BinaryReader reader)
        {
            p.X = reader.ReadInt32();
            p.Y = reader.ReadInt32();
            p.Z = reader.ReadInt32();
            p.Intensity = reader.ReadUInt16();
            p.ReturnBits = reader.ReadByte();
            p.Classification = reader.ReadByte();
            p.ScanAngleRank = reader.ReadByte();
            p.UserData = reader.ReadByte();
            p.PointSourceID = reader.ReadUInt16();
        }

        public static void ReadFormat1(PointData p, BinaryReader reader)
        {
            p.X = reader.ReadInt32();
            p.Y = reader.ReadInt32();
            p.Z = reader.ReadInt32();
            p.Intensity = reader.ReadUInt16();
            p.ReturnBits = reader.ReadByte();
            p.Classification = reader.ReadByte();
            p.ScanAngleRank = reader.ReadByte();
            p.UserData = reader.ReadByte();
            p.PointSourceID = reader.ReadUInt16();
            p.GPSTime = reader.ReadDouble();
        }

        public static void ReadFormat2(PointData p, BinaryReader reader)
        {
            p.X = reader.ReadInt32();
            p.Y = reader.ReadInt32();
            p.Z = reader.ReadInt32();
            p.Intensity = reader.ReadUInt16();
            p.ReturnBits = reader.ReadByte();
            p.Classification = reader.ReadByte();
            p.ScanAngleRank = reader.ReadByte();
            p.UserData = reader.ReadByte();
            p.PointSourceID = reader.ReadUInt16();
            p.GPSTime = reader.ReadDouble();
            p.Red = reader.ReadUInt16();
            p.Green = reader.ReadUInt16();
            p.Blue = reader.ReadUInt16();
        }

        public static void ReadFormat3(PointData p, BinaryReader reader)
        {
            p.X = reader.ReadInt32();
            p.Y = reader.ReadInt32();
            p.Z = reader.ReadInt32();
            p.Intensity = reader.ReadUInt16();
            p.ReturnBits = reader.ReadByte();
            p.Classification = reader.ReadByte();
            p.ScanAngleRank = reader.ReadByte();
            p.UserData = reader.ReadByte();
            p.PointSourceID = reader.ReadUInt16();
            p.GPSTime = reader.ReadDouble();
            p.Red = reader.ReadUInt16();
            p.Green = reader.ReadUInt16();
            p.Blue = reader.ReadUInt16();
        }

       
    }

    [Serializable]
    public class PointCloudVariableLengthHeader
    {
        public ushort Reserved;
        public string UserId; // 16 bytes
        public ushort RecordId;
        public ushort RecordLengthAfterHeader;
        public string Description;
        public byte[] Data;

        public PointCloudVariableLengthHeader()
        {

        }

        public PointCloudVariableLengthHeader(BinaryReader reader)
            : this()
        {
            Read(reader);
        }

        public void Read(BinaryReader reader)
        {
            Reserved = reader.ReadUInt16();
            UserId = reader.ReadString(16);
            RecordId = reader.ReadUInt16();
            RecordLengthAfterHeader = reader.ReadUInt16();
            Description = reader.ReadString(32);
            Data = reader.ReadBytes(RecordLengthAfterHeader);            
        }
    }
}
