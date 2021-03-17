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
        private LCD lcd = null;

        public Gameboy(ROM romToPlay)
        {
            rom = romToPlay;
            memory = new AddressSpace();
            cpu = new CPU(memory, romToPlay);
            lcd = new LCD();
        }


        public void Start()
        {
            // Perform Runtime Validations by Executing OPs from Address 0x0000 to 0x00FF
            rom.printRomHeader();

            // Start CPU
            cpu.reset();

            while (!cpu.done)
            {
                Byte nextInstruction = cpu.fetch();
                if (nextInstruction == 0x10)
                {
                    // Halt CPU and LCD
                    lcd.stop();
                }

                cpu.decodeAndExecute(nextInstruction);
            }

        }
    }
}
