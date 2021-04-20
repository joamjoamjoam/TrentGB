using System;
using System.Collections.Generic;
using System.Diagnostics;
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


    public class AddressSpace
    {
        private byte[] bytes = new byte[0xFFFF + 1];
        ushort echoOffset = (0xE000 - 0xC000);

        // Test Rom Ascii byte (No Graphics). Store ASCII encoded Byte at FF01. Print to console when 0x81 is written to 0xFF02
        public Char testChar;
        public ROM rom;

        public enum DebugCheck
        {
            None,
            WriteOccurred,
            ReadOccurred,
            AccessOcurred
        }
        public class AddressSpaceBreakpoint
        {
            DebugCheck checkType = DebugCheck.None;
            ushort address = 0x0000;
            bool breakHit = false;

            public AddressSpaceBreakpoint(ushort address, DebugCheck type)
            {
                checkType = type;
                this.address = address;
            }

            public void update(ushort address, DebugCheck type)
            {
                if (!breakHit)
                {
                    breakHit = ((address == this.address) && ((type == checkType) || (checkType == DebugCheck.AccessOcurred)));
                }
            }

            public bool wasHit()
            {
                return breakHit;
            }



            public void reset()
            {
                breakHit = false;
            }

            public new String ToString()
            {
                return $"{checkType.ToString()} at 0x{(address.ToString("X4"))} {((breakHit) ? "Was Hit" : "Was Not Hit")}";
            }
        }

        public DebugCheck debugStopRequested = DebugCheck.None;
        public List<AddressSpaceBreakpoint> breakpointList = new List<AddressSpaceBreakpoint>();

        public AddressSpace(ROM rom)
        {
            Array.Clear(bytes, 0, bytes.Length);
            this.rom = rom;

            // TODO: Copy Internal GB Boot Rom to 0x0000 - 0x1000

        }
        
        private void updateDebugRequests(ushort address, DebugCheck type)
        {
            breakpointList.ForEach(bp => bp.update(address, type));
        }

        public bool checkDebugRequests()
        {
            return (breakpointList.Count(bp => bp.wasHit()) > 0);
        }

        public void resetAllBreakpoints()
        {
            breakpointList.ForEach(bp => bp.reset());
        }

        public void setBreakPoint(ushort address, CPUDebugger.DebugType type)
        {
            DebugCheck realType = DebugCheck.None;

            switch (type)
            {
                case CPUDebugger.DebugType.MemoryAccess:
                    realType = DebugCheck.AccessOcurred;
                    break;
                case CPUDebugger.DebugType.MemoryRead:
                    realType = DebugCheck.ReadOccurred;
                    break;
                case CPUDebugger.DebugType.MemoryWrite:
                    realType = DebugCheck.WriteOccurred;
                    break;
                default:
                    throw new Exception($"Attempted to create a Memory Breakpoint with the Wrong Type: {type.ToString()}");
            }

            setBreakPoint(address, realType);
        }

        public void setBreakPoint(ushort address, DebugCheck type)
        {
            breakpointList.Clear(); // only one for now
            breakpointList.Add(new AddressSpaceBreakpoint(address, type));
        }

        public byte[] getBytes()
        {
            return bytes;
        }
        public byte getByte(ushort address)
        {
            Byte rv = 0;
            updateDebugRequests(address, DebugCheck.ReadOccurred);
            // ROM Space
            if (address >= 0x0000 && address <= 0x7FFF)
            {
                // get Byte From ROM
                rv = rom.getByte(address);
            }
            else
            {
                rv = bytes[address];
            }


            return rv;
        }

        public byte peekByte(ushort address)
        {
            // Used for Cycle Inaccurate Memory Access for Debugging
            return bytes[address];
        }

        //public byte[] getBytes(ushort address, ushort count)
        //{
        //    byte[] rv = new Byte[count];
        //    if (((UInt32)address + (UInt32)count) <= 0xFFFF)
        //    {
        //        Array.ConstrainedCopy(bytes, address,  rv, 0, (int)count);
        //    }
        //    else
        //    {
        //        rv = null;
        //    }
            
        //    return rv;
        //}

        public void setByte(ushort address, Byte value)
        {
            updateDebugRequests(address, DebugCheck.WriteOccurred);
            bytes[address] = value;

            if (address > 0x0000 && address <= 0x8000)
            {
                rom.setByte(address, value);
            }

            // No Graphics test Mode for Blargg Test Roms
            if (address == 0xFF01)
            {
                testChar = (char)(value);
            }
            else if (address == 0xFF02)
            {

            }

            if (address == 0xFF02 && value == 0x81)
            {
                Debug.Write(testChar);
            }


        }

        public void requestInterrupt(CPU.InterruptType type)
        {
            setByte(0xFF0F, (Byte)((getByte(0xFF0F) | (int)type)));
        }

        //public void setBytes(ushort address, Byte[] values)
        //{

        //    if (((UInt32)address + values.Count()) <= 0xFFFF)
        //    {
        //        Array.ConstrainedCopy(values, 0, bytes, address, values.Count());

        //        if (address >= 0xC000 && address <= 0xDE00)
        //        {
        //            Array.ConstrainedCopy(values, 0, bytes, (address + echoOffset), values.Count());
        //        }
        //        else if (address >= 0xE000 && address <= 0xFE00)
        //        {
        //            Array.ConstrainedCopy(values, 0, bytes, (address - echoOffset), values.Count());
        //        }
        //    }
        //    else
        //    {
        //        throw new Exception($"GetBytes: Writing a Value out of Range of Address Space");
        //    }
        //}

        //public void loadRom(ROM rom)
        //{
        //    int end1 = (rom.size > 0x4000) ? 0x4000 : rom.size;
        //    int end2 = (rom.size > 0x8000) ? 0x8000 : rom.size;

        //    for (ushort i = 0; i < end1; i++)
        //    {
        //        setByte(i, rom.getByte(i)); // Copy 1st Rom bank to RAM. This is alwasy ROM Bank 0 for evry cart type
        //    }

        //    for (ushort i = 0x4000; i < end2; i++)
        //    {
        //        setByte(i, rom.getByte(i)); // Copy 2nd Rom bank to RAM. This is only for ROM_ONLY ROMs. This is the swappable bank for other 
        //    }
        //}

        public Dictionary<String, String> getState()
        {
            Dictionary<string, String> rv = new Dictionary<string, string>();

            for (int addr = 0; addr <= 0xFFFF; addr++)
            {
                rv.Add(addr.ToString("X4"), bytes[addr].ToString("X2"));
            }

            return rv;
        }
    }
}
