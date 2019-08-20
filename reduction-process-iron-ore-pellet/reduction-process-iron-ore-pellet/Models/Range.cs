using System.Drawing;

namespace grain_growth.Models
{
    public class Range
    {
        public Range(int width, int height)
        {
            Width = width;
            Height = height;
            GrainsArray = new Grain[width, height];
            StructureBitmap = new Bitmap(width, height);
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public Bitmap StructureBitmap { get; set; }

        public Grain[,] GrainsArray { get; set; }
    }
}