using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace trentGB
{
    // OP Code Dict is Below.

    public class Instruction
    {
        public readonly byte cycles;
        public readonly byte opCode;
        public readonly byte length;
        public int address;
        public byte[] parameters = new byte[0];
        public readonly Action opFunc = null;
        public readonly String desc = "";
        

        public Instruction(byte opCode, byte length, byte cycles , Action act, int PC, ROM rom)
        {
            // PC is at the adress for the first parametr if applicable

            this.opCode = opCode;
            this.cycles = cycles;
            this.length = length;
            this.address = PC;
            this.opFunc = act;
            desc = act.Method.Name;
            
            if (opCode == 0xCB)
            {
                length = getCBOpLength();
                // Append CB Instruction Length and Cycles
                cycles = getCBCycles(rom.getByte(PC));
            }

            parameters = new byte[length - 1];

            for (int i = 0; i < length; i++)
            {
                parameters[i] = rom.getByte(PC + i);
            }
        }

        public Instruction(Instruction model, int PC, ROM rom)
        {
            // PC is at the adress for the first parametr if applicable

            this.opCode = model.opCode;
            this.cycles = model.cycles;
            this.length = model.length;
            this.address = PC;
            this.opFunc = model.opFunc;
            desc = opFunc.Method.Name;
            

            if (opCode == 0xCB && model.length == 1)
            {
                length = getCBOpLength();
                // Append CB Instruction Length and Cycles
                cycles = getCBCycles(rom.getByte(PC));
            }

            parameters = new byte[length - 1];

            for (int i = 0; i < (length-1); i++)
            {
                parameters[i] = rom.getByte(PC + i);
            }
        }

        public Instruction(byte opCode, byte length, byte cycles , Action act)
        {
            // PC is at the adress for the first parametr if applicable

            this.opCode = opCode;
            this.cycles = cycles;
            this.length = length;
            this.opFunc = act;
            desc = act.Method.Name;
        }

        public void execute()
        {
            opFunc.Invoke();
        }

        public new String ToString()
        {
            String paramRealValue = "";

            if (opCode == 0xCB)
            {
                if (length == 3) // 8 bit Operand
                {
                    paramRealValue = $" ({parameters[1].ToString("X2")} = )";
                }
                else if (length == 4) // 16 Bit Operand
                {
                    paramRealValue = $" ({CPU.getUInt16ForBytes(parameters[2], parameters[3]).ToString("X4")})";
                }
            }
            else
            {
                if (length == 2) // 8 bit Operand
                {
                    paramRealValue = $" ({parameters[0].ToString("X2")})";
                }
                else if (length == 3) // 16 Bit Operand
                {
                    paramRealValue = $" ({CPU.getUInt16ForBytes(parameters).ToString("X4")})";
                }
            }

            return $"{address.ToString("X4")}: {desc} {((parameters.Length > 0) ? "0x" : "")}{String.Join(", 0x", parameters.Select(b => b.ToString("X2")))}{paramRealValue}";
        }

        public Byte getCBOpLength()
        {
            return 3;
        }

        public Byte getCBCycles(Byte cbOpCode)
        {
            Byte rv = 8;
            switch (cbOpCode)
            {
                case 0x06:
                case 0x16:
                case 0x26:
                case 0x36:
                case 0x86:
                case 0x96:
                case 0xA6:
                case 0xB6:
                case 0xC6:
                case 0xD6:
                case 0xE6:
                case 0xF6:
                case 0x0E:
                case 0x1E:
                case 0x2E:
                case 0x3E:
                case 0x8E:
                case 0x9E:
                case 0xAE:
                case 0xBE:
                case 0xCE:
                case 0xDE:
                case 0xEE:
                case 0xFE:
                    rv = 16;
                    break;

                case 0x46:
                case 0x56:
                case 0x66:
                case 0x76:
                case 0x4E:
                case 0x5E:
                case 0x6E:
                case 0x7E:
                    rv = 12;
                    break;

                default:
                    rv = 8;
                    break;
            }

            return rv;
        }
    }


    class CPU
    {
        enum Add16Type
        {
            Normal,
            HL
        }


        public enum SpeedMode
        {
            GB,
            CGBSingleSpeed,
            CGBDoubleSpeed
        }

        public enum BitOperationType
        {
            Test,
            Reset,
            Set
        }

        public enum CPUState
        {
            Running,
            Halted,
            Stopped
        }

        public enum InterruptType
        {
            VBlank = 0x01,
            LCDStat = 0x02,
            Timer = 0x04,
            Serial = 0x08,
            Joypad = 0x10
        }


        private Byte A = 0;
        private Byte B = 0;
        private Byte C = 0;
        private Byte D = 0;
        private Byte E = 0;
        private Byte H = 0;
        // Flags Register
        private Byte F = 0;
        private Byte L = 0;

        private ushort PC = 0;
        private ushort SP = 0;
        private bool IME = false;
        private CPUState state = CPUState.Running;

        private AddressSpace mem = null;
        private Dictionary<Byte, Instruction> opCodeTranslationDict = new Dictionary<byte, Instruction>();

        public bool shouldDisableInterrupts = false;
        public bool shouldEnableInterrupts = false;
        private long ticksSinceLastCycle = 0;



        public ROM rom = null;

        // Debug
        Stopwatch clock = null;
        ushort breakAtInstruction = 0x0100;
        CPUDebugger debuggerForm = null;


        public CPU(AddressSpace m, ROM rom, Stopwatch clock)
        {
            mem = m;
            this.rom = rom;
            loadOpCodeMap();
            this.clock = clock;
            debuggerForm = new CPUDebugger(this.rom.disassemble(opCodeTranslationDict));
        }

        public Dictionary<String, String> getStateDict()
        {
            Dictionary<String, String> rv = new Dictionary<string, string>();

            rv.Add("AF", $"{getAF().ToString("X4")}");
            rv.Add("BC", $"{getBC().ToString("X4")}");
            rv.Add("DE", $"{getDE().ToString("X4")}");
            rv.Add("HL", $"{getHL().ToString("X4")}");
            rv.Add("SP", $"{getSP().ToString("X4")}");
            rv.Add("PC", $"{getPC().ToString("X4")}");
            rv.Add("A", $"{getA().ToString("X2")}");
            rv.Add("B", $"{getB().ToString("X2")}");
            rv.Add("C", $"{getC().ToString("X2")}");
            rv.Add("D", $"{getD().ToString("X2")}");
            rv.Add("E", $"{getE().ToString("X2")}");
            rv.Add("H", $"{getH().ToString("X2")}");
            rv.Add("L", $"{getL().ToString("X2")}");
            rv.Add("F", $"{getF().ToString("X2")} ({generateFlagsStr()})");
            rv.Add("(BC)", $"{mem.peekByte(getBC()).ToString("X2")}");
            rv.Add("(DE)", $"{mem.peekByte(getDE()).ToString("X2")}");
            rv.Add("(HL)", $"{mem.peekByte(getHL()).ToString("X2")}");

            Dictionary<String, String> memDict = mem.getState();

            foreach (KeyValuePair<String, String> kp in memDict)
            {
                rv.Add(kp.Key, kp.Value);
            }

            return rv;
        }

        public SpeedMode getSpeedMode()
        {
            // Hardcoded GB
            SpeedMode rv = SpeedMode.CGBSingleSpeed;
            if ((mem.getByte(0xFF4D) & 0x80) > 0)
            {
                rv = SpeedMode.CGBDoubleSpeed;
            }
            else
            {
                rv = SpeedMode.CGBSingleSpeed;
            }

            return rv;
        }

        public String generateFlagsStr()
        {
            String rv = $"{((getZeroFlag()) ? "Z" : "-")}{((getSubtractFlag()) ? "N" : "-")}{((getHalfCarryFlag()) ? "H" : "-")}{((getCarryFlag()) ? "C" : "-")}";

            return rv;
        }

        public String getOpCodeDesc(Byte opCode)
        {
            String rv = $"{opCodeTranslationDict[opCode].desc}";

            for (int i = rv.Length - 1; i < 50; i++)
            {
                rv += " ";
            }

            return rv;
        }

        public new String ToString()
        {
            return $"AF = 0x{getAF().ToString("X4")}, BC = 0x{getBC().ToString("X4")}, DE = 0x{getDE().ToString("X4")}, HL = 0x{getHL().ToString("X4")}, SP = 0x{getSP().ToString("X4")}, PC = 0x{getPC().ToString("X4")}, A = 0x{getA().ToString("X2")}, B = 0x{getB().ToString("X2")}, C = 0x{getC().ToString("X2")}, D = 0x{getD().ToString("X2")}, E = 0x{getE().ToString("X2")}, H = 0x{getH().ToString("X2")}, L = 0x{getL().ToString("X2")}, F = 0x{getF().ToString("X2")} ({generateFlagsStr()}), (BC) = 0x{mem.peekByte(getBC()).ToString("X2")}, (DE) = 0x{mem.peekByte(getDE()).ToString("X2")}, (HL) = 0x{mem.peekByte(getHL()).ToString("X2")}";
        }

        public void tick()
        {
            bool shouldPerformCycle = false;
            ticksSinceLastCycle++;
            if (getSpeedMode() == SpeedMode.CGBDoubleSpeed)
            {
                // Should Cycle evry 2 ticks 2.10 MHz or ~ clocks 4.194/2
                if (ticksSinceLastCycle == 2)
                {
                    shouldPerformCycle = true;
                    ticksSinceLastCycle = 0;
                }
            }
            else
            {
                // Should Cycle every 4 ticks effective cycle rate of 1.04 MHz
                if (ticksSinceLastCycle == 4)
                {
                    shouldPerformCycle = true;
                    ticksSinceLastCycle = 0;
                }
            }

            if (shouldPerformCycle)
            {
                Byte nextInstruction = fetch();
                if (nextInstruction == 0x10)
                {
                    // Halt CPU and LCD

                    // lcd.stop();
                }

                // Check For Interrupts

                if (IME && isInterruptEnabled(InterruptType.VBlank) && isInterruptRequested(InterruptType.VBlank)) 
                {
                    fireInterrupt(InterruptType.VBlank);
                }
                else if (IME && isInterruptEnabled(InterruptType.LCDStat) && isInterruptRequested(InterruptType.LCDStat))
                {
                    fireInterrupt(InterruptType.LCDStat);
                }
                else if (IME && isInterruptEnabled(InterruptType.Timer) && isInterruptRequested(InterruptType.Timer))
                {
                    fireInterrupt(InterruptType.Timer);
                }
                else if (IME && isInterruptEnabled(InterruptType.Serial) && isInterruptRequested(InterruptType.Serial))
                {
                    fireInterrupt(InterruptType.Serial);
                }
                else if (IME && isInterruptEnabled(InterruptType.Joypad) && isInterruptRequested(InterruptType.Joypad))
                {
                    fireInterrupt(InterruptType.Joypad);
                }
                else if (state == CPUState.Halted || state == CPUState.Stopped)
                {
                    // Do Nothing
                }
                else
                {
                    decodeAndExecute(nextInstruction);
                }
            }
        }


        public void reset()
        {
            setAF(0x01B0); // Set this based on ROMs GB Mode. For Now Hardcode for GB A = 0x01
            setBC(0x0013);
            setDE(0x00D8);
            setHL(0x014D);
            setSP(0xFFFE);
            setPC(0x0100); // Skip Internal Boot ROM at 0x0000

            // Set These Faux Registers from HardCodes for now. In the future load these from Inter Boot ROM at 0x0000

            mem.setByte(0xFF05, 0x00); // TIMA
            mem.setByte(0xFF06, 0x00); // TMA
            mem.setByte(0xFF07, 0x00); // TAC
            mem.setByte(0xFF10, 0x80); // NR10
            mem.setByte(0xFF11, 0xBF); // NR11
            mem.setByte(0xFF12, 0xF3); // NR12
            mem.setByte(0xFF14, 0xBF); // NR14
            mem.setByte(0xFF16, 0x3F); // NR21
            mem.setByte(0xFF17, 0x00); // NR22
            mem.setByte(0xFF19, 0xBF); // NR24
            mem.setByte(0xFF1A, 0x7F); // NR30
            mem.setByte(0xFF1B, 0xFF); // NR31
            mem.setByte(0xFF1C, 0x9F); // NR32
            mem.setByte(0xFF1E, 0xBF); // NR33
            mem.setByte(0xFF20, 0xFF); // NR41
            mem.setByte(0xFF21, 0x00); // NR42
            mem.setByte(0xFF22, 0x00); // NR43
            mem.setByte(0xFF23, 0xBF); // NR30
            mem.setByte(0xFF24, 0x77); // NR50
            mem.setByte(0xFF25, 0xF3); // NR51
            mem.setByte(0xFF26, 0xF1); // 0xF1-GB, 0xF0-SGB  NR52
            mem.setByte(0xFF40, 0x91); // LCDC
            mem.setByte(0xFF42, 0x00); // SCY
            mem.setByte(0xFF43, 0x00); // SCX
            mem.setByte(0xFF45, 0x00); // LYC
            mem.setByte(0xFF47, 0xFC); // BGP
            mem.setByte(0xFF48, 0xFF); // OBP0
            mem.setByte(0xFF49, 0xFF); // OBP1
            mem.setByte(0xFF4A, 0x00); // WY
            mem.setByte(0xFF4B, 0x00); // WX
            mem.setByte(0xFFFF, 0x00); // IE
        }

        public Byte fetch()
        {
            return mem.getByte(PC++);
        }

        public void decodeAndExecute(Byte opCode)
        {
            bool enableInterrupts = shouldEnableInterrupts;
            bool disableInterrupts = shouldDisableInterrupts;

            

            // Debug Window
            Byte[] peekBytes = new byte[2];
            peekBytes[0] = mem.peekByte(getPC());
            peekBytes[1] = mem.peekByte((ushort)((getPC() + 1) & 0xFFFF));
            ushort peek16 = getUInt16ForBytes(peekBytes);
            String beforeText = $"Before:\nOP[0x{(getPC() - 1).ToString("X4")}] 0x{opCode.ToString("X2")} -> {getOpCodeDesc(opCode)}\nPossible Params = 0x{peekBytes[0].ToString("X2")} 0x{peekBytes[1].ToString("X2")}\nAs UI16 0x{peek16.ToString("X4")}\n\n(0x{peek16.ToString("X4")}) = 0x{mem.peekByte(peek16).ToString("X2")}\n\nContinue Debugging??";
            bool showAfterText = false;

            if (breakAtInstruction == (getPC() - 1))
            {
                clock.Stop();
                rom.disassemble(opCodeTranslationDict);
                debuggerForm.updateMemoryWindow(getStateDict());
                debuggerForm.setContinueAddr(getPC());

                debuggerForm.currentAddress = (ushort)((getPC() - 1) & 0xFFFF);

                DialogResult res = debuggerForm.ShowDialog();
                if (res == DialogResult.No)
                {
                    showAfterText = false;
                }
                else if(res == DialogResult.Ignore)
                {
                    // Continue Pressed
                    breakAtInstruction = debuggerForm.getContinueAddr();
                }
                else
                {
                    // yes Pressed
                    showAfterText = true;

                }
                debuggerForm.wait = showAfterText;
                clock.Start();
            }


            opCodeTranslationDict[opCode].execute();



            if (enableInterrupts)
            {
                IME = true;
                shouldEnableInterrupts = false;
            }
            if (disableInterrupts)
            {
                IME = false;
                shouldDisableInterrupts = false;
            }

            if (showAfterText)
            {
                clock.Stop();
                debuggerForm.setContinueAddr(getPC());
                breakAtInstruction = getPC();

                String afterText = $"After: \nOP [0x{(getPC() - 1).ToString("X4")}] 0x{opCode.ToString("X2")} -> {getOpCodeDesc(opCode)}\nPossible Params = 0x{peekBytes[0].ToString("X2")} 0x{peekBytes[1].ToString("X2")}\nAs UI16 0x{peek16.ToString("X4")}\n\n(0x{peek16.ToString("X4")}) = 0x{mem.peekByte(peek16).ToString("X2")}\n\nNext OP: 0x{mem.peekByte(getPC())} -> {getOpCodeDesc(mem.peekByte(getPC()))}\n\nContinue Debugging??";
                debuggerForm.updateMemoryWindow(getStateDict());
                DialogResult res = debuggerForm.ShowDialog(getPC());

                while (debuggerForm.wait)
                {
                    Thread.Sleep(200);
                }

                if (res == DialogResult.No)
                {
                    showAfterText = false;
                }
                else if (res == DialogResult.Ignore)
                {
                    // Continue Pressed
                    breakAtInstruction = debuggerForm.getContinueAddr();
                }
                else
                {
                    // yes Pressed
                    showAfterText = true;
                }
                clock.Start();
            }

        }

        #region CPU Instruction Map
        private void loadOpCodeMap()
        {
            opCodeTranslationDict.Add(0x00, new Instruction(0x00, 1, 4, opNOP));
            opCodeTranslationDict.Add(0x01, new Instruction(0x01, 3, 12, ldBC16));
            opCodeTranslationDict.Add(0x02, new Instruction(0x02, 1, 8, ldAToMemBC));
            opCodeTranslationDict.Add(0x03, new Instruction(0x03, 1, 8, incBC));
            opCodeTranslationDict.Add(0x04, new Instruction(0x04, 1, 4, incB));
            opCodeTranslationDict.Add(0x05, new Instruction(0x05, 1, 4, decB));
            opCodeTranslationDict.Add(0x06, new Instruction(0x06, 2, 8, ldB));
            opCodeTranslationDict.Add(0x07, new Instruction(0x07, 1, 4, rlcA));
            opCodeTranslationDict.Add(0x08, new Instruction(0x08, 3, 20, ldSPFromMem16));
            opCodeTranslationDict.Add(0x09, new Instruction(0x09, 1, 8, addBCtoHL));
            opCodeTranslationDict.Add(0x0A, new Instruction(0x0A, 1, 8, ldAMemBC));
            opCodeTranslationDict.Add(0x0B, new Instruction(0x0B, 1, 8, decBC));
            opCodeTranslationDict.Add(0x0C, new Instruction(0x0C, 1, 4, incC));
            opCodeTranslationDict.Add(0x0D, new Instruction(0x0D, 1, 4, decC));
            opCodeTranslationDict.Add(0x0E, new Instruction(0x0E, 2, 8, ldC));
            opCodeTranslationDict.Add(0x0F, new Instruction(0x0F, 1, 4, rrcA));
            opCodeTranslationDict.Add(0x10, new Instruction(0x10, 2, 4, stopCPU));
            opCodeTranslationDict.Add(0x11, new Instruction(0x11, 3, 12, ldDE16));
            opCodeTranslationDict.Add(0x12, new Instruction(0x12, 1, 8, ldAIntoMemDE16));
            opCodeTranslationDict.Add(0x13, new Instruction(0x13, 1, 8, incDE));
            opCodeTranslationDict.Add(0x14, new Instruction(0x14, 1, 4, incD));
            opCodeTranslationDict.Add(0x15, new Instruction(0x15, 1, 4, decD));
            opCodeTranslationDict.Add(0x16, new Instruction(0x16, 2, 8, ldD));
            opCodeTranslationDict.Add(0x17, new Instruction(0x17, 1, 4, rlA));
            opCodeTranslationDict.Add(0x18, new Instruction(0x18, 2, 12, jumpPCPlusN));
            opCodeTranslationDict.Add(0x19, new Instruction(0x19, 1, 8, addDEtoHL));
            opCodeTranslationDict.Add(0x1A, new Instruction(0x1A, 1, 8, ldAMemDE));
            opCodeTranslationDict.Add(0x1B, new Instruction(0x1B, 1, 8, decDE));
            opCodeTranslationDict.Add(0x1C, new Instruction(0x1C, 1, 4, incE));
            opCodeTranslationDict.Add(0x1D, new Instruction(0x1D, 1, 4, decE));
            opCodeTranslationDict.Add(0x1E, new Instruction(0x1E, 2, 8, ldE));
            opCodeTranslationDict.Add(0x1F, new Instruction(0x1F, 1, 4, rrA));
            opCodeTranslationDict.Add(0x20, new Instruction(0x20, 2, 12, jumpIfZeroFlagResetPlusN)); // 8 - 12 cycles
            opCodeTranslationDict.Add(0x21, new Instruction(0x21, 3, 12, ldHL16));
            opCodeTranslationDict.Add(0x22, new Instruction(0x22, 1, 8, ldiMemHLWithA));
            opCodeTranslationDict.Add(0x23, new Instruction(0x23, 1, 8, incHL));
            opCodeTranslationDict.Add(0x24, new Instruction(0x24, 1, 4, incH));
            opCodeTranslationDict.Add(0x25, new Instruction(0x25, 1, 4, decH));
            opCodeTranslationDict.Add(0x26, new Instruction(0x26, 2, 8, ldH));
            opCodeTranslationDict.Add(0x27, new Instruction(0x27, 1, 4, daaRegA));
            opCodeTranslationDict.Add(0x28, new Instruction(0x28, 2, 12, jumpIfZeroFlagSetPlusN)); // 8 - 12 cycles
            opCodeTranslationDict.Add(0x29, new Instruction(0x29, 1, 8, addHLtoHL));
            opCodeTranslationDict.Add(0x2A, new Instruction(0x2A, 1, 8, ldiAMemHL));
            opCodeTranslationDict.Add(0x2B, new Instruction(0x2B, 1, 8, decHL));
            opCodeTranslationDict.Add(0x2C, new Instruction(0x2C, 1, 4, incL));
            opCodeTranslationDict.Add(0x2D, new Instruction(0x2D, 1, 4, decL));
            opCodeTranslationDict.Add(0x2E, new Instruction(0x2E, 2, 8, ldL));
            opCodeTranslationDict.Add(0x2F, new Instruction(0x2F, 1, 4, complementA));
            opCodeTranslationDict.Add(0x30, new Instruction(0x30, 2, 12, jumpIfCarryFlagResetPlusN)); // 8-12
            opCodeTranslationDict.Add(0x31, new Instruction(0x31, 3, 12, ldSP16));
            opCodeTranslationDict.Add(0x32, new Instruction(0x32, 1, 8, lddMemHLWithA));
            opCodeTranslationDict.Add(0x33, new Instruction(0x33, 1, 8, incSP));
            opCodeTranslationDict.Add(0x34, new Instruction(0x34, 1, 12, incHLMem));
            opCodeTranslationDict.Add(0x35, new Instruction(0x35, 1, 12, decHLMem));
            opCodeTranslationDict.Add(0x36, new Instruction(0x36, 2, 12, ldHLMem));
            opCodeTranslationDict.Add(0x37, new Instruction(0x37, 1, 4, highCarryFlag));
            opCodeTranslationDict.Add(0x38, new Instruction(0x38, 2, 12, jumpIfCarryFlagSetPlusN)); // 8-12
            opCodeTranslationDict.Add(0x39, new Instruction(0x39, 1, 8, addSPtoHL));
            opCodeTranslationDict.Add(0x3A, new Instruction(0x3A, 1, 8, lddAMemHL));
            opCodeTranslationDict.Add(0x3B, new Instruction(0x3B, 1, 8, decSP));
            opCodeTranslationDict.Add(0x3C, new Instruction(0x3C, 1, 4, incA));
            opCodeTranslationDict.Add(0x3D, new Instruction(0x3D, 1, 4, decA));
            opCodeTranslationDict.Add(0x3E, new Instruction(0x3E, 2, 8, ldA));
            opCodeTranslationDict.Add(0x3F, new Instruction(0x3F, 1, 4, complementCarryFlag));
            opCodeTranslationDict.Add(0x40, new Instruction(0x40, 1, 4, ldrBB));
            opCodeTranslationDict.Add(0x41, new Instruction(0x41, 1, 4, ldrBC));
            opCodeTranslationDict.Add(0x42, new Instruction(0x42, 1, 4, ldrBD));
            opCodeTranslationDict.Add(0x43, new Instruction(0x43, 1, 4, ldrBE));
            opCodeTranslationDict.Add(0x44, new Instruction(0x44, 1, 4, ldrBH));
            opCodeTranslationDict.Add(0x45, new Instruction(0x45, 1, 4, ldrBL));
            opCodeTranslationDict.Add(0x46, new Instruction(0x46, 1, 8, ldrBFromMemHL));
            opCodeTranslationDict.Add(0x47, new Instruction(0x47, 1, 4, ldrBA));
            opCodeTranslationDict.Add(0x48, new Instruction(0x48, 1, 4, ldrCB));
            opCodeTranslationDict.Add(0x49, new Instruction(0x49, 1, 4, ldrCC));
            opCodeTranslationDict.Add(0x4A, new Instruction(0x4A, 1, 4, ldrCD));
            opCodeTranslationDict.Add(0x4B, new Instruction(0x4B, 1, 4, ldrCE));
            opCodeTranslationDict.Add(0x4C, new Instruction(0x4C, 1, 4, ldrCH));
            opCodeTranslationDict.Add(0x4D, new Instruction(0x4D, 1, 4, ldrCL));
            opCodeTranslationDict.Add(0x4E, new Instruction(0x4E, 1, 8, ldrCFromMemHL));
            opCodeTranslationDict.Add(0x4F, new Instruction(0x4F, 1, 4, ldrCA));
            opCodeTranslationDict.Add(0x50, new Instruction(0x50, 1, 4, ldrDB));
            opCodeTranslationDict.Add(0x51, new Instruction(0x51, 1, 4, ldrDC));
            opCodeTranslationDict.Add(0x52, new Instruction(0x52, 1, 4, ldrDD));
            opCodeTranslationDict.Add(0x53, new Instruction(0x53, 1, 4, ldrDE));
            opCodeTranslationDict.Add(0x54, new Instruction(0x54, 1, 4, ldrDH));
            opCodeTranslationDict.Add(0x55, new Instruction(0x55, 1, 4, ldrDL));
            opCodeTranslationDict.Add(0x56, new Instruction(0x56, 1, 8, ldrDFromMemHL));
            opCodeTranslationDict.Add(0x57, new Instruction(0x57, 1, 4, ldrDA));
            opCodeTranslationDict.Add(0x58, new Instruction(0x58, 1, 4, ldrEB));
            opCodeTranslationDict.Add(0x59, new Instruction(0x59, 1, 4, ldrEC));
            opCodeTranslationDict.Add(0x5A, new Instruction(0x5A, 1, 4, ldrED));
            opCodeTranslationDict.Add(0x5B, new Instruction(0x5B, 1, 4, ldrEE));
            opCodeTranslationDict.Add(0x5C, new Instruction(0x5C, 1, 4, ldrEH));
            opCodeTranslationDict.Add(0x5D, new Instruction(0x5D, 1, 4, ldrEL));
            opCodeTranslationDict.Add(0x5E, new Instruction(0x5E, 1, 8, ldrEFromMemHL));
            opCodeTranslationDict.Add(0x5F, new Instruction(0x5F, 1, 4, ldrEA));
            opCodeTranslationDict.Add(0x60, new Instruction(0x60, 1, 4, ldrHB));
            opCodeTranslationDict.Add(0x61, new Instruction(0x61, 1, 4, ldrHC));
            opCodeTranslationDict.Add(0x62, new Instruction(0x62, 1, 4, ldrHD));
            opCodeTranslationDict.Add(0x63, new Instruction(0x63, 1, 4, ldrHE));
            opCodeTranslationDict.Add(0x64, new Instruction(0x64, 1, 4, ldrHH));
            opCodeTranslationDict.Add(0x65, new Instruction(0x65, 1, 4, ldrHL));
            opCodeTranslationDict.Add(0x66, new Instruction(0x66, 1, 8, ldrHFromMemHL));
            opCodeTranslationDict.Add(0x67, new Instruction(0x67, 1, 4, ldrHA));
            opCodeTranslationDict.Add(0x68, new Instruction(0x68, 1, 4, ldrLB));
            opCodeTranslationDict.Add(0x69, new Instruction(0x69, 1, 4, ldrLC));
            opCodeTranslationDict.Add(0x6A, new Instruction(0x6A, 1, 4, ldrLD));
            opCodeTranslationDict.Add(0x6B, new Instruction(0x6B, 1, 4, ldrLE));
            opCodeTranslationDict.Add(0x6C, new Instruction(0x6C, 1, 4, ldrLH));
            opCodeTranslationDict.Add(0x6D, new Instruction(0x6D, 1, 4, ldrLL));
            opCodeTranslationDict.Add(0x6E, new Instruction(0x6E, 1, 8, ldrLFromMemHL));
            opCodeTranslationDict.Add(0x6F, new Instruction(0x6F, 1, 4, ldrLA));
            opCodeTranslationDict.Add(0x70, new Instruction(0x70, 1, 8, ldHLMemFromB));
            opCodeTranslationDict.Add(0x71, new Instruction(0x71, 1, 8, ldHLMemFromC));
            opCodeTranslationDict.Add(0x72, new Instruction(0x72, 1, 8, ldHLMemFromD));
            opCodeTranslationDict.Add(0x73, new Instruction(0x73, 1, 8, ldHLMemFromE));
            opCodeTranslationDict.Add(0x74, new Instruction(0x74, 1, 8, ldHLMemFromH));
            opCodeTranslationDict.Add(0x75, new Instruction(0x75, 1, 8, ldHLMemFromL));
            opCodeTranslationDict.Add(0x76, new Instruction(0x76, 1, 4, haltCPU));
            opCodeTranslationDict.Add(0x77, new Instruction(0x77, 1, 8, ldAIntoMemHL16));
            opCodeTranslationDict.Add(0x78, new Instruction(0x78, 1, 4, ldrAB));
            opCodeTranslationDict.Add(0x79, new Instruction(0x79, 1, 4, ldrAC));
            opCodeTranslationDict.Add(0x7A, new Instruction(0x7A, 1, 4, ldrAD));
            opCodeTranslationDict.Add(0x7B, new Instruction(0x7B, 1, 4, ldrAE));
            opCodeTranslationDict.Add(0x7C, new Instruction(0x7C, 1, 4, ldrAH));
            opCodeTranslationDict.Add(0x7D, new Instruction(0x7D, 1, 4, ldrAL));
            opCodeTranslationDict.Add(0x7E, new Instruction(0x7E, 1, 8, ldrAFromMemHL));
            opCodeTranslationDict.Add(0x7F, new Instruction(0x7F, 1, 4, ldrAA));
            opCodeTranslationDict.Add(0x80, new Instruction(0x80, 1, 4, addBtoA));
            opCodeTranslationDict.Add(0x81, new Instruction(0x81, 1, 4, addCtoA));
            opCodeTranslationDict.Add(0x82, new Instruction(0x82, 1, 4, addDtoA));
            opCodeTranslationDict.Add(0x83, new Instruction(0x83, 1, 4, addEtoA));
            opCodeTranslationDict.Add(0x84, new Instruction(0x84, 1, 4, addHtoA));
            opCodeTranslationDict.Add(0x85, new Instruction(0x85, 1, 4, addLtoA));
            opCodeTranslationDict.Add(0x86, new Instruction(0x86, 1, 8, addMemAtHLtoA));
            opCodeTranslationDict.Add(0x87, new Instruction(0x87, 1, 4, addAtoA));
            opCodeTranslationDict.Add(0x88, new Instruction(0x88, 1, 4, addCarryBtoA));
            opCodeTranslationDict.Add(0x89, new Instruction(0x89, 1, 4, addCarryCtoA));
            opCodeTranslationDict.Add(0x8A, new Instruction(0x8A, 1, 4, addCarryDtoA));
            opCodeTranslationDict.Add(0x8B, new Instruction(0x8B, 1, 4, addCarryEtoA));
            opCodeTranslationDict.Add(0x8C, new Instruction(0x8C, 1, 4, addCarryHtoA));
            opCodeTranslationDict.Add(0x8D, new Instruction(0x8D, 1, 4, addCarryLtoA));
            opCodeTranslationDict.Add(0x8E, new Instruction(0x8E, 1, 8, addCarryMemAtHLtoA));
            opCodeTranslationDict.Add(0x8F, new Instruction(0x8F, 1, 4, addCarryAtoA));
            opCodeTranslationDict.Add(0x90, new Instruction(0x90, 1, 4, subBFromA));
            opCodeTranslationDict.Add(0x91, new Instruction(0x91, 1, 4, subCFromA));
            opCodeTranslationDict.Add(0x92, new Instruction(0x92, 1, 4, subDFromA));
            opCodeTranslationDict.Add(0x93, new Instruction(0x93, 1, 4, subEFromA));
            opCodeTranslationDict.Add(0x94, new Instruction(0x94, 1, 4, subHFromA));
            opCodeTranslationDict.Add(0x95, new Instruction(0x95, 1, 4, subLFromA));
            opCodeTranslationDict.Add(0x96, new Instruction(0x96, 1, 8, subMemAtHLFromA));
            opCodeTranslationDict.Add(0x97, new Instruction(0x97, 1, 4, subAFromA));
            opCodeTranslationDict.Add(0x98, new Instruction(0x98, 1, 4, subCarryBFromA));
            opCodeTranslationDict.Add(0x99, new Instruction(0x99, 1, 4, subCarryCFromA));
            opCodeTranslationDict.Add(0x9A, new Instruction(0x9A, 1, 4, subCarryDFromA));
            opCodeTranslationDict.Add(0x9B, new Instruction(0x9B, 1, 4, subCarryEFromA));
            opCodeTranslationDict.Add(0x9C, new Instruction(0x9C, 1, 4, subCarryHFromA));
            opCodeTranslationDict.Add(0x9D, new Instruction(0x9D, 1, 4, subCarryLFromA));
            opCodeTranslationDict.Add(0x9E, new Instruction(0x9E, 1, 8, subCarryMemAtHLFromA));
            opCodeTranslationDict.Add(0x9F, new Instruction(0x9F, 1, 4, subCarryAFromA));
            opCodeTranslationDict.Add(0xA0, new Instruction(0xA0, 1, 4, andABinA));
            opCodeTranslationDict.Add(0xA1, new Instruction(0xA1, 1, 4, andACinA));
            opCodeTranslationDict.Add(0xA2, new Instruction(0xA2, 1, 4, andADinA));
            opCodeTranslationDict.Add(0xA3, new Instruction(0xA3, 1, 4, andAEinA));
            opCodeTranslationDict.Add(0xA4, new Instruction(0xA4, 1, 4, andAHinA));
            opCodeTranslationDict.Add(0xA5, new Instruction(0xA5, 1, 4, andALinA));
            opCodeTranslationDict.Add(0xA6, new Instruction(0xA6, 1, 8, andAMemHLinA));
            opCodeTranslationDict.Add(0xA7, new Instruction(0xA7, 1, 4, andAAinA));
            opCodeTranslationDict.Add(0xA8, new Instruction(0xA8, 1, 4, xorABinA));
            opCodeTranslationDict.Add(0xA9, new Instruction(0xA9, 1, 4, xorACinA));
            opCodeTranslationDict.Add(0xAA, new Instruction(0xAA, 1, 4, xorADinA));
            opCodeTranslationDict.Add(0xAB, new Instruction(0xAB, 1, 4, xorAEinA));
            opCodeTranslationDict.Add(0xAC, new Instruction(0xAC, 1, 4, xorAHinA));
            opCodeTranslationDict.Add(0xAD, new Instruction(0xAD, 1, 4, xorALinA));
            opCodeTranslationDict.Add(0xAE, new Instruction(0xAE, 1, 8, xorAMemHLinA));
            opCodeTranslationDict.Add(0xAF, new Instruction(0xAF, 1, 4, xorAAinA));
            opCodeTranslationDict.Add(0xB0, new Instruction(0xB0, 1, 4, orABinA));
            opCodeTranslationDict.Add(0xB1, new Instruction(0xB1, 1, 4, orACinA));
            opCodeTranslationDict.Add(0xB2, new Instruction(0xB2, 1, 4, orADinA));
            opCodeTranslationDict.Add(0xB3, new Instruction(0xB3, 1, 4, orAEinA));
            opCodeTranslationDict.Add(0xB4, new Instruction(0xB4, 1, 4, orAHinA));
            opCodeTranslationDict.Add(0xB5, new Instruction(0xB5, 1, 4, orALinA));
            opCodeTranslationDict.Add(0xB6, new Instruction(0xB6, 1, 8, orAMemHLinA));
            opCodeTranslationDict.Add(0xB7, new Instruction(0xB7, 1, 4, orAAinA));
            opCodeTranslationDict.Add(0xB8, new Instruction(0xB8, 1, 4, cmpAB));
            opCodeTranslationDict.Add(0xB9, new Instruction(0xB9, 1, 4, cmpAC));
            opCodeTranslationDict.Add(0xBA, new Instruction(0xBA, 1, 4, cmpAD));
            opCodeTranslationDict.Add(0xBB, new Instruction(0xBB, 1, 4, cmpAE));
            opCodeTranslationDict.Add(0xBC, new Instruction(0xBC, 1, 4, cmpAH));
            opCodeTranslationDict.Add(0xBD, new Instruction(0xBD, 1, 4, cmpAL));
            opCodeTranslationDict.Add(0xBE, new Instruction(0xBE, 1, 8, cmpAMemHL));
            opCodeTranslationDict.Add(0xBF, new Instruction(0xBF, 1, 4, cmpAA));
            opCodeTranslationDict.Add(0xC0, new Instruction(0xC0, 1, 20, retIfZReset)); // 8-20
            opCodeTranslationDict.Add(0xC1, new Instruction(0xC1, 1, 12, popIntoBC));
            opCodeTranslationDict.Add(0xC2, new Instruction(0xC2, 3, 16, jumpIfZeroFlagReset)); //12-16
            opCodeTranslationDict.Add(0xC3, new Instruction(0xC3, 3, 16, jumpToNN));
            opCodeTranslationDict.Add(0xC4, new Instruction(0xC4, 3, 24, callNNIfZReset)); //12-24
            opCodeTranslationDict.Add(0xC5, new Instruction(0xC5, 1, 16, pushBCToStack));
            opCodeTranslationDict.Add(0xC6, new Instruction(0xC6, 2, 8, addNtoA));
            opCodeTranslationDict.Add(0xC7, new Instruction(0xC7, 1, 16, rst00));
            opCodeTranslationDict.Add(0xC8, new Instruction(0xC8, 1, 20, retIfZSet)); // 8-20
            opCodeTranslationDict.Add(0xC9, new Instruction(0xC9, 1, 16, ret));
            opCodeTranslationDict.Add(0xCA, new Instruction(0xCA, 3, 16, jumpIfZeroFlagSet)); // 12-16
            opCodeTranslationDict.Add(0xCB, new Instruction(0xCB, 1, 4, executeCBPrefixedOpCode)); // trent have to Add in CB Instructions length and cycles
            opCodeTranslationDict.Add(0xCC, new Instruction(0xCC, 3, 24, callNNIfZSet)); //12-24
            opCodeTranslationDict.Add(0xCD, new Instruction(0xCD, 3, 24, callNN));
            opCodeTranslationDict.Add(0xCE, new Instruction(0xCE, 2, 8, addCarryNtoA));
            opCodeTranslationDict.Add(0xCF, new Instruction(0xCF, 1, 16, rst08));
            opCodeTranslationDict.Add(0xD0, new Instruction(0xD0, 1, 20, retIfCarryReset)); // 8-20
            opCodeTranslationDict.Add(0xD1, new Instruction(0xD1, 1, 12, popIntoDE));
            opCodeTranslationDict.Add(0xD2, new Instruction(0xD2, 3, 16, jumpIfCarryFlagReset)); // 12-16
            opCodeTranslationDict.Add(0xD3, new Instruction(0xD3, 1, 4, unusedD3));
            opCodeTranslationDict.Add(0xD4, new Instruction(0xD4, 3, 24, callNNIfCReset)); //12-24
            opCodeTranslationDict.Add(0xD5, new Instruction(0xD5, 1, 16, pushDEToStack));
            opCodeTranslationDict.Add(0xD6, new Instruction(0xD6, 2, 8, subNFromA));
            opCodeTranslationDict.Add(0xD7, new Instruction(0xD7, 1, 16, rst10));
            opCodeTranslationDict.Add(0xD8, new Instruction(0xD8, 1, 20, retIfCarrySet)); //8-20
            opCodeTranslationDict.Add(0xD9, new Instruction(0xD9, 1, 16, retEnableInterrupts));
            opCodeTranslationDict.Add(0xDA, new Instruction(0xDA, 3, 16, jumpIfCarryFlagSet)); // 12-16
            opCodeTranslationDict.Add(0xDB, new Instruction(0xDB, 1, 4, unusedDB));
            opCodeTranslationDict.Add(0xDC, new Instruction(0xDC, 3, 24, callNNIfCSet)); //12-24
            opCodeTranslationDict.Add(0xDD, new Instruction(0xDD, 1, 4, unusedDD));
            opCodeTranslationDict.Add(0xDE, new Instruction(0xDE, 2, 8, subCarryNFromA));  // SBC A,n
            opCodeTranslationDict.Add(0xDF, new Instruction(0xDF, 1, 16, rst18));
            opCodeTranslationDict.Add(0xE0, new Instruction(0xE0, 2, 12, putAIntoIOPlusMem));
            opCodeTranslationDict.Add(0xE1, new Instruction(0xE1, 1, 12, popIntoHL));
            opCodeTranslationDict.Add(0xE2, new Instruction(0xE2, 1, 8, ldAIntoIOPlusC));
            opCodeTranslationDict.Add(0xE3, new Instruction(0xE3, 1, 4, unusedE3));
            opCodeTranslationDict.Add(0xE4, new Instruction(0xE4, 1, 4, unusedE4));
            opCodeTranslationDict.Add(0xE5, new Instruction(0xE5, 1, 16, pushHLToStack));
            opCodeTranslationDict.Add(0xE6, new Instruction(0xE6, 2, 8, andANinA));
            opCodeTranslationDict.Add(0xE7, new Instruction(0xE7, 1, 16, rst20));
            opCodeTranslationDict.Add(0xE8, new Instruction(0xE8, 2, 16, addNtoSP));
            opCodeTranslationDict.Add(0xE9, new Instruction(0xE9, 1, 4, jumpMemHL));
            opCodeTranslationDict.Add(0xEA, new Instruction(0xEA, 3, 16, ldAIntoNN16));
            opCodeTranslationDict.Add(0xEB, new Instruction(0xEB, 1, 4, unusedEB));
            opCodeTranslationDict.Add(0xEC, new Instruction(0xEC, 1, 4, unusedEC));
            opCodeTranslationDict.Add(0xED, new Instruction(0xED, 1, 4, unusedED));
            opCodeTranslationDict.Add(0xEE, new Instruction(0xEE, 2, 8, xorANinA));
            opCodeTranslationDict.Add(0xEF, new Instruction(0xEF, 1, 16, rst28));
            opCodeTranslationDict.Add(0xF0, new Instruction(0xF0, 2, 12, putIOPlusMemIntoA));
            opCodeTranslationDict.Add(0xF1, new Instruction(0xF1, 1, 12, popIntoAF));
            opCodeTranslationDict.Add(0xF2, new Instruction(0xF2, 1, 8, ldIOPlusCToA));
            opCodeTranslationDict.Add(0xF3, new Instruction(0xF3, 1, 4, disableInterruptsAfterNextIns));
            opCodeTranslationDict.Add(0xF4, new Instruction(0xF4, 1, 4, unusedF4));
            opCodeTranslationDict.Add(0xF5, new Instruction(0xF5, 1, 16, pushAFToStack));
            opCodeTranslationDict.Add(0xF6, new Instruction(0xF6, 2, 8, orANinA));
            opCodeTranslationDict.Add(0xF7, new Instruction(0xF7, 1, 16, rst30));
            opCodeTranslationDict.Add(0xF8, new Instruction(0xF8, 2, 12, ldHLFromSPPlusN));
            opCodeTranslationDict.Add(0xF9, new Instruction(0xF9, 1, 8, ldSPFromHL));
            opCodeTranslationDict.Add(0xFA, new Instruction(0xFA, 3, 16, ld16A));
            opCodeTranslationDict.Add(0xFB, new Instruction(0xFB, 1, 4, enableInterruptsAfterNextIns));
            opCodeTranslationDict.Add(0xFC, new Instruction(0xFC, 1, 4, unusedFC));
            opCodeTranslationDict.Add(0xFD, new Instruction(0xFD, 1, 4, unusedFD));
            opCodeTranslationDict.Add(0xFE, new Instruction(0xFE, 2, 8, cmpAN));
            opCodeTranslationDict.Add(0xFF, new Instruction(0xFF, 1, 16, rst38));
        }
        #endregion

        #region CB Prefixed Instructions
        private void executeCBOperation(Byte extOpCode)
        {
            switch (extOpCode)
            {
                case 0x00:
                    setB(rotateLeftCarry(getB()));
                    break;
                case 0x01:
                    setC(rotateLeftCarry(getC()));
                    break;
                case 0x02:
                    setD(rotateLeftCarry(getD()));
                    break;
                case 0x03:
                    setE(rotateLeftCarry(getE()));
                    break;
                case 0x04:
                    setH(rotateLeftCarry(getH()));
                    break;
                case 0x05:
                    setL(rotateLeftCarry(getL()));
                    break;
                case 0x06:
                    mem.setByte(getHL(), rotateLeftCarry(mem.getByte(getHL())));
                    break;
                case 0x07:
                    setA(rotateLeftCarry(getA()));
                    break;
                case 0x08:
                    setB(rotateRightCarry(getB()));
                    break;
                case 0x09:
                    setC(rotateRightCarry(getC()));
                    break;
                case 0x0A:
                    setD(rotateRightCarry(getD()));
                    break;
                case 0x0B:
                    setE(rotateRightCarry(getE()));
                    break;
                case 0x0C:
                    setH(rotateRightCarry(getH()));
                    break;
                case 0x0D:
                    setL(rotateRightCarry(getL()));
                    break;
                case 0x0E:
                    mem.setByte(getHL(), rotateRightCarry(mem.getByte(getHL())));
                    break;
                case 0x0F:
                    setA(rotateRightCarry(getA()));
                    break;
                case 0x10:
                    setB(rotateLeft(getB()));
                    break;
                case 0x11:
                    setC(rotateLeft(getC()));
                    break;
                case 0x12:
                    setD(rotateLeft(getD()));
                    break;
                case 0x13:
                    setE(rotateLeft(getE()));
                    break;
                case 0x14:
                    setH(rotateLeft(getH()));
                    break;
                case 0x15:
                    setL(rotateLeft(getL()));
                    break;
                case 0x16:
                    mem.setByte(getHL(), rotateLeft(mem.getByte(getHL())));
                    break;
                case 0x17:
                    setA(rotateLeft(getA()));
                    break;
                case 0x18:
                    setB(rotateRight(getB()));
                    break;
                case 0x19:
                    setC(rotateRight(getC()));
                    break;
                case 0x1A:
                    setD(rotateRight(getD()));
                    break;
                case 0x1B:
                    setE(rotateRight(getE()));
                    break;
                case 0x1C:
                    setH(rotateRight(getH()));
                    break;
                case 0x1D:
                    setL (rotateRight(getL()));
                    break;
                case 0x1E:
                    mem.setByte(getHL(), rotateRight(mem.getByte(getHL())));
                    break;
                case 0x1F:
                    setA(rotateRight(getA()));
                    break;
                case 0x20:
                    setB(sla(getB()));
                    break;
                case 0x21:
                    setC(sla(getC()));
                    break;
                case 0x22:
                    setD(sla(getD()));
                    break;
                case 0x23:
                    setE(sla(getE()));
                    break;
                case 0x24:
                    setH(sla(getH()));
                    break;
                case 0x25:
                    setL(sla(getL()));
                    break;
                case 0x26:
                    mem.setByte(getHL(), sla(mem.getByte(getHL())));
                    break;
                case 0x27:
                    setA(sla(getA()));
                    break;
                case 0x28:
                    setB(sra(getB()));
                    break;
                case 0x29:
                    setC(sra(getC()));
                    break;
                case 0x2A:
                    setD(sra(getD()));
                    break;
                case 0x2B:
                    setE(sra(getE()));
                    break;
                case 0x2C:
                    setH(sra(getH()));
                    break;
                case 0x2D:
                    setL(sra(getL()));
                    break;
                case 0x2E:
                    mem.setByte(getHL(), sra(mem.getByte(getHL())));
                    break;
                case 0x2F:
                    setA(sra(getA()));
                    break;
                case 0x30:
                    setB(swapNibbles(getB()));
                    break;
                case 0x31:
                    setC(swapNibbles(getC()));
                    break;
                case 0x32:
                    setD(swapNibbles(getD()));
                    break;
                case 0x33:
                    setE(swapNibbles(getE()));
                    break;
                case 0x34:
                    setH(swapNibbles(getH()));
                    break;
                case 0x35:
                    setL(swapNibbles(getL()));
                    break;
                case 0x36:
                    mem.setByte(getHL(), swapNibbles(mem.getByte(getHL())));
                    break;
                case 0x37:
                    setA(swapNibbles(getA()));
                    break;
                case 0x38:
                    setB(srl(getB()));
                    break;
                case 0x39:
                    setC(srl(getC()));
                    break;
                case 0x3A:
                    setD(srl(getD()));
                    break;
                case 0x3B:
                    setE(srl(getE()));
                    break;
                case 0x3C:
                    setH(srl(getH()));
                    break;
                case 0x3D:
                    setL(srl(getL()));
                    break;
                case 0x3E:
                    mem.setByte(getHL(), srl(mem.getByte(getHL())));
                    break;
                case 0x3F:
                    setA(srl(getA()));
                    break;
                case 0x40:
                    executeBitOperation(BitOperationType.Test, 'B', 0x01);
                    break;
                case 0x41:
                    executeBitOperation(BitOperationType.Test, 'C', 0x01);
                    break;
                case 0x42:
                    executeBitOperation(BitOperationType.Test, 'D', 0x01);
                    break;
                case 0x43:
                    executeBitOperation(BitOperationType.Test, 'E', 0x01);
                    break;
                case 0x44:
                    executeBitOperation(BitOperationType.Test, 'H', 0x01);
                    break;
                case 0x45:
                    executeBitOperation(BitOperationType.Test, 'L', 0x01);
                    break;
                case 0x46:
                    executeBitOperation(BitOperationType.Test, 'M', 0x01);
                    break;
                case 0x47:
                    executeBitOperation(BitOperationType.Test, 'A', 0x01);
                    break;
                case 0x48:
                    executeBitOperation(BitOperationType.Test, 'B', 0x02);
                    break;
                case 0x49:
                    executeBitOperation(BitOperationType.Test, 'C', 0x02);
                    break;
                case 0x4A:
                    executeBitOperation(BitOperationType.Test, 'D', 0x02);
                    break;
                case 0x4B:
                    executeBitOperation(BitOperationType.Test, 'E', 0x02);
                    break;
                case 0x4C:
                    executeBitOperation(BitOperationType.Test, 'H', 0x02);
                    break;
                case 0x4D:
                    executeBitOperation(BitOperationType.Test, 'L', 0x02);
                    break;
                case 0x4E:
                    executeBitOperation(BitOperationType.Test, 'M', 0x02);
                    break;
                case 0x4F:
                    executeBitOperation(BitOperationType.Test, 'A', 0x02);
                    break;
                case 0x50:
                    executeBitOperation(BitOperationType.Test, 'B', 0x04);
                    break;
                case 0x51:
                    executeBitOperation(BitOperationType.Test, 'C', 0x04);
                    break;
                case 0x52:
                    executeBitOperation(BitOperationType.Test, 'D', 0x04);
                    break;
                case 0x53:
                    executeBitOperation(BitOperationType.Test, 'E', 0x04);
                    break;
                case 0x54:
                    executeBitOperation(BitOperationType.Test, 'H', 0x04);
                    break;
                case 0x55:
                    executeBitOperation(BitOperationType.Test, 'L', 0x04);
                    break;
                case 0x56:
                    executeBitOperation(BitOperationType.Test, 'M', 0x04);
                    break;
                case 0x57:
                    executeBitOperation(BitOperationType.Test, 'A', 0x04);
                    break;
                case 0x58:
                    executeBitOperation(BitOperationType.Test, 'B', 0x08);
                    break;
                case 0x59:
                    executeBitOperation(BitOperationType.Test, 'C', 0x08);
                    break;
                case 0x5A:
                    executeBitOperation(BitOperationType.Test, 'D', 0x08);
                    break;
                case 0x5B:
                    executeBitOperation(BitOperationType.Test, 'E', 0x08);
                    break;
                case 0x5C:
                    executeBitOperation(BitOperationType.Test, 'H', 0x08);
                    break;
                case 0x5D:
                    executeBitOperation(BitOperationType.Test, 'L', 0x08);
                    break;
                case 0x5E:
                    executeBitOperation(BitOperationType.Test, 'M', 0x08);
                    break;
                case 0x5F:
                    executeBitOperation(BitOperationType.Test, 'A', 0x08);
                    break;
                case 0x60:
                    executeBitOperation(BitOperationType.Test, 'B', 0x10);
                    break;
                case 0x61:
                    executeBitOperation(BitOperationType.Test, 'C', 0x10);
                    break;
                case 0x62:
                    executeBitOperation(BitOperationType.Test, 'D', 0x10);
                    break;
                case 0x63:
                    executeBitOperation(BitOperationType.Test, 'E', 0x10);
                    break;
                case 0x64:
                    executeBitOperation(BitOperationType.Test, 'H', 0x10);
                    break;
                case 0x65:
                    executeBitOperation(BitOperationType.Test, 'L', 0x10);
                    break;
                case 0x66:
                    executeBitOperation(BitOperationType.Test, 'M', 0x10);
                    break;
                case 0x67:
                    executeBitOperation(BitOperationType.Test, 'A', 0x10);
                    break;
                case 0x68:
                    executeBitOperation(BitOperationType.Test, 'B', 0x20);
                    break;
                case 0x69:
                    executeBitOperation(BitOperationType.Test, 'C', 0x20);
                    break;
                case 0x6A:
                    executeBitOperation(BitOperationType.Test, 'D', 0x20);
                    break;
                case 0x6B:
                    executeBitOperation(BitOperationType.Test, 'E', 0x20);
                    break;
                case 0x6C:
                    executeBitOperation(BitOperationType.Test, 'H', 0x20);
                    break;
                case 0x6D:
                    executeBitOperation(BitOperationType.Test, 'L', 0x20);
                    break;
                case 0x6E:
                    executeBitOperation(BitOperationType.Test, 'M', 0x20);
                    break;
                case 0x6F:
                    executeBitOperation(BitOperationType.Test, 'A', 0x20);
                    break;
                case 0x70:
                    executeBitOperation(BitOperationType.Test, 'B', 0x40);
                    break;
                case 0x71:
                    executeBitOperation(BitOperationType.Test, 'C', 0x40);
                    break;
                case 0x72:
                    executeBitOperation(BitOperationType.Test, 'D', 0x40);
                    break;
                case 0x73:
                    executeBitOperation(BitOperationType.Test, 'E', 0x40);
                    break;
                case 0x74:
                    executeBitOperation(BitOperationType.Test, 'H', 0x40);
                    break;
                case 0x75:
                    executeBitOperation(BitOperationType.Test, 'L', 0x40);
                    break;
                case 0x76:
                    executeBitOperation(BitOperationType.Test, 'M', 0x40);
                    break;
                case 0x77:
                    executeBitOperation(BitOperationType.Test, 'A', 0x40);
                    break;
                case 0x78:
                    executeBitOperation(BitOperationType.Test, 'B', 0x80);
                    break;
                case 0x79:
                    executeBitOperation(BitOperationType.Test, 'C', 0x80);
                    break;
                case 0x7A:
                    executeBitOperation(BitOperationType.Test, 'D', 0x80);
                    break;
                case 0x7B:
                    executeBitOperation(BitOperationType.Test, 'E', 0x80);
                    break;
                case 0x7C:
                    executeBitOperation(BitOperationType.Test, 'H', 0x80);
                    break;
                case 0x7D:
                    executeBitOperation(BitOperationType.Test, 'L', 0x80);
                    break;
                case 0x7E:
                    executeBitOperation(BitOperationType.Test, 'M', 0x80);
                    break;
                case 0x7F:
                    executeBitOperation(BitOperationType.Test, 'A', 0x80);
                    break;
                case 0x80:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x01);
                    break;
                case 0x81:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x01);
                    break;
                case 0x82:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x01);
                    break;
                case 0x83:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x01);
                    break;
                case 0x84:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x01);
                    break;
                case 0x85:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x01);
                    break;
                case 0x86:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x01);
                    break;
                case 0x87:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x01);
                    break;
                case 0x88:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x02);
                    break;
                case 0x89:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x02);
                    break;
                case 0x8A:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x02);
                    break;
                case 0x8B:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x02);
                    break;
                case 0x8C:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x02);
                    break;
                case 0x8D:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x02);
                    break;
                case 0x8E:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x02);
                    break;
                case 0x8F:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x02);
                    break;
                case 0x90:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x04);
                    break;
                case 0x91:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x04);
                    break;
                case 0x92:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x04);
                    break;
                case 0x93:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x04);
                    break;
                case 0x94:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x04);
                    break;
                case 0x95:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x04);
                    break;
                case 0x96:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x04);
                    break;
                case 0x97:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x04);
                    break;
                case 0x98:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x08);
                    break;
                case 0x99:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x08);
                    break;
                case 0x9A:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x08);
                    break;
                case 0x9B:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x08);
                    break;
                case 0x9C:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x08);
                    break;
                case 0x9D:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x08);
                    break;
                case 0x9E:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x08);
                    break;
                case 0x9F:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x08);
                    break;
                case 0xA0:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x10);
                    break;
                case 0xA1:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x10);
                    break;
                case 0xA2:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x10);
                    break;
                case 0xA3:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x10);
                    break;
                case 0xA4:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x10);
                    break;
                case 0xA5:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x10);
                    break;
                case 0xA6:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x10);
                    break;
                case 0xA7:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x10);
                    break;
                case 0xA8:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x20);
                    break;
                case 0xA9:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x20);
                    break;
                case 0xAA:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x20);
                    break;
                case 0xAB:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x20);
                    break;
                case 0xAC:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x20);
                    break;
                case 0xAD:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x20);
                    break;
                case 0xAE:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x20);
                    break;
                case 0xAF:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x20);
                    break;
                case 0xB0:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x40);
                    break;
                case 0xB1:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x40);
                    break;
                case 0xB2:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x40);
                    break;
                case 0xB3:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x40);
                    break;
                case 0xB4:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x40);
                    break;
                case 0xB5:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x40);
                    break;
                case 0xB6:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x40);
                    break;
                case 0xB7:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x40);
                    break;
                case 0xB8:
                    executeBitOperation(BitOperationType.Reset, 'B', 0x80);
                    break;
                case 0xB9:
                    executeBitOperation(BitOperationType.Reset, 'C', 0x80);
                    break;
                case 0xBA:
                    executeBitOperation(BitOperationType.Reset, 'D', 0x80);
                    break;
                case 0xBB:
                    executeBitOperation(BitOperationType.Reset, 'E', 0x80);
                    break;
                case 0xBC:
                    executeBitOperation(BitOperationType.Reset, 'H', 0x80);
                    break;
                case 0xBD:
                    executeBitOperation(BitOperationType.Reset, 'L', 0x80);
                    break;
                case 0xBE:
                    executeBitOperation(BitOperationType.Reset, 'M', 0x80);
                    break;
                case 0xBF:
                    executeBitOperation(BitOperationType.Reset, 'A', 0x80);
                    break;
                case 0xC0:
                    executeBitOperation(BitOperationType.Set, 'B', 0x01);
                    break;
                case 0xC1:
                    executeBitOperation(BitOperationType.Set, 'C', 0x01);
                    break;
                case 0xC2:
                    executeBitOperation(BitOperationType.Set, 'D', 0x01);
                    break;
                case 0xC3:
                    executeBitOperation(BitOperationType.Set, 'E', 0x01);
                    break;
                case 0xC4:
                    executeBitOperation(BitOperationType.Set, 'H', 0x01);
                    break;
                case 0xC5:
                    executeBitOperation(BitOperationType.Set, 'L', 0x01);
                    break;
                case 0xC6:
                    executeBitOperation(BitOperationType.Set, 'M', 0x01);
                    break;
                case 0xC7:
                    executeBitOperation(BitOperationType.Set, 'A', 0x01);
                    break;
                case 0xC8:
                    executeBitOperation(BitOperationType.Set, 'B', 0x02);
                    break;
                case 0xC9:
                    executeBitOperation(BitOperationType.Set, 'C', 0x02);
                    break;
                case 0xCA:
                    executeBitOperation(BitOperationType.Set, 'D', 0x02);
                    break;
                case 0xCB:
                    executeBitOperation(BitOperationType.Set, 'E', 0x02);
                    break;
                case 0xCC:
                    executeBitOperation(BitOperationType.Set, 'H', 0x02);
                    break;
                case 0xCD:
                    executeBitOperation(BitOperationType.Set, 'L', 0x02);
                    break;
                case 0xCE:
                    executeBitOperation(BitOperationType.Set, 'M', 0x02);
                    break;
                case 0xCF:
                    executeBitOperation(BitOperationType.Set, 'A', 0x02);
                    break;
                case 0xD0:
                    executeBitOperation(BitOperationType.Set, 'B', 0x04);
                    break;
                case 0xD1:
                    executeBitOperation(BitOperationType.Set, 'C', 0x04);
                    break;
                case 0xD2:
                    executeBitOperation(BitOperationType.Set, 'D', 0x04);
                    break;
                case 0xD3:
                    executeBitOperation(BitOperationType.Set, 'E', 0x04);
                    break;
                case 0xD4:
                    executeBitOperation(BitOperationType.Set, 'H', 0x04);
                    break;
                case 0xD5:
                    executeBitOperation(BitOperationType.Set, 'L', 0x04);
                    break;
                case 0xD6:
                    executeBitOperation(BitOperationType.Set, 'M', 0x04);
                    break;
                case 0xD7:
                    executeBitOperation(BitOperationType.Set, 'A', 0x04);
                    break;
                case 0xD8:
                    executeBitOperation(BitOperationType.Set, 'B', 0x08);
                    break;
                case 0xD9:
                    executeBitOperation(BitOperationType.Set, 'C', 0x08);
                    break;
                case 0xDA:
                    executeBitOperation(BitOperationType.Set, 'D', 0x08);
                    break;
                case 0xDB:
                    executeBitOperation(BitOperationType.Set, 'E', 0x08);
                    break;
                case 0xDC:
                    executeBitOperation(BitOperationType.Set, 'H', 0x08);
                    break;
                case 0xDD:
                    executeBitOperation(BitOperationType.Set, 'L', 0x08);
                    break;
                case 0xDE:
                    executeBitOperation(BitOperationType.Set, 'M', 0x08);
                    break;
                case 0xDF:
                    executeBitOperation(BitOperationType.Set, 'A', 0x08);
                    break;
                case 0xE0:
                    executeBitOperation(BitOperationType.Set, 'B', 0x10);
                    break;
                case 0xE1:
                    executeBitOperation(BitOperationType.Set, 'C', 0x10);
                    break;
                case 0xE2:
                    executeBitOperation(BitOperationType.Set, 'D', 0x10);
                    break;
                case 0xE3:
                    executeBitOperation(BitOperationType.Set, 'E', 0x10);
                    break;
                case 0xE4:
                    executeBitOperation(BitOperationType.Set, 'H', 0x10);
                    break;
                case 0xE5:
                    executeBitOperation(BitOperationType.Set, 'L', 0x10);
                    break;
                case 0xE6:
                    executeBitOperation(BitOperationType.Set, 'M', 0x10);
                    break;
                case 0xE7:
                    executeBitOperation(BitOperationType.Set, 'A', 0x10);
                    break;
                case 0xE8:
                    executeBitOperation(BitOperationType.Set, 'B', 0x20);
                    break;
                case 0xE9:
                    executeBitOperation(BitOperationType.Set, 'C', 0x20);
                    break;
                case 0xEA:
                    executeBitOperation(BitOperationType.Set, 'D', 0x20);
                    break;
                case 0xEB:
                    executeBitOperation(BitOperationType.Set, 'E', 0x20);
                    break;
                case 0xEC:
                    executeBitOperation(BitOperationType.Set, 'H', 0x20);
                    break;
                case 0xED:
                    executeBitOperation(BitOperationType.Set, 'L', 0x20);
                    break;
                case 0xEE:
                    executeBitOperation(BitOperationType.Set, 'M', 0x20);
                    break;
                case 0xEF:
                    executeBitOperation(BitOperationType.Set, 'A', 0x20);
                    break;
                case 0xF0:
                    executeBitOperation(BitOperationType.Set, 'B', 0x40);
                    break;
                case 0xF1:
                    executeBitOperation(BitOperationType.Set, 'C', 0x40);
                    break;
                case 0xF2:
                    executeBitOperation(BitOperationType.Set, 'D', 0x40);
                    break;
                case 0xF3:
                    executeBitOperation(BitOperationType.Set, 'E', 0x40);
                    break;
                case 0xF4:
                    executeBitOperation(BitOperationType.Set, 'H', 0x40);
                    break;
                case 0xF5:
                    executeBitOperation(BitOperationType.Set, 'L', 0x40);
                    break;
                case 0xF6:
                    executeBitOperation(BitOperationType.Set, 'M', 0x40);
                    break;
                case 0xF7:
                    executeBitOperation(BitOperationType.Set, 'A', 0x40);
                    break;
                case 0xF8:
                    executeBitOperation(BitOperationType.Set, 'B', 0x80);
                    break;
                case 0xF9:
                    executeBitOperation(BitOperationType.Set, 'C', 0x80);
                    break;
                case 0xFA:
                    executeBitOperation(BitOperationType.Set, 'D', 0x80);
                    break;
                case 0xFB:
                    executeBitOperation(BitOperationType.Set, 'E', 0x80);
                    break;
                case 0xFC:
                    executeBitOperation(BitOperationType.Set, 'H', 0x80);
                    break;
                case 0xFD:
                    executeBitOperation(BitOperationType.Set, 'L', 0x80);
                    break;
                case 0xFE:
                    executeBitOperation(BitOperationType.Set, 'M', 0x80);
                    break;
                case 0xFF:
                    executeBitOperation(BitOperationType.Set, 'A', 0x80);
                    break;
            }
        }
        #endregion

        private void executeBitOperation(BitOperationType type, Char reg, Byte mask)
        {
            if (type == BitOperationType.Test)
            {
                bool set = false;

                setSubtractFlag(false);
                setHalfCarryFlag(true);
                setZeroFlag(false);

                switch (reg)
                {
                    case 'A':
                        set = ((getA() & mask) > 0);
                        break;
                    case 'B':
                        set = ((getB() & mask) > 0);
                        break;
                    case 'C':
                        set = ((getC() & mask) > 0);
                        break;
                    case 'D':
                        set = ((getD() & mask) > 0);
                        break;
                    case 'E':
                        set = ((getE() & mask) > 0);
                        break;
                    case 'H':
                        set = ((getH() & mask) > 0);
                        break;
                    case 'L':
                        set = ((getL() & mask) > 0);
                        break;
                    case 'M':
                        set = ((mem.getByte(getHL()) & mask) > 0);
                        break;
                }

                if (!set)
                {
                    setZeroFlag(true);
                }
            }
            else if (type == BitOperationType.Set)
            {
                switch (reg)
                {
                    case 'A':
                        setA((byte)(getA() | mask));
                        break;
                    case 'B':
                        setB((byte)(getB() | mask));
                        break;
                    case 'C':
                        setC((byte)(getC() | mask));
                        break;
                    case 'D':
                        setD((byte)(getD() | mask));
                        break;
                    case 'E':
                        setE((byte)(getE() | mask));
                        break;
                    case 'H':
                        setH((byte)(getH() | mask));
                        break;
                    case 'L':
                        setL((byte)(getL() | mask));
                        break;
                    case 'M':
                        ushort val = getHL();
                        mem.setByte(val, (byte)((mem.getByte(val) | mask)));
                        break;
                }
            }
            else if(type == BitOperationType.Reset)
            {
                switch (reg)
                {
                    case 'A':
                        setA((byte)(getA() & (~mask)));
                        break;
                    case 'B':
                        setB((byte)(getB() & (~mask)));
                        break;
                    case 'C':
                        setC((byte)(getC() & (~mask)));
                        break;
                    case 'D':
                        setD((byte)(getD() & (~mask)));
                        break;
                    case 'E':
                        setE((byte)(getE() & (~mask)));
                        break;
                    case 'H':
                        setH((byte)(getH() & (~mask)));
                        break;
                    case 'L':
                        setL((byte)(getL() & (~mask)));
                        break;
                    case 'M':
                        ushort val = getHL();
                        mem.setByte(val, (byte)((mem.getByte(val) & (~mask))));
                        break;
                }
            }
        }

        #region Interrupt Operations
        private bool isAnInterruptRequested()
        {
            return ((mem.getByte(0xFF0F) & 0x01F) > 0);
        }

        private bool isInterruptRequested(InterruptType type)
        {
            return ((mem.getByte(0xFF0F) & (int)type) > 0);
        }

        private bool isInterruptEnabled(InterruptType type)
        {
            bool rv = false;
            if (IME && ((mem.getByte(0xFFFF) & (int)type) > 0))
            {
                // Interrupt is Enabled and Requested
                rv = true;
            }

            return rv;
        }


        private bool fireInterrupt(InterruptType type)
        {
            // SHould Break this out to make it cycle accurate later
            bool rv = false;
            IME = false;
            Byte handlerAddress = 0;

            switch (type)
            {
                case InterruptType.VBlank:
                    handlerAddress = 0x40;
                    mem.setByte(0xFF0F, (Byte)((mem.getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.LCDStat:
                    handlerAddress = 0x48;
                    mem.setByte(0xFF0F, (Byte)((mem.getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.Timer:
                    handlerAddress = 0x50;
                    mem.setByte(0xFF0F, (Byte)((mem.getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.Serial:
                    handlerAddress = 0x58;
                    mem.setByte(0xFF0F, (Byte)((mem.getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.Joypad:
                    handlerAddress = 0x60;
                    mem.setByte(0xFF0F, (Byte)((mem.getByte(0xFF0F) & ~(int)type)));
                    break;
            }

            pushOnStack(getPC());
            setPC(handlerAddress);

            return rv;
        }
        #endregion


        #region Register Operations

        public Byte getA()
        {
            return A;
        }
        private void setA(Byte value)
        {
            A = value;
        }

        public Byte getB()
        {
            return B;
        }
        private void setB(Byte value)
        {
            B = value;
        }

        public Byte getC()
        {
            return C;
        }
        private void setC(Byte value)
        {
            C = value;
        }

        public Byte getD()
        {
            return D;
        }
        private void setD(Byte value)
        {
            D = value;
        }

        public Byte getE()
        {
            return E;
        }
        private void setE(Byte value)
        {
            E = value;
        }

        public Byte getH()
        {
            return H;
        }
        private void setH(Byte value)
        {
            H = value;
        }

        public Byte getL()
        {
            return L;
        }
        private void setL(Byte value)
        {
            L = value;
        }

        public Byte getF()
        {
            return F;
        }
        private void setF(Byte value)
        {
            F = value;
        }

        public ushort getSP()
        {
            return SP;
        }
        private void setSP(ushort value)
        {
            SP = value;
        }

        public ushort getPC()
        {
            return PC;
        }
        private void setPC(ushort value)
        {
            PC = value;
        }
        private void incrementPC()
        {
            PC = increment16(PC);
        }
        private void decrementPC()
        {
            PC = decrement16(PC);
        }

        private void incrementSP()
        {
            SP = increment16(SP);
        }
        private void decrementSP()
        {
            SP = decrement16(SP);
        }



        public ushort getAF()
        {
            // See how F register is spevial
            ushort rv = 0;
            rv = (ushort)((A << (ushort)8) | F);
            return rv;
        }

        private void setAF(ushort value)
        {
            A = (Byte)((value & 0xFF00) >> 8);
            F = (Byte)(value & 0x00FF);
        }

        public ushort getBC()
        {
            ushort rv = 0;
            rv = (ushort)((B << (ushort)8) | C);
            return rv;
        }

        private void setBC(ushort value)
        {
            B = (Byte)((value & 0xFF00) >> 8);
            C = (Byte)(value & 0x00FF);
        }

        public ushort getDE()
        {
            ushort rv = 0;
            rv = (ushort)((D << (ushort)8) | E);
            return rv;
        }

        private void setDE(ushort value)
        {
            D = (Byte)((value & 0xFF00) >> 8);
            E = (Byte)(value & 0x00FF);
        }

        public ushort getHL()
        {
            ushort rv = 0;
            rv = (ushort)((H << (ushort)8) | L);
            return rv;
        }

        private void setHL(ushort value)
        {
            H = (Byte)((value & 0xFF00) >> 8);
            L = (Byte)(value & 0x00FF);
        }

        public bool getCarryFlag()
        {

            return ((F & 0x10) > 0);
        }

        private void setCarryFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | 0x10);
            }
            else
            {
                F = (Byte)(F & (~0x10));
            }

        }

        public bool getHalfCarryFlag()
        {

            return ((F & 0x20) > 0);
        }

        private void setHalfCarryFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | 0x20);
            }
            else
            {
                F = (Byte)(F & (~0x20));
            }
        }

        public bool getSubtractFlag()
        {

            return ((F & 0x40) > 0);
        }

        private void setSubtractFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | 0x40);
            }
            else
            {
                F = (Byte)(F & (~0x40));
            }
        }

        public bool getZeroFlag()
        {

            return ((F & 0x80) > 0);
        }

        private void setZeroFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | 0x80);
            }
            else
            {
                F = (Byte)(F & (~0x80));
            }
        }

        private void resetFlagsNibble()
        {
            F = (Byte)(F & 0x0F);
        }

        #endregion

        #region CPU Instructions
        private void opNOP() // OP Code 0x00
        {
            return;
        }

        private void ldBC16() // 0x01
        {
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setBC(rv);
        }
        private void ldAToMemBC() //0x02
        {
            ushort addr = getBC();
            byte value = getA();

            mem.setByte(addr, value);
        }
        private void incBC() //0x03
        {
            ushort value = getBC();

            value = increment16(value);

            setBC(value);
        }
        private void incB() // 0x04
        {
            Byte value = getB();

            value = increment(value);

            setB(value);
        }
        private void decB() // 0x05
        {
            Byte value = getB();

            value = decrement(value);

            setB(value);
        }
        private void ldB() // 0x06
        {
            Byte value = fetch();
            setB(value);
        }
        private void rlcA() // 0x07
        {
            setA(rotateLeftCarry(getA()));
        }
        private void ldSPFromMem16() // 0x08
        {
            Byte value1 = fetch(); // lower
            Byte value2 = fetch(); // upper
            ushort value = (ushort) ((value2 << 8) + value1);

            setSP(value);
        }
        private void addBCtoHL() //0x09
        {
            setHL(add16(getHL(), getBC(), Add16Type.HL));
        }
        private void ldAMemBC() // 0x0A
        {
            Byte value = mem.getByte(getBC());
            setA(value);
        }
        private void decBC() //0x0B
        {
            ushort value = getBC();

            value = decrement16(value);

            setBC(value);
        }
        private void incC() // 0x0C
        {
            Byte value = getC();

            value = increment(value);

            setC(value);
        }
        private void decC() // 0x0D
        {
            Byte value = getC();

            value = decrement(value);

            setC(value);
        }
        private void ldC() // 0x0E
        {
            Byte value = fetch();
            setC(value);
        }
        private void rrcA() // 0x0F
        {
            setA(rotateRightCarry(getA()));
        }
        private void stopCPU() // 0x10
        {
            Byte command = fetch();

            if (command == 0x00)
            {
                state = CPUState.Stopped;
            }
            else
            {
                //throw new NotImplementedException($"0x10 prefix Command 0x{command.ToString("X2")} is not implented.");
            }
            
        }
        private void ldDE16() // 0x11
        {
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setDE(rv);
        }
        private void ldAIntoMemDE16() // 0x12
        {
            mem.setByte(getDE(), getA());
        }
        private void incDE() //0x03
        {
            ushort value = getDE();

            value = increment16(value);

            setDE(value);
        }
        private void incD() // 0x14
        {
            Byte value = getD();

            value = increment(value);

            setD(value);
        }
        private void decD() // 0x15
        {
            Byte value = getD();

            value = decrement(value);

            setD(value);
        }
        private void ldD() // 0x16
        {
            Byte value = fetch();
            setD(value);
        }
        private void rlA() // 0x17
        {
            setA(rotateLeft(getA()));
        }
        private void jumpPCPlusN() // 0x18
        {
            SByte offset = (SByte)fetch();
            ushort address = add16IgnoreFlags(PC, offset);

            setPC(address);
        }
        private void addDEtoHL() //0x19
        {
            setHL(add16(getHL(), getDE(), Add16Type.HL));
        }
        private void ldAMemDE() // 0x1A
        {
            Byte value = mem.getByte(getDE());
            setA(value);
        }
        private void decDE() //0x1B
        {
            ushort value = getDE();

            value = decrement16(value);

            setDE(value);
        }
        private void incE() // 0x1C
        {
            Byte value = getE();

            value = increment(value);

            setE(value);
        }
        private void decE() // 0x1D
        {
            Byte value = getE();

            value = decrement(value);

            setE(value);
        }
        private void ldE() // 0x1E
        {
            Byte value = fetch();
            setE(value);
        }
        private void rrA() // 0x1F
        {
            setA(rotateRight(getA()));
        }
        private void jumpIfZeroFlagResetPlusN() // 0x20
        {
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (!getZeroFlag())
            {
                setPC(value);
            }
            else
            {

            }
        }
        private void ldHL16() // 0x21
        {
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setHL(rv);
        }
        private void ldiMemHLWithA() // 0x22
        {
            Byte value = getA();
            mem.setByte(getHL(), value);

            incrementHL();
        }
        private void incHL() //0x23
        {
            ushort value = getHL();

            value = increment16(value);

            setHL(value);
        }
        private void incH() // 0x24
        {
            Byte value = getH();

            value = increment(value);

            setH(value);
        }
        private void decH() // 0x25
        {
            Byte value = getH();

            value = decrement(value);

            setH(value);
        }
        private void ldH() // 0x26
        {
            Byte value = fetch();
            setH(value);
        }
        private void daaRegA() // 0x27
        {
            Byte value = 0;

            value = daa(getA());

            setA(value);
        }
        private void jumpIfZeroFlagSetPlusN() // 0x28
        {
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (getZeroFlag())
            {
                setPC(value);
            }
        }
        private void addHLtoHL() //0x29
        {
            setHL(add16(getHL(), getHL(), Add16Type.HL));
        }
        private void ldiAMemHL() // 0x2A
        {
            Byte value = mem.getByte(getHL());
            setA(value);

            incrementHL();
        }
        private void decHL() //0x2B
        {
            ushort value = getHL();

            value = decrement16(value);

            setHL(value);
        }
        private void incL() // 0x2C
        {
            Byte value = getL();

            value = increment(value);

            setL(value);
        }
        private void decL() // 0x2D
        {
            Byte value = getL();

            value = decrement(value);

            setL(value);
        }
        private void ldL() // 0x2E
        {
            Byte value = fetch();
            setL(value);
        }
        private void complementA() // 0x2F
        {
            Byte value = (Byte)((~getA()) & 0xFF);

            setSubtractFlag(true);
            setHalfCarryFlag(true);
            setA(value);
        }
        private void jumpIfCarryFlagResetPlusN() // 0x30
        {
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (!getCarryFlag())
            {
                setPC(value);
            }
        }
        private void ldSP16() // 0x31
        {
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setSP(rv);
        }
        private void lddMemHLWithA() // 0x32
        {
            Byte value = getA();
            mem.setByte(getHL(), value);

            decrementHL();
        }
        private void incSP() //0x03
        {
            ushort value = getSP();

            value = increment16(value);

            setSP(value);
        }
        private void incHLMem() // 0x34
        {
            Byte value = mem.getByte(getHL());

            value = increment(value);

            mem.setByte(getHL(), value);
        }
        private void decHLMem() // 0x35
        {
            Byte value = mem.getByte(getHL());

            value = decrement(value);

            mem.setByte(getHL(), value);
        }
        private void ldHLMem() // 0x36
        {
            Byte value = fetch();
            mem.setByte(getHL(), value);
        }
        private void highCarryFlag() // 0x37
        {
            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag(true);
        }
        private void jumpIfCarryFlagSetPlusN() // 0x38
        {
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (getCarryFlag())
            {
                setPC(value);
            }
        }
        private void addSPtoHL() //0x39
        {
            setHL(add16(getHL(), getSP(), Add16Type.HL));
        }
        private void lddAMemHL() // 0x3A
        {
            Byte value = mem.getByte(getHL());
            setA(value);

            decrementHL();
        }
        private void decSP() //0x3B
        {
            ushort value = getSP();

            value = decrement16(value);

            setSP(value);
        }
        private void incA() // 0x04
        {
            Byte value = getA();

            value = increment(value);

            setA(value);
        }
        private void decA() // 0x3D
        {
            Byte value = getA();

            value = decrement(value);

            setA(value);
        }
        private void ldA() //0x3E
        {
            Byte value = fetch();
            setA(value);
        }
        private void complementCarryFlag() // 0x3F
        {
            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag(!getCarryFlag());
        }
        private void ldrBB() // 0x40
        {
            Byte value = getB();
            setB(value);
        }
        private void ldrBC() // 0x41
        {
            Byte value = getC();
            setB(value);
        }
        private void ldrBD() // 0x42
        {
            Byte value = getD();
            setB(value);
        }
        private void ldrBE() // 0x43
        {
            Byte value = getE();
            setB(value);
        }
        private void ldrBH() // 0x44
        {
            Byte value = getH();
            setB(value);
        }
        private void ldrBL() // 0x45
        {
            Byte value = getL();
            setB(value);
        }
        private void ldrBFromMemHL() // 0x46
        {
            Byte value = mem.getByte(getHL());
            setB(value);
        }
        private void ldrBA() // 0x47
        {
            Byte value = getA();
            setB(value);
        }
        private void ldrCB() // 0x48
        {
            Byte value = getB();
            setC(value);
        }
        private void ldrCC() // 0x49
        {
            Byte value = getC();
            setC(value);
        }
        private void ldrCD() // 0x4A
        {
            Byte value = getD();
            setC(value);
        }
        private void ldrCE() // 0x4B
        {
            Byte value = getE();
            setC(value);
        }
        private void ldrCH() // 0x4C
        {
            Byte value = getH();
            setC(value);
        }
        private void ldrCL() // 0x4D
        {
            Byte value = getL();
            setC(value);
        }
        private void ldrCFromMemHL() // 0x4E
        {
            Byte value = mem.getByte(getHL());
            setC(value);
        }
        private void ldrCA() // 0x4F
        {
            Byte value = getA();
            setC(value);
        }
        private void ldrDB() // 0x50
        {
            Byte value = getB();
            setD(value);
        }
        private void ldrDC() // 0x51
        {
            Byte value = getC();
            setD(value);
        }
        private void ldrDD() // 0x52
        {
            Byte value = getD();
            setD(value);
        }
        private void ldrDE() // 0x53
        {
            Byte value = getE();
            setD(value);
        }
        private void ldrDH() // 0x54
        {
            Byte value = getH();
            setD(value);
        }
        private void ldrDL() // 0x55
        {
            Byte value = getL();
            setD(value);
        }
        private void ldrDFromMemHL() // 0x56
        {
            Byte value = mem.getByte(getHL());
            setD(value);
        }
        private void ldrDA() // 0x57
        {
            Byte value = getA();
            setD(value);
        }
        private void ldrEB() // 0x58
        {
            Byte value = getB();
            setE(value);
        }
        private void ldrEC() // 0x59
        {
            Byte value = getC();
            setE(value);
        }
        private void ldrED() // 0x5A
        {
            Byte value = getD();
            setE(value);
        }
        private void ldrEE() // 0x5B
        {
            Byte value = getE();
            setE(value);
        }
        private void ldrEH() // 0x5C
        {
            Byte value = getH();
            setE(value);
        }
        private void ldrEL() // 0x5D
        {
            Byte value = getL();
            setE(value);
        }
        private void ldrEFromMemHL() // 0x5E
        {
            Byte value = mem.getByte(getHL());
            setE(value);
        }
        private void ldrEA() // 0x5F
        {
            Byte value = getA();
            setE(value);
        }
        private void ldrHB() // 0x60
        {
            Byte value = getB();
            setH(value);
        }
        private void ldrHC() // 0x61
        {
            Byte value = getC();
            setH(value);
        }
        private void ldrHD() // 0x62
        {
            Byte value = getD();
            setH(value);
        }
        private void ldrHE() // 0x63
        {
            Byte value = getE();
            setH(value);
        }
        private void ldrHH() // 0x64
        {
            Byte value = getH();
            setH(value);
        }
        private void ldrHL() // 0x65
        {
            Byte value = getL();
            setH(value);
        }
        private void ldrHFromMemHL() // 0x66
        {
            Byte value = mem.getByte(getHL());
            setH(value);
        }
        private void ldrHA() // 0x67
        {
            Byte value = getA();
            setH(value);
        }
        private void ldrLB() // 0x68
        {
            Byte value = getB();
            setL(value);
        }
        private void ldrLC() // 0x69
        {
            Byte value = getC();
            setL(value);
        }
        private void ldrLD() // 0x6A
        {
            Byte value = getD();
            setL(value);
        }
        private void ldrLE() // 0x6B
        {
            Byte value = getE();
            setL(value);
        }
        private void ldrLH() // 0x6C
        {
            Byte value = getH();
            setL(value);
        }
        private void ldrLL() // 0x6D
        {
            Byte value = getL();
            setL(value);
        }
        private void ldrLFromMemHL() // 0x6E
        {
            Byte value = mem.getByte(getHL());
            setL(value);
        }
        private void ldrLA() // 0x6F
        {
            Byte value = getA();
            setL(value);
        }
        private void ldHLMemFromB() // 0x70
        {
            Byte value = getB();
            mem.setByte(getHL(), value);
        }
        private void ldHLMemFromC() // 0x71
        {
            Byte value = getC();
            mem.setByte(getHL(), value);
        }
        private void ldHLMemFromD() // 0x72
        {
            Byte value = getD();
            mem.setByte(getHL(), value);
        }
        private void ldHLMemFromE() // 0x73
        {
            Byte value = getE();
            mem.setByte(getHL(), value);
        }
        private void ldHLMemFromH() // 0x74
        {
            Byte value = getH();
            mem.setByte(getHL(), value);
        }
        private void ldHLMemFromL() // 0x75
        {
            Byte value = getL();
            mem.setByte(getHL(), value);
        }
        private void haltCPU() // 0x76
        {
            state = CPUState.Halted;
        }
        private void ldAIntoMemHL16() // 0x77
        {
            mem.setByte(getHL(), getA());
        }
        private void ldrAB() // 0x78
        {
            Byte value = getB();
            setA(value);
        }
        private void ldrAC() // 0x79
        {
            Byte value = getC();
            setA(value);
        }
        private void ldrAD() // 0x7A
        {
            Byte value = getD();
            setA(value);
        }
        private void ldrAE() // 0x7B
        {
            Byte value = getE();
            setA(value);
        }
        private void ldrAH() // 0x7C
        {
            Byte value = getH();
            setA(value);
        }
        private void ldrAL() // 0x7D
        {
            Byte value = getL();
            setA(value);
        }
        private void ldrAFromMemHL() // 0x7E
        {
            Byte value = mem.getByte(getHL());
            setA(value);
        }
        private void ldrAA() // 0x7F
        {
            Byte value = getA();
            setA(value);
        }
        private void addBtoA() // 0x80
        {
            Byte value = 0;
            value = add(getA(), getB());

            setA(value);
        }
        private void addCtoA() // 0x81
        {
            Byte value = 0;
            value = add(getA(), getC());

            setA(value);
        }
        private void addDtoA() // 0x82
        {
            Byte value = 0;
            value = add(getA(), getD());

            setA(value);
        }
        private void addEtoA() // 0x83
        {
            Byte value = 0;
            value = add(getA(), getE());

            setA(value);
        }
        private void addHtoA() // 0x84
        {
            Byte value = 0;
            value = add(getA(), getH());

            setA(value);
        }
        private void addLtoA() // 0x85
        {
            Byte value = 0;
            value = add(getA(), getL());

            setA(value);
        }
        private void addMemAtHLtoA() // 0x86
        {
            Byte value = mem.getByte(getHL());
            value = add(getA(), value);

            setA(value);
        }
        private void addAtoA() // 0x87
        {
            Byte value = getA();
            value = add(value, value);

            setA(value);
        }
        private void addCarryBtoA() // 0x88
        {
            Byte value = 0;
            value = addCarry(getA(), getB());

            setA(value);
        }
        private void addCarryCtoA() // 0x89
        {
            Byte value = 0;
            value = addCarry(getA(), getC());

            setA(value);
        }
        private void addCarryDtoA() // 0x8A
        {
            Byte value = 0;
            value = addCarry(getA(), getD());

            setA(value);
        }
        private void addCarryEtoA() // 0x8B
        {
            Byte value = 0;
            value = addCarry(getA(), getE());

            setA(value);
        }
        private void addCarryHtoA() // 0x8C
        {
            Byte value = 0;
            value = addCarry(getA(), getH());

            setA(value);
        }
        private void addCarryLtoA() // 0x8D
        {
            Byte value = 0;
            value = addCarry(getA(), getL());

            setA(value);
        }
        private void addCarryMemAtHLtoA() // 0x8E
        {
            Byte value = mem.getByte(getHL());
            value = addCarry(getA(), value);

            setA(value);
        }
        private void addCarryAtoA() // 0x8F
        {
            Byte value = 0;
            value = addCarry(value, value);

            setA(value);
        }
        private void subBFromA() // 0x90
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getB(), getA());

            setA(value);
        }
        private void subCFromA() // 0x91
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getC(), getA());

            setA(value);
        }
        private void subDFromA() // 0x92
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getD(), getA());

            setA(value);
        }
        private void subEFromA() // 0x93
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getE(), getA());

            setA(value);
        }
        private void subHFromA() // 0x94
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getH(), getA());

            setA(value);
        }
        private void subLFromA() // 0x95
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getL(), getA());

            setA(value);
        }
        private void subMemAtHLFromA() // 0x96
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(mem.getByte(getHL()), getA());

            setA(value);
        }
        private void subAFromA() // 0x97
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getA(), getA());

            setA(value);
        }
        private void subCarryBFromA() // 0x98
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtractCarry(getB(), getA());

            setA(value);
        }
        private void subCarryCFromA() // 0x99
        {
            Byte value = 0;

            // subtract(n, A)
            value = subtractCarry(getC(), getA());

            setA(value);
        }
        private void subCarryDFromA() // 0x9A
        {
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getD(), getA());

            setA(value);
        }
        private void subCarryEFromA() // 0x9B
        {
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getE(), getA());

            setA(value);
        }
        private void subCarryHFromA() // 0x9C
        {
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getH(), getA());

            setA(value);
        }
        private void subCarryLFromA() // 0x9D
        {
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getL(), getA());

            setA(value);
        }
        private void subCarryMemAtHLFromA() // 0x9E
        {
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(mem.getByte(getHL()), getA());

            setA(value);
        }
        private void subCarryAFromA() // 0x9F
        {
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getA(), getA());

            setA(value);
        }
        private void andABinA() // 0xA0
        {
            Byte value = 0;
            // and(n,A)
            value = and(getB(), getA());

            setA(value);
        }
        private void andACinA() // 0xA1
        {
            Byte value = 0;
            // and(n,A)
            value = and(getC(), getA());

            setA(value);
        }
        private void andADinA() // 0xA2
        {
            Byte value = 0;
            // and(n,A)
            value = and(getD(), getA());

            setA(value);
        }
        private void andAEinA() // 0xA3
        {
            Byte value = 0;
            // and(n,A)
            value = and(getE(), getA());

            setA(value);
        }
        private void andAHinA() // 0xA4
        {
            Byte value = 0;
            // and(n,A)
            value = and(getH(), getA());

            setA(value);
        }
        private void andALinA() // 0xA5
        {
            Byte value = 0;
            // and(n,A)
            value = and(getL(), getA());

            setA(value);
        }
        private void andAMemHLinA() // 0xA6
        {
            Byte value = mem.getByte(getHL());
            // and(n,A)
            value = and(value, getA());

            setA(value);
        }
        private void andAAinA() // 0xA7
        {
            Byte value = 0;
            // and(n,A)
            value = and(getA(), getA());

            setA(value);
        }
        private void xorABinA() // 0xA8
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getB(), getA());

            setA(value);
        }
        private void xorACinA() // 0xA9
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getC(), getA());

            setA(value);
        }
        private void xorADinA() // 0xAA
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getD(), getA());

            setA(value);
        }
        private void xorAEinA() // 0xAB
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getE(), getA());

            setA(value);
        }
        private void xorAHinA() // 0xAC
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getH(), getA());

            setA(value);
        }
        private void xorALinA() // 0xAD
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getL(), getA());

            setA(value);
        }
        private void xorAMemHLinA() // 0xAE
        {
            Byte value = mem.getByte(getHL());
            // xor(n,A)
            value = xor(value, getA());

            setA(value);
        }
        private void xorAAinA() // 0xAF
        {
            Byte value = 0;
            // xor(n,A)
            value = xor(getA(), getA());

            setA(value);
        }
        private void orABinA() // 0xB0
        {
            Byte value = 0;
            // or(n,A)
            value = or(getB(), getA());

            setA(value);
        }
        private void orACinA() // 0xB1
        {
            Byte value = 0;
            // or(n,A)
            value = or(getC(), getA());

            setA(value);
        }
        private void orADinA() // 0xB2
        {
            Byte value = 0;
            // or(n,A)
            value = or(getD(), getA());

            setA(value);
        }
        private void orAEinA() // 0xB3
        {
            Byte value = 0;
            // or(n,A)
            value = or(getE(), getA());

            setA(value);
        }
        private void orAHinA() // 0xB4
        {
            Byte value = 0;
            // or(n,A)
            value = or(getH(), getA());

            setA(value);
        }
        private void orALinA() // 0xB5
        {
            Byte value = 0;
            // or(n,A)
            value = or(getL(), getA());

            setA(value);
        }
        private void orAMemHLinA() // 0xB6
        {
            Byte value = mem.getByte(getHL());
            // or(n,A)
            value = or(value, getA());

            setA(value);
        }
        private void orAAinA() // 0xB7
        {
            Byte value = 0;
            // or(n,A)
            value = or(getA(), getA());

            setA(value);
        }
        private void cmpAB() // 0xB8
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getB(), getA());

            setA(value);
        }
        private void cmpAC() // 0xB9
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getC(), getA());

            setA(value);
        }
        private void cmpAD() // 0xBA
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getD(), getA());

            setA(value);
        }
        private void cmpAE() // 0xBB
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getE(), getA());

            setA(value);
        }
        private void cmpAH() // 0xBC
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getH(), getA());

            setA(value);
        }
        private void cmpAL() // 0xBD
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getL(), getA());

            setA(value);
        }
        private void cmpAMemHL() // 0xBE
        {
            Byte value = mem.getByte(getHL());
            // cmp(n,A)
            value = cmp(value, getA());

            setA(value);
        }
        private void cmpAA() // 0xBF
        {
            Byte value = 0;
            // cmp(n,A)
            value = cmp(getA(), getA());

            setA(value);
        }
        private void retIfZReset() // 0xC0
        {
            if (!getZeroFlag())
            {
                setPC(pop16OffStack());
            }
        }
        private void popIntoBC() //0xC1
        {
            Byte lsb = mem.getByte(getSP());
            incrementSP();
            Byte msb = mem.getByte(getSP());
            incrementSP();

            ushort value = (ushort)((msb << 8) + lsb);

            setBC(value);
        }
        private void jumpIfZeroFlagReset() // 0xC2
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = (ushort)((msb << 8) + lsb);
            if (!getZeroFlag())
            {
                setPC(value);
            }
        }
        private void jumpToNN() // 0xC3
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = (ushort)((msb << 8) + lsb);
            setPC(value);
        }
        private void callNNIfZReset() //0xC4
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            if (!getZeroFlag())
            {
                pushOnStack(getUInt16ForBytes(lsb, msb));
            }
        }
        private void pushBCToStack() //0xC5
        {
            byte[] value = getByteArrayForUInt16(getBC());
            pushOnStack(value);
        }
        private void addNtoA() // 0xC6
        {
            Byte value = fetch();
            value = add(getA(), value);

            setA(value);
        }
        private void rst00() // 0xC7
        {
            pushOnStack(getPC());
            setPC(0x0000);
        }
        private void retIfZSet() // 0xC8
        {
            if (getZeroFlag())
            {
                setPC(pop16OffStack());
            }
        }
        private void ret() // 0xC9
        {
            setPC(pop16OffStack());
        }
        private void jumpIfZeroFlagSet() // 0xCA
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = (ushort)((msb << 8) + lsb);
            if (getZeroFlag())
            {
                setPC(value);
            }
        }
        private void executeCBPrefixedOpCode() // 0xCB
        {
            Byte extOpCode = fetch();

            executeCBOperation(extOpCode);
        }
        private void callNNIfZSet() //0xCC
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            if (getZeroFlag())
            {
                pushOnStack(getUInt16ForBytes(lsb, msb));
            }
        }
        private void callNN() //0xCD
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            pushOnStack(getUInt16ForBytes(lsb, msb));
        }
        private void addCarryNtoA() // 0xCE
        {
            Byte value = fetch();
            Byte rv = 0;
            rv = addCarry(getA(), value);

            setA(rv);
        }
        private void rst08() // 0xCF
        {
            pushOnStack(getPC());
            setPC(0x0008);
        }
        private void retIfCarryReset() // 0xD0
        {
            if (!getCarryFlag())
            {
                setPC(pop16OffStack());
            }
        }
        private void popIntoDE() //0xD1
        {
            Byte lsb = mem.getByte(getSP());
            incrementSP();
            Byte msb = mem.getByte(getSP());
            incrementSP();

            ushort value = (ushort)((msb << 8) + lsb);

            setDE(value);
        }
        private void jumpIfCarryFlagReset() // 0xD2
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = (ushort)((msb << 8) + lsb);
            if (!getCarryFlag())
            {
                setPC(value);
            }
        }
        private void unusedD3() // 0xD3
        {
            throw new NotImplementedException(" 0xD3 is Unused");
        }
        private void callNNIfCReset() //0xD4
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            if (!getCarryFlag())
            {
                pushOnStack(getUInt16ForBytes(lsb, msb));
            }
        }
        private void pushDEToStack() //0xD5
        {
            byte[] value = getByteArrayForUInt16(getDE());
            pushOnStack(value);
        }
        private void subNFromA() // 0xD6
        {
            Byte value = fetch();

            // subtract(n, A)
            value = subtract(value, getA());

            setA(value);
        }
        private void rst10() // 0xD7
        {
            pushOnStack(getPC());
            setPC(0x0010);
        }
        private void retIfCarrySet() // 0xD8
        {
            if (getCarryFlag())
            {
                setPC(pop16OffStack());
            }
        }
        private void retEnableInterrupts() // 0xD9
        {
            setPC(pop16OffStack());
            shouldEnableInterrupts = true;
        }
        private void jumpIfCarryFlagSet() // 0xDA
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = (ushort)((msb << 8) + lsb);
            if (getCarryFlag())
            {
                setPC(value);
            }
        }
        private void unusedDB() // 0xDB
        {
            throw new NotImplementedException(" 0xDB is Unused");
        }
        private void callNNIfCSet() //0xDC
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            if (getCarryFlag())
            {
                pushOnStack(getUInt16ForBytes(lsb, msb));
            }
        }
        private void unusedDD() // 0xDD
        {
            throw new NotImplementedException(" 0xDD is Unused");
        }
        private void subCarryNFromA() // 0xDE
        {
            Byte value = fetch();

            // subtractCarry(n, A)
            value = subtractCarry(value, getA());

            setA(value);
        }
        private void rst18() // 0xDF
        {
            pushOnStack(getPC());
            setPC(0x0018);
        }
        private void putAIntoIOPlusMem() // 0xE0
        {
            Byte value = getA();
            Byte offset = fetch();
            mem.setByte((ushort)(0xFF00 + offset), value);
        }
        private void popIntoHL() //0xE1
        {
            Byte lsb = mem.getByte(getSP());
            incrementSP();
            Byte msb = mem.getByte(getSP());
            incrementSP();

            ushort value = (ushort)((msb << 8) + lsb);

            setHL(value);
        }
        private void ldAIntoIOPlusC() // 0xE2
        {
            Byte value = getA();
            mem.setByte((ushort)(0xFF00 + getC()), value);
        }
        private void unusedE3() // 0xE3
        {
            throw new NotImplementedException(" 0xE3 is Unused");
        }
        private void unusedE4() // 0xE4
        {
            throw new NotImplementedException(" 0xE4 is Unused");
        }
        private void pushHLToStack() //0xE5
        {
            byte[] value = getByteArrayForUInt16(getHL());
            pushOnStack(value);
        }
        private void andANinA() // 0xE6
        {
            Byte value = fetch();
            // and(n,A)
            value = and(value, getA());

            setA(value);
        }
        private void rst20() // 0xE7
        {
            pushOnStack(getPC());
            setPC(0x0020);
        }
        private void addNtoSP() // 0xE8
        {
            SByte offset = (SByte)fetch();
            ushort compValue = addSP(getSP(), offset);

            setSP(compValue);
        }
        private void jumpMemHL() // 0xE9
        {
            byte[] value = mem.getBytes(getHL(), 2);

            setPC(getUInt16ForBytes(value));
        }
        private void ldAIntoNN16() // 0xEA
        {
            Byte lsb = fetch();
            Byte msb = fetch();
            mem.setByte(getUInt16ForBytes(lsb, msb), getA());
        }
        private void unusedEB() // 0xEB
        {
            throw new NotImplementedException(" 0xEB is Unused");
        }
        private void unusedEC() // 0xEC
        {
            throw new NotImplementedException(" 0xEC is Unused");
        }
        private void unusedED() // 0xED
        {
            throw new NotImplementedException(" 0xED is Unused");
        }
        private void xorANinA() // 0xEE
        {
            Byte value = fetch();
            // xor(n,A)
            value = xor(value, getA());

            setA(value);
        }
        private void rst28() // 0xEF
        {
            pushOnStack(getPC());
            setPC(0x0028);
        }
        private void putIOPlusMemIntoA() // 0xF0
        {
            Byte offset = fetch();
            Byte value = mem.getByte((ushort)(0xFF00 + offset));
            
            setA(value);
        }
        private void popIntoAF() //0xF1
        {
            Byte lsb = mem.getByte(getSP());
            incrementSP();
            Byte msb = mem.getByte(getSP());
            incrementSP();

            ushort value = (ushort)((msb << 8) + lsb);

            setAF(value);
        }
        private void ldIOPlusCToA() // 0xF2
        {
            Byte value = mem.getByte((ushort)(0xFF00 + getC()));
            setA(value);
        }
        private void disableInterruptsAfterNextIns() // 0xF3
        {
            shouldDisableInterrupts = true;
        }
        private void unusedF4() // 0xF4
        {
            throw new NotImplementedException(" 0xF4 is Unused");
        }
        private void pushAFToStack() //0xF5
        {
            byte[] value = getByteArrayForUInt16(getAF());
            pushOnStack(value);
        }
        private void orANinA() // 0xF6
        {
            Byte value = fetch();
            // add(n,A)
            value = or(value, getA());

            setA(value);
        }
        private void rst30() // 0xF7
        {
            pushOnStack(getPC());
            setPC(0x0030);
        }
        private void ldHLFromSPPlusN() // 0xF8
        {
            SByte offset = (SByte)fetch();
            ushort compValue = addSP(getSP(), offset);

            setHL(compValue);
        }
        private void ldSPFromHL() // 0xF9
        {
            setSP(getHL());
        }
        private void ld16A() // 0xFA
        {
            int value1 = fetch();
            int value2 = fetch();
            ushort value = (ushort)((value2 << 8) + value1);
            setAF(value);
            // HACK this May Not Work??
        }
        private void enableInterruptsAfterNextIns() // 0xFB
        {
            shouldEnableInterrupts = true;
        }
        private void unusedFC() // 0xFC
        {
            throw new NotImplementedException(" 0xFC is Unused");
        }
        private void unusedFD() // 0xFD
        {
            throw new NotImplementedException(" 0xFD is Unused");
        }
        private void cmpAN() // 0xFE
        {
            Byte value = fetch();
            // cmp(n,A)
            value = cmp(value, getA());

            setA(value);
        }
        private void rst38() // 0xFF
        {
            pushOnStack(getPC());
            setPC(0x0038);
        }





        #endregion

        #region Instruction Helper Functions

        #region 8-Bit Math Functions
        private Byte increment(Byte value)
        {
            byte result = (byte)((((int)value) + 1) & 0xFF);

            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            // Ignore Carry Flag
            if (result == 0)
            {
                setZeroFlag(true);
            }
            else if ((value & 0x0F) == 0x0F)
            {
                value++;
                setHalfCarryFlag(true);
            }
            else
            {
                value++;
            }

            return result;
        }

        private Byte decrement(Byte value)
        {
            byte result = (byte)((((int)value) - 1) & 0xFF);

            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(true);
            // Ignore Carry Flag

            if (result == 0)
            {
                setZeroFlag(true);
            }

            if ((value & 0x0F) == 0x00)
            {
                setHalfCarryFlag(true);
            }

            return result;
        }

        private Byte add(Byte op1, Byte op2)
        {
            Byte value = (Byte)(((int)op1 + (int)op2) & 0xFF);
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            setCarryFlag(false);

            if (value == 0)
            {
                setZeroFlag(true);
            }
            if ((op1 & 0x0F) + (op2 & 0x0F) > 0x0F)
            {
                // Adding LSB Nibble > 0x0F (15)
                setHalfCarryFlag(true);
            }
            if ((op1 & 0xFF) + (op2 & 0xFF) > 0xFF)
            {
                // Overflow Detected result > 0xFF (255)
                setCarryFlag(true);
            }

            return value;
        }

        private Byte addCarry(Byte op1, Byte op2)
        {
            int carryFlag = (getCarryFlag()) ? 1 : 0;
            Byte value = (Byte)(((int)op1 + (int)op2 + carryFlag) & 0xFF);
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            setCarryFlag(false);

            if (value == 0)
            {
                setZeroFlag(true);
            }
            if (((op1 & 0x0F) + (op2 & 0x0F) + carryFlag) > 0x0F)
            {
                // Adding LSB Nibble > 0x0F (15)
                setHalfCarryFlag(true);
            }
            if (((op1 & 0xFF) + (op2 & 0xFF) + carryFlag) > 0xFF)
            {
                // Overflow Detected result > 0xFF (255)
                setCarryFlag(true);
            }

            return value;
        }

        private Byte subtract(Byte op1, Byte op2)
        {
            int result = (int)op2 - (int)op1;
            Byte value = (byte)((result & 0xFF));
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(true);
            setCarryFlag(false);

            if (value == 0)
            {
                setZeroFlag(true);
            }
            if (((op2 & 0xF) > (op1 & 0xF))) // Lower Nible of op2 is higher we need to borrow
            {
                setHalfCarryFlag(true);
            }
            if (op2 > op1) // Lower Nible of op2 is higher we need to borrow
            {
                setCarryFlag(true);
            }

            return value;
        }
        
        private Byte subtractCarry(Byte op1, Byte op2)
        {
            int carryFlag = (getCarryFlag()) ? 1 : 0;
            int result = (int)op2 - (int)op1 - carryFlag;
            Byte value = (byte)((result & 0xFF));
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(true);
            setCarryFlag(false);

            if (value == 0)
            {
                setZeroFlag(true);
            }
            if (((op2 ^ op1 ^ (value & 0xff)) & (1 << 4)) != 0)
            {
                setHalfCarryFlag(true);
            }
            if (result < 0)
            {
                setCarryFlag(true);
            }

            return value;
        }

        private Byte and(Byte op1, Byte op2)
        {
            setHalfCarryFlag(true);
            setSubtractFlag(false);
            setCarryFlag(false);
            setZeroFlag(false);

            byte value = (byte)((op2 & op1));

            if (value == 0)
            {
                setZeroFlag(true);
            }

            return value;
        }

        private Byte or(Byte op1, Byte op2)
        {
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            setCarryFlag(false);
            setZeroFlag(false);

            byte value = (byte)((op2 | op1));

            if (value == 0)
            {
                setZeroFlag(true);
            }

            return value;
        }

        private Byte xor(Byte op1, Byte op2)
        {
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            setCarryFlag(false);
            setZeroFlag(false);

            byte value = (byte)(((op2 ^ op1) & 0xFF));

            if (value == 0)
            {
                setZeroFlag(true);
            }

            return value;
        }

        private Byte cmp(Byte op1, Byte op2)
        {
            int result = (int)op2 - (int)op1;
            Byte value = (byte)((result & 0xFF));
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(true);
            setCarryFlag(false);

            if (value == 0)
            {
                setZeroFlag(true);
            }
            if (((op2 & 0xF) > (op1 & 0xF))) // Lower Nible of op2 is higher we need to borrow
            {
                setHalfCarryFlag(true);
            }
            if (op2 > op1) // Lower Nible of op2 is higher we need to borrow
            {
                setCarryFlag(true);
            }

            return value;
        }

        private Byte daa(byte arg)
        {
            int result = arg;
            Byte rv = 0;
            bool carryFlag = getCarryFlag();
            bool halfCarryFlag = getHalfCarryFlag();
            bool subFlag = getSubtractFlag();
            bool zeroFlag = getZeroFlag();

            setHalfCarryFlag(false);
            setZeroFlag(false);
            setCarryFlag(false);

            if (subFlag)
            {
                if (halfCarryFlag)
                {
                    result = (result - 6) & 0xff;
                }
                if (carryFlag)
                {
                    result = (result - 0x60) & 0xff;
                }
            }
            else
            {
                if (halfCarryFlag || (result & 0xf) > 9)
                {
                    result += 0x06;
                }
                if (carryFlag || result > 0x9f)
                {
                    result += 0x60;
                }
            }
            setHalfCarryFlag(false);
            if (result > 0xff)
            {
                setCarryFlag(true);
            }
            rv = (Byte) (result & 0xff);

            setZeroFlag(rv == 0);

            
            return rv;
        }

        private Byte rotateLeftCarry(Byte op1)
        {
            Byte result = (byte) ((op1 << 1) & 0xFF);
            if ((op1 & 0x80) != 0)
            {
                // rotated off a one
                result |= 0x01;
                setCarryFlag(true);
            }
            else
            {
                // rotated off a 0
                setCarryFlag(false);
            }
            setZeroFlag(false);
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        private Byte rotateLeft(Byte op1)
        {
            int carryFlag = (getCarryFlag()) ? 1 : 0;
            Byte result = (byte)((op1 << 1) & 0xFF);
            result = (byte)(result | carryFlag);
            setCarryFlag((op1 & 0x80) != 0);

            setZeroFlag(false);
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        private Byte rotateRightCarry(Byte op1)
        {
            Byte result = (byte)((op1 >> 1));
            if ((op1 & 0x01) == 1)
            {
                // rotated off a one
                result |= 0x80;
                setCarryFlag(true);
            }
            else
            {
                // rotated off a 0
                setCarryFlag(false);
            }
            setZeroFlag(false);
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        private Byte rotateRight(Byte op1)
        {
            Byte carryFlagAdj =(Byte) ((getCarryFlag()) ? 0x80 : 0);
            Byte result = (byte)((op1 >> 1));
            result = (byte)(result | carryFlagAdj);
            setCarryFlag((op1 & 0x01) != 0);

            setZeroFlag(false);
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        private Byte swapNibbles(byte value)
        {
            Byte msn =(Byte) ((value & 0x0F) << 4);
            Byte lsn = (Byte) ((value & 0xF0) >> 4);

            return ((Byte) (msn | lsn));
        }

        private Byte sla(byte value)
        {
            Byte rv = (byte)((((int)value) << 1) & 0xFF);

            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag((value & 0x80) != 0);
            setZeroFlag(rv == 0);


            return rv;
        }

        private Byte sra(byte value)
        {
            Byte rv = (Byte)(((((int)value) >> 1) | (((int)value) & 0x80)) & 0xFF);

            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag((value & 0x01) != 0);
            setZeroFlag(rv == 0);

            return rv;
        }

        private Byte srl(byte value)
        {
            Byte rv = (Byte)((((int)value) >> 1) & 0xFF);

            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag((value & 0x01) != 0);
            setZeroFlag(rv == 0);

            return rv;
        }


        #endregion

        #region 16-Bit Math Fucntions

        private ushort add16(ushort op1, ushort op2, Add16Type type)
        {
            ushort value = (ushort)(((int)op1 + (int)op2) & 0xFFFF);


            if (type == Add16Type.HL)
            {
                setHalfCarryFlag(false);
                setSubtractFlag(false);
                setCarryFlag(false);

                if ((op1 & 0x0fff) + (op2 & 0x0FFF) > 0x0FFF)
                {
                    setHalfCarryFlag(true);
                }
                if ((op1 & 0xFFFF) + (op2 & 0xFFFF) > 0xFFFF)
                {
                    // Overflow Detected result > 0xFFFF (65535)
                    setCarryFlag(true);
                }
            }
            else
            {
                // Doesnt Exist???? No 16-Bit ADD n,n
            }
            

            return value;
        }

        private ushort addSP(ushort sp, Byte n)
        {
            ushort value = (ushort)(((int)sp + (int)n) & 0xFFFF);
            setZeroFlag(false);
            setSubtractFlag(false);

            setHalfCarryFlag(false);
            setCarryFlag(false);

            if ((((sp & 0xff) + (n & 0xff)) & 0x100) != 0)
            {
                // overflow
                setCarryFlag(true);
            }
            if ((((sp & 0x0f) + (n & 0x0f)) & 0x10) != 0)
            {
                setHalfCarryFlag(true);

            }

            return value;
        }

        private ushort addSP(ushort sp, SByte n)
        {
            ushort value = (ushort)(((int)sp + (int)n) & 0xFFFF);
            setZeroFlag(false);
            setSubtractFlag(false);

            setHalfCarryFlag(false);
            setCarryFlag(false);

            if ((((sp & 0xff) + (n & 0xff)) & 0x100) != 0)
            {
                // overflow
                setCarryFlag(true);
            }
            if ((((sp & 0x0f) + (n & 0x0f)) & 0x10) != 0)
            {
                setHalfCarryFlag(true);

            }

            return value;
        }


        private ushort increment16(ushort value)
        {
            // Ignore All Flags
            if (value == 0xFFFF)
            {
                // Rollover
                value = 0;
            }
            else
            {
                value++;
            }

            return value;
        }



        private ushort decrement16(ushort value)
        {
            // Ignore All Flags
            if (value == 0)
            {
                value = 0xFFFF;
            }
            else
            {
                value--;
            }

            return value;
        }

        #endregion

        #region Misc Arithmatic functions

        private void decrementHL()
        {
            ushort valueToDecrement = getHL();
            valueToDecrement = decrement16(valueToDecrement);
            setHL(valueToDecrement);
        }

        private void incrementHL()
        {
            ushort valueToIncrement = getHL();
            valueToIncrement = increment16(valueToIncrement);
            setHL(valueToIncrement);
        }

        private void pushOnStack(Byte value)
        {
            mem.setByte(getSP(), value);
            decrementSP();
        }

        private void pushOnStack(ushort value)
        {
            byte[] tmp = getByteArrayForUInt16(value);
            pushOnStack(tmp[1]);
            pushOnStack(tmp[0]);
            
        }
        private void pushOnStack(byte[] value)
        {
            pushOnStack(value[1]);
            pushOnStack(value[0]);
        }

        private Byte popOffStack()
        {
            Byte value = mem.getByte(getSP());
            incrementSP();

            return value;
        }
        private ushort pop16OffStack()
        {
            Byte lsb = popOffStack();
            Byte msb = popOffStack();
            return getUInt16ForBytes(lsb, msb);
        }
        #endregion

        #region C# Data Rollover Helper Functions

        // Essentially same as CPU Helpers above but no flags are affected

        private Byte decrementIgnoreFlags(Byte value)
        {
            if (value == 0)
            {
                value = 0xFF;
            }
            else if (value == 1)
            {
                value--;
            }
            else
            {
                value--;
            }

            return value;
        }

        private Byte incrementIgnoreFlags(Byte value)
        {
            if (value == 0xFF)
            {
                // Rollover
                value = 0;
            }
            else if ((value & 0x0F) == 0x0F)
            {
                value++;
            }
            else
            {
                value++;
            }

            return value;
        }

        private Byte addIgnoreFlags(Byte value1, Byte value2)
        {
            return (Byte)((value1 + value2) & 0xFF);
        }

        private ushort add16IgnoreFlags(Byte value1, ushort value2)
        {
            return add16IgnoreFlags((ushort)value1, value2);
        }

        private ushort add16IgnoreFlags(ushort value1, Byte value2)
        {
            return add16IgnoreFlags(value1, (ushort) value2);
        }

        private ushort add16IgnoreFlags(ushort value1, SByte value2)
        {
            return (ushort)((value1 + value2) & 0xFFFF);
        }

        private ushort add16IgnoreFlags(ushort value1, ushort value2)
        {
            return (ushort)((value1 + value2) & 0xFFFF);
        }

        public byte[] getByteArrayForUInt16(ushort value)
        {
            // LSB First
            byte[] rv = new byte[2];
            rv[0] = (Byte)(value & 0x00FF);
            rv[1] = (Byte)(((value & 0xFF00) >> 8) & 0xFF);

            return rv;
        }

        public static ushort getUInt16ForBytes(Byte lsb, Byte msb)
        {
            return (ushort) (((msb << 8) + lsb) & 0xFFFF);
        }

        public static ushort getUInt16ForBytes(byte[] arr)
        {
            return getUInt16ForBytes(arr[0], arr[1]);
        }

        public SByte getSignedByte(byte val)
        {
            return (SByte)(((~val) + 1) & 0xFF);
        }

        #endregion

        #endregion
    }
}
