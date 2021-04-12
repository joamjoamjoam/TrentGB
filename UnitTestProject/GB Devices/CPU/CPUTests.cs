using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using trentGB;
using System.Collections.Generic;
using System;

namespace trentGB.Tests
{
    [TestClass]
    public class CPUTests
    {
        public Stopwatch clock = new Stopwatch();

        public CPU setupOpCode(byte opCode, byte param1 = 0x00, byte param2 = 0x00)
        {
            ROM blankRom = new ROM(opCode, param1, param2);
            AddressSpace addrSpace = new AddressSpace(blankRom);
            CPU cpu = new CPU(addrSpace, blankRom, clock);
            cpu.disableDebugger();
            cpu.setNextBreak(0x0000);
            cpu.reset();
            Assert.AreEqual(0x100, cpu.getPC());
            Assert.AreEqual(opCode, cpu.mem.getByte(0x100));
            Assert.AreEqual(param1, cpu.mem.getByte(0x101));
            Assert.AreEqual(param2, cpu.mem.getByte(0x102));

            return cpu;
        }

        #region Helper Functions
        [DataRow((ushort)1u, (ushort)0xFFFFu)]
        [DataRow((ushort)1u, (ushort)100u)]
        [DataTestMethod]
        [TestCategory("Misc Functions")]
        public void add16_UShort(ushort op1, ushort op2)
        {
            byte opCode = 0x00;
            CPU cpu = setupOpCode(opCode);
            Assert.AreEqual((op1 + op2) & 0xFFFF, cpu.add16(op1, op2, CPU.Add16Type.Normal));

        }

        [DataRow((byte)1u, (ushort)0xFFFFu)]
        [DataRow((byte)1u, (ushort)0x00FFu)]
        [DataTestMethod]
        [TestCategory("Misc Functions")]
        public void add16_ByteParams(byte op1, ushort op2)
        {
            byte opCode = 0x00;
            CPU cpu = setupOpCode(opCode);
            ushort shortOp = (ushort)(op2 & 0x00FF);


            Assert.AreEqual((op1 + op2) & 0xFFFF, cpu.add16(op1, op2, CPU.Add16Type.Normal));
            Assert.AreEqual((op1 + op2) & 0xFFFF, cpu.add16(op2, op1, CPU.Add16Type.Normal));
            Assert.AreEqual((op1 + shortOp) & 0xFFFF, cpu.add16(shortOp, op1, CPU.Add16Type.Normal));

        }

        #endregion


        // Test Cycle Counts in Intruction Dictionary matches lengths used


        #region Op Code Tests

        [TestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x00 - NOP")]
        public void decodeAndExecute_OPCode_0x00_NOP()
        {
            byte opCode = 0x00;
            CPU cpu = setupOpCode(opCode);

            cpu.decodeAndExecute(cpu.fetch());

            Assert.AreEqual(0x100 + cpu.getInstructionForOpCode(opCode).length, cpu.getPC());
        }

        [DataRow((byte)0xFFu, (byte)0xFF)]
        [DataRow((byte)0x01, (byte)0xFFu)]
        [DataRow((byte)0x00, (byte)0x00u)]
        [DataRow((byte)0xFF, (byte)0x00u)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x01 - Load BC")]
        public void decodeAndExecute_ldBC16(byte op1, byte op2)
        {
            byte opCode = 0x01;

            CPU cpu = setupOpCode(opCode, op1, op2);

            // First arg Byte at 0x0101
            cpu.decodeAndExecute(cpu.fetch());

            Assert.AreEqual(0x100 + cpu.getInstructionForOpCode(opCode).length, cpu.getPC());
            Assert.AreEqual((((op2 << 8) + op1) & 0xFFFF), cpu.getBC());

        }

        #endregion

        [DataTestMethod]
        [DataRow((ushort)0xFFFFu)]
        [DataRow((ushort)0u)]
        [DataRow((ushort)0xFFu)]
        public void increment16_Test(ushort op1)
        {
            byte opCode = 0x01;

            CPU cpu = setupOpCode(opCode);

            // First arg Byte at 0x0101
            cpu.decodeAndExecute(cpu.fetch());

            Assert.AreEqual(((op1 + 1) & 0xFFFF), cpu.increment16(op1));
        }

        [DataTestMethod]
        [DataRow((ushort)0xFFFFu)]
        [DataRow((ushort)0u)]
        [DataRow((ushort)0xFFu)]
        public void decrement16_Test(ushort op1)
        {
            byte opCode = 0x00;

            CPU cpu = setupOpCode(opCode);

            // First arg Byte at 0x0101
            cpu.decodeAndExecute(cpu.fetch());
            //Console.WriteLine("test Output");
            Assert.AreEqual(((op1 - 1) & 0xFFFF), cpu.decrement16(op1));
            
        }
    }


}
