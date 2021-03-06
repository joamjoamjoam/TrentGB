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
            Assert.That.AreEqual(opCode, cpu.mem.getByte(cpu, 0x100), "X2");
            logMessage("Checking Loaded Param 1");
            Assert.That.AreEqual(param1, cpu.mem.getByte(cpu, 0x101), "X2");
            logMessage("Checking Loaded Param 2");
            Assert.That.AreEqual(param2, cpu.mem.getByte(cpu, 0x102), "X2");

            logMessage($"\nStarting Test: {testName}");

            return cpu;
        }

        public void executeCurrentInstruction(CPU cpu, bool useCycleAccuraccyTest = true)
        {
            Byte opCode = cpu.peek();
            Byte cbOpcode = 0x00;
            if (opCode == 0xCB)
            {
                cbOpcode = cpu.peek(1);
            }
            cpu.decodeAndExecute();
            while (cpu.getCurrentInstruction() != null)
            {
                cpu.decodeAndExecute();
            }
            assertInstructionFinished(cpu, opCode, cbOpcode, useCycleAccuraccyTest);
        }

        public void assertInstructionFinished(CPU cpu, byte opCode, byte cbOpcode = 0x00, bool useCycleAccuraccyTest = true)
        {
            Assert.IsNotNull(cpu.getLastExecutedInstruction(), $"Command Is not Finished {((cpu.getCurrentInstruction() != null) ? cpu.getCurrentInstruction().ToString() : "")}");
            Assert.IsNull(cpu.getCurrentInstruction(), message: $"Current command is still Executing {((cpu.getCurrentInstruction() != null) ? cpu.getCurrentInstruction().ToString() : "")}");
            if (useCycleAccuraccyTest)
            {
                Assert.IsTrue(cpu.getLastExecutedInstruction().isCompleted(), message: $"Command is Not Completed or is not Cycle Accurate {cpu.getLastExecutedInstruction().ToString()}");
            }
            else
            {
                logMessage($"Cycle Accuraccy Test Skipped. Expected Cycles {cpu.getLastExecutedInstruction().getExpectedCycles()}, Actual Cycles {cpu.getLastExecutedInstruction().getCycleCount()}");
            }
            
            logMessage("Checking that Scheduled Opcode is same as Executed OpCode");
            Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().opCode, "X2", message: $"Last Executed Opcode is not a match in Op {cpu.getLastExecutedInstruction().ToString()}");
            Assert.That.AreEqual(cpu.getLastExecutedInstruction().length, cpu.getLastExecutedInstruction().getFetchCount(), "", message: $"Fetches Do not Match Length in last executed Operation");
            if (opCode == 0xCB)
            {
                Assert.That.AreEqual(cbOpcode, cpu.getLastExecutedInstruction().parameters[0], "X2", message: $"CB Opcode Did not Match in Instruction {cpu.getLastExecutedInstruction().ToString()}");
            }
        }

        public void fetchAndLoadInstruction(CPU cpu, byte opCode)
        {
            Assert.IsNull(cpu.getCurrentInstruction());
            logMessage($"Starting Execution of Instruction {cpu.getInstructionForOpCode(opCode).ToString()}");
            tick(cpu);
            if (cpu.getInstructionForOpCode(opCode).getExpectedCycles() == 4)
            {
                Assert.IsNull(cpu.getCurrentInstruction());
                logMessage("Check fetched Instruction is correct.");
                Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().opCode, "X2", "Fetched opcode is Not Correct");
            }
            else
            {
                Assert.IsNotNull(cpu.getCurrentInstruction());
                logMessage("Check fetched Instruction is correct.");
                Assert.That.AreEqual(opCode, cpu.getCurrentInstruction().opCode, "X2", "Fetched Opcode is Not Correct");
            }

        }

        public void tick(CPU cpu)
        {
            int cycle = 0;
            int nextCycle = 0;
            if (cpu.getCurrentInstruction() != null)
            {
                cycle = cpu.getCurrentInstruction().getCycleCount();
                nextCycle = cycle + 4;
            }
            else
            {
                nextCycle = 4;
            }
            logMessage($"CPU State: Cycle {cycle}\n{cpu.ToString()}");
            logMessage($"Executing Cycle: {nextCycle}");
            
            CPU.ExecutionStatus rv = cpu.decodeAndExecute();

            Assert.AreNotEqual(rv.ToString(), CPU.ExecutionStatus.ErrorOpFinishedWithMismatchedCycles.ToString(), "Recieved an Error When Executing decode and Execute");

            if (cpu.getCurrentInstruction() != null)
            {
                cycle = cpu.getCurrentInstruction().getCycleCount();
                Assert.That.AreEqual(nextCycle, cycle, "", "Expected Cycle Counts did not match after tick.");
            }
            else
            {
                Assert.IsNull(cpu.getCurrentInstruction(), "Command was not finished when expected");
                Assert.IsNotNull(cpu.getLastExecutedInstruction(), "Command Finished but was not loaded into History");
                Assert.IsTrue(cpu.getLastExecutedInstruction().isCompleted(), $"Command Loaded into History but was not completed. Cycles {cpu.getLastExecutedInstruction().getCycleCount()}/{cpu.getLastExecutedInstruction().getExpectedCycles()}");
                Assert.That.AreEqual(cpu.getLastExecutedInstruction().getExpectedCycles(), cpu.getLastExecutedInstruction().getCycleCount(), "", $"Expected Cycle Count did not Match Executed Cycles for Last Executed Instruction {cpu.getLastExecutedInstruction()}");
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

        [DataTestMethod]
        [DataRow((ushort)0x0000u, (SByte)1, (ushort) 0x0001, (Byte)0x00)]
        [TestCategory("Misc Functions")]
        [TestCategory("16 Bit Math Functions")]
        [TestCategory("addSP")]
        public void addSP_UnsignedByte_NoCarry_Test(ushort sp, sbyte offset, ushort result, Byte flags)
        {
            byte opCode = 0x00;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(0);
            cpu.setSP(sp);
            logMessage("Check Stack Pointer and Flags");
            Assert.That.AreEqual(0, cpu.getF(), "X2");
            Assert.That.AreEqual(sp, cpu.getSP(), "X4");

            logMessage($"Check Result of {cpu.getSP()} + {offset}");
            Assert.That.AreEqual(result, cpu.addSP(cpu.getSP(), offset));

            Assert.That.FlagsEqual(cpu, flags);

        }

        #endregion

        #region CPU Data Tests



        [TestMethod]
        [Timeout(20000)]
        [TestCategory("CPU Data Tests")]
        public void getStateDict()
        {
            CPU cpu = setupOpCode(0x00, MethodBase.GetCurrentMethod().Name);
            Dictionary<String, String> snap = cpu.getStateDict();
            Assert.IsNotNull(snap);
        }


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
                    executeCurrentInstruction(cpu, useCycleAccuraccyTest: false);
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
                    executeCurrentInstruction(cpu, useCycleAccuraccyTest:false);
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

            executeCurrentInstruction(cpu, useCycleAccuraccyTest: false);
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
                    // Check Number of Cycles
                    Assert.That.AreEqual(cpu.getLastExecutedInstruction().getExpectedCycles(), cpu.getLastExecutedInstruction().getCycleCount());
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
        public void decodeAndExecute_Cycles_CBCodes_Test(Byte opCode)
        {
            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            logMessage($"Testing CB Prefixed OpCode 0x{opCode.ToString("X2")} - {cpu.getInstructionForOpCode(opCode).desc}");

            executeCurrentInstruction(cpu);

            // Check Number of Cycles
            Assert.That.AreEqual(0xCB, cpu.getLastExecutedInstruction().opCode, "X2", message:"OpCode was not 0xCB");
            Assert.That.AreEqual(opCode, cpu.getLastExecutedInstruction().parameters[0], "X2", "CB Prefixed Opcode was Incorrect");
            Assert.That.AreEqual(cpu.getLastExecutedInstruction().getExpectedCycles(), cpu.getLastExecutedInstruction().getCycleCount(), "", "Actual Cycles did match Model Cycles");
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
            cpu.mem.setByte(cpu, address, 0x00);
            cpu.setBC(address);
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(0x00, cpu.mem.getByte(cpu, cpu.getBC()));
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getA());
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu, cpu.getBC()));
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
            cpu.mem.setByte(cpu, address, op1);
            cpu.setBC(address);
            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(0x00, cpu.getA());
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getA());
            Assert.That.AreEqual(address, cpu.getBC());
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu, cpu.getBC()));


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

        #region 0x18 - Jump PC + N
        [DataRow((ushort)0x0100, (Byte)0x00, (ushort)0x0102)]
        [DataRow((ushort)0x0100, (Byte)0x01, (ushort)0x0103)]
        [DataRow((ushort)0x0100, (Byte)0x10, (ushort)0x0112)]
        [DataRow((ushort)0x0100, (Byte)0xFF, (ushort)0x0101)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x18 - Jump PC + N")]
        public void decodeAndExecute_jumpRelN(ushort pc, byte jump, ushort nextAddr)
        {
            byte opCode = 0x18;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, jump);
            cpu.setPC(pc);
            cpu.setF(0xF0);

            fetchAndLoadInstruction(cpu, opCode);
            tick(cpu);
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(nextAddr, cpu.getPC(), "X4", "PC was not at the correct address after Jump");

            Assert.That.FlagsEqual(cpu, 0xF0);

        }
        #endregion

        #region 0x27 - DAA
        [DataRow((byte)0x00, (Byte)0x00, (byte)0x00, (byte)0x80)]
        [DataRow((byte)0x0F, (Byte)0x15, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x10, (Byte)0x10, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x1F, (Byte)0x25, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x7F, (Byte)0x85, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x80, (Byte)0x80, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0xF0, (Byte)0x50, (byte)0x00, (byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0x65, (byte)0x00, (byte)0x10)]
        [DataRow((byte)0x02, (Byte)0x02, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x04, (Byte)0x04, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x08, (Byte)0x08, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x20, (Byte)0x20, (byte)0x00, (byte)0x00)]
        [DataRow((byte)0x40, (Byte)0x40, (byte)0x00, (byte)0x00)]

        [DataRow((byte)0x00, (Byte)0x60, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x01, (Byte)0x61, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x0F, (Byte)0x75, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x10, (Byte)0x70, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x1F, (Byte)0x85, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x7F, (Byte)0xE5, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x80, (Byte)0xE0, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0xF0, (Byte)0x50, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0x65, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x02, (Byte)0x62, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x04, (Byte)0x64, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x08, (Byte)0x68, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x20, (Byte)0x80, (byte)0x10, (byte)0x10)]
        [DataRow((byte)0x40, (Byte)0xA0, (byte)0x10, (byte)0x10)]

        [DataRow((byte)0x00, (Byte)0xFA, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFB, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x0F, (Byte)0x09, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x10, (Byte)0x0A, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x1F, (Byte)0x19, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x7F, (Byte)0x79, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x80, (Byte)0x7A, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0xF0, (Byte)0xEA, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xF9, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x02, (Byte)0xFC, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x04, (Byte)0xFE, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x08, (Byte)0x02, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x20, (Byte)0x1A, (byte)0xE0, (byte)0x40)]
        [DataRow((byte)0x40, (Byte)0x3A, (byte)0xE0, (byte)0x40)]

        [DataRow((byte)0x00, (Byte)0x9A, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x01, (Byte)0x9B, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x0F, (Byte)0xA9, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x10, (Byte)0xAA, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x1F, (Byte)0xB9, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x7F, (Byte)0x19, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x80, (Byte)0x1A, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0xF0, (Byte)0x8A, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0xFF, (Byte)0x99, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x02, (Byte)0x9C, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x04, (Byte)0x9E, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x08, (Byte)0xA2, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x20, (Byte)0xBA, (byte)0xF0, (byte)0x50)]
        [DataRow((byte)0x40, (Byte)0xDA, (byte)0xF0, (byte)0x50)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x27 DAA")]
        public void decodeAndExecute_daa(byte a, byte result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0x27;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x80 - Add B to A
        //[DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x30)]
        [DataRow((byte)0xF0, (Byte)0xFF, (Byte)0xEF, (Byte)0xF0, (Byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xB0)]
        [DataRow((byte)0xF0, (Byte)0x0F, (Byte)0xFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x0F, (Byte)0x01, (Byte)0x10, (Byte)0xF0, (Byte)0x20)]
        [DataRow((byte)0x01, (Byte)0x0F, (Byte)0x10, (Byte)0xF0, (Byte)0x20)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x80 - Add B to A")]
        public void decodeAndExecute_addBToA(byte a, byte b, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x80;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setB(b);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x88 - Add Carry B to A
        //[DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x30)]
        [DataRow((byte)0xF0, (Byte)0xFE, (Byte)0xEF, (Byte)0xF0, (Byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xB0)]
        [DataRow((byte)0xF0, (Byte)0x0E, (Byte)0xFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x0F, (Byte)0x00, (Byte)0x10, (Byte)0xF0, (Byte)0x20)]
        [DataRow((byte)0x01, (Byte)0x0E, (Byte)0x10, (Byte)0xF0, (Byte)0x20)]

        [DataRow((byte)0xF0, (Byte)0xFF, (Byte)0xEF, (Byte)0x00, (Byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0x01, (Byte)0x00, (Byte)0x00, (Byte)0xB0)]
        [DataRow((byte)0xF0, (Byte)0x0F, (Byte)0xFF, (Byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x0F, (Byte)0x01, (Byte)0x10, (Byte)0x00, (Byte)0x20)]
        [DataRow((byte)0x01, (Byte)0x0F, (Byte)0x10, (Byte)0x00, (Byte)0x20)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x88 - Add Carry B to A")]
        public void decodeAndExecute_addCarryBToA(byte a, byte b, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x88;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setB(b);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x90 - Subtract B from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x90 - Subtract B from A")]
        public void decodeAndExecute_subBFromA(byte b, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x90;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setB(b);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x91 - Subtract C from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x91 - Subtract C from A")]
        public void decodeAndExecute_subCFromA(byte c, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x91;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setC(c);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x92 - Subtract D from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x92 - Subtract D from A")]
        public void decodeAndExecute_subDFromA(byte d, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x92;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setD(d);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x93 - Subtract E from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x93 - Subtract E from A")]
        public void decodeAndExecute_subEFromA(byte e, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x93;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setE(e);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x94 - Subtract H from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x93 - Subtract H from A")]
        public void decodeAndExecute_subHFromA(byte h, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x94;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setH(h);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x95 - Subtract L from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x95 - Subtract L from A")]
        public void decodeAndExecute_subLFromA(byte l, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x95;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setL(l);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x96 - Subtract Mem HL from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x96 - Subtract Mem HL from A")]
        public void decodeAndExecute_subMemHLFromA(byte hl, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x96;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setHL(0xC000);
            cpu.mem.setByte(cpu, 0xC000, hl); 

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(hl, cpu.mem.getByte(cpu, 0xC000), "X2", "Byte was modified when it shouldnt be");
            Assert.That.AreEqual(a, cpu.getA(), "X2", "A was modified when it shouldnt be");
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xD6 - Subtract N from A
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xD6 - Subtract N from A")]
        public void decodeAndExecute_subNFromA(byte n, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0xD6;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, n);
            cpu.setF(initialFlags);
            cpu.setA(a);

            fetchAndLoadInstruction(cpu, opCode);
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x98 - Subtract Carry B from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x98 - Subtract Carry B from A")]
        public void decodeAndExecute_subCarryBFromA(byte b, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x98;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setB(b);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x99 - Subtract Carry C from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x99 - Subtract Carry C from A")]
        public void decodeAndExecute_subCarryCFromA(byte c, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x99;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setC(c);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x9A - Subtract Carry D from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x9A - Subtract Carry D from A")]
        public void decodeAndExecute_subCarryDFromA(byte d, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x9A;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setD(d);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x9B - Subtract Carry E from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x9B - Subtract Carry E from A")]
        public void decodeAndExecute_subCarryEFromA(byte e, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x9B;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setE(e);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x9C - Subtract Carry H from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x9C - Subtract Carry H from A")]
        public void decodeAndExecute_subCarryHFromA(byte h, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x9C;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setH(h);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x9D - Subtract Carry L from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x9D - Subtract Carry L from A")]
        public void decodeAndExecute_subCarryLFromA(byte l, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x9D;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setL(l);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0x9E - Subtract Carry Mem HL from A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFE, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xFF, (Byte)0xE0, (Byte)0x70)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFD, (Byte)0xF0, (Byte)0x40)]
        [DataRow((byte)0x01, (Byte)0xFF, (Byte)0xFE, (Byte)0xE0, (Byte)0x40)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xF0, (Byte)0x70)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0E, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x01, (Byte)0x10, (Byte)0x0F, (Byte)0xE0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x10, (Byte)0x0F, (Byte)0xF0, (Byte)0x60)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xE0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x01, (Byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x9E - Subtract Carry Mem HL from A")]
        public void decodeAndExecute_subCarryMemHLFromA(byte hl, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0x9E;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setHL(0xC000);
            cpu.mem.setByte(cpu, 0xC000, hl);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(hl, cpu.mem.getByte(cpu, 0xC000), "X2", "Byte was modified when it shouldnt be");
            Assert.That.AreEqual(a, cpu.getA(), "X2", "A was modified when it shouldnt be");
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

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
        [TestCategory("OP Code 0xB8 - Compare A with B")]
        public void decodeAndExecute_cpAB(byte a, byte b, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xB8;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setB(b);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0xB9 - Compare A with C
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
        [TestCategory("OP Code 0xB9 - Compare A with C")]
        public void decodeAndExecute_cpAC(byte a, byte c, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xB9;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setC(c);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0xBA - Compare A with D
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
        [TestCategory("OP Code 0xBA - Compare A with D")]
        public void decodeAndExecute_cpAD(byte a, byte d, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xBA;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setD(d);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0xBB - Compare A with E
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
        [TestCategory("OP Code 0xBB - Compare A with E")]
        public void decodeAndExecute_cpAE(byte a, byte e, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xBB;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setE(e);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0xBC - Compare A with H
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
        [TestCategory("OP Code 0xBC - Compare A with H")]
        public void decodeAndExecute_cpAH(byte a, byte h, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xBC;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setH(h);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0xBD - Compare A with L
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
        [TestCategory("OP Code 0xBD - Compare A with L")]
        public void decodeAndExecute_cpAL(byte a, byte l, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xBD;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);
            cpu.setL(l);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0xBE - Compare Mem HL from A
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
        [TestCategory("OP Code 0xBE - Compare Mem HL from A")]
        public void decodeAndExecute_cmpMemHLToA(byte a, byte hl, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0xBE;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initialFlags);
            cpu.setA(a);
            cpu.setHL(0xC000);
            cpu.mem.setByte(cpu, 0xC000, hl);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(hl, cpu.mem.getByte(cpu, 0xC000), "X2", "Byte was modified when it shouldnt be");
            Assert.That.AreEqual(a, cpu.getA(), "X2", "A was modified when it shouldnt be");
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xBF - Compare A with A
        [DataRow((byte)0x01, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x25, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0xFF, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0x25, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0xFF, (Byte)0x00, (Byte)0xC0)]
        [DataRow((byte)0xFE, (Byte)0x00, (Byte)0xC0)]

        [DataRow((byte)0x01, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x25, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0xFF, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x00, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0x25, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0xFF, (Byte)0xF0, (Byte)0xC0)]
        [DataRow((byte)0xFE, (Byte)0xF0, (Byte)0xC0)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xBF - Compare A with A")]
        public void decodeAndExecute_cpAA(byte a, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xBF;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(initFlags);
            cpu.setA(a);

            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        #region 0x33 - Increment SP
        [DataRow((ushort)0xDFFF, (ushort)0xE000)]
        [DataRow((ushort)0xC000, (ushort)0xC001)]
        [DataRow((ushort)0x0000, (ushort)0x0001)]
        [DataRow((ushort)0xFFFF, (ushort)0x0000)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x33 - Increment SP")]
        public void decodeAndExecute_incSP(ushort initSP, ushort result)
        {
            byte opCode = 0x33;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setSP(initSP);
            cpu.setF(0xF0);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Incremented Value was not calculated correctly");
            Assert.That.AreEqual(((initSP & 0xFF00) + CPU.getByteInUInt16(CPU.BytePlacement.LSB, cpu.getCurrentInstruction().storage)), cpu.getSP(), "X4", "SP LSB was not set correctly");
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getSP(), "X4", "SP MSB was not set correctly");
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, 0xF0);

        }
        #endregion

        #region 0x3B - Decrement SP
        [DataRow((ushort)0xE000, (ushort)0xDFFF)]
        [DataRow((ushort)0xC001, (ushort)0xC000)]
        [DataRow((ushort)0x0001, (ushort)0x0000)]
        [DataRow((ushort)0x0000, (ushort)0xFFFF)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x3B - Decrement SP")]
        public void decodeAndExecute_decSP(ushort initSP, ushort result)
        {
            byte opCode = 0x3B;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setSP(initSP);
            cpu.setF(0xF0);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Incremented Value was not calculated correctly");
            Assert.That.AreEqual(((initSP & 0xFF00) + CPU.getByteInUInt16(CPU.BytePlacement.LSB, cpu.getCurrentInstruction().storage)), cpu.getSP(), "X4", "SP LSB was not set correctly");
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getSP(), "X4", "SP MSB was not set correctly");
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, 0xF0);

        }
        #endregion

        #region 0x39 - Add SP to HL
        [DataRow((ushort)0x0FFF, (ushort)0x0001, (ushort)0x1000, (Byte)0xF0, (Byte)0xA0)]
        [DataRow((ushort)0xFFFF, (ushort)0x0001, (ushort)0x0000, (Byte)0xF0, (Byte)0xB0)]
        [DataRow((ushort)0x0100, (ushort)0x0100, (ushort)0x0200, (Byte)0xF0, (Byte)0x80)]
        [DataRow((ushort)0x6FFF, (ushort)0x0001, (ushort)0x7000, (Byte)0xF0, (Byte)0xA0)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0x39 - Add SP to HL")]
        public void decodeAndExecute_addSPToHL(ushort sp, ushort hl, ushort result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0x39;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setSP(sp);
            cpu.setHL(hl);
            cpu.setF(initFlags);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Value was not calculated correctly");
            Assert.That.AreEqual(CPU.getByteInUInt16(CPU.BytePlacement.LSB, result), cpu.getL(), "X4", "L was not calculated correctly");
            tick(cpu);
            Assert.That.AreEqual(CPU.getByteInUInt16(CPU.BytePlacement.MSB,result), cpu.getH(), "X4", "H was not calculated correctly");
            Assert.That.AreEqual(result, cpu.getHL(), "X4", "Result was not calculated correctly");
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xC9 - Ret
        [DataRow((byte)0x00, (byte)0xC0, (ushort)0xC000)]
        [DataRow((byte)0xFF, (byte)0xAF, (ushort)0xAFFF)]
        [DataRow((byte)0x13, (byte)0xA0, (ushort)0xA013)]

        [DataTestMethod]
        [Timeout(5000)]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xC9 - Ret")]
        public void decodeAndExecute_ret(byte lsb, byte msb, ushort result)
        {
            byte opCode = 0xCD;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, lsb, msb);
            cpu.setSP(0xDFFF);
            cpu.setF(0xF0);

            byte retOpCode = 0xC9;

            cpu.mem.setByte(cpu, result, retOpCode);

            executeCurrentInstruction(cpu); // Execute a Call to result
            Assert.That.AreEqual(result, cpu.getPC(), "X4", "Call failed to set PC correctly");

            fetchAndLoadInstruction(cpu, retOpCode);
            tick(cpu);
            Assert.That.AreEqual(0x03, cpu.getCurrentInstruction().storage);
            tick(cpu);
            Assert.That.AreEqual(0x0103, cpu.getCurrentInstruction().storage);
            tick(cpu);
            Assert.That.AreEqual(0x0103, cpu.getPC());
            Assert.That.FlagsEqual(cpu, 0xF0);

        }
        #endregion

        #region 0xCD - Call NN
        [DataRow((byte)0x00, (byte)0xC0, (ushort)0xC000)]
        [DataRow((byte)0xFF, (byte)0xAF, (ushort)0xAFFF)]
        [DataRow((byte)0x13, (byte)0x02, (ushort)0x0213)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCD - Call NN")]
        public void decodeAndExecute_callNN(byte lsb, byte msb, ushort result)
        {
            byte opCode = 0xCD;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, lsb, msb);
            cpu.setSP(0xDFFF);
            cpu.setF(0xF0);

            fetchAndLoadInstruction(cpu, opCode);
            tick(cpu);
            Assert.That.AreEqual(lsb, cpu.getCurrentInstruction().storage, "X4", "Didnt load LSB correctly");
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Didnt load MSB correctly");
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Storage modified when it shouldnt be");
            tick(cpu);
            Assert.That.AreEqual(0xDFFE, cpu.getSP(), "X4", "Error Decrementing Stack");
            Assert.That.AreEqual(0x01, cpu.mem.getByte(cpu, 0xDFFE), "X4", "Error Loading MSB into Stack");
            tick(cpu);
            Assert.That.AreEqual(0xDFFD, cpu.getSP(), "X4", "Error Loading LSB into Stack");
            Assert.That.AreEqual(0x03, cpu.mem.getByte(cpu, 0xDFFD), "X4", "Error Decrementing Stack");
            Assert.That.AreEqual(result, cpu.getPC(), "X4", "PC not set correctly after call");
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, 0xF0);

        }
        #endregion

        #region 0xE9 - Jump to HL
        [DataRow((ushort)0xFFFF)]
        [DataRow((ushort)0xC100)]
        [DataRow((ushort)0xFF00)]
        [DataRow((ushort)0xC0FF)]
        [DataRow((ushort)0xCFFF)]
        [DataRow((ushort)0xCFF0)]
        [DataRow((ushort)0xC234)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xE9 - Jump HL")]
        public void decodeAndExecute_jumpHL(ushort op1)
        {
            byte opCode = 0xE9;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setF(0xF0);
            cpu.mem.setByte(cpu, op1, 0x12);
            Byte flagsByte = cpu.getF();
            cpu.setHL(op1);

            Assert.That.AreEqual(0x100, cpu.getPC());
            Assert.That.AreEqual(0x12, cpu.mem.getByte(cpu, op1));
            fetchAndLoadInstruction(cpu, opCode);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(op1, cpu.getHL());
            Assert.That.AreEqual(op1, cpu.getPC());
            Assert.That.AreEqual(0x12, cpu.mem.getByte(cpu, op1));
            
            Assert.That.FlagsEqual(cpu, flagsByte);
        }
        #endregion

        #region 0xEE - Xor A ^ N
        [DataRow((byte)0xFF, (Byte)0x56, (Byte)0xA9, (Byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0xFF, (Byte)0xFF, (Byte)0x00, (Byte)0xF0, (Byte)0x80)]
        [DataRow((byte)0xF0, (Byte)0x0F, (Byte)0xFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x00, (Byte)0x00, (Byte)0x00, (Byte)0xF0, (Byte)0x80)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xEE - Xor A ^ N")]
        public void decodeAndExecute_xorNA(byte n, byte a, Byte result, byte initialFlags, byte expectedFlags)
        {
            byte opCode = 0xEE;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, n);
            cpu.setF(initialFlags);
            cpu.setA(a);

            fetchAndLoadInstruction(cpu, opCode);
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xF9 - Load SP With HL
        [DataRow((ushort)0xE000, (ushort)0xDFFF, (ushort)0xDFFF)]
        [DataRow((ushort)0xE000, (ushort)0x0000, (ushort)0x0000)]
        [DataRow((ushort)0xE000, (ushort)0xFFFF, (ushort)0xFFFF)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xF9 - Load SP With HL")]
        public void decodeAndExecute_ldSPFromHL(ushort initSP, ushort hl, ushort result)
        {
            byte opCode = 0xF9;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name);
            cpu.setSP(initSP);
            cpu.setHL(hl);
            cpu.setF(0xF0);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(hl, cpu.getCurrentInstruction().storage, "X4", "HL was not loaded correctly");
            Assert.That.AreEqual(((initSP & 0xFF00) + CPU.getByteInUInt16(CPU.BytePlacement.LSB, cpu.getCurrentInstruction().storage)), cpu.getSP(), "X4", "SP LSB was not set correctly");
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getSP(), "X4", "SP MSB was not set correctly");
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, 0xF0);

        }
        #endregion

        #region 0xE8 - Add N to SP
        [DataRow((ushort)0x0FFF, (SByte)0x01, (ushort)0x1000, (Byte)0xF0, (Byte)0x30)]
        [DataRow((ushort)0xFFFF, (SByte)0x01, (ushort)0x0000, (Byte)0xF0, (Byte)0x30)]
        [DataRow((ushort)0x0100, (SByte)0x10, (ushort)0x0110, (Byte)0xF0, (Byte)0x00)]
        [DataRow((ushort)0xFF0F, (SByte)0x01, (ushort)0xFF10, (Byte)0xF0, (Byte)0x20)]
        [DataRow((ushort)0x6FFF, (SByte)0x01, (ushort)0x7000, (Byte)0xF0, (Byte)0x30)]

        [DataRow((ushort)0x1000, (SByte)(-1), (ushort)0x0FFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((ushort)0x0000, (SByte)(-1), (ushort)0xFFFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((ushort)0x0110, (SByte)(-0x10), (ushort)0x0100, (Byte)0xF0, (Byte)0x10)]
        [DataRow((ushort)0xFF10, (SByte)(-1), (ushort)0xFF0F, (Byte)0xF0, (Byte)0x10)]
        [DataRow((ushort)0x7000, (SByte)(-1), (ushort)0x6FFF, (Byte)0xF0, (Byte)0x00)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xE8 - Add N to SP")]
        public void decodeAndExecute_addNtoSP(ushort sp, SByte n, ushort result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0xE8;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, (Byte)n);
            cpu.setSP(sp);
            cpu.setF(initFlags);

            fetchAndLoadInstruction(cpu, opCode);
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Result was not Calculated correctly");
            tick(cpu);
            Assert.That.AreEqual(((sp & 0xFF00) + CPU.getByteInUInt16(CPU.BytePlacement.LSB, cpu.getCurrentInstruction().storage)), cpu.getSP(), "X4", "SP LSB was not set correctly");
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getSP(), "X4", "SP MSB was not set correctly");
            assertInstructionFinished(cpu, opCode);

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xF8 - Load HL from SP Plus N
        [DataRow((ushort)0x0FFF, (SByte)0x01, (ushort)0x1000, (Byte)0xF0, (Byte)0x30)]
        [DataRow((ushort)0xFFFF, (SByte)0x01, (ushort)0x0000, (Byte)0xF0, (Byte)0x30)]
        [DataRow((ushort)0x0100, (SByte)0x10, (ushort)0x0110, (Byte)0xF0, (Byte)0x00)]
        [DataRow((ushort)0xFF0F, (SByte)0x01, (ushort)0xFF10, (Byte)0xF0, (Byte)0x20)]
        [DataRow((ushort)0x6FFF, (SByte)0x01, (ushort)0x7000, (Byte)0xF0, (Byte)0x30)]

        [DataRow((ushort)0x1000, (SByte)(-1), (ushort)0x0FFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((ushort)0x0000, (SByte)(-1), (ushort)0xFFFF, (Byte)0xF0, (Byte)0x00)]
        [DataRow((ushort)0x0110, (SByte)(-0x10), (ushort)0x0100, (Byte)0xF0, (Byte)0x10)]
        [DataRow((ushort)0xFF10, (SByte)(-1), (ushort)0xFF0F, (Byte)0xF0, (Byte)0x10)]
        [DataRow((ushort)0x7000, (SByte)(-1), (ushort)0x6FFF, (Byte)0xF0, (Byte)0x00)]

        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xF8 - Load HL from SP Plus N")]
        public void decodeAndExecute_laodHLSPPlusN(ushort sp, SByte n, ushort result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0xF8;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, (Byte) n);
            cpu.setSP(sp);
            cpu.setHL(0x0000);
            cpu.setF(initFlags);

            fetchAndLoadInstruction(cpu, opCode);
            tick(cpu);
            Assert.That.AreEqual(result, cpu.getCurrentInstruction().storage, "X4", "Value was not calculated correctly");
            Assert.That.AreEqual(CPU.getByteInUInt16(CPU.BytePlacement.LSB, result), cpu.getL(), "X4", "L was not calculated correctly");
            tick(cpu);
            assertInstructionFinished(cpu, opCode);
            Assert.That.AreEqual(CPU.getByteInUInt16(CPU.BytePlacement.MSB, result), cpu.getH(), "X4", "H was not calculated correctly");
            Assert.That.AreEqual(result, cpu.getHL(), "X4", "Result was not set in HL correctly");

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xFE - Compare A with N
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
        [TestCategory("OP Code 0xFE - Compare A with N")]
        public void decodeAndExecute_cpAN(byte a, byte n, byte initFlags, byte afterFlags)
        {
            byte opCode = 0xFE;

            CPU cpu = setupOpCode(opCode, MethodBase.GetCurrentMethod().Name, n);
            cpu.setF(initFlags);
            cpu.setA(a);

            fetchAndLoadInstruction(cpu, opCode);
            Assert.That.AreEqual(a, cpu.getA());
            tick(cpu);
            assertInstructionFinished(cpu, opCode);

            Assert.That.AreEqual(a, cpu.getA());

            Assert.That.FlagsEqual(cpu, afterFlags);

        }
        #endregion

        // trent Check IME is Enabled Porperly

        #endregion

        #region CB OPCode Tests

        #region 0xCB 0x00 - Rotate Left Carry B
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
        [TestCategory("OP Code 0xCB 0x00 - Rotate Left Carry B")]
        public void decodeAndExecute_CB_rlcB(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x00;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setB(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

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

        #region 0xCB 0x01 - Rotate Left Carry C
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
        [TestCategory("OP Code 0xCB 0x01 - Rotate Left Carry C")]
        public void decodeAndExecute_CB_rlcC(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x01;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setC(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getC());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getC());

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

        #region 0xCB 0x02 - Rotate Left Carry D
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
        [TestCategory("OP Code 0xCB 0x02 - Rotate Left Carry D")]
        public void decodeAndExecute_CB_rlcD(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x02;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setD(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getD());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getD());

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

        #region 0xCB 0x03 - Rotate Left Carry E
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
        [TestCategory("OP Code 0xCB 0x03 - Rotate Left Carry E")]
        public void decodeAndExecute_CB_rlcE(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x03;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setE(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getE());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getE());

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

        #region 0xCB 0x04 - Rotate Left Carry H
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
        [TestCategory("OP Code 0xCB 0x04 - Rotate Left Carry H")]
        public void decodeAndExecute_CB_rlcH(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x04;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setH(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getH());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getH());

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

        #region 0xCB 0x05 - Rotate Left Carry L
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
        [TestCategory("OP Code 0xCB 0x05 - Rotate Left Carry L")]
        public void decodeAndExecute_CB_rlcL(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x05;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setL(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getL());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getL());

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

        #region 0xCB 0x06 - Rotate Left Carry MemHL
        [DataRow((ushort)0xC000, (byte)0x80, (Byte)0x01, false, true)]
        [DataRow((ushort)0xC000, (byte)0x40, (Byte)0x80, false, false)]
        [DataRow((ushort)0xC000, (byte)0x40, (Byte)0x80, true, false)]
        [DataRow((ushort)0xC000, (byte)0x00, (Byte)0x00, false, false)]
        [DataRow((ushort)0xC000, (byte)0x00, (Byte)0x00, true, false)]
        [DataRow((ushort)0xC000, (byte)0xFF, (Byte)0xFF, false, true)]
        [DataRow((ushort)0xC000, (byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((ushort)0xC000, (byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x06 - Rotate Left Carry Mem HL")]
        public void decodeAndExecute_CB_rlcMemHL(ushort address, byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x06;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.mem.setByte(cpu, address, op1);
            cpu.setHL(address);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            tick(cpu);
            Assert.That.AreEqual(opCode, cpu.getCurrentInstruction().parameters[0]);
            Assert.That.AreEqual(0x0000, cpu.getCurrentInstruction().storage);
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getCurrentInstruction().storage);
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu, address));
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(address, cpu.getHL());
            Assert.That.AreEqual(result, cpu.mem.getByte(cpu, address));

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

        #region 0xCB 0x07 - Rotate Left Carry A
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
        [TestCategory("OP Code 0xCB 0x07 - Rotate Left Carry A")]
        public void decodeAndExecute_CB_rlcA(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x07;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setA(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getA());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
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

        #region 0xCB 0x08 - Rotate Right Carry B
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
        [TestCategory("OP Code 0xCB 0x08 - Rotate Right Carry B")]
        public void decodeAndExecute_CB_rrcB(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x08;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setB(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

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

        #region 0xCB 0x09 - Rotate Right Carry C
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
        [TestCategory("OP Code 0xCB 0x09 - Rotate Right Carry C")]
        public void decodeAndExecute_CB_rrcC(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x09;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setC(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getC());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getC());

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

        #region 0xCB 0x0A - Rotate Right Carry D
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
        [TestCategory("OP Code 0xCB 0x0A - Rotate Right Carry D")]
        public void decodeAndExecute_CB_rrcD(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0A;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setD(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getD());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getD());

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

        #region 0xCB 0x0B - Rotate Right Carry E
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
        [TestCategory("OP Code 0xCB 0x0B - Rotate Right Carry B")]
        public void decodeAndExecute_CB_rrcE(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0B;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setE(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getE());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getE());

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

        #region 0xCB 0x0C - Rotate Right Carry H
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
        [TestCategory("OP Code 0xCB 0x0C - Rotate Right Carry H")]
        public void decodeAndExecute_CB_rrcH(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0C;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setH(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getH());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getH());

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

        #region 0xCB 0x0D - Rotate Right Carry L
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
        [TestCategory("OP Code 0xCB 0x0D - Rotate Right Carry L")]
        public void decodeAndExecute_CB_rrcL(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0D;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setL(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getL());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getL());

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

        #region 0xCB 0x0E - Rotate Right Carry Mem HL
        [DataRow((ushort)0xC000, (byte)0x01, (Byte)0x80, false, true)]
        [DataRow((ushort)0xC000, (byte)0x01, (Byte)0x80, true, true)]
        [DataRow((ushort)0xC000, (byte)0x80, (Byte)0x40, false, false)]
        [DataRow((ushort)0xC000, (byte)0x80, (Byte)0x40, true, false)]
        [DataRow((ushort)0xC000, (byte)0x00, (Byte)0x00, false, false)]
        [DataRow((ushort)0xC000, (byte)0x00, (Byte)0x00, true, false)]
        [DataRow((ushort)0xC000, (byte)0xFF, (Byte)0xFF, false, true)]
        [DataRow((ushort)0xC000, (byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((ushort)0xC000, (byte)0x1E, (Byte)0x0F, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x0E - Rotate Right Carry Mem HL")]
        public void decodeAndExecute_CB_rrcMemHL(ushort address, byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0E;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.mem.setByte(cpu, address, op1);
            cpu.setHL(address);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            tick(cpu);
            Assert.That.AreEqual(opCode, cpu.getCurrentInstruction().parameters[0]);
            Assert.That.AreEqual(0x0000, cpu.getCurrentInstruction().storage);
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getCurrentInstruction().storage);
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu, address));
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(address, cpu.getHL());
            Assert.That.AreEqual(result, cpu.mem.getByte(cpu, address));

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

        #region 0xCB 0x0F - Rotate Right Carry A
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
        [TestCategory("OP Code 0xCB 0x0F - Rotate Right Carry A")]
        public void decodeAndExecute_CB_rrcA(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x0F;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setA(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getA());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
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

        #region 0xCB 0x10 - Rotate Left B
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x10 - Rotate Left B")]
        public void decodeAndExecute_CB_rlB(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x10;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setB(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x11 - Rotate Left C
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x11 - Rotate Left C")]
        public void decodeAndExecute_CB_rlC(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x11;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setC(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getC());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getC());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x12 - Rotate Left D
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x12 - Rotate Left D")]
        public void decodeAndExecute_CB_rlD(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x12;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setD(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getD());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getD());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x13 - Rotate Left E
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x13 - Rotate Left E")]
        public void decodeAndExecute_CB_rlE(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x13;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setE(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getE());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getE());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x14 - Rotate Left H
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x14 - Rotate Left H")]
        public void decodeAndExecute_CB_rlH(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x14;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setH(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getH());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getH());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x15 - Rotate Left L
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x15 - Rotate Left L")]
        public void decodeAndExecute_CB_rlL(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x15;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setL(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getL());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getL());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x16 - Rotate Left Mem HL
        [DataRow((ushort)0xC000, (byte)0x80, (Byte)0x00, false, true)]
        [DataRow((ushort)0xC000, (byte)0x80, (Byte)0x01, true, true)]
        [DataRow((ushort)0xC000, (byte)0x40, (Byte)0x80, false, false)]
        [DataRow((ushort)0xC000, (byte)0x40, (Byte)0x81, true, false)]
        [DataRow((ushort)0xC000, (byte)0x00, (Byte)0x00, false, false)]
        [DataRow((ushort)0xC000, (byte)0x00, (Byte)0x01, true, false)]
        [DataRow((ushort)0xC000, (byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((ushort)0xC000, (byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((ushort)0xC000, (byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x16 - Rotate Left MemHL")]
        public void decodeAndExecute_CB_rlMemHL(ushort address, byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x16;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.mem.setByte(cpu, address, op1);
            cpu.setHL(address);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            tick(cpu);
            Assert.That.AreEqual(opCode, cpu.getCurrentInstruction().parameters[0]);
            Assert.That.AreEqual(0x0000, cpu.getCurrentInstruction().storage);
            tick(cpu);
            Assert.That.AreEqual(op1, cpu.getCurrentInstruction().storage);
            Assert.That.AreEqual(op1, cpu.mem.getByte(cpu, address));
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(address, cpu.getHL());
            Assert.That.AreEqual(result, cpu.mem.getByte(cpu, address));

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

        #region 0xCB 0x17 - Rotate Left A
        [DataRow((byte)0x80, (Byte)0x00, false, true)]
        [DataRow((byte)0x80, (Byte)0x01, true, true)]
        [DataRow((byte)0x40, (Byte)0x80, false, false)]
        [DataRow((byte)0x40, (Byte)0x81, true, false)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x01, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFE, false, true)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0x0F, (Byte)0x1E, false, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x17 - Rotate Left A")]
        public void decodeAndExecute_CB_rlA(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x17;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setA(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getA());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getA());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x18 - Rotate Right B
        [DataRow((byte)0x00, (Byte)0x80, true, false)]
        [DataRow((byte)0x01, (Byte)0x80, true, true)]
        [DataRow((byte)0x80, (Byte)0x40, false, false)]
        [DataRow((byte)0x81, (Byte)0x40, false, true)]
        [DataRow((byte)0x00, (Byte)0x00, false, false)]
        [DataRow((byte)0x00, (Byte)0x80, true, false)]
        [DataRow((byte)0x01, (Byte)0x00, false, true)]
        [DataRow((byte)0xFE, (Byte)0xFF, true, false)]
        [DataRow((byte)0xFF, (Byte)0xFF, true, true)]
        [DataRow((byte)0xFF, (Byte)0x7F, false, true)]
        [DataRow((byte)0x1E, (Byte)0x0F, false, false)]
        [DataRow((byte)0x1E, (Byte)0x8F, true, false)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x18 - Rotate Right B")]
        public void decodeAndExecute_CB_rrB(byte op1, byte result, bool carryState, bool carryAfterState)
        {
            byte opCode = 0x18;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF((Byte)(((carryState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0) | (Byte)CPU.CPUFlagsMask.Subtract | (Byte)CPU.CPUFlagsMask.HalfCarry));
            cpu.setB(op1);

            byte expectedFlags = (Byte)((((carryAfterState) ? (byte)CPU.CPUFlagsMask.Carry : (byte)0)) + (((result == 0) ? (byte)CPU.CPUFlagsMask.Zero : (byte)0)));

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            if (carryState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

            if (carryAfterState)
            {
                Assert.IsTrue(cpu.getCarryFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getCarryFlag());
            }

            if (result == 0)
            {
                Assert.IsTrue(cpu.getZeroFlag());
            }
            else
            {
                Assert.IsFalse(cpu.getZeroFlag());
            }

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x20 - Shift Left B
        [DataRow((byte)0xFF, (Byte)0xFE, (byte)0xF0, (Byte)0x10)]
        [DataRow((byte)0x80, (Byte)0x00, (byte)0xF0, (Byte)0x90)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x20 - Shift Left B")]
        public void decodeAndExecute_CB_slB(byte op1, byte result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0x20;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF(initFlags);
            cpu.setB(op1);

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x28 - SRA B
        [DataRow((byte)0xFF, (Byte)0xFF, (byte)0xF0, (Byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0xFF, (byte)0x00, (Byte)0x10)]
        [DataRow((byte)0x80, (Byte)0xC0, (byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x80, (Byte)0xC0, (byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x81, (Byte)0xC0, (byte)0xF0, (Byte)0x10)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x28 - SRA B")]
        public void decodeAndExecute_CB_sraB(byte op1, byte result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0x28;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF(initFlags);
            cpu.setB(op1);

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x30 - Swap B
        [DataRow((byte)0xF0, (Byte)0x0F, (byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x32, (Byte)0x23, (byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x1F, (Byte)0xF1, (byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x86, (Byte)0x68, (byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x00, (Byte)0x00, (byte)0xF0, (Byte)0x80)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x30 - Swap B")]
        public void decodeAndExecute_CB_swapB(byte op1, byte result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0x30;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF(initFlags);
            cpu.setB(op1);

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion

        #region 0xCB 0x38 - SRL B
        [DataRow((byte)0xFF, (Byte)0x7F, (byte)0xF0, (Byte)0x10)]
        [DataRow((byte)0xFF, (Byte)0x7F, (byte)0x00, (Byte)0x10)]
        [DataRow((byte)0x80, (Byte)0x40, (byte)0x00, (Byte)0x00)]
        [DataRow((byte)0x81, (Byte)0x40, (byte)0x00, (Byte)0x10)]
        [DataRow((byte)0x80, (Byte)0x40, (byte)0xF0, (Byte)0x00)]
        [DataRow((byte)0x81, (Byte)0x40, (byte)0xF0, (Byte)0x10)]
        [DataTestMethod]
        [TestCategory("OP Codes")]
        [TestCategory("OP Code 0xCB 0x38 - SRL B")]
        public void decodeAndExecute_CB_srlB(byte op1, byte result, byte initFlags, byte expectedFlags)
        {
            byte opCode = 0x38;

            CPU cpu = setupOpCode(0xCB, MethodBase.GetCurrentMethod().Name, opCode);
            cpu.setF(initFlags);
            cpu.setB(op1);

            fetchAndLoadInstruction(cpu, 0xCB);
            Assert.That.AreEqual(op1, cpu.getB());
            tick(cpu);
            assertInstructionFinished(cpu, 0xCB, opCode);
            Assert.That.AreEqual(result, cpu.getB());

            Assert.That.FlagsEqual(cpu, expectedFlags);

        }
        #endregion


        #endregion
    }
}

public static class AssertExtensions
{
    public static void AreEqual(this Assert assert, int expected, int actual, String format = "X4", String message = "")
    {
        if (message == "")
        {
            message = $"Mismatch Found: ({expected}, {actual})";
        }
        format = format.Trim();
        String expectedStr = $"{((format.StartsWith("X")) ? "0x" : "")}{expected.ToString(format)}";
        String actualStr = $"{((format.StartsWith("X")) ? "0x" : "")}{actual.ToString(format)}";
        try
        {
            Assert.AreEqual(expected, actual, message);
            
            // Successful

            Console.WriteLine($"Match Found: ({expectedStr}, {actualStr})");
        }
        catch(AssertFailedException e)
        {
            Console.WriteLine($"Match NOT Found: ({expectedStr}, {actualStr})");
            throw;
        }



        
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
