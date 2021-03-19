using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Khronos;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace trentGB
{
    class Gameboy
    {
        public static double clockTimingInMHz = 4.194;
        public static double clockTimingInHz = 4194000;
        private Stopwatch clock = new Stopwatch();

        private AddressSpace memory = null;
        private ROM rom = null;
        private CPU cpu = null;
        private LCD lcd = null;
        private LCDController lcdController = null;

        private Thread runLoopThread = null;

        public Gameboy(ROM romToPlay, PictureBox display)
        {
            rom = romToPlay;
            memory = new AddressSpace();
            cpu = new CPU(memory, romToPlay, clock);
            lcd = new LCD(display);
            lcdController = new LCDController(lcd, memory);

            memory.loadRom(romToPlay);
        }




        public void Start()
        {
            // Perform Runtime Validations by Executing OPs from Address 0x0000 to 0x00FF
            rom.printRomHeader();
            // Start CPU
            cpu.reset();

            runLoopThread = new Thread(runLoop);
            runLoopThread.Start();
        }

        private void runLoop()
        {
            
            clock.Start();
            long startTime = clock.ElapsedMilliseconds;
            while (true)
            {
                long delta = clock.ElapsedMilliseconds - startTime;

                //double fps = framesDrawnSinceLastLoop / ((double)(delta * 1000));

                // Tick at CPU Clock frequency 4.194 Mhz
                int ticksNeeded = (int)(delta / 500); // ms to .5us

                while (ticksNeeded-- > 0)
                {
                    performSystemTick();
                }
            }
        }

        private void performSystemTick()
        {
            // This should tick at 4.194Mhz or 1tick every .5 us
            cpu.tick();

            // 4.194 MHz Dot Clock
            lcdController.tick();
        }
    }
}
