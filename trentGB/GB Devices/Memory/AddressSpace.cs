using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    /// <summary>
    /// Memory Map
    /// </summary>
    /// <remarks>
    ///    Interrupt Enable Register
    ///    --------------------------- FFFF
    ///    Internal RAM
    ///    --------------------------- FF80
    ///    Empty but unusable for I/O
    ///    --------------------------- FF4C
    ///    I/O ports
    ///    --------------------------- FF00
    ///    Empty but unusable for I/O
    ///    --------------------------- FEA0
    ///    Sprite Attrib Memory(OAM)
    ///    --------------------------- FE00
    ///    Echo of 8kB Internal RAM
    ///    --------------------------- E000
    ///    8kB Internal RAM
    ///    --------------------------- C000
    ///    8kB switchable RAM bank
    ///    --------------------------- A000
    ///    8kB Video RAM
    ///    --------------------------- 8000 --
    ///    16kB switchable ROM bank |
    ///    --------------------------- 4000 |= 32kB Cartrigbe
    ///    16kB ROM bank #0 |
    ///    --------------------------- 0000 --
    /// </remarks> 


    class AddressSpace
    {
        private byte[] bytes = new byte[0xFFFF+1];
        ushort echoOffset = (0xE000 - 0xC000);

        public AddressSpace()
        {
            Array.Clear(bytes, 0, bytes.Length);

            // TODO: Copy Internal GB Boot Rom to 0x0000 - 0x1000

        }
        

        public byte[] getBytes()
        {
            return bytes;
        }
        public byte getByte(ushort address)
        {
            return bytes[address];
        
        }

        public byte peekByte(ushort address)
        {
            // Used for CYcle Inaccurate Memory Access for Debugging
            return bytes[address];
        }

        public byte[] getBytes(ushort address, ushort count)
        {
            byte[] rv = new Byte[count];
            if (((UInt32)address + (UInt32)count) <= 0xFFFF)
            {
                Array.ConstrainedCopy(bytes, address,  rv, 0, (int)count);
            }
            else
            {
                rv = null;
            }
            
            return rv;
        }

        public void setByte(ushort address, Byte value)
        {
            bytes[address] = value;

            // On Set Byte Make Sure we maintain the Memory Echo Space

            if (address >= 0xC000 && address <= 0xDE00)
            {
                bytes[address + echoOffset] = value;
            }
            else if (address >= 0xE000 && address <= 0xFE00)
            {
                bytes[address - echoOffset] = value;
            }
        }

        public void setBytes(ushort address, Byte[] values)
        {

            if (((UInt32)address + values.Count()) <= 0xFFFF)
            {
                Array.ConstrainedCopy(values, 0, bytes, address, values.Count());

                if (address >= 0xC000 && address <= 0xDE00)
                {
                    Array.ConstrainedCopy(values, 0, bytes, (address + echoOffset), values.Count());
                }
                else if (address >= 0xE000 && address <= 0xFE00)
                {
                    Array.ConstrainedCopy(values, 0, bytes, (address - echoOffset), values.Count());
                }
            }
            else
            {
                throw new Exception($"GetBytes: Writing a Value out of Range of Address Space");
            }
        }

        public void loadRom(ROM rom)
        {
            int end1 = (rom.size > 0x4000) ? 0x4000 : rom.size;
            int end2 = (rom.size > 0x8000) ? 0x8000 : rom.size;

            for (ushort i = 0; i < end1; i++)
            {
                setByte(i, rom.getByte(i)); // Copy 1st Rom bank to RAM
            }

            for (ushort i = 0x4000; i < end2; i++)
            {
                setByte(i, rom.getByte(i)); // Copy 2nd Rom bank to RAM
            }
        }
    }
}
