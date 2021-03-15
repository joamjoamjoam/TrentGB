using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    class Gameboy
    {
        private AddressSpace memory = null;
        private ROM rom = null;
        private CPU cpu = null;

        public Gameboy(ROM romToPlay)
        {
            rom = romToPlay;
            memory = new AddressSpace();
            cpu = new CPU(memory, romToPlay);
        }


        public void Start()
        {
            // Perform Runtime Validations by Executing OPs from Address 0x0000 to 0x00FF
            rom.printRomHeader();

            // Start CPU
            cpu.reset();

            while (!cpu.done)
            {
                cpu.decodeAndExecute(cpu.fetch());
            }

        }
    }
}
