using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenGL;

namespace trentGB
{
    class LCD
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct GbColor
        {
            public byte R;
            public byte G;
            public byte B;
        }
        private bool isStopped = false;
        private Bitmap framebuffer = null;
        private PictureBox display;

        public LCD(PictureBox disp)
        {
            display = disp;
        }

        public Bitmap buildRandomSolidColorImage()
        {
            int width = 160;
            int height = 144;
            Random rand = new Random();
            Bitmap Bmp = new Bitmap(width, height);
            using (Graphics gfx = Graphics.FromImage(Bmp))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255))))
            {
                gfx.FillRectangle(brush, 0, 0, width, height);
            }
            return Bmp;
        }

        public void drawImage()
        {
            try
            {
                display.Image = framebuffer;
            }
            catch
            {
                
            }
            
        }

        public void stop()
        {
            isStopped = true;
        }

        public void setFrameBuffer(Bitmap bmp)
        {
            framebuffer = bmp;
        }

        public Bitmap getFrame()
        {
            return framebuffer;
        }

        public void tick()
        {
        }
    }
}
