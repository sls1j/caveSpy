using Bee.Eee.Utility.Logging;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CaveSpy
{
  class Map
  {
    public Map()
    {
    }

    public double[] elevations;
    //public IReadOnlyDictionary<int, List<PointData>> pointCloud;
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

    public Map Clone()
    {
      return this.MemberwiseClone() as Map;
    }

    public void Save(string filePath)
    {
      using (var stream = new FileStream(filePath, FileMode.Create))
      using (var writer = new BinaryWriter(stream))
      {
        writer.Write(width);
        writer.Write(height);
        writer.WriteArray(elevations);
        writer.WriteArray(classifications);
        writer.WriteArray(colors);
        writer.Write(zone);
        writer.Write(physicalLeft);
        writer.Write(physicalRight);
        writer.Write(physicalTop);
        writer.Write(physicalBottom);
        writer.Write(physicalHeight);
        writer.Write(physicalWidth);
        writer.Write(physicalHigh);
        writer.Write(physicalLow);
      }
    }

    public static Map Load(string filePath, ILogger logger)
    {
      using (var stream = new FileStream(filePath, FileMode.Open))
      using (var reader = new BinaryReader(stream))
      {
        Map map = new Map();
        map.width = reader.ReadInt32();
        map.height = reader.ReadInt32();
        map.elevations = reader.ReadDoubleArray();
        map.classifications = reader.ReadByteArray();
        map.colors = reader.ReadColorArray();
        map.zone = reader.ReadString();
        map.physicalLeft = reader.ReadDouble();
        map.physicalRight = reader.ReadDouble();
        map.physicalTop = reader.ReadDouble();
        map.physicalBottom = reader.ReadDouble();
        map.physicalHeight = reader.ReadDouble();
        map.physicalWidth = reader.ReadDouble();
        map.physicalHigh = reader.ReadDouble();
        map.physicalLow = reader.ReadDouble();
        return map;
      }
    }
  }

  public class Color
  {
    public ushort red;
    public ushort green;
    public ushort blue;

    public Color()
    {

    }

    public Color(ushort red, ushort green, ushort blue)
    {
      this.red = red;
      this.green = green;
      this.blue = blue;
    }

    public static readonly Color Empty = new Color(0, 0, 0);
  }
}
