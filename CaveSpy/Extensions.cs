using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveSpy
{
    public static class Extensions
    {
        public static void NeighborhoodLoop(int width, int height, int x, int y, Action<int, int, int> a)
        {
            for (int yi = -1; yi <= 1; yi++)
            {
                int yl = y + yi;
                if (yl < 0 || yl >= height)
                    continue;

                for (int xi = -1; xi <= 1; xi++)
                {
                    int xl = x + xi;
                    if (xl < 0 || xl >= width)
                        continue;

                    int ii = yl * width + xl;
                    a(ii, xl, yl);
                }
            }
        }
    }
}
