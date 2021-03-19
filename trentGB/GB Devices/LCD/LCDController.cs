﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    class LCDController
    {
        private int vBlankCycleCount = 0;
        private int vBlankCycleTiming = 0;
        private LCD display = null;

        public LCDController(LCD display)
        {
            this.display = display;

            // perform vBlank at 59.73 Hz or this many Cycles
            vBlankCycleTiming = (int)(Gameboy.clockTimingInHz / 59.73f);
        }


        public void tick()
        {
            vBlankCycleCount++;
            // Divide Clock to 59.73 Hz (Probably D)

            // perform VBlank at 59.73 HZ or every
            if (vBlankCycleCount >= vBlankCycleTiming)
            {
                display.setFrameBuffer(display.buildRandomSolidColorImage());
                display.drawImage();
                vBlankCycleCount = 0;
            }
        }
    }
}