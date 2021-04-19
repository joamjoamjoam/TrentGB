using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using trentGB;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace trentGB.Tests
{
    [TestClass]
    public class CPUTests
    {
        public Stopwatch clock = new Stopwatch();

        // Assert override

        #region Shared Functions
        public CPU setupOpCode(byte opCode, String testName, byte param1 = 0x00, byte param2 = 0x00, byte[] extraParams = null)
        {
            int count = 3;
            if (extraParams != null)
            {
                count += extraParams.Length;
            }

            byte[] arr = new byte[count];
            arr[0] = opCode;
            arr[1] = param1;
            arr[2] = param2;
            if (extraParams != null)
            {
                Array.Copy(extraParams, 0, arr, 3, extraParams.Length);
            }

            ROM blankRom = new ROM(arr);
            AddressSpace addrSpace = new AddressSpace(blankRom);
            CPU cpu = new CPU(addrSpace, blankRom, clock);
            cpu.disableDebugger();
            cpu.setDebugParams(CPUDebugger.DebugType.None, 0x0000);
            cpu.reset();
            logMessage("Checking PC = 0x100");
            Assert.That.AreEqual(0x100, cpu.getPC(), "X4");
            logMessage("Checking Loaded Op Code");
            Assert.That.AreEqual(opCode, cpu.mem.getByte(0x100), "X2");
            logMessage("Checking Loaded Param 1");
            Assert.That.AreEqual(param1, cpu.mem.getByte(0x101), "X2");
            logMessage("Checking Loaded Param 2");
            Assert.That.AreEqual(param2, cpu.mem.getByte(0x102), "X2");

            logMessage($"\nStarting Test: {testName}");

            return cpu;
        }

        public void executeCurrentInstruction(CPU cpu)
        {
            cpu.decodeAndExecute();
            while (cpu.getCurrentInstruction() != null)
            {
                cpu.decodeAndExecute();
            }
            Assert.IsNull(cpu.getCurrentInstruction(), "Instruction did not Finish Executing");
            Assert.IsNotNull(cpu.getLastExecutedInstruction(), "Execute Instruction did not get loaded into Command History");
            Assert.IsTrue(cpu.getLastExecutedInstruction().isCompleted(), "Instruction did not Report Completion");
        }

        public void assertInstructionFinished(CPU cpu, byte opCode)
        {
            Assert.IsTrue(cpu.getLastExecutedInstruction().isCompleted());
            Assert.IsNotNull(cpu.getLastExecutedInstruction());
            logMessage("Checking that Scheduled Opcode is same as Executed OpCode");
            Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().opCode);
        }

        public void fetchAndLoadInstruction(CPU cpu, byte opCode)
        {
            Assert.IsNull(cpu.getCurrentInstruction());
            logMessage($"Starting Execution of Instruction {cpu.getInstructionForOpCode(opCode).ToString()}");
            tick(cpu);
            if (cpu.getInstructionForOpCode(opCode).cycles == 4)
            {
                Assert.IsNull(cpu.getCurrentInstruction());
                logMessage("Check fetched Instruction is correct.");
                Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().opCode);
            }
            else
            {
                Assert.IsNotNull(cpu.getCurrentInstruction());
                logMessage("Check fetched Instruction is correct.");
                Assert.That.AreEqual(opCode, cpu.getCurrentInstruction().opCode);
            }

        }

        public void tick(CPU cpu)
        {
            int cycle = 0;
            int endCycle = 0;
            if (cpu.getCurrentInstruction() != null)
            {
                cycle = cpu.getCurrentInstruction().getCycleCount();
            }
            logMessage($"CPU State: Cycle {cycle}\n{cpu.ToString()}");
            logMessage($"Executing Cycle: {cycle + 4}");
            cpu.decodeAndExecute();

            if (cpu.getCurrentInstruction() != null)
            {
                cycle = cpu.getCurrentInstruction().getCycleCount();
            }
            logMessage($"CPU State After Tick\n{cpu.ToString()}");

        }

        private void logMessage(String message)
        {
            Console.WriteLine(message);
        }
        #endregion

        #region Helper Functions

        [DataRow((ushort)1u, (ushort)0xFFFFu)]
        [DataRow((ushort)1u, (ushort)100u)]
        [DataTestMethod]
        [TestCategory("Misc Functions")]
        [TestCategory("16 Bit Math Functions")]
        public void add16_UShort(ushort op1, ushort op2)
        {
            byte opCode = 0x00;

            CPU cpu = setupOpCode(opCode, $"{MethodBase.GetCurrentMethod().Name}({op1.ToString("X4")}, {op2.ToString("X4")})");
            Byte flagsByte = cpu.getF();

            Assert.That.AreEqual((op1 + op2) & 0xFFFF, cpu.add16(op1, op2, CPU.Add16Type.Normal));
            Assert.That.FlagsEqual(cpu, flagsByte);

        }

        [DataRow((byte)1u, (ushort)0xFFFFu)]
        [DataRow((byte)1u, (ushort)0x00FFu)]
        [DataTestMethod]
        [TestCategory("Misc Functions")]
        [TestCategory("16 Bit Math Functions")]
        public void add16_ByteParams(byte op1, ushort op2)
        {
            byte opCode = 0x00;
            CPU cpu = setupOpCode(opCode, $"{MethodBase.GetCurrentMethod().Name}({op1.ToString("X2")}, {op2.ToString("X4")})");
            ushort shortOp = (ushort)(op2 & 0x00FF);

            Byte flagsByte = cpu.getF();
            Assert.That.AreEqual((op1 + op2) & 0xFFFF, cpu.add16(op1, op2, CPU.Add16Type.Normal));
            Assert.That.FlagsEqual(cpu, flagsByte);
            Assert.That.AreEqual((op1 + op2) & 0xFFFF, cpu.add16(op2, op1, CPU.Add16Type.Normal));
            Assert.That.FlagsEqual(cpu, flagsByte);
            Assert.That.AreEqual((op1 + shortOp) & 0xFFFF, cpu.add16(shortOp, op1, CPU.Add16Type.Normal));
            Assert.That.FlagsEqual(cpu, flagsByte);
        }

        [DataTestMethod]
        [DataRow((ushort)0xFFFFu)]
        [DataRow((ushort)0u)]
        [DataRow((ushort)0xFFu)]
        [TestCategory("Misc Functions")]
        [TestCategory("16 Bit Math Functions")]
        public void increment16_Test(ushort op1)
        {
            byte opCode = 0x00;

            CPU cpu = setupOpCode(opCode, $"{MethodBase.GetCurrentMethod().Name}({op1.ToString("X2")})");
            Byte flagsByte = cpu.getF();

            Assert.That.AreEqual(((op1 + 1) & 0xFFFF), cpu.increment16(op1));
            Assert.That.FlagsEqual(cpu, flagsByte);
        }

        [DataTestMethod]
        [DataRow((ushort)0xFFFFu)]
        [DataRow((ushort)0u)]
        [DataRow((ushort)0xFFu)]
        [TestCategory("Misc Functions")]
        [TestCategory("16 Bit Math Functions")]
        public void decrement16_Test(ushort op1)
        {
            byte opCode = 0x00;

            CPU cpu = setupOpCode(opCode, $"{MethodBase.GetCurrentMethod().Name}({op1.ToString("X2")})");
            Byte flagsByte = cpu.getF();

            Assert.That.AreEqual(((op1 - 1) & 0xFFFF), cpu.decrement16(op1));
            Assert.That.FlagsEqual(cpu, flagsByte);

        }

        [TestMethod]
        [TestCategory("Misc Functions")]
        [TestCategory("16 Bit Math Functions")]
        [TestCategory("addSP")]
        public void addSP_UnsignedByte_NoCarry_Test()
        {
            byte opCode = 0x31;
            byte lsb = 0x00;
            byte msb = 0x00;
            SByte addNum = 1;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, lsb, msb);
            executeCurrentInstruction(cpu);
            cpu.setF(0);
            logMessage("Check Stack Pointer and Flags");
            Assert.That.AreEqual(0, cpu.getF(), "X4");
            Assert.That.AreEqual(0x0000, cpu.getSP(), "X4");

            logMessage($"Check Result of {cpu.getSP()} + {addNum}");
            Assert.That.AreEqual(1, cpu.addSP(cpu.getSP(), addNum));

            Assert.That.FlagsEqual(cpu, halfCarrySet: false, carrySet: false, zeroSet: false, subSet: false);

        }

        #endregion

        #region CPU Data Tests
        // Test Cycle Counts in Intruction Dictionary matches lengths used
        [DataTestMethod]
        [Timeout(10000)]
        #region Test Data Rows
        [DataRow((byte)0x00u)]
        [DataRow((byte)0x01u)]
        [DataRow((byte)0x02u)]
        [DataRow((byte)0x03u)]
        [DataRow((byte)0x04u)]
        [DataRow((byte)0x05u)]
        [DataRow((byte)0x06u)]
        [DataRow((byte)0x07u)]
        [DataRow((byte)0x08u)]
        [DataRow((byte)0x09u)]
        [DataRow((byte)0x0Au)]
        [DataRow((byte)0x0Bu)]
        [DataRow((byte)0x0Cu)]
        [DataRow((byte)0x0Du)]
        [DataRow((byte)0x0Eu)]
        [DataRow((byte)0x0Fu)]
        [DataRow((byte)0x10u)]
        [DataRow((byte)0x11u)]
        [DataRow((byte)0x12u)]
        [DataRow((byte)0x13u)]
        [DataRow((byte)0x14u)]
        [DataRow((byte)0x15u)]
        [DataRow((byte)0x16u)]
        [DataRow((byte)0x17u)]
        [DataRow((byte)0x18u)]
        [DataRow((byte)0x19u)]
        [DataRow((byte)0x1Au)]
        [DataRow((byte)0x1Bu)]
        [DataRow((byte)0x1Cu)]
        [DataRow((byte)0x1Du)]
        [DataRow((byte)0x1Eu)]
        [DataRow((byte)0x1Fu)]
        [DataRow((byte)0x20u)]
        [DataRow((byte)0x21u)]
        [DataRow((byte)0x22u)]
        [DataRow((byte)0x23u)]
        [DataRow((byte)0x24u)]
        [DataRow((byte)0x25u)]
        [DataRow((byte)0x26u)]
        [DataRow((byte)0x27u)]
        [DataRow((byte)0x28u)]
        [DataRow((byte)0x29u)]
        [DataRow((byte)0x2Au)]
        [DataRow((byte)0x2Bu)]
        [DataRow((byte)0x2Cu)]
        [DataRow((byte)0x2Du)]
        [DataRow((byte)0x2Eu)]
        [DataRow((byte)0x2Fu)]
        [DataRow((byte)0x30u)]
        [DataRow((byte)0x31u)]
        [DataRow((byte)0x32u)]
        [DataRow((byte)0x33u)]
        [DataRow((byte)0x34u)]
        [DataRow((byte)0x35u)]
        [DataRow((byte)0x36u)]
        [DataRow((byte)0x37u)]
        [DataRow((byte)0x38u)]
        [DataRow((byte)0x39u)]
        [DataRow((byte)0x3Au)]
        [DataRow((byte)0x3Bu)]
        [DataRow((byte)0x3Cu)]
        [DataRow((byte)0x3Du)]
        [DataRow((byte)0x3Eu)]
        [DataRow((byte)0x3Fu)]
        [DataRow((byte)0x40u)]
        [DataRow((byte)0x41u)]
        [DataRow((byte)0x42u)]
        [DataRow((byte)0x43u)]
        [DataRow((byte)0x44u)]
        [DataRow((byte)0x45u)]
        [DataRow((byte)0x46u)]
        [DataRow((byte)0x47u)]
        [DataRow((byte)0x48u)]
        [DataRow((byte)0x49u)]
        [DataRow((byte)0x4Au)]
        [DataRow((byte)0x4Bu)]
        [DataRow((byte)0x4Cu)]
        [DataRow((byte)0x4Du)]
        [DataRow((byte)0x4Eu)]
        [DataRow((byte)0x4Fu)]
        [DataRow((byte)0x50u)]
        [DataRow((byte)0x51u)]
        [DataRow((byte)0x52u)]
        [DataRow((byte)0x53u)]
        [DataRow((byte)0x54u)]
        [DataRow((byte)0x55u)]
        [DataRow((byte)0x56u)]
        [DataRow((byte)0x57u)]
        [DataRow((byte)0x58u)]
        [DataRow((byte)0x59u)]
        [DataRow((byte)0x5Au)]
        [DataRow((byte)0x5Bu)]
        [DataRow((byte)0x5Cu)]
        [DataRow((byte)0x5Du)]
        [DataRow((byte)0x5Eu)]
        [DataRow((byte)0x5Fu)]
        [DataRow((byte)0x60u)]
        [DataRow((byte)0x61u)]
        [DataRow((byte)0x62u)]
        [DataRow((byte)0x63u)]
        [DataRow((byte)0x64u)]
        [DataRow((byte)0x65u)]
        [DataRow((byte)0x66u)]
        [DataRow((byte)0x67u)]
        [DataRow((byte)0x68u)]
        [DataRow((byte)0x69u)]
        [DataRow((byte)0x6Au)]
        [DataRow((byte)0x6Bu)]
        [DataRow((byte)0x6Cu)]
        [DataRow((byte)0x6Du)]
        [DataRow((byte)0x6Eu)]
        [DataRow((byte)0x6Fu)]
        [DataRow((byte)0x70u)]
        [DataRow((byte)0x71u)]
        [DataRow((byte)0x72u)]
        [DataRow((byte)0x73u)]
        [DataRow((byte)0x74u)]
        [DataRow((byte)0x75u)]
        [DataRow((byte)0x76u)]
        [DataRow((byte)0x77u)]
        [DataRow((byte)0x78u)]
        [DataRow((byte)0x79u)]
        [DataRow((byte)0x7Au)]
        [DataRow((byte)0x7Bu)]
        [DataRow((byte)0x7Cu)]
        [DataRow((byte)0x7Du)]
        [DataRow((byte)0x7Eu)]
        [DataRow((byte)0x7Fu)]
        [DataRow((byte)0x80u)]
        [DataRow((byte)0x81u)]
        [DataRow((byte)0x82u)]
        [DataRow((byte)0x83u)]
        [DataRow((byte)0x84u)]
        [DataRow((byte)0x85u)]
        [DataRow((byte)0x86u)]
        [DataRow((byte)0x87u)]
        [DataRow((byte)0x88u)]
        [DataRow((byte)0x89u)]
        [DataRow((byte)0x8Au)]
        [DataRow((byte)0x8Bu)]
        [DataRow((byte)0x8Cu)]
        [DataRow((byte)0x8Du)]
        [DataRow((byte)0x8Eu)]
        [DataRow((byte)0x8Fu)]
        [DataRow((byte)0x90u)]
        [DataRow((byte)0x91u)]
        [DataRow((byte)0x92u)]
        [DataRow((byte)0x93u)]
        [DataRow((byte)0x94u)]
        [DataRow((byte)0x95u)]
        [DataRow((byte)0x96u)]
        [DataRow((byte)0x97u)]
        [DataRow((byte)0x98u)]
        [DataRow((byte)0x99u)]
        [DataRow((byte)0x9Au)]
        [DataRow((byte)0x9Bu)]
        [DataRow((byte)0x9Cu)]
        [DataRow((byte)0x9Du)]
        [DataRow((byte)0x9Eu)]
        [DataRow((byte)0x9Fu)]
        [DataRow((byte)0xA0u)]
        [DataRow((byte)0xA1u)]
        [DataRow((byte)0xA2u)]
        [DataRow((byte)0xA3u)]
        [DataRow((byte)0xA4u)]
        [DataRow((byte)0xA5u)]
        [DataRow((byte)0xA6u)]
        [DataRow((byte)0xA7u)]
        [DataRow((byte)0xA8u)]
        [DataRow((byte)0xA9u)]
        [DataRow((byte)0xAAu)]
        [DataRow((byte)0xABu)]
        [DataRow((byte)0xACu)]
        [DataRow((byte)0xADu)]
        [DataRow((byte)0xAEu)]
        [DataRow((byte)0xAFu)]
        [DataRow((byte)0xB0u)]
        [DataRow((byte)0xB1u)]
        [DataRow((byte)0xB2u)]
        [DataRow((byte)0xB3u)]
        [DataRow((byte)0xB4u)]
        [DataRow((byte)0xB5u)]
        [DataRow((byte)0xB6u)]
        [DataRow((byte)0xB7u)]
        [DataRow((byte)0xB8u)]
        [DataRow((byte)0xB9u)]
        [DataRow((byte)0xBAu)]
        [DataRow((byte)0xBBu)]
        [DataRow((byte)0xBCu)]
        [DataRow((byte)0xBDu)]
        [DataRow((byte)0xBEu)]
        [DataRow((byte)0xBFu)]
        [DataRow((byte)0xC0u)]
        [DataRow((byte)0xC1u)]
        [DataRow((byte)0xC2u)]
        [DataRow((byte)0xC3u)]
        [DataRow((byte)0xC4u)]
        [DataRow((byte)0xC5u)]
        [DataRow((byte)0xC6u)]
        [DataRow((byte)0xC7u)]
        [DataRow((byte)0xC8u)]
        [DataRow((byte)0xC9u)]
        [DataRow((byte)0xCAu)]
        [DataRow((byte)0xCBu)]
        [DataRow((byte)0xCCu)]
        [DataRow((byte)0xCDu)]
        [DataRow((byte)0xCEu)]
        [DataRow((byte)0xCFu)]
        [DataRow((byte)0xD0u)]
        [DataRow((byte)0xD1u)]
        [DataRow((byte)0xD2u)]
        [DataRow((byte)0xD3u)]
        [DataRow((byte)0xD4u)]
        [DataRow((byte)0xD5u)]
        [DataRow((byte)0xD6u)]
        [DataRow((byte)0xD7u)]
        [DataRow((byte)0xD8u)]
        [DataRow((byte)0xD9u)]
        [DataRow((byte)0xDAu)]
        [DataRow((byte)0xDBu)]
        [DataRow((byte)0xDCu)]
        [DataRow((byte)0xDDu)]
        [DataRow((byte)0xDEu)]
        [DataRow((byte)0xDFu)]
        [DataRow((byte)0xE0u)]
        [DataRow((byte)0xE1u)]
        [DataRow((byte)0xE2u)]
        [DataRow((byte)0xE3u)]
        [DataRow((byte)0xE4u)]
        [DataRow((byte)0xE5u)]
        [DataRow((byte)0xE6u)]
        [DataRow((byte)0xE7u)]
        [DataRow((byte)0xE8u)]
        [DataRow((byte)0xE9u)]
        [DataRow((byte)0xEAu)]
        [DataRow((byte)0xEBu)]
        [DataRow((byte)0xECu)]
        [DataRow((byte)0xEDu)]
        [DataRow((byte)0xEEu)]
        [DataRow((byte)0xEFu)]
        [DataRow((byte)0xF0u)]
        [DataRow((byte)0xF1u)]
        [DataRow((byte)0xF2u)]
        [DataRow((byte)0xF3u)]
        [DataRow((byte)0xF4u)]
        [DataRow((byte)0xF5u)]
        [DataRow((byte)0xF6u)]
        [DataRow((byte)0xF7u)]
        [DataRow((byte)0xF8u)]
        [DataRow((byte)0xF9u)]
        [DataRow((byte)0xFAu)]
        [DataRow((byte)0xFBu)]
        [DataRow((byte)0xFCu)]
        [DataRow((byte)0xFDu)]
        [DataRow((byte)0xFEu)]
        [DataRow((byte)0xFFu)]
        #endregion
        [TestCategory("CPU Data Tests")]
        public void decodeAndExecute_Length_Test(Byte opCode)
        {

            List<Byte> notImplementedOpCodes = new List<byte>() { 0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD };
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            logMessage($"Testing OpCode 0x{opCode.ToString("X2")} - {cpu.getInstructionForOpCode(opCode).desc}");
            if (notImplementedOpCodes.Contains(opCode))
            {
                try
                {
                    executeCurrentInstruction(cpu);
                    Exception e = new Exception();
                    logMessage($"Expected Exception Not Fired ({typeof(NotImplementedException).ToString()}, None)");
                    Assert.IsInstanceOfType(e, typeof(NotImplementedException));
                }
                catch (AssertFailedException ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logMessage($"Excpetion Recieved ({typeof(NotImplementedException).ToString()}, {ex.GetType().ToString()})");
                    Assert.IsInstanceOfType(ex, typeof(NotImplementedException));
                }
            }
            else
            {
                if (opCode != 0xCB)
                {
                    executeCurrentInstruction(cpu);
                    logMessage("Executed Length:");
                    Assert.That.AreEqual(cpu.getLastExecutedInstruction().length, cpu.getLastExecutedInstruction().getFetchCount());
                }
                else
                {
                    Assert.Inconclusive("CB Prefixed Opcodes are handled by their own tests.");
                }

            }
        }

        [DataTestMethod]
        [Timeout(10000)]
        #region Test Data Rows
        [DataRow((byte)0x00u)]
        [DataRow((byte)0x01u)]
        [DataRow((byte)0x02u)]
        [DataRow((byte)0x03u)]
        [DataRow((byte)0x04u)]
        [DataRow((byte)0x05u)]
        [DataRow((byte)0x06u)]
        [DataRow((byte)0x07u)]
        [DataRow((byte)0x08u)]
        [DataRow((byte)0x09u)]
        [DataRow((byte)0x0Au)]
        [DataRow((byte)0x0Bu)]
        [DataRow((byte)0x0Cu)]
        [DataRow((byte)0x0Du)]
        [DataRow((byte)0x0Eu)]
        [DataRow((byte)0x0Fu)]
        [DataRow((byte)0x10u)]
        [DataRow((byte)0x11u)]
        [DataRow((byte)0x12u)]
        [DataRow((byte)0x13u)]
        [DataRow((byte)0x14u)]
        [DataRow((byte)0x15u)]
        [DataRow((byte)0x16u)]
        [DataRow((byte)0x17u)]
        [DataRow((byte)0x18u)]
        [DataRow((byte)0x19u)]
        [DataRow((byte)0x1Au)]
        [DataRow((byte)0x1Bu)]
        [DataRow((byte)0x1Cu)]
        [DataRow((byte)0x1Du)]
        [DataRow((byte)0x1Eu)]
        [DataRow((byte)0x1Fu)]
        [DataRow((byte)0x20u)]
        [DataRow((byte)0x21u)]
        [DataRow((byte)0x22u)]
        [DataRow((byte)0x23u)]
        [DataRow((byte)0x24u)]
        [DataRow((byte)0x25u)]
        [DataRow((byte)0x26u)]
        [DataRow((byte)0x27u)]
        [DataRow((byte)0x28u)]
        [DataRow((byte)0x29u)]
        [DataRow((byte)0x2Au)]
        [DataRow((byte)0x2Bu)]
        [DataRow((byte)0x2Cu)]
        [DataRow((byte)0x2Du)]
        [DataRow((byte)0x2Eu)]
        [DataRow((byte)0x2Fu)]
        [DataRow((byte)0x30u)]
        [DataRow((byte)0x31u)]
        [DataRow((byte)0x32u)]
        [DataRow((byte)0x33u)]
        [DataRow((byte)0x34u)]
        [DataRow((byte)0x35u)]
        [DataRow((byte)0x36u)]
        [DataRow((byte)0x37u)]
        [DataRow((byte)0x38u)]
        [DataRow((byte)0x39u)]
        [DataRow((byte)0x3Au)]
        [DataRow((byte)0x3Bu)]
        [DataRow((byte)0x3Cu)]
        [DataRow((byte)0x3Du)]
        [DataRow((byte)0x3Eu)]
        [DataRow((byte)0x3Fu)]
        [DataRow((byte)0x40u)]
        [DataRow((byte)0x41u)]
        [DataRow((byte)0x42u)]
        [DataRow((byte)0x43u)]
        [DataRow((byte)0x44u)]
        [DataRow((byte)0x45u)]
        [DataRow((byte)0x46u)]
        [DataRow((byte)0x47u)]
        [DataRow((byte)0x48u)]
        [DataRow((byte)0x49u)]
        [DataRow((byte)0x4Au)]
        [DataRow((byte)0x4Bu)]
        [DataRow((byte)0x4Cu)]
        [DataRow((byte)0x4Du)]
        [DataRow((byte)0x4Eu)]
        [DataRow((byte)0x4Fu)]
        [DataRow((byte)0x50u)]
        [DataRow((byte)0x51u)]
        [DataRow((byte)0x52u)]
        [DataRow((byte)0x53u)]
        [DataRow((byte)0x54u)]
        [DataRow((byte)0x55u)]
        [DataRow((byte)0x56u)]
        [DataRow((byte)0x57u)]
        [DataRow((byte)0x58u)]
        [DataRow((byte)0x59u)]
        [DataRow((byte)0x5Au)]
        [DataRow((byte)0x5Bu)]
        [DataRow((byte)0x5Cu)]
        [DataRow((byte)0x5Du)]
        [DataRow((byte)0x5Eu)]
        [DataRow((byte)0x5Fu)]
        [DataRow((byte)0x60u)]
        [DataRow((byte)0x61u)]
        [DataRow((byte)0x62u)]
        [DataRow((byte)0x63u)]
        [DataRow((byte)0x64u)]
        [DataRow((byte)0x65u)]
        [DataRow((byte)0x66u)]
        [DataRow((byte)0x67u)]
        [DataRow((byte)0x68u)]
        [DataRow((byte)0x69u)]
        [DataRow((byte)0x6Au)]
        [DataRow((byte)0x6Bu)]
        [DataRow((byte)0x6Cu)]
        [DataRow((byte)0x6Du)]
        [DataRow((byte)0x6Eu)]
        [DataRow((byte)0x6Fu)]
        [DataRow((byte)0x70u)]
        [DataRow((byte)0x71u)]
        [DataRow((byte)0x72u)]
        [DataRow((byte)0x73u)]
        [DataRow((byte)0x74u)]
        [DataRow((byte)0x75u)]
        [DataRow((byte)0x76u)]
        [DataRow((byte)0x77u)]
        [DataRow((byte)0x78u)]
        [DataRow((byte)0x79u)]
        [DataRow((byte)0x7Au)]
        [DataRow((byte)0x7Bu)]
        [DataRow((byte)0x7Cu)]
        [DataRow((byte)0x7Du)]
        [DataRow((byte)0x7Eu)]
        [DataRow((byte)0x7Fu)]
        [DataRow((byte)0x80u)]
        [DataRow((byte)0x81u)]
        [DataRow((byte)0x82u)]
        [DataRow((byte)0x83u)]
        [DataRow((byte)0x84u)]
        [DataRow((byte)0x85u)]
        [DataRow((byte)0x86u)]
        [DataRow((byte)0x87u)]
        [DataRow((byte)0x88u)]
        [DataRow((byte)0x89u)]
        [DataRow((byte)0x8Au)]
        [DataRow((byte)0x8Bu)]
        [DataRow((byte)0x8Cu)]
        [DataRow((byte)0x8Du)]
        [DataRow((byte)0x8Eu)]
        [DataRow((byte)0x8Fu)]
        [DataRow((byte)0x90u)]
        [DataRow((byte)0x91u)]
        [DataRow((byte)0x92u)]
        [DataRow((byte)0x93u)]
        [DataRow((byte)0x94u)]
        [DataRow((byte)0x95u)]
        [DataRow((byte)0x96u)]
        [DataRow((byte)0x97u)]
        [DataRow((byte)0x98u)]
        [DataRow((byte)0x99u)]
        [DataRow((byte)0x9Au)]
        [DataRow((byte)0x9Bu)]
        [DataRow((byte)0x9Cu)]
        [DataRow((byte)0x9Du)]
        [DataRow((byte)0x9Eu)]
        [DataRow((byte)0x9Fu)]
        [DataRow((byte)0xA0u)]
        [DataRow((byte)0xA1u)]
        [DataRow((byte)0xA2u)]
        [DataRow((byte)0xA3u)]
        [DataRow((byte)0xA4u)]
        [DataRow((byte)0xA5u)]
        [DataRow((byte)0xA6u)]
        [DataRow((byte)0xA7u)]
        [DataRow((byte)0xA8u)]
        [DataRow((byte)0xA9u)]
        [DataRow((byte)0xAAu)]
        [DataRow((byte)0xABu)]
        [DataRow((byte)0xACu)]
        [DataRow((byte)0xADu)]
        [DataRow((byte)0xAEu)]
        [DataRow((byte)0xAFu)]
        [DataRow((byte)0xB0u)]
        [DataRow((byte)0xB1u)]
        [DataRow((byte)0xB2u)]
        [DataRow((byte)0xB3u)]
        [DataRow((byte)0xB4u)]
        [DataRow((byte)0xB5u)]
        [DataRow((byte)0xB6u)]
        [DataRow((byte)0xB7u)]
        [DataRow((byte)0xB8u)]
        [DataRow((byte)0xB9u)]
        [DataRow((byte)0xBAu)]
        [DataRow((byte)0xBBu)]
        [DataRow((byte)0xBCu)]
        [DataRow((byte)0xBDu)]
        [DataRow((byte)0xBEu)]
        [DataRow((byte)0xBFu)]
        [DataRow((byte)0xC0u)]
        [DataRow((byte)0xC1u)]
        [DataRow((byte)0xC2u)]
        [DataRow((byte)0xC3u)]
        [DataRow((byte)0xC4u)]
        [DataRow((byte)0xC5u)]
        [DataRow((byte)0xC6u)]
        [DataRow((byte)0xC7u)]
        [DataRow((byte)0xC8u)]
        [DataRow((byte)0xC9u)]
        [DataRow((byte)0xCAu)]
        [DataRow((byte)0xCBu)]
        [DataRow((byte)0xCCu)]
        [DataRow((byte)0xCDu)]
        [DataRow((byte)0xCEu)]
        [DataRow((byte)0xCFu)]
        [DataRow((byte)0xD0u)]
        [DataRow((byte)0xD1u)]
        [DataRow((byte)0xD2u)]
        [DataRow((byte)0xD3u)]
        [DataRow((byte)0xD4u)]
        [DataRow((byte)0xD5u)]
        [DataRow((byte)0xD6u)]
        [DataRow((byte)0xD7u)]
        [DataRow((byte)0xD8u)]
        [DataRow((byte)0xD9u)]
        [DataRow((byte)0xDAu)]
        [DataRow((byte)0xDBu)]
        [DataRow((byte)0xDCu)]
        [DataRow((byte)0xDDu)]
        [DataRow((byte)0xDEu)]
        [DataRow((byte)0xDFu)]
        [DataRow((byte)0xE0u)]
        [DataRow((byte)0xE1u)]
        [DataRow((byte)0xE2u)]
        [DataRow((byte)0xE3u)]
        [DataRow((byte)0xE4u)]
        [DataRow((byte)0xE5u)]
        [DataRow((byte)0xE6u)]
        [DataRow((byte)0xE7u)]
        [DataRow((byte)0xE8u)]
        [DataRow((byte)0xE9u)]
        [DataRow((byte)0xEAu)]
        [DataRow((byte)0xEBu)]
        [DataRow((byte)0xECu)]
        [DataRow((byte)0xEDu)]
        [DataRow((byte)0xEEu)]
        [DataRow((byte)0xEFu)]
        [DataRow((byte)0xF0u)]
        [DataRow((byte)0xF1u)]
        [DataRow((byte)0xF2u)]
        [DataRow((byte)0xF3u)]
        [DataRow((byte)0xF4u)]
        [DataRow((byte)0xF5u)]
        [DataRow((byte)0xF6u)]
        [DataRow((byte)0xF7u)]
        [DataRow((byte)0xF8u)]
        [DataRow((byte)0xF9u)]
        [DataRow((byte)0xFAu)]
        [DataRow((byte)0xFBu)]
        [DataRow((byte)0xFCu)]
        [DataRow((byte)0xFDu)]
        [DataRow((byte)0xFEu)]
        [DataRow((byte)0xFFu)]
        #endregion
        [TestCategory("CPU Data Tests")]
        public void decodeAndExecute_Length_CBCodes_Test(Byte opCode)
        {

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            logMessage($"Testing CB Prefixed OpCode 0x{opCode.ToString("X2")} - {cpu.getInstructionForOpCode(opCode).desc}");

            executeCurrentInstruction(cpu);
            logMessage("Executed Length:");
            Assert.That.AreEqual(0xCB, cpu.getLastExecutedInstruction().opCode);
            Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().parameters[0]);
            Assert.That.AreEqual(cpu.getLastExecutedInstruction().length, cpu.getLastExecutedInstruction().getFetchCount());
        }


        [DataTestMethod]
        [Timeout(10000)]
        #region Test Data Rows
        [DataRow((byte)0x00u)]
        [DataRow((byte)0x01u)]
        [DataRow((byte)0x02u)]
        [DataRow((byte)0x03u)]
        [DataRow((byte)0x04u)]
        [DataRow((byte)0x05u)]
        [DataRow((byte)0x06u)]
        [DataRow((byte)0x07u)]
        [DataRow((byte)0x08u)]
        [DataRow((byte)0x09u)]
        [DataRow((byte)0x0Au)]
        [DataRow((byte)0x0Bu)]
        [DataRow((byte)0x0Cu)]
        [DataRow((byte)0x0Du)]
        [DataRow((byte)0x0Eu)]
        [DataRow((byte)0x0Fu)]
        [DataRow((byte)0x10u)]
        [DataRow((byte)0x11u)]
        [DataRow((byte)0x12u)]
        [DataRow((byte)0x13u)]
        [DataRow((byte)0x14u)]
        [DataRow((byte)0x15u)]
        [DataRow((byte)0x16u)]
        [DataRow((byte)0x17u)]
        [DataRow((byte)0x18u)]
        [DataRow((byte)0x19u)]
        [DataRow((byte)0x1Au)]
        [DataRow((byte)0x1Bu)]
        [DataRow((byte)0x1Cu)]
        [DataRow((byte)0x1Du)]
        [DataRow((byte)0x1Eu)]
        [DataRow((byte)0x1Fu)]
        [DataRow((byte)0x20u)]
        [DataRow((byte)0x21u)]
        [DataRow((byte)0x22u)]
        [DataRow((byte)0x23u)]
        [DataRow((byte)0x24u)]
        [DataRow((byte)0x25u)]
        [DataRow((byte)0x26u)]
        [DataRow((byte)0x27u)]
        [DataRow((byte)0x28u)]
        [DataRow((byte)0x29u)]
        [DataRow((byte)0x2Au)]
        [DataRow((byte)0x2Bu)]
        [DataRow((byte)0x2Cu)]
        [DataRow((byte)0x2Du)]
        [DataRow((byte)0x2Eu)]
        [DataRow((byte)0x2Fu)]
        [DataRow((byte)0x30u)]
        [DataRow((byte)0x31u)]
        [DataRow((byte)0x32u)]
        [DataRow((byte)0x33u)]
        [DataRow((byte)0x34u)]
        [DataRow((byte)0x35u)]
        [DataRow((byte)0x36u)]
        [DataRow((byte)0x37u)]
        [DataRow((byte)0x38u)]
        [DataRow((byte)0x39u)]
        [DataRow((byte)0x3Au)]
        [DataRow((byte)0x3Bu)]
        [DataRow((byte)0x3Cu)]
        [DataRow((byte)0x3Du)]
        [DataRow((byte)0x3Eu)]
        [DataRow((byte)0x3Fu)]
        [DataRow((byte)0x40u)]
        [DataRow((byte)0x41u)]
        [DataRow((byte)0x42u)]
        [DataRow((byte)0x43u)]
        [DataRow((byte)0x44u)]
        [DataRow((byte)0x45u)]
        [DataRow((byte)0x46u)]
        [DataRow((byte)0x47u)]
        [DataRow((byte)0x48u)]
        [DataRow((byte)0x49u)]
        [DataRow((byte)0x4Au)]
        [DataRow((byte)0x4Bu)]
        [DataRow((byte)0x4Cu)]
        [DataRow((byte)0x4Du)]
        [DataRow((byte)0x4Eu)]
        [DataRow((byte)0x4Fu)]
        [DataRow((byte)0x50u)]
        [DataRow((byte)0x51u)]
        [DataRow((byte)0x52u)]
        [DataRow((byte)0x53u)]
        [DataRow((byte)0x54u)]
        [DataRow((byte)0x55u)]
        [DataRow((byte)0x56u)]
        [DataRow((byte)0x57u)]
        [DataRow((byte)0x58u)]
        [DataRow((byte)0x59u)]
        [DataRow((byte)0x5Au)]
        [DataRow((byte)0x5Bu)]
        [DataRow((byte)0x5Cu)]
        [DataRow((byte)0x5Du)]
        [DataRow((byte)0x5Eu)]
        [DataRow((byte)0x5Fu)]
        [DataRow((byte)0x60u)]
        [DataRow((byte)0x61u)]
        [DataRow((byte)0x62u)]
        [DataRow((byte)0x63u)]
        [DataRow((byte)0x64u)]
        [DataRow((byte)0x65u)]
        [DataRow((byte)0x66u)]
        [DataRow((byte)0x67u)]
        [DataRow((byte)0x68u)]
        [DataRow((byte)0x69u)]
        [DataRow((byte)0x6Au)]
        [DataRow((byte)0x6Bu)]
        [DataRow((byte)0x6Cu)]
        [DataRow((byte)0x6Du)]
        [DataRow((byte)0x6Eu)]
        [DataRow((byte)0x6Fu)]
        [DataRow((byte)0x70u)]
        [DataRow((byte)0x71u)]
        [DataRow((byte)0x72u)]
        [DataRow((byte)0x73u)]
        [DataRow((byte)0x74u)]
        [DataRow((byte)0x75u)]
        [DataRow((byte)0x76u)]
        [DataRow((byte)0x77u)]
        [DataRow((byte)0x78u)]
        [DataRow((byte)0x79u)]
        [DataRow((byte)0x7Au)]
        [DataRow((byte)0x7Bu)]
        [DataRow((byte)0x7Cu)]
        [DataRow((byte)0x7Du)]
        [DataRow((byte)0x7Eu)]
        [DataRow((byte)0x7Fu)]
        [DataRow((byte)0x80u)]
        [DataRow((byte)0x81u)]
        [DataRow((byte)0x82u)]
        [DataRow((byte)0x83u)]
        [DataRow((byte)0x84u)]
        [DataRow((byte)0x85u)]
        [DataRow((byte)0x86u)]
        [DataRow((byte)0x87u)]
        [DataRow((byte)0x88u)]
        [DataRow((byte)0x89u)]
        [DataRow((byte)0x8Au)]
        [DataRow((byte)0x8Bu)]
        [DataRow((byte)0x8Cu)]
        [DataRow((byte)0x8Du)]
        [DataRow((byte)0x8Eu)]
        [DataRow((byte)0x8Fu)]
        [DataRow((byte)0x90u)]
        [DataRow((byte)0x91u)]
        [DataRow((byte)0x92u)]
        [DataRow((byte)0x93u)]
        [DataRow((byte)0x94u)]
        [DataRow((byte)0x95u)]
        [DataRow((byte)0x96u)]
        [DataRow((byte)0x97u)]
        [DataRow((byte)0x98u)]
        [DataRow((byte)0x99u)]
        [DataRow((byte)0x9Au)]
        [DataRow((byte)0x9Bu)]
        [DataRow((byte)0x9Cu)]
        [DataRow((byte)0x9Du)]
        [DataRow((byte)0x9Eu)]
        [DataRow((byte)0x9Fu)]
        [DataRow((byte)0xA0u)]
        [DataRow((byte)0xA1u)]
        [DataRow((byte)0xA2u)]
        [DataRow((byte)0xA3u)]
        [DataRow((byte)0xA4u)]
        [DataRow((byte)0xA5u)]
        [DataRow((byte)0xA6u)]
        [DataRow((byte)0xA7u)]
        [DataRow((byte)0xA8u)]
        [DataRow((byte)0xA9u)]
        [DataRow((byte)0xAAu)]
        [DataRow((byte)0xABu)]
        [DataRow((byte)0xACu)]
        [DataRow((byte)0xADu)]
        [DataRow((byte)0xAEu)]
        [DataRow((byte)0xAFu)]
        [DataRow((byte)0xB0u)]
        [DataRow((byte)0xB1u)]
        [DataRow((byte)0xB2u)]
        [DataRow((byte)0xB3u)]
        [DataRow((byte)0xB4u)]
        [DataRow((byte)0xB5u)]
        [DataRow((byte)0xB6u)]
        [DataRow((byte)0xB7u)]
        [DataRow((byte)0xB8u)]
        [DataRow((byte)0xB9u)]
        [DataRow((byte)0xBAu)]
        [DataRow((byte)0xBBu)]
        [DataRow((byte)0xBCu)]
        [DataRow((byte)0xBDu)]
        [DataRow((byte)0xBEu)]
        [DataRow((byte)0xBFu)]
        [DataRow((byte)0xC0u)]
        [DataRow((byte)0xC1u)]
        [DataRow((byte)0xC2u)]
        [DataRow((byte)0xC3u)]
        [DataRow((byte)0xC4u)]
        [DataRow((byte)0xC5u)]
        [DataRow((byte)0xC6u)]
        [DataRow((byte)0xC7u)]
        [DataRow((byte)0xC8u)]
        [DataRow((byte)0xC9u)]
        [DataRow((byte)0xCAu)]
        [DataRow((byte)0xCBu)]
        [DataRow((byte)0xCCu)]
        [DataRow((byte)0xCDu)]
        [DataRow((byte)0xCEu)]
        [DataRow((byte)0xCFu)]
        [DataRow((byte)0xD0u)]
        [DataRow((byte)0xD1u)]
        [DataRow((byte)0xD2u)]
        [DataRow((byte)0xD3u)]
        [DataRow((byte)0xD4u)]
        [DataRow((byte)0xD5u)]
        [DataRow((byte)0xD6u)]
        [DataRow((byte)0xD7u)]
        [DataRow((byte)0xD8u)]
        [DataRow((byte)0xD9u)]
        [DataRow((byte)0xDAu)]
        [DataRow((byte)0xDBu)]
        [DataRow((byte)0xDCu)]
        [DataRow((byte)0xDDu)]
        [DataRow((byte)0xDEu)]
        [DataRow((byte)0xDFu)]
        [DataRow((byte)0xE0u)]
        [DataRow((byte)0xE1u)]
        [DataRow((byte)0xE2u)]
        [DataRow((byte)0xE3u)]
        [DataRow((byte)0xE4u)]
        [DataRow((byte)0xE5u)]
        [DataRow((byte)0xE6u)]
        [DataRow((byte)0xE7u)]
        [DataRow((byte)0xE8u)]
        [DataRow((byte)0xE9u)]
        [DataRow((byte)0xEAu)]
        [DataRow((byte)0xEBu)]
        [DataRow((byte)0xECu)]
        [DataRow((byte)0xEDu)]
        [DataRow((byte)0xEEu)]
        [DataRow((byte)0xEFu)]
        [DataRow((byte)0xF0u)]
        [DataRow((byte)0xF1u)]
        [DataRow((byte)0xF2u)]
        [DataRow((byte)0xF3u)]
        [DataRow((byte)0xF4u)]
        [DataRow((byte)0xF5u)]
        [DataRow((byte)0xF6u)]
        [DataRow((byte)0xF7u)]
        [DataRow((byte)0xF8u)]
        [DataRow((byte)0xF9u)]
        [DataRow((byte)0xFAu)]
        [DataRow((byte)0xFBu)]
        [DataRow((byte)0xFCu)]
        [DataRow((byte)0xFDu)]
        [DataRow((byte)0xFEu)]
        [DataRow((byte)0xFFu)]
        #endregion
        [TestCategory("CPU Data Tests")]
        public void decodeAndExecute_Cycles_Test(Byte opCode)
        {

            List<Byte> notImplementedOpCodes = new List<byte>() { 0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD };
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            logMessage($"Testing OpCode 0x{opCode.ToString("X2")} - {cpu.getInstructionForOpCode(opCode).desc}");
            if (notImplementedOpCodes.Contains(opCode))
            {
                try
                {
                    executeCurrentInstruction(cpu);
                    Exception e = new Exception();
                    logMessage($"Expected Exception Not Fired ({typeof(NotImplementedException).ToString()}, None)");
                    Assert.IsInstanceOfType(e, typeof(NotImplementedException));
                }
                catch (AssertFailedException ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logMessage($"Excpetion Recieved ({typeof(NotImplementedException).ToString()}, {ex.GetType().ToString()})");
                    Assert.IsInstanceOfType(ex, typeof(NotImplementedException));
                }
            }
            else
            {
                if (opCode != 0xCB)
                {
                    executeCurrentInstruction(cpu);
                }
                else
                {
                    Assert.Inconclusive("CB Prefixed Opcodes are handled by their own tests.");
                }
            }

            // Check Number of Cycles
            Assert.That.AreEqual(cpu.getLastExecutedInstruction().cycles, cpu.getLastExecutedInstruction().getCycleCount());
        }

        [DataTestMethod]
        [Timeout(10000)]
        #region Test Data Rows
        [DataRow((byte)0x00u)]
        [DataRow((byte)0x01u)]
        [DataRow((byte)0x02u)]
        [DataRow((byte)0x03u)]
        [DataRow((byte)0x04u)]
        [DataRow((byte)0x05u)]
        [DataRow((byte)0x06u)]
        [DataRow((byte)0x07u)]
        [DataRow((byte)0x08u)]
        [DataRow((byte)0x09u)]
        [DataRow((byte)0x0Au)]
        [DataRow((byte)0x0Bu)]
        [DataRow((byte)0x0Cu)]
        [DataRow((byte)0x0Du)]
        [DataRow((byte)0x0Eu)]
        [DataRow((byte)0x0Fu)]
        [DataRow((byte)0x10u)]
        [DataRow((byte)0x11u)]
        [DataRow((byte)0x12u)]
        [DataRow((byte)0x13u)]
        [DataRow((byte)0x14u)]
        [DataRow((byte)0x15u)]
        [DataRow((byte)0x16u)]
        [DataRow((byte)0x17u)]
        [DataRow((byte)0x18u)]
        [DataRow((byte)0x19u)]
        [DataRow((byte)0x1Au)]
        [DataRow((byte)0x1Bu)]
        [DataRow((byte)0x1Cu)]
        [DataRow((byte)0x1Du)]
        [DataRow((byte)0x1Eu)]
        [DataRow((byte)0x1Fu)]
        [DataRow((byte)0x20u)]
        [DataRow((byte)0x21u)]
        [DataRow((byte)0x22u)]
        [DataRow((byte)0x23u)]
        [DataRow((byte)0x24u)]
        [DataRow((byte)0x25u)]
        [DataRow((byte)0x26u)]
        [DataRow((byte)0x27u)]
        [DataRow((byte)0x28u)]
        [DataRow((byte)0x29u)]
        [DataRow((byte)0x2Au)]
        [DataRow((byte)0x2Bu)]
        [DataRow((byte)0x2Cu)]
        [DataRow((byte)0x2Du)]
        [DataRow((byte)0x2Eu)]
        [DataRow((byte)0x2Fu)]
        [DataRow((byte)0x30u)]
        [DataRow((byte)0x31u)]
        [DataRow((byte)0x32u)]
        [DataRow((byte)0x33u)]
        [DataRow((byte)0x34u)]
        [DataRow((byte)0x35u)]
        [DataRow((byte)0x36u)]
        [DataRow((byte)0x37u)]
        [DataRow((byte)0x38u)]
        [DataRow((byte)0x39u)]
        [DataRow((byte)0x3Au)]
        [DataRow((byte)0x3Bu)]
        [DataRow((byte)0x3Cu)]
        [DataRow((byte)0x3Du)]
        [DataRow((byte)0x3Eu)]
        [DataRow((byte)0x3Fu)]
        [DataRow((byte)0x40u)]
        [DataRow((byte)0x41u)]
        [DataRow((byte)0x42u)]
        [DataRow((byte)0x43u)]
        [DataRow((byte)0x44u)]
        [DataRow((byte)0x45u)]
        [DataRow((byte)0x46u)]
        [DataRow((byte)0x47u)]
        [DataRow((byte)0x48u)]
        [DataRow((byte)0x49u)]
        [DataRow((byte)0x4Au)]
        [DataRow((byte)0x4Bu)]
        [DataRow((byte)0x4Cu)]
        [DataRow((byte)0x4Du)]
        [DataRow((byte)0x4Eu)]
        [DataRow((byte)0x4Fu)]
        [DataRow((byte)0x50u)]
        [DataRow((byte)0x51u)]
        [DataRow((byte)0x52u)]
        [DataRow((byte)0x53u)]
        [DataRow((byte)0x54u)]
        [DataRow((byte)0x55u)]
        [DataRow((byte)0x56u)]
        [DataRow((byte)0x57u)]
        [DataRow((byte)0x58u)]
        [DataRow((byte)0x59u)]
        [DataRow((byte)0x5Au)]
        [DataRow((byte)0x5Bu)]
        [DataRow((byte)0x5Cu)]
        [DataRow((byte)0x5Du)]
        [DataRow((byte)0x5Eu)]
        [DataRow((byte)0x5Fu)]
        [DataRow((byte)0x60u)]
        [DataRow((byte)0x61u)]
        [DataRow((byte)0x62u)]
        [DataRow((byte)0x63u)]
        [DataRow((byte)0x64u)]
        [DataRow((byte)0x65u)]
        [DataRow((byte)0x66u)]
        [DataRow((byte)0x67u)]
        [DataRow((byte)0x68u)]
        [DataRow((byte)0x69u)]
        [DataRow((byte)0x6Au)]
        [DataRow((byte)0x6Bu)]
        [DataRow((byte)0x6Cu)]
        [DataRow((byte)0x6Du)]
        [DataRow((byte)0x6Eu)]
        [DataRow((byte)0x6Fu)]
        [DataRow((byte)0x70u)]
        [DataRow((byte)0x71u)]
        [DataRow((byte)0x72u)]
        [DataRow((byte)0x73u)]
        [DataRow((byte)0x74u)]
        [DataRow((byte)0x75u)]
        [DataRow((byte)0x76u)]
        [DataRow((byte)0x77u)]
        [DataRow((byte)0x78u)]
        [DataRow((byte)0x79u)]
        [DataRow((byte)0x7Au)]
        [DataRow((byte)0x7Bu)]
        [DataRow((byte)0x7Cu)]
        [DataRow((byte)0x7Du)]
        [DataRow((byte)0x7Eu)]
        [DataRow((byte)0x7Fu)]
        [DataRow((byte)0x80u)]
        [DataRow((byte)0x81u)]
        [DataRow((byte)0x82u)]
        [DataRow((byte)0x83u)]
        [DataRow((byte)0x84u)]
        [DataRow((byte)0x85u)]
        [DataRow((byte)0x86u)]
        [DataRow((byte)0x87u)]
        [DataRow((byte)0x88u)]
        [DataRow((byte)0x89u)]
        [DataRow((byte)0x8Au)]
        [DataRow((byte)0x8Bu)]
        [DataRow((byte)0x8Cu)]
        [DataRow((byte)0x8Du)]
        [DataRow((byte)0x8Eu)]
        [DataRow((byte)0x8Fu)]
        [DataRow((byte)0x90u)]
        [DataRow((byte)0x91u)]
        [DataRow((byte)0x92u)]
        [DataRow((byte)0x93u)]
        [DataRow((byte)0x94u)]
        [DataRow((byte)0x95u)]
        [DataRow((byte)0x96u)]
        [DataRow((byte)0x97u)]
        [DataRow((byte)0x98u)]
        [DataRow((byte)0x99u)]
        [DataRow((byte)0x9Au)]
        [DataRow((byte)0x9Bu)]
        [DataRow((byte)0x9Cu)]
        [DataRow((byte)0x9Du)]
        [DataRow((byte)0x9Eu)]
        [DataRow((byte)0x9Fu)]
        [DataRow((byte)0xA0u)]
        [DataRow((byte)0xA1u)]
        [DataRow((byte)0xA2u)]
        [DataRow((byte)0xA3u)]
        [DataRow((byte)0xA4u)]
        [DataRow((byte)0xA5u)]
        [DataRow((byte)0xA6u)]
        [DataRow((byte)0xA7u)]
        [DataRow((byte)0xA8u)]
        [DataRow((byte)0xA9u)]
        [DataRow((byte)0xAAu)]
        [DataRow((byte)0xABu)]
        [DataRow((byte)0xACu)]
        [DataRow((byte)0xADu)]
        [DataRow((byte)0xAEu)]
        [DataRow((byte)0xAFu)]
        [DataRow((byte)0xB0u)]
        [DataRow((byte)0xB1u)]
        [DataRow((byte)0xB2u)]
        [DataRow((byte)0xB3u)]
        [DataRow((byte)0xB4u)]
        [DataRow((byte)0xB5u)]
        [DataRow((byte)0xB6u)]
        [DataRow((byte)0xB7u)]
        [DataRow((byte)0xB8u)]
        [DataRow((byte)0xB9u)]
        [DataRow((byte)0xBAu)]
        [DataRow((byte)0xBBu)]
        [DataRow((byte)0xBCu)]
        [DataRow((byte)0xBDu)]
        [DataRow((byte)0xBEu)]
        [DataRow((byte)0xBFu)]
        [DataRow((byte)0xC0u)]
        [DataRow((byte)0xC1u)]
        [DataRow((byte)0xC2u)]
        [DataRow((byte)0xC3u)]
        [DataRow((byte)0xC4u)]
        [DataRow((byte)0xC5u)]
        [DataRow((byte)0xC6u)]
        [DataRow((byte)0xC7u)]
        [DataRow((byte)0xC8u)]
        [DataRow((byte)0xC9u)]
        [DataRow((byte)0xCAu)]
        [DataRow((byte)0xCBu)]
        [DataRow((byte)0xCCu)]
        [DataRow((byte)0xCDu)]
        [DataRow((byte)0xCEu)]
        [DataRow((byte)0xCFu)]
        [DataRow((byte)0xD0u)]
        [DataRow((byte)0xD1u)]
        [DataRow((byte)0xD2u)]
        [DataRow((byte)0xD3u)]
        [DataRow((byte)0xD4u)]
        [DataRow((byte)0xD5u)]
        [DataRow((byte)0xD6u)]
        [DataRow((byte)0xD7u)]
        [DataRow((byte)0xD8u)]
        [DataRow((byte)0xD9u)]
        [DataRow((byte)0xDAu)]
        [DataRow((byte)0xDBu)]
        [DataRow((byte)0xDCu)]
        [DataRow((byte)0xDDu)]
        [DataRow((byte)0xDEu)]
        [DataRow((byte)0xDFu)]
        [DataRow((byte)0xE0u)]
        [DataRow((byte)0xE1u)]
        [DataRow((byte)0xE2u)]
        [DataRow((byte)0xE3u)]
        [DataRow((byte)0xE4u)]
        [DataRow((byte)0xE5u)]
        [DataRow((byte)0xE6u)]
        [DataRow((byte)0xE7u)]
        [DataRow((byte)0xE8u)]
        [DataRow((byte)0xE9u)]
        [DataRow((byte)0xEAu)]
        [DataRow((byte)0xEBu)]
        [DataRow((byte)0xECu)]
        [DataRow((byte)0xEDu)]
        [DataRow((byte)0xEEu)]
        [DataRow((byte)0xEFu)]
        [DataRow((byte)0xF0u)]
        [DataRow((byte)0xF1u)]
        [DataRow((byte)0xF2u)]
        [DataRow((byte)0xF3u)]
        [DataRow((byte)0xF4u)]
        [DataRow((byte)0xF5u)]
        [DataRow((byte)0xF6u)]
        [DataRow((byte)0xF7u)]
        [DataRow((byte)0xF8u)]
        [DataRow((byte)0xF9u)]
        [DataRow((byte)0xFAu)]
        [DataRow((byte)0xFBu)]
        [DataRow((byte)0xFCu)]
        [DataRow((byte)0xFDu)]
        [DataRow((byte)0xFEu)]
        [DataRow((byte)0xFFu)]
        #endregion
        [TestCategory("CPU Data Tests")]
        public void decodeAndExecute_Cycles_CBCodes_Test(Byte opCode)
        {
            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            logMessage($"Testing CB Prefixed OpCode 0x{opCode.ToString("X2")} - {cpu.getInstructionForOpCode(opCode).desc}");

            executeCurrentInstruction(cpu);

            // Check Number of Cycles
            Assert.That.AreEqual(0xCB, cpu.getLastExecutedInstruction().opCode, "OpCode was not 0xCB");
            Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().parameters[0], "CB Prefixed Opcode was Incorrect");
            Assert.That.AreEqual(cpu.getLastExecutedInstruction().cycles, cpu.getLastExecutedInstruction().getCycleCount(), "Actual Cycles did match Model Cycles");
        }
        #endregion


        #region Op Code Tests

        #region 0x00 - NOP
        [TestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x00 - NOP")]
        public void decodeAndExecute_OPCode_0x00_NOP()
        {
            byte opCode = 0x00;
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            Byte flagsByte = cpu.getF();

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, flagsByte);
        }
        #endregion

        #region 0x01 - Load BC

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

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, op1, op2);
            Byte flagsByte = cpu.getF();

            fetchAndLoadInstruction(cpu, opCode);
            Byte b = cpu.getB();
            tick(cpu);
            Assert.That.AreEqual(b, cpu.getB());
            Assert.That.AreEqual(op1, cpu.getC());
            tick(cpu);
            Assert.That.AreEqual(op2, cpu.getB());
            Assert.That.AreEqual(op1, cpu.getC());
            Assert.That.AreEqual(CPU.getUInt16ForBytes(op1, op2), cpu.getBC());
            assertInstructionFinished(cpu, opCode);
            Assert.That.FlagsEqual(cpu, flagsByte);

        }
        #endregion

        #region 0x02 - A to Mem BC
        [DataRow((byte)0xFFu, (ushort)0xC000)]
        [DataRow((byte)0x01u, (ushort)0xC000)]
        [DataRow((byte)0x0Fu, (ushort)0xC000)]
        [DataRow((byte)0xF0u, (ushort)0xC000)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x02 - A to Mem BC")]
        public void decodeAndExecute_OPCode_0x02_ldAToMemBC(byte op1, ushort address)
        {
            byte opCode = 0x02;
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(0xF0);
            Byte flagsByte = cpu.getF();
            cpu.setA(op1);
            cpu.mem.setByte(address, 0x00);
            cpu.setBC(address);
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(0x00, cpu.mem.getByte(cpu.getBC()));
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getA());
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu.getBC()));
            assertInstructionFinished(cpu, opCode);
            Assert.That.FlagsEqual(cpu, flagsByte);
        }
        #endregion

        #region 0x03 - Increment BC
        [DataRow((ushort)0xFFFF)]
        [DataRow((ushort)0xFFFE)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x03 - Increment BC")]
        public void decodeAndExecute_incBC(ushort bc)
        {
            byte opCode = 0x03;
            byte loadBCOpCode = 0x01;

            // Setup CPU and Load BC with Data
            CPU cpu = setupOpCode(loadBCOpCode, MethodBase.GetCurrentMethod().Name, CPU.getLSB(bc), CPU.getMSB(bc), new byte[] { opCode });
            executeCurrentInstruction(cpu);
            Assert.That.AreEqual(bc, cpu.getBC());
            Assert.That.AreEqual(0x100 + cpu.getInstructionForOpCode(loadBCOpCode).length, cpu.getPC());


            // BC is loaded Increment it
            ushort expectedResult = cpu.add16IgnoreFlags(1, bc);
            Byte flagsByte = cpu.getF();

            Assert.That.AreEqual(opCode, cpu.peek());
            Byte b = cpu.getB();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(b, cpu.getB());
            Assert.That.AreEqual(CPU.getLSB(expectedResult), cpu.getC());
            tick(cpu);
            Assert.That.AreEqual(CPU.getMSB(expectedResult), cpu.getB());
            Assert.That.AreEqual(CPU.getLSB(expectedResult), cpu.getC());
            Assert.That.AreEqual(expectedResult, cpu.getBC());
            assertInstructionFinished(cpu, opCode);
            Assert.That.FlagsEqual(cpu, flagsByte);

        }
        #endregion

        #region 0x04 - Increment B
        [DataRow((byte)0xFF, (byte)0x00, (Byte)0x50, (Byte)0xB0)]
        [DataRow((byte)0x00, (byte)0x01, (Byte)0x50, (Byte)0x10)]
        [DataRow((byte)0x0F, (byte)0x10, (Byte)0x50, (Byte)0x30)]

        [DataRow((byte)0xFF, (byte)0x00, (Byte)0x00, (Byte)0xA0)]
        [DataRow((byte)0x00, (byte)0x01, (Byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x0F, (byte)0x10, (Byte)0x00, (Byte)0x20)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x04 - Increment B")]
        public void decodeAndExecute_incB(byte op1, byte result, byte initialFlags, byte newFlags)
        {
            byte opCode = 0x04;

            // Setup CPU and Load BC with Data
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setB(op1);

            // BC is loaded Increment it
            cpu.setF(initialFlags);

            Assert.That.AreEqual(opCode, cpu.peek());
            Byte b = cpu.getB();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getB());
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, newFlags);

        }
        #endregion

        #region 0x05 - Decrement B
        [DataRow((byte)0x01, (byte)0x00, (Byte)0x50, (Byte)0xD0)]
        [DataRow((byte)0x00, (byte)0xFF, (Byte)0x50, (Byte)0x70)]
        [DataRow((byte)0x10, (byte)0x0F, (Byte)0x50, (Byte)0x70)]
        [DataRow((byte)0x0F, (byte)0x0E, (Byte)0x50, (Byte)0x50)]

        [DataRow((byte)0x01, (byte)0x00, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x00, (byte)0xFF, (Byte)0x00, (Byte)0x60)]
        [DataRow((byte)0x10, (byte)0x0F, (Byte)0x00, (Byte)0x60)]
        [DataRow((byte)0x0F, (byte)0x0E, (Byte)0x00, (Byte)0x40)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x05 - Decrement B")]
        public void decodeAndExecute_decB(byte op1, byte result, byte initialFlags, byte newFlags)
        {
            byte opCode = 0x05;

            // Setup CPU and Load BC with Data
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setB(op1);

            // BC is loaded Increment it
            cpu.setF(initialFlags);

            Assert.That.AreEqual(opCode, cpu.peek());
            Byte b = cpu.getB();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getB());
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, newFlags);

        }
        #endregion

        #region 0x06 - Load B
        [DataRow((byte)0x01)]
        [DataRow((byte)0x00)]
        [DataRow((byte)0x10)]
        [DataRow((byte)0x0F)]
        [DataRow((byte)0xFF)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x06 - Load B")]
        public void decodeAndExecute_ldB(byte op1)
        {
            byte opCode = 0x06;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, op1);
            Byte flagsByte = cpu.getF();
            cpu.setB(0x03);

            Byte b = cpu.getB();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(b, cpu.getB());
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getB());
            assertInstructionFinished(cpu, opCode);
            Assert.That.FlagsEqual(cpu, flagsByte);

        }
        #endregion

        #region 0x07- Rotate Left Carry A
        [DataRow((byte)0x80, (Byte)0x01, false, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x80, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x00, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFF, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x07 - Rotate Left Carry A")]
        public void decodeAndExecute_rlcA(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x07;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setA(op1);

            byte expectedFlags = (Byte)(((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)); // Zero Flag is Always Reset. Confirmed by BGB EMu

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x08 - Load SP Mem 16
        [DataRow((Byte)0xFF, (Byte)0xFF, (ushort)0xFFFF)]
        [DataRow((Byte)0x00, (Byte)0x00, (ushort)0x0000)]
        [DataRow((Byte)0x0F, (Byte)0x00, (ushort)0x000F)]
        [DataRow((Byte)0xFF, (Byte)0x00, (ushort)0x00FF)]
        [DataRow((Byte)0x00, (Byte)0xFF, (ushort)0xFF00)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x08 - Load SP from Mem 16")]
        public void decodeAndExecute_ldSP_Mem16(byte op1, byte op2, ushort result)
        {
            byte opCode = 0x08;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, op1, op2);
            cpu.setF(0xF0);

            byte expectedFlags = 0xF0;
            ushort sp = cpu.getSP();

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(sp, cpu.getSP());
            tick(cpu);
            Assert.That.AreEqual(sp, cpu.getSP());
            Assert.That.AreEqual(op1, cpu.getCurrentInstruction().storage & 0x00FF);
            tick(cpu);
            Assert.That.AreEqual(sp, cpu.getSP());
            Assert.That.AreEqual(CPU.getUInt16ForBytes(op1, op2), cpu.getCurrentInstruction().storage);
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getSP() & 0x00FF);
            Assert.That.AreEqual((sp & 0xFF00), cpu.getSP() & 0xFF00);
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getSP());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x09 - Add BC to HL
        [DataRow((ushort)0x0001, (ushort)0xFFFF, (ushort)0x0000, (Byte)0xF0, (Byte)((Byte)CPU.CPUFlagsMask.Zero + (Byte)CPU.CPUFlagsMask.Carry + (Byte)CPU.CPUFlagsMask.HalfCarry))]
        [DataRow((ushort)0x00FF, (ushort)0xFF00, (ushort)0xFFFF, (Byte)0xF0, ((Byte)CPU.CPUFlagsMask.Zero))]
        [DataRow((ushort)0x0FFF, (ushort)0x0001, (ushort)0x1000, (Byte)0xF0, (Byte)((Byte)CPU.CPUFlagsMask.Zero + (Byte)CPU.CPUFlagsMask.HalfCarry))]
        [DataRow((ushort)0xF000, (ushort)0x1000, (ushort)0x0000, (Byte)0xF0, (Byte)((Byte)CPU.CPUFlagsMask.Zero + (Byte)CPU.CPUFlagsMask.Carry))]
        [DataRow((ushort)0x000A, (ushort)0x0000, (ushort)0x000A, (Byte)0xF0, (Byte)CPU.CPUFlagsMask.Zero)]
        [DataRow((ushort)0x0000, (ushort)0x000A, (ushort)0x000A, (Byte)0xF0, (Byte)CPU.CPUFlagsMask.Zero)]
        [DataRow((ushort)0x6301, (ushort)0x1276, (ushort)0x7577, (Byte)0xF0, (Byte)CPU.CPUFlagsMask.Zero)]

        [DataRow((ushort)0x0001, (ushort)0xFFFF, (ushort)0x0000, (Byte)0x00, (Byte)((Byte)CPU.CPUFlagsMask.Carry + (Byte)CPU.CPUFlagsMask.HalfCarry))]
        [DataRow((ushort)0x00FF, (ushort)0xFF00, (ushort)0xFFFF, (Byte)0x00, (Byte)0)]
        [DataRow((ushort)0x0FFF, (ushort)0x0001, (ushort)0x1000, (Byte)0x00, (Byte)((Byte)CPU.CPUFlagsMask.HalfCarry))]
        [DataRow((ushort)0xF000, (ushort)0x1000, (ushort)0x0000, (Byte)0x00, (Byte)((Byte)CPU.CPUFlagsMask.Carry))]
        [DataRow((ushort)0x000A, (ushort)0x0000, (ushort)0x000A, (Byte)0x00, (Byte)0)]
        [DataRow((ushort)0x0000, (ushort)0x000A, (ushort)0x000A, (Byte)0x00, (Byte)0)]
        [DataRow((ushort)0x6301, (ushort)0x1276, (ushort)0x7577, (Byte)0x00, (Byte)0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x09 - Add BC to HL")]
        public void decodeAndExecute_addBCToHL(ushort bc, ushort hl, ushort result, byte initialFlags, byte flags)
        {
            byte opCode = 0x09;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags); // Zero and Subtract Should Be Cleared
            cpu.setHL(hl);
            cpu.setBC(bc);

            ushort interRes = CPU.getUInt16ForBytes(CPU.getLSB(result), CPU.getMSB(cpu.getHL()));

            fetchAndLoadInstruction(cpu, opCode);
            Assert.IsNotNull(cpu.getCurrentInstruction());
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage);
            Assert.That.AreEqual(interRes, cpu.getHL());
            Assert.That.AreEqual(CPU.getLSB(result), cpu.getL());
            tick(cpu);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(result, cpu.getHL());
            Assert.That.AreEqual(CPU.getMSB(result), cpu.getH());


            Assert.That.FlagsEqual(cpu, flags);

        }
        #endregion

        #region 0x0A - Mem BC to A
        [DataRow((byte)0xFFu, (ushort)0xC000)]
        [DataRow((byte)0x01u, (ushort)0xC000)]
        [DataRow((byte)0x0Fu, (ushort)0xC000)]
        [DataRow((byte)0xF0u, (ushort)0xC000)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x0A - Mem BC to A")]
        public void decodeAndExecute_OPCode_ldMemBCToA(byte op1, ushort address)
        {
            byte opCode = 0x0A;
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(0xF0);
            Byte flagsByte = cpu.getF();
            cpu.setA(0x00);
            cpu.mem.setByte(address, op1);
            cpu.setBC(address);
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(0x00, cpu.getA());
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getA());
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu.getBC()));


            assertInstructionFinished(cpu, opCode);
            Assert.That.FlagsEqual(cpu, flagsByte);
        }
        #endregion

        #region 0x0B - Decrement BC
        [DataRow((ushort)0x0000, (ushort)0xFFFF)]
        [DataRow((ushort)0xFFFF, (ushort)0xFFFE)]
        [DataRow((ushort)0x00FF, (ushort)0x00FE)]
        [DataRow((ushort)0x0FFF, (ushort)0x0FFE)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x0B - Decrement BC")]
        public void decodeAndExecute_decBC(ushort bc, ushort result)
        {
            byte opCode = 0x0B;

            // Setup CPU and Load BC with Data
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setBC(bc);

            cpu.setF(0xF0);
            Byte flagsByte = cpu.getF();

            Byte b = cpu.getB();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage);
            Assert.That.AreEqual(b, cpu.getB());
            Assert.That.AreEqual(CPU.getLSB(result), cpu.getC());
            tick(cpu);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(CPU.getMSB(result), cpu.getB());
            Assert.That.AreEqual(CPU.getLSB(result), cpu.getC());
            Assert.That.AreEqual(result, cpu.getBC());

            Assert.That.FlagsEqual(cpu, flagsByte);

        }
        #endregion

        #region 0x0C - Increment C
        [DataRow((byte)0xFF, (byte)0x00, (Byte)0x50, (Byte)0xB0)]
        [DataRow((byte)0x00, (byte)0x01, (Byte)0x50, (Byte)0x10)]
        [DataRow((byte)0x0F, (byte)0x10, (Byte)0x50, (Byte)0x30)]

        [DataRow((byte)0xFF, (byte)0x00, (Byte)0x00, (Byte)0xA0)]
        [DataRow((byte)0x00, (byte)0x01, (Byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x0F, (byte)0x10, (Byte)0x00, (Byte)0x20)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x0C - Increment C")]
        public void decodeAndExecute_incC(byte op1, byte result, byte initialFlags, byte newFlags)
        {
            byte opCode = 0x0C;

            // Setup CPU and Load BC with Data
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setC(op1);

            // BC is loaded Increment it
            cpu.setF(initialFlags);

            Assert.That.AreEqual(opCode, cpu.peek());
            Byte c = cpu.getC();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getC());
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, newFlags);

        }
        #endregion

        #region 0x0D - Decrement C
        [DataRow((byte)0x01, (byte)0x00, (Byte)0x50, (Byte)0xD0)]
        [DataRow((byte)0x00, (byte)0xFF, (Byte)0x50, (Byte)0x70)]
        [DataRow((byte)0x10, (byte)0x0F, (Byte)0x50, (Byte)0x70)]
        [DataRow((byte)0x0F, (byte)0x0E, (Byte)0x50, (Byte)0x50)]

        [DataRow((byte)0x01, (byte)0x00, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x00, (byte)0xFF, (Byte)0x00, (Byte)0x60)]
        [DataRow((byte)0x10, (byte)0x0F, (Byte)0x00, (Byte)0x60)]
        [DataRow((byte)0x0F, (byte)0x0E, (Byte)0x00, (Byte)0x40)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x0D - Decrement C")]
        public void decodeAndExecute_decC(byte op1, byte result, byte initialFlags, byte newFlags)
        {
            byte opCode = 0x0D;

            // Setup CPU and Load BC with Data
            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setC(op1);

            // BC is loaded Increment it
            cpu.setF(initialFlags);

            Assert.That.AreEqual(opCode, cpu.peek());
            Byte c = cpu.getC();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getC());
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, newFlags);

        }
        #endregion

        #region 0x0E - Load C
        [DataRow((byte)0x01)]
        [DataRow((byte)0x00)]
        [DataRow((byte)0x10)]
        [DataRow((byte)0x0F)]
        [DataRow((byte)0xFF)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x0E - Load C")]
        public void decodeAndExecute_ldC(byte op1)
        {
            byte opCode = 0x0E;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, op1);
            cpu.setF(0xF0);
            Byte flagsByte = cpu.getF();
            cpu.setC(0x03);

            Byte c = cpu.getC();
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(c, cpu.getC());
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getC());
            assertInstructionFinished(cpu, opCode);
            Assert.That.FlagsEqual(cpu, flagsByte);
        }
        #endregion

        #region 0x0F- Rotate Right Carry A
        [DataRow((byte)0x01, (Byte)0x80, false, true)]
        [DataRow((byte)0x01, (Byte)0x80, true, true)]
        [DataRow((byte)0x80, (Byte)0x40, false, false)]
        [DataRow((byte)0x80, (Byte)0x40, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x00, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFF, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x1E, (Byte)0x0F, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x0F - Rotate Right Carry A")]
        public void decodeAndExecute_rrcA(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0F;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setA(op1);

            byte expectedFlags = (Byte)(((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)); // Zero Flag is Always Reset. Confirmed by BGB EMU

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xB8 - Compare A with B
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0x00, (Byte)0x40)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0x70)]
        [DataRow((byte)0x25, (Byte)0xFF, (Byte)0x00, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0x25, (Byte)0x00, (Byte)0x40)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x25, (Byte)0x25, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0xFF, (Byte)0xFE, (Byte)0x00, (Byte)0x40)]
        [DataRow((byte)0xFE, (Byte)0xFF, (Byte)0x00, (Byte)0x70)]

        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x25, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0x25, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x25, (Byte)0x25, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0xFE, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("Op Code 0xB8 - Compare A with B")]
        public void decodeAndExecute_cpAB(byte a, byte b, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xB8;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setB(b);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion




        #endregion


    }
}

public static class AssertExtensions
{
    public static void AreEqual(this Assert assert, int expected, int actual, String format = "", String message = "")
    {
        if (message == "")
        {
            message = $"Mismatch Found: ({expected}, {actual})";
        }

        Assert.AreEqual(expected, actual, message);
        format = format.Trim();
        // Successful Exception
        String expectedStr = $"{((format.StartsWith("X")) ? "0x" : "")}{expected.ToString(format)}";
        String actualStr = $"{((format.StartsWith("X")) ? "0x" : "")}{actual.ToString(format)}";

        Console.WriteLine($"Match Found: ({expectedStr}, {actualStr})");
    }

    public static void FlagsEqual(this Assert assert, CPU cpu, bool halfCarrySet, bool carrySet, bool subSet, bool zeroSet, String message = "")
    {
        Byte flagsByte = 0x00;

        if (halfCarrySet)
        {
            flagsByte = (Byte)(flagsByte | (Byte)CPU.CPUFlagsMask.HalfCarry);
        }
        if (carrySet)
        {
            flagsByte = (Byte)(flagsByte | (Byte)CPU.CPUFlagsMask.Carry);
        }
        if (zeroSet)
        {
            flagsByte = (Byte)(flagsByte | (Byte)CPU.CPUFlagsMask.Zero);
        }
        if (subSet)
        {
            flagsByte = (Byte)(flagsByte | (Byte)CPU.CPUFlagsMask.Subtract);
        }

        Assert.That.FlagsEqual(cpu, flagsByte, message: message);
    }

    public static void FlagsEqual(this Assert assert, CPU cpu, Byte flags, String message = "")
    {
        Console.WriteLine($"Checking Flags State");
        String expectedStr = $"{cpu.generateFlagsStr(flags)}";
        String actualStr = $"{cpu.generateFlagsStr()}";

        try
        {
            if (message == "")
            {
                message = $"Mismatch Found: ({expectedStr}, {actualStr})";
            }

            Assert.AreEqual(flags, cpu.getF(), message);
            Console.WriteLine($"Match Found: ({expectedStr}, {actualStr})");
        }
        catch
        {
            Console.WriteLine($"Mismatch Found: ({expectedStr}, {actualStr})");
            throw;
        }

    }
}
