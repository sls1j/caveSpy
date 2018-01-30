using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    // Define other methods and classes here
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


        public PointCloudHeader(BinaryReader r)
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
        }
    }


    class PointDataRecordFormat
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

        public static void ReadFormat1(PointDataRecordFormat p, BinaryReader reader)
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

        public static void ReadFormat2(PointDataRecordFormat p, BinaryReader reader)
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

        public static void ReadFormat3(PointDataRecordFormat p, BinaryReader reader)
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
}
