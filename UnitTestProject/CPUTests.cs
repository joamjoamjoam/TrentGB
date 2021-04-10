using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using trentGB;
using System.Collections.Generic;

namespace TrentGBTestProject
{
    [TestClass]
    public class CPUTests
    {
        public Stopwatch clock = new Stopwatch();

        public CPU setupOpCode(byte opCode)
        {
            ROM blankRom = new ROM();
            AddressSpace addrSpace = new AddressSpace(blankRom);
            CPU cpu = new CPU(addrSpace, blankRom, clock);
            cpu.disableDebugger();
            cpu.setNextBreak(0x0000);
            cpu.reset();
            Assert.AreEqual(0x100, cpu.getPC());
            cpu.mem.setByte(0x100, 0x00); // Load Opcode

            return cpu;
        }
        [TestMethod]
        public void decodeAndExecute_OPCode_0x00_NOP()
        {
            CPU cpu = setupOpCode(0x00);
            

            // First arg Byte at 0x0101
            cpu.decodeAndExecute(cpu.fetch());

            Assert.AreEqual(0x101, cpu.getPC());
        }
    }
}
