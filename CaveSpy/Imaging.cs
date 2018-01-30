using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace CaveSpy
{
    class Imaging
    {
        private byte[] _image;
        private int _height;
        private int _width;
        
        public void MakeMap(double[] map, int width, int height, double max, double min, bool addShading)
        {

            double scale = 256 / (max - min);
            _image = new byte[map.Length * 4];
            _width = width;
            _height = height;

            for (int i = 0, ii = 0; i < map.Length; i++)
            {
                double v = map[i];
                double dv = 0;
                if (addShading)
                {
                    if (i > 5)
                        dv = (map[i - 5] - map[i]) * 5;
                    else
                        dv = 0;
                }
                    
                double scaledValue = (v - min) * scale + dv;
                byte color = (byte)Math.Max(Math.Min(scaledValue,255),0);
                _image[ii++] = color;
                _image[ii++] = color;
                _image[ii++] = color;
                _image[ii++] = 255;
            }               
        }        

        public void AddCaves(List<Cave> caves, int red, int green, int blue)
        {
            if (_image == null)
                throw new InvalidOperationException("Must create an image first!");

            for (int i = 0; i < caves.Count; i++)
            {
                var c = caves[i];
                if (c.X < 0 || c.X >= _width || c.Y < 0 || c.Y >= _height)
                    continue;

                var ii = (int)(c.X + c.Y * _width) * 4;
                _image[ii] = (byte)0;
                _image[ii + 1] = (byte)0;
                _image[ii + 2] = (byte)255;
            }
        }

        public void Save(string fileName)
        {
            if (_image == null)
                throw new InvalidOperationException("Must create an image first!");

            unsafe
            {
                fixed (byte* ptr = _image)
                {
                    using (Bitmap image = new Bitmap(_width, _height, _width * 4,
                                PixelFormat.Format32bppRgb, new IntPtr(ptr)))
                    {
                        image.Save(fileName);
                    }
                }
            }
        }
    }
}
