using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    class Gameboy
    {
        private Memory memory = null;
        private ROM rom = null;
        private byte PC = 0;

        public Gameboy(ROM romToPlay)
        {
            rom = romToPlay;
            memory = new Memory(rom);
            Start();
        }


        public void Start()
        {
            // Perform Runtime Validations by Executing OPs from Address 0x0000 to 0x00FF
            rom.printRomHeader();
        }
    }
}
