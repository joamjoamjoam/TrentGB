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

    public class CPUInstructionStack
    {
        public int capacity = 0;
        private List<Instruction> stack = new List<Instruction>();

        public CPUInstructionStack(int capacity)
        {
            this.capacity = capacity;
            stack = new List<Instruction>();
        }
        
        public void Push(Instruction inst)
        {
            if (inst != null)
            {
                stack.Insert(0, inst);

                if (stack.Count > capacity)
                {
                    // Dropout all items after capacity
                    for (int i = stack.Count - 1; i > capacity - 1; i--)
                    {
                        stack.RemoveAt(i);
                    }
                }
            }
            else
            {
                throw new NullReferenceException("Attempted to Push a null Instruction on to the Command Stack");
            }

        }

        public Instruction Pop()
        {
            Instruction ins = (stack.Count > 0) ? stack[0] : null;

            if (ins != null)
            {
                stack.RemoveAt(0);
            }

            return ins;
        }

        public Instruction Peek()
        {
            return (stack.Count > 0) ? stack[0] : null;
        }

        public List<Instruction> getStackCopy()
        {
            return stack.ConvertAll(ins => ins.Copy<Instruction>());
        }
    }

    public class Instruction
    {
        private byte cycles;
        public readonly byte opCode;
        public readonly byte length;
        public ushort address;
        public byte[] parameters = new byte[0];
        public readonly Func<bool> opFunc = null;
        public readonly String desc = "";

        public int executedCycles = 0;
        public int fetches = 0;
        public bool opReportedComplete = false; // Some Commands skip cycles if conditions are unmet see opCodeTranslatioDict for a list
        public ushort storage = 0;


        public Instruction(byte opCode, byte length, byte cycles , Func<bool> act, ushort PC, ROM rom)
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
                cycles = getCBCycles(rom.getByteDirect(PC));
            }

            parameters = new byte[length - 1];

            for (int i = 1; i < length; i++)
            {
                parameters[i] = rom.getByteDirect(PC + i);
            }
        }

        public Instruction(Instruction model, ushort PC, ROM rom)
        {
            // PC is at the adress for the first parametr if applicable

            this.opCode = model.opCode;
            this.cycles = model.cycles;
            this.length = model.length;
            this.address = PC;
            this.opFunc = model.opFunc;
            desc = opFunc.Method.Name;
            

            if (opCode == 0xCB && model.length == 2)
            {
                length = getCBOpLength();
                // Append CB Instruction Length and Cycles
                cycles = getCBCycles(rom.getByteDirect(PC));
            }

            parameters = new byte[length - 1];

            for (int i = 1; i < (length-1); i++)
            {
                parameters[i] = rom.getByteDirect(PC + i);
            }
        }

        public Instruction(Instruction model, ushort PC)
        {
            // PC is at the adress for the first parametr if applicable

            this.opCode = model.opCode;
            this.cycles = model.cycles;
            this.length = model.length;
            this.address = PC;
            this.opFunc = model.opFunc;
            desc = opFunc.Method.Name;


            if (opCode == 0xCB && model.length == 2)
            {
                length = getCBOpLength();
                cycles = 8; // Minimum Cycles for CB Ops
            }

            parameters = new byte[length - 1];
        }

        public Instruction(byte opCode, byte length, byte cycles , Func<bool> act)
        {
            // PC is at the adress for the first parametr if applicable

            this.opCode = opCode;
            this.cycles = cycles;
            this.length = length;
            this.opFunc = act;
            desc = act.Method.Name;

            parameters = new byte[length-1];
        }

        public bool execute()
        {
            incrementExecutedCycles();
            bool rv = false;
            if (!opReportedComplete)
            {
                rv = opFunc.Invoke();
                if (rv)
                {
                    opReportedComplete = true;
                }
            }
            else
            {
                throw new Exception($"OpFunc reported Done but we are still calling it. {ToString()}");
            }


            return rv;
        }

        public byte getExpectedCycles()
        {
            return cycles;
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
                    paramRealValue = $" ({CPU.getUInt16ForBytes(parameters[1], parameters[2]).ToString("X4")})";
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
            
            return $"Addr: {address.ToString("X4")}: OP: 0x{(opCode.ToString("X2"))} - {desc} {((parameters.Length > 0) ? "0x" : "")}{String.Join(", 0x", parameters.Select(b => b.ToString("X2")))}{paramRealValue}, Cycles: {getCycleCount()}/{cycles} {((getOpFuncReportedState()) ? "Done" : "")} Cycle Accurate Emulation: {((isCompleted()) ? "True" : "False")}";
        }

        private Byte getCBOpLength()
        {
            return 2;
        }

        private Byte getCBCycles(Byte cbOpCode)
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

        public void setCBCycles(Byte cbOpCode)
        {
            // Append CB Instruction Length and Cycles
            cycles = getCBCycles(cbOpCode);
        }

        private void incrementExecutedCycles()
        {
            executedCycles += 4;
        }

        public void incrementFetches()
        {
            fetches++;
        }

        public int getFetchCount()
        {
            return fetches;
        }
        public int getCycleCount()
        {
            return executedCycles;
        }

        public bool isCompleted()
        {
            return (executedCycles == cycles) && getOpFuncReportedState();
        }

        public bool getOpFuncReportedState() // Use this until all commands are cycle accurate
        {
            return opReportedComplete;
        }
    }


    public class CPU
    {
        public enum Add16Type
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

        public enum CPUFlagsMask
        {
            Carry = 0x10,
            HalfCarry = 0x20,
            Subtract = 0x40,
            Zero = 0x80
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

        public AddressSpace mem = null;
        private Dictionary<Byte, Instruction> opCodeTranslationDict = new Dictionary<byte, Instruction>();

        public bool shouldDisableInterrupts = false;
        public bool shouldEnableInterrupts = false;
        private long ticksSinceLastCycle = 0;

        public delegate void DebugFormHandler();
        public event EventHandler ShowDebugForm;

        public CPUInstructionStack commandHistoryList = new CPUInstructionStack(500);
        CPUDebugger.DebugType debugType = CPUDebugger.DebugType.Address;

        public ROM rom = null;

        // Debug
        Stopwatch clock = null;
        ushort breakAtInstruction = 0x0100;
        CPUDebugger debuggerForm = null;

        // Tsting/Cycle Accurate Counters
        private Instruction currentInstruction = null;

        public CPU(AddressSpace m, ROM rom, Stopwatch clock)
        {
            mem = m;
            this.rom = rom;
            loadOpCodeMap();
            this.clock = clock;
            debuggerForm = new CPUDebugger(this.commandHistoryList.getStackCopy());
        }

        public Instruction getCurrentInstruction()
        {
            return currentInstruction;
        }

        public Instruction getLastExecutedInstruction()
        {
            return commandHistoryList.Peek();
        }

        public Byte getByte(ushort address)
        {
            return mem.getByte(this, address);
        }

        public void setByte(ushort address, Byte value)
        {
            mem.setByte(this, address, value);
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

            Dictionary<String, String> romState = rom.getState();
            foreach (KeyValuePair<String, String> kp in romState)
            {
                rv.Add(kp.Key, kp.Value);
            }

            return rv;
        }

        public SpeedMode getSpeedMode()
        {
            // Hardcoded GB
            SpeedMode rv = SpeedMode.CGBSingleSpeed;
            if ((getByte(0xFF4D) & 0x80) > 0)
            {
                rv = SpeedMode.CGBDoubleSpeed;
            }
            else
            {
                rv = SpeedMode.CGBSingleSpeed;
            }

            return rv;
        }

        public static Byte generateFlagsByte(bool halfCarrySet, bool carrySet, bool subSet, bool zeroSet)
        {
            byte rv = 0;

            if (halfCarrySet)
            {
                rv += (Byte)CPUFlagsMask.HalfCarry;
            }
            if (carrySet)
            {
                rv += (Byte)CPUFlagsMask.Carry;
            }
            if (subSet)
            {
                rv += (Byte)CPUFlagsMask.Subtract;
            }
            if (zeroSet)
            {
                rv += (Byte)CPUFlagsMask.Zero;
            }

            return rv;
        }

        public String generateFlagsStr(short flags = -1)
        {
            Byte flagsByte = 0;
            bool carryFlag = false;
            bool halfCarry = false;
            bool zero = false;
            bool subtract = false;

            if (flags == -1)
            {
                flagsByte = getF();
                carryFlag = getCarryFlag();
                halfCarry = getHalfCarryFlag();
                zero = getZeroFlag();
                subtract = getSubtractFlag();
            }
            else
            {
                flagsByte = (byte)(flags & 0xFF);
                carryFlag = (flagsByte & (byte)CPUFlagsMask.Carry) > 0;
                zero = (flagsByte & (byte)CPUFlagsMask.Zero) > 0;
                halfCarry = (flagsByte & (byte)CPUFlagsMask.HalfCarry) > 0;
                subtract = (flagsByte & (byte)CPUFlagsMask.Subtract) > 0;
            }

            String rv = $"{((zero) ? "Z" : "-")}{((subtract) ? "N" : "-")}{((halfCarry) ? "H" : "-")}{((carryFlag) ? "C" : "-")}";

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

        public void enableDebugger()
        {
            debugType = CPUDebugger.DebugType.StopNext;
        }

        public void disableDebugger()
        {
            debugType = CPUDebugger.DebugType.None;
        }

        public new String ToString()
        {
            return $"Current Inst. = {((getCurrentInstruction() == null) ? "NULL" : getCurrentInstruction().ToString())} AF = 0x{getAF().ToString("X4")}, BC = 0x{getBC().ToString("X4")}, DE = 0x{getDE().ToString("X4")}, HL = 0x{getHL().ToString("X4")}, SP = 0x{getSP().ToString("X4")}, PC = 0x{getPC().ToString("X4")}, A = 0x{getA().ToString("X2")}, B = 0x{getB().ToString("X2")}, C = 0x{getC().ToString("X2")}, D = 0x{getD().ToString("X2")}, E = 0x{getE().ToString("X2")}, H = 0x{getH().ToString("X2")}, L = 0x{getL().ToString("X2")}, F = 0x{getF().ToString("X2")} ({generateFlagsStr()}), (BC) = 0x{mem.peekByte(getBC()).ToString("X2")}, (DE) = 0x{mem.peekByte(getDE()).ToString("X2")}, (HL) = 0x{mem.peekByte(getHL()).ToString("X2")}";
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
                //if (nextInstruction == 0x10)
                //{
                //    // Halt CPU and LCD

                //    // lcd.stop();
                //}

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
                    if (debugStopRequested())
                    {
                        clock.Stop();
                        //debugType = CPUDebugger.DebugType.None;
                        debuggerForm.updateMemoryWindow(getStateDict());
                        debuggerForm.setContinueAddr(getPC());
                        debuggerForm.updateDisassembledRom(commandHistoryList.getStackCopy());
                        debuggerForm.currentAddress = (ushort)((getPC() - 1) & 0xFFFF);
                        DialogResult res = debuggerForm.ShowDialog();
                        if (res == DialogResult.No || res == DialogResult.Cancel)
                        {
                            showAfterText = false;
                            debugType = CPUDebugger.DebugType.None;
                        }
                        else if (res == DialogResult.Ignore)
                        {
                            // Continue Pressed
                            setDebugParams(debuggerForm.getContinueAddr());
                        }
                        else
                        {
                            // yes Pressed
                            debugType = CPUDebugger.DebugType.StopNext;
                        }
                        debuggerForm.wait = showAfterText;

                        clock.Start();
                    }
                    if (debugType == CPUDebugger.DebugType.InstrCount)
                    {
                        if (breakAtInstruction > 0)
                        {
                            breakAtInstruction--;
                        }
                    }
                }
                else
                {

                    // Keep on Keepin On
                    ExecutionStatus rv = decodeAndExecute();
                    if (rv == ExecutionStatus.ErrorOpFinishedWithMismatchedCycles)
                    {
                        // We Should throw an Exception here when all commands are cycle accurate
                        //throw new ArgumentException($"Operation Reported Complete But not all Cycles were Used. {currentInstruction.ToString()}");
                    }
                }
            }
        }

        public void setDebugParams(CPUDebugger.DebugType type, ushort value)
        {
            debugType = type;
            breakAtInstruction = value;
        }
        public void setDebugParams(List<ushort> debugParams)
        {
            if (debugParams != null && debugParams.Count == 2)
            {
                CPUDebugger.DebugType type = (CPUDebugger.DebugType)debugParams[0];
                ushort address = debugParams[1];
                debugType = (CPUDebugger.DebugType)debugParams[0];
                switch (type)
                {
                    case CPUDebugger.DebugType.MemoryAccess:
                    case CPUDebugger.DebugType.MemoryRead:
                    case CPUDebugger.DebugType.MemoryWrite:
                        mem.setBreakPoint(address, type);
                        break;
                    default:
                        breakAtInstruction = debugParams[1];
                        break;
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
             
            setByte(0xFF05, 0x00); // TIMA
            setByte(0xFF06, 0x00); // TMA
            setByte(0xFF07, 0x00); // TAC
            setByte(0xFF10, 0x80); // NR10
            setByte(0xFF11, 0xBF); // NR11
            setByte(0xFF12, 0xF3); // NR12
            setByte(0xFF14, 0xBF); // NR14
            setByte(0xFF16, 0x3F); // NR21
            setByte(0xFF17, 0x00); // NR22
            setByte(0xFF19, 0xBF); // NR24
            setByte(0xFF1A, 0x7F); // NR30
            setByte(0xFF1B, 0xFF); // NR31
            setByte(0xFF1C, 0x9F); // NR32
            setByte(0xFF1E, 0xBF); // NR33
            setByte(0xFF20, 0xFF); // NR41
            setByte(0xFF21, 0x00); // NR42
            setByte(0xFF22, 0x00); // NR43
            setByte(0xFF23, 0xBF); // NR30
            setByte(0xFF24, 0x77); // NR50
            setByte(0xFF25, 0xF3); // NR51
            setByte(0xFF26, 0xF1); // 0xF1-GB, 0xF0-SGB  NR52
            setByte(0xFF40, 0x91); // LCDC
            setByte(0xFF42, 0x00); // SCY
            setByte(0xFF43, 0x00); // SCX
            setByte(0xFF45, 0x00); // LYC
            setByte(0xFF47, 0xFC); // BGP
            setByte(0xFF48, 0xFF); // OBP0
            setByte(0xFF49, 0xFF); // OBP1
            setByte(0xFF4A, 0x00); // WY
            setByte(0xFF4B, 0x00); // WX
            setByte(0xFFFF, 0x00); // IE
        }

        public Byte fetch()
        {
            Byte rv = getByte(PC++);
            if (currentInstruction != null)
            {
                currentInstruction.incrementFetches();

                // Add Fetch to Instructions Parameters Array if not opCode
                if (currentInstruction.getFetchCount() > 1)
                {
                    if (currentInstruction.getFetchCount() <= currentInstruction.length)
                    {
                        currentInstruction.parameters[currentInstruction.getFetchCount() - 2] = rv;
                    }
                    else
                    {
                        throw new Exception($"Attrempted to fetch a new Byte but we already met our length: {currentInstruction.ToString()}");
                    }
                    
                }
            }
            return rv;
        }

        public Byte peek(ushort n = 0)
        {
            ushort address = add16IgnoreFlags(getPC(), n);
            return getByte(address);
        }

        private bool debugStopRequested()
        {
            bool rv = false;
            ushort executingAddress = (getCurrentInstruction() != null) ? getCurrentInstruction().address : (ushort)(getPC() - 1);
            switch (debugType)
            {
                case CPUDebugger.DebugType.None:
                    rv = false;
                    break;
                case CPUDebugger.DebugType.InstrCount:
                    rv = (breakAtInstruction <= 0);
                    break;
                case CPUDebugger.DebugType.Address:
                    rv = (breakAtInstruction == executingAddress);
                    break;
                case CPUDebugger.DebugType.StopNext:
                    rv = true;
                    break;
                case CPUDebugger.DebugType.MemoryWrite:
                case CPUDebugger.DebugType.MemoryRead:
                case CPUDebugger.DebugType.MemoryAccess:
                    rv = mem.checkDebugRequests();
                    break;
                case CPUDebugger.DebugType.StopNextCall:
                    if (currentInstruction != null)
                    {
                        ushort nextOpCode = (currentInstruction.opCode == 0xCB) ? getUInt16ForBytes(currentInstruction.parameters[0], currentInstruction.opCode) : getUInt16ForBytes(currentInstruction.opCode, 0);
                        if (breakAtInstruction == nextOpCode)
                        {
                            rv = true;
                        }
                    }

                    break;
            }

            return rv;
        }

        bool showAfterText = false;
        public enum ExecutionStatus
        {
            None,
            OpStarted,
            OPFinished,
            ErrorOpFinishedWithMismatchedCycles
        }

        public ExecutionStatus decodeAndExecute()
        {
            ExecutionStatus rv = ExecutionStatus.None;
            Byte opCode = 0x00;
            bool enableInterrupts = shouldEnableInterrupts;
            bool disableInterrupts = shouldDisableInterrupts;
            bool opCompleted = false;

            if (currentInstruction == null)
            {
                // Is New Command
                currentInstruction = new Instruction(opCodeTranslationDict[peek()], getPC());
                rv = ExecutionStatus.OpStarted;
                opCode = fetch();

                // Debug Window
                String beforeText = $"Before:\nOP: {currentInstruction.ToString()}\n\nContinue Debugging??";
                showAfterText = false;

                if (debugStopRequested())
                {
                    clock.Stop();
                    //debugType = CPUDebugger.DebugType.None;
                    //rom.disassemble(opCodeTranslationDict);
                    debuggerForm.updateMemoryWindow(getStateDict());
                    debuggerForm.setContinueAddr(getPC());
                    List<Instruction> commandList = commandHistoryList.getStackCopy();
                    commandList.Insert(0, currentInstruction);
                    debuggerForm.updateDisassembledRom(commandList);
                    debuggerForm.currentAddress = (ushort)((getPC() - 1) & 0xFFFF);
                    debuggerForm.printText(beforeText);
                    DialogResult res = debuggerForm.ShowDialog();
                    if (res == DialogResult.No || res == DialogResult.Cancel)
                    {
                        showAfterText = false;
                        debugType = CPUDebugger.DebugType.None;
                    }
                    else if (res == DialogResult.Ignore)
                    {
                        // Continue Pressed
                        setDebugParams(debuggerForm.getContinueAddr());
                    }
                    else
                    {
                        // yes Pressed
                        showAfterText = debuggerForm.getShowAfterState();
                        debugType = CPUDebugger.DebugType.StopNext;
                    }

                    debuggerForm.wait = showAfterText;

                    clock.Start();
                }

                opCompleted = currentInstruction.execute();

                // HACK: All Commands are 2 cycle for now when they are all fixed for cycle accuraccy removw this.
                //if (((opCode >> 4) == 0) || opCode == 0xCB) 
                //{
                //    // Command is cycle accurate do Nothing
                //}
                //else
                //{
                //    // Command is not cycle accurate they only run once
                //    currentInstruction.earlyCompletionRequested = true;
                //}
            }
            else
            {
                opCode = currentInstruction.opCode;
                // Continue Execution
                opCompleted = currentInstruction.execute();





                // HACK: All Commands are 2 cycle for now when they are all fixed for cycle accuraccy removw this.
                //if ((opCode != 0xCB) || (opCode == 0xCB && getCurrentInstruction().getCycleCount() >= 8 && getCurrentInstruction().parameters[0] <= 0x17)) // CB functions from 0x00 to 0x08 are cycle accurate
                //{
                //    // Command is cycle accurate do Nothing
                //}
                //else
                //{
                //    // Command is not cycle accurate they only run once
                //    currentInstruction.earlyCompletionRequested = true;
                //}

            }


            if (opCompleted)
            {
                // Store it and reset for a new instruction
                commandHistoryList.Push(currentInstruction);
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
                    Instruction nextInst = new Instruction(opCodeTranslationDict[peek()], getPC());
                    String afterText = $"After: \nOP: {currentInstruction.ToString()}\n\n Next OP: {nextInst.ToString()}\n\nContinue Debugging??";
                    debuggerForm.updateMemoryWindow(getStateDict());
                    debuggerForm.updateDisassembledRom(commandHistoryList.getStackCopy());
                    DialogResult res = debuggerForm.ShowDialog(getPC());

                    while (debuggerForm.wait)
                    {
                        Thread.Sleep(200);
                    }

                    if (res == DialogResult.No || res == DialogResult.Cancel)
                    {
                        debugType = CPUDebugger.DebugType.None;
                        
                    }
                    else if (res == DialogResult.Ignore)
                    {
                        // Continue Pressed
                        setDebugParams(debuggerForm.getContinueAddr());
                    }
                    else
                    {
                        // yes Pressed
                        debugType = CPUDebugger.DebugType.StopNext;
                    }
                    clock.Start();
                }
                if (debugType == CPUDebugger.DebugType.InstrCount)
                {
                    if (breakAtInstruction > 0)
                    {
                        breakAtInstruction--;
                    }
                }

                if (!currentInstruction.isCompleted())
                {
                    // Operation returned Complete but we didnt use expected Cycles
                    // We Should throw execption when evrything is cycle accurate.
                    currentInstruction = null;
                    rv = ExecutionStatus.ErrorOpFinishedWithMismatchedCycles;
                    
                }
                else
                {
                    rv = ExecutionStatus.OPFinished;
                }

                // reset for next Instruction
                currentInstruction = null;
            }
            return rv;
        }

        public Instruction getInstructionForOpCode(Byte opCode)
        {
            return opCodeTranslationDict[opCode];
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
            opCodeTranslationDict.Add(0xCB, new Instruction(0xCB, 2, 8, executeCBPrefixedOpCode)); // 8 - 20
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
            opCodeTranslationDict.Add(0xE9, new Instruction(0xE9, 1, 4, jumpHL));
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
        private bool executeCBOperation()
        {
            bool done = false;
            if (getCurrentInstruction().getCycleCount() == 4)
            {
                // 0xCB fetched
            }
            else if (getCurrentInstruction().getCycleCount() >= 8)
            {
                // 0xCB prefixed Opcode fetched
                if (getCurrentInstruction().getCycleCount() == 8)
                {
                    getCurrentInstruction().parameters[0] = fetch();
                    getCurrentInstruction().setCBCycles(getCurrentInstruction().parameters[0]);
                }

                switch (getCurrentInstruction().parameters[0])
                {
                    case 0x00:
                        setB(rotateLeftCarry(getB()));
                        done = true;
                        break;
                    case 0x01:
                        setC(rotateLeftCarry(getC()));
                        done = true;
                        break;
                    case 0x02:
                        setD(rotateLeftCarry(getD()));
                        done = true;
                        break;
                    case 0x03:
                        setE(rotateLeftCarry(getE()));
                        done = true;
                        break;
                    case 0x04:
                        setH(rotateLeftCarry(getH()));
                        done = true;
                        break;
                    case 0x05:
                        setL(rotateLeftCarry(getL()));
                        done = true;
                        break;
                    case 0x06:
                        if (getCurrentInstruction().getCycleCount() == 12)
                        {
                            getCurrentInstruction().storage = getByte(getHL());
                        }
                        else if (getCurrentInstruction().getCycleCount() == 16)
                        {
                            setByte(getHL(), rotateLeftCarry((Byte) (getCurrentInstruction().storage & 0x00FF)));
                            done = true;
                        }
                        break;
                    case 0x07:
                        setA(rotateLeftCarry(getA()));
                        done = true;
                        break;
                    case 0x08:
                        setB(rotateRightCarry(getB()));
                        done = true;
                        break;
                    case 0x09:
                        setC(rotateRightCarry(getC()));
                        done = true;
                        break;
                    case 0x0A:
                        setD(rotateRightCarry(getD()));
                        done = true;
                        break;
                    case 0x0B:
                        setE(rotateRightCarry(getE()));
                        done = true;
                        break;
                    case 0x0C:
                        setH(rotateRightCarry(getH()));
                        done = true;
                        break;
                    case 0x0D:
                        setL(rotateRightCarry(getL()));
                        done = true;
                        break;
                    case 0x0E:
                        if (getCurrentInstruction().getCycleCount() == 12)
                        {
                            getCurrentInstruction().storage = getByte(getHL());
                        }
                        else if (getCurrentInstruction().getCycleCount() == 16)
                        {
                            setByte(getHL(), rotateRightCarry((Byte)(getCurrentInstruction().storage & 0x00FF)));
                            done = true;
                        }
                        break;
                    case 0x0F:
                        setA(rotateRightCarry(getA()));
                        done = true;
                        break;
                    case 0x10:
                        setB(rotateLeft(getB()));
                        done = true;
                        break;
                    case 0x11:
                        setC(rotateLeft(getC()));
                        done = true;
                        break;
                    case 0x12:
                        setD(rotateLeft(getD()));
                        done = true;
                        break;
                    case 0x13:
                        setE(rotateLeft(getE()));
                        done = true;
                        break;
                    case 0x14:
                        setH(rotateLeft(getH()));
                        done = true;
                        break;
                    case 0x15:
                        setL(rotateLeft(getL()));
                        done = true;
                        break;
                    case 0x16:
                        if (getCurrentInstruction().getCycleCount() == 12)
                        {
                            getCurrentInstruction().storage = getByte(getHL());
                        }
                        else if (getCurrentInstruction().getCycleCount() == 16)
                        {
                            setByte(getHL(), rotateLeft((Byte)(getCurrentInstruction().storage & 0x00FF)));
                            done = true;
                        }
                        
                        break;
                    case 0x17:
                        setA(rotateLeft(getA()));
                        done = true;
                        break;
                    case 0x18:
                        setB(rotateRight(getB()));
                        done = true;
                        break;
                    case 0x19:
                        setC(rotateRight(getC()));
                        done = true;
                        break;
                    case 0x1A:
                        setD(rotateRight(getD()));
                        done = true;
                        break;
                    case 0x1B:
                        setE(rotateRight(getE()));
                        done = true;
                        break;
                    case 0x1C:
                        setH(rotateRight(getH()));
                        done = true;
                        break;
                    case 0x1D:
                        setL(rotateRight(getL()));
                        done = true;
                        break;
                    case 0x1E:
                        ushort hl = getHL();
                        setByte(hl, rotateRight(getByte(hl)));
                        done = true;
                        break;
                    case 0x1F:
                        setA(rotateRight(getA()));
                        done = true;
                        break;
                    case 0x20:
                        setB(sla(getB()));
                        done = true;
                        break;
                    case 0x21:
                        setC(sla(getC()));
                        done = true;
                        break;
                    case 0x22:
                        setD(sla(getD()));
                        done = true;
                        break;
                    case 0x23:
                        setE(sla(getE()));
                        done = true;
                        break;
                    case 0x24:
                        setH(sla(getH()));
                        done = true;
                        break;
                    case 0x25:
                        setL(sla(getL()));
                        done = true;
                        break;
                    case 0x26:
                        hl = getHL();
                        setByte(hl, sla(getByte(hl)));
                        done = true;
                        break;
                    case 0x27:
                        setA(sla(getA()));
                        done = true;
                        break;
                    case 0x28:
                        setB(sra(getB()));
                        done = true;
                        break;
                    case 0x29:
                        setC(sra(getC()));
                        done = true;
                        break;
                    case 0x2A:
                        setD(sra(getD()));
                        done = true;
                        break;
                    case 0x2B:
                        setE(sra(getE()));
                        done = true;
                        break;
                    case 0x2C:
                        setH(sra(getH()));
                        done = true;
                        break;
                    case 0x2D:
                        setL(sra(getL()));
                        done = true;
                        break;
                    case 0x2E:
                        hl = getHL();
                        setByte(hl, sra(getByte(hl)));
                        done = true;
                        break;
                    case 0x2F:
                        setA(sra(getA()));
                        done = true;
                        break;
                    case 0x30:
                        setB(swapNibbles(getB()));
                        done = true;
                        break;
                    case 0x31:
                        setC(swapNibbles(getC()));
                        done = true;
                        break;
                    case 0x32:
                        setD(swapNibbles(getD()));
                        done = true;
                        break;
                    case 0x33:
                        setE(swapNibbles(getE()));
                        done = true;
                        break;
                    case 0x34:
                        setH(swapNibbles(getH()));
                        done = true;
                        break;
                    case 0x35:
                        setL(swapNibbles(getL()));
                        done = true;
                        break;
                    case 0x36:
                        hl = getHL();
                        setByte(hl, swapNibbles(getByte(hl)));
                        done = true;
                        break;
                    case 0x37:
                        setA(swapNibbles(getA()));
                        done = true;
                        break;
                    case 0x38:
                        setB(srl(getB()));
                        done = true;
                        break;
                    case 0x39:
                        setC(srl(getC()));
                        done = true;
                        break;
                    case 0x3A:
                        setD(srl(getD()));
                        done = true;
                        break;
                    case 0x3B:
                        setE(srl(getE()));
                        done = true;
                        break;
                    case 0x3C:
                        setH(srl(getH()));
                        done = true;
                        break;
                    case 0x3D:
                        setL(srl(getL()));
                        done = true;
                        break;
                    case 0x3E:
                        hl = getHL();
                        setByte(hl, srl(getByte(hl)));
                        done = true;
                        break;
                    case 0x3F:
                        setA(srl(getA()));
                        done = true;
                        break;
                    case 0x40:
                        executeBitOperation(BitOperationType.Test, 'B', 0x01);
                        done = true;
                        break;
                    case 0x41:
                        executeBitOperation(BitOperationType.Test, 'C', 0x01);
                        done = true;
                        break;
                    case 0x42:
                        executeBitOperation(BitOperationType.Test, 'D', 0x01);
                        done = true;
                        break;
                    case 0x43:
                        executeBitOperation(BitOperationType.Test, 'E', 0x01);
                        done = true;
                        break;
                    case 0x44:
                        executeBitOperation(BitOperationType.Test, 'H', 0x01);
                        done = true;
                        break;
                    case 0x45:
                        executeBitOperation(BitOperationType.Test, 'L', 0x01);
                        done = true;
                        break;
                    case 0x46:
                        executeBitOperation(BitOperationType.Test, 'M', 0x01);
                        done = true;
                        break;
                    case 0x47:
                        executeBitOperation(BitOperationType.Test, 'A', 0x01);
                        done = true;
                        break;
                    case 0x48:
                        executeBitOperation(BitOperationType.Test, 'B', 0x02);
                        done = true;
                        break;
                    case 0x49:
                        executeBitOperation(BitOperationType.Test, 'C', 0x02);
                        done = true;
                        break;
                    case 0x4A:
                        executeBitOperation(BitOperationType.Test, 'D', 0x02);
                        done = true;
                        break;
                    case 0x4B:
                        executeBitOperation(BitOperationType.Test, 'E', 0x02);
                        done = true;
                        break;
                    case 0x4C:
                        executeBitOperation(BitOperationType.Test, 'H', 0x02);
                        done = true;
                        break;
                    case 0x4D:
                        executeBitOperation(BitOperationType.Test, 'L', 0x02);
                        done = true;
                        break;
                    case 0x4E:
                        executeBitOperation(BitOperationType.Test, 'M', 0x02);
                        done = true;
                        break;
                    case 0x4F:
                        executeBitOperation(BitOperationType.Test, 'A', 0x02);
                        done = true;
                        break;
                    case 0x50:
                        executeBitOperation(BitOperationType.Test, 'B', 0x04);
                        done = true;
                        break;
                    case 0x51:
                        executeBitOperation(BitOperationType.Test, 'C', 0x04);
                        done = true;
                        break;
                    case 0x52:
                        executeBitOperation(BitOperationType.Test, 'D', 0x04);
                        done = true;
                        break;
                    case 0x53:
                        executeBitOperation(BitOperationType.Test, 'E', 0x04);
                        done = true;
                        break;
                    case 0x54:
                        executeBitOperation(BitOperationType.Test, 'H', 0x04);
                        done = true;
                        break;
                    case 0x55:
                        executeBitOperation(BitOperationType.Test, 'L', 0x04);
                        done = true;
                        break;
                    case 0x56:
                        executeBitOperation(BitOperationType.Test, 'M', 0x04);
                        done = true;
                        break;
                    case 0x57:
                        executeBitOperation(BitOperationType.Test, 'A', 0x04);
                        done = true;
                        break;
                    case 0x58:
                        executeBitOperation(BitOperationType.Test, 'B', 0x08);
                        done = true;
                        break;
                    case 0x59:
                        executeBitOperation(BitOperationType.Test, 'C', 0x08);
                        done = true;
                        break;
                    case 0x5A:
                        executeBitOperation(BitOperationType.Test, 'D', 0x08);
                        done = true;
                        break;
                    case 0x5B:
                        executeBitOperation(BitOperationType.Test, 'E', 0x08);
                        done = true;
                        break;
                    case 0x5C:
                        executeBitOperation(BitOperationType.Test, 'H', 0x08);
                        done = true;
                        break;
                    case 0x5D:
                        executeBitOperation(BitOperationType.Test, 'L', 0x08);
                        done = true;
                        break;
                    case 0x5E:
                        executeBitOperation(BitOperationType.Test, 'M', 0x08);
                        done = true;
                        break;
                    case 0x5F:
                        executeBitOperation(BitOperationType.Test, 'A', 0x08);
                        done = true;
                        break;
                    case 0x60:
                        executeBitOperation(BitOperationType.Test, 'B', 0x10);
                        done = true;
                        break;
                    case 0x61:
                        executeBitOperation(BitOperationType.Test, 'C', 0x10);
                        done = true;
                        break;
                    case 0x62:
                        executeBitOperation(BitOperationType.Test, 'D', 0x10);
                        done = true;
                        break;
                    case 0x63:
                        executeBitOperation(BitOperationType.Test, 'E', 0x10);
                        done = true;
                        break;
                    case 0x64:
                        executeBitOperation(BitOperationType.Test, 'H', 0x10);
                        done = true;
                        break;
                    case 0x65:
                        executeBitOperation(BitOperationType.Test, 'L', 0x10);
                        done = true;
                        break;
                    case 0x66:
                        executeBitOperation(BitOperationType.Test, 'M', 0x10);
                        done = true;
                        break;
                    case 0x67:
                        executeBitOperation(BitOperationType.Test, 'A', 0x10);
                        done = true;
                        break;
                    case 0x68:
                        executeBitOperation(BitOperationType.Test, 'B', 0x20);
                        done = true;
                        break;
                    case 0x69:
                        executeBitOperation(BitOperationType.Test, 'C', 0x20);
                        done = true;
                        break;
                    case 0x6A:
                        executeBitOperation(BitOperationType.Test, 'D', 0x20);
                        done = true;
                        break;
                    case 0x6B:
                        executeBitOperation(BitOperationType.Test, 'E', 0x20);
                        done = true;
                        break;
                    case 0x6C:
                        executeBitOperation(BitOperationType.Test, 'H', 0x20);
                        done = true;
                        break;
                    case 0x6D:
                        executeBitOperation(BitOperationType.Test, 'L', 0x20);
                        done = true;
                        break;
                    case 0x6E:
                        executeBitOperation(BitOperationType.Test, 'M', 0x20);
                        done = true;
                        break;
                    case 0x6F:
                        executeBitOperation(BitOperationType.Test, 'A', 0x20);
                        done = true;
                        break;
                    case 0x70:
                        executeBitOperation(BitOperationType.Test, 'B', 0x40);
                        done = true;
                        break;
                    case 0x71:
                        executeBitOperation(BitOperationType.Test, 'C', 0x40);
                        done = true;
                        break;
                    case 0x72:
                        executeBitOperation(BitOperationType.Test, 'D', 0x40);
                        done = true;
                        break;
                    case 0x73:
                        executeBitOperation(BitOperationType.Test, 'E', 0x40);
                        done = true;
                        break;
                    case 0x74:
                        executeBitOperation(BitOperationType.Test, 'H', 0x40);
                        done = true;
                        break;
                    case 0x75:
                        executeBitOperation(BitOperationType.Test, 'L', 0x40);
                        done = true;
                        break;
                    case 0x76:
                        executeBitOperation(BitOperationType.Test, 'M', 0x40);
                        done = true;
                        break;
                    case 0x77:
                        executeBitOperation(BitOperationType.Test, 'A', 0x40);
                        done = true;
                        break;
                    case 0x78:
                        executeBitOperation(BitOperationType.Test, 'B', 0x80);
                        done = true;
                        break;
                    case 0x79:
                        executeBitOperation(BitOperationType.Test, 'C', 0x80);
                        done = true;
                        break;
                    case 0x7A:
                        executeBitOperation(BitOperationType.Test, 'D', 0x80);
                        done = true;
                        break;
                    case 0x7B:
                        executeBitOperation(BitOperationType.Test, 'E', 0x80);
                        done = true;
                        break;
                    case 0x7C:
                        executeBitOperation(BitOperationType.Test, 'H', 0x80);
                        done = true;
                        break;
                    case 0x7D:
                        executeBitOperation(BitOperationType.Test, 'L', 0x80);
                        done = true;
                        break;
                    case 0x7E:
                        executeBitOperation(BitOperationType.Test, 'M', 0x80);
                        done = true;
                        break;
                    case 0x7F:
                        executeBitOperation(BitOperationType.Test, 'A', 0x80);
                        done = true;
                        break;
                    case 0x80:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x01);
                        done = true;
                        break;
                    case 0x81:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x01);
                        done = true;
                        break;
                    case 0x82:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x01);
                        done = true;
                        break;
                    case 0x83:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x01);
                        done = true;
                        break;
                    case 0x84:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x01);
                        done = true;
                        break;
                    case 0x85:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x01);
                        done = true;
                        break;
                    case 0x86:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x01);
                        done = true;
                        break;
                    case 0x87:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x01);
                        done = true;
                        break;
                    case 0x88:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x02);
                        done = true;
                        break;
                    case 0x89:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x02);
                        done = true;
                        break;
                    case 0x8A:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x02);
                        done = true;
                        break;
                    case 0x8B:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x02);
                        done = true;
                        break;
                    case 0x8C:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x02);
                        done = true;
                        break;
                    case 0x8D:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x02);
                        done = true;
                        break;
                    case 0x8E:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x02);
                        done = true;
                        break;
                    case 0x8F:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x02);
                        done = true;
                        break;
                    case 0x90:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x04);
                        done = true;
                        break;
                    case 0x91:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x04);
                        done = true;
                        break;
                    case 0x92:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x04);
                        done = true;
                        break;
                    case 0x93:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x04);
                        done = true;
                        break;
                    case 0x94:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x04);
                        done = true;
                        break;
                    case 0x95:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x04);
                        done = true;
                        break;
                    case 0x96:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x04);
                        done = true;
                        break;
                    case 0x97:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x04);
                        done = true;
                        break;
                    case 0x98:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x08);
                        done = true;
                        break;
                    case 0x99:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x08);
                        done = true;
                        break;
                    case 0x9A:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x08);
                        done = true;
                        break;
                    case 0x9B:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x08);
                        done = true;
                        break;
                    case 0x9C:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x08);
                        done = true;
                        break;
                    case 0x9D:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x08);
                        done = true;
                        break;
                    case 0x9E:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x08);
                        done = true;
                        break;
                    case 0x9F:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x08);
                        done = true;
                        break;
                    case 0xA0:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x10);
                        done = true;
                        break;
                    case 0xA1:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x10);
                        done = true;
                        break;
                    case 0xA2:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x10);
                        done = true;
                        break;
                    case 0xA3:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x10);
                        done = true;
                        break;
                    case 0xA4:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x10);
                        done = true;
                        break;
                    case 0xA5:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x10);
                        done = true;
                        break;
                    case 0xA6:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x10);
                        done = true;
                        break;
                    case 0xA7:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x10);
                        done = true;
                        break;
                    case 0xA8:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x20);
                        done = true;
                        break;
                    case 0xA9:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x20);
                        done = true;
                        break;
                    case 0xAA:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x20);
                        done = true;
                        break;
                    case 0xAB:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x20);
                        done = true;
                        break;
                    case 0xAC:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x20);
                        done = true;
                        break;
                    case 0xAD:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x20);
                        done = true;
                        break;
                    case 0xAE:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x20);
                        done = true;
                        break;
                    case 0xAF:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x20);
                        done = true;
                        break;
                    case 0xB0:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x40);
                        done = true;
                        break;
                    case 0xB1:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x40);
                        done = true;
                        break;
                    case 0xB2:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x40);
                        done = true;
                        break;
                    case 0xB3:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x40);
                        done = true;
                        break;
                    case 0xB4:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x40);
                        done = true;
                        break;
                    case 0xB5:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x40);
                        done = true;
                        break;
                    case 0xB6:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x40);
                        done = true;
                        break;
                    case 0xB7:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x40);
                        done = true;
                        break;
                    case 0xB8:
                        executeBitOperation(BitOperationType.Reset, 'B', 0x80);
                        done = true;
                        break;
                    case 0xB9:
                        executeBitOperation(BitOperationType.Reset, 'C', 0x80);
                        done = true;
                        break;
                    case 0xBA:
                        executeBitOperation(BitOperationType.Reset, 'D', 0x80);
                        done = true;
                        break;
                    case 0xBB:
                        executeBitOperation(BitOperationType.Reset, 'E', 0x80);
                        done = true;
                        break;
                    case 0xBC:
                        executeBitOperation(BitOperationType.Reset, 'H', 0x80);
                        done = true;
                        break;
                    case 0xBD:
                        executeBitOperation(BitOperationType.Reset, 'L', 0x80);
                        done = true;
                        break;
                    case 0xBE:
                        executeBitOperation(BitOperationType.Reset, 'M', 0x80);
                        done = true;
                        break;
                    case 0xBF:
                        executeBitOperation(BitOperationType.Reset, 'A', 0x80);
                        done = true;
                        break;
                    case 0xC0:
                        executeBitOperation(BitOperationType.Set, 'B', 0x01);
                        done = true;
                        break;
                    case 0xC1:
                        executeBitOperation(BitOperationType.Set, 'C', 0x01);
                        done = true;
                        break;
                    case 0xC2:
                        executeBitOperation(BitOperationType.Set, 'D', 0x01);
                        done = true;
                        break;
                    case 0xC3:
                        executeBitOperation(BitOperationType.Set, 'E', 0x01);
                        done = true;
                        break;
                    case 0xC4:
                        executeBitOperation(BitOperationType.Set, 'H', 0x01);
                        done = true;
                        break;
                    case 0xC5:
                        executeBitOperation(BitOperationType.Set, 'L', 0x01);
                        done = true;
                        break;
                    case 0xC6:
                        executeBitOperation(BitOperationType.Set, 'M', 0x01);
                        done = true;
                        break;
                    case 0xC7:
                        executeBitOperation(BitOperationType.Set, 'A', 0x01);
                        done = true;
                        break;
                    case 0xC8:
                        executeBitOperation(BitOperationType.Set, 'B', 0x02);
                        done = true;
                        break;
                    case 0xC9:
                        executeBitOperation(BitOperationType.Set, 'C', 0x02);
                        done = true;
                        break;
                    case 0xCA:
                        executeBitOperation(BitOperationType.Set, 'D', 0x02);
                        done = true;
                        break;
                    case 0xCB:
                        executeBitOperation(BitOperationType.Set, 'E', 0x02);
                        done = true;
                        break;
                    case 0xCC:
                        executeBitOperation(BitOperationType.Set, 'H', 0x02);
                        done = true;
                        break;
                    case 0xCD:
                        executeBitOperation(BitOperationType.Set, 'L', 0x02);
                        done = true;
                        break;
                    case 0xCE:
                        executeBitOperation(BitOperationType.Set, 'M', 0x02);
                        done = true;
                        break;
                    case 0xCF:
                        executeBitOperation(BitOperationType.Set, 'A', 0x02);
                        done = true;
                        break;
                    case 0xD0:
                        executeBitOperation(BitOperationType.Set, 'B', 0x04);
                        done = true;
                        break;
                    case 0xD1:
                        executeBitOperation(BitOperationType.Set, 'C', 0x04);
                        done = true;
                        break;
                    case 0xD2:
                        executeBitOperation(BitOperationType.Set, 'D', 0x04);
                        done = true;
                        break;
                    case 0xD3:
                        executeBitOperation(BitOperationType.Set, 'E', 0x04);
                        done = true;
                        break;
                    case 0xD4:
                        executeBitOperation(BitOperationType.Set, 'H', 0x04);
                        done = true;
                        break;
                    case 0xD5:
                        executeBitOperation(BitOperationType.Set, 'L', 0x04);
                        done = true;
                        break;
                    case 0xD6:
                        executeBitOperation(BitOperationType.Set, 'M', 0x04);
                        done = true;
                        break;
                    case 0xD7:
                        executeBitOperation(BitOperationType.Set, 'A', 0x04);
                        done = true;
                        break;
                    case 0xD8:
                        executeBitOperation(BitOperationType.Set, 'B', 0x08);
                        done = true;
                        break;
                    case 0xD9:
                        executeBitOperation(BitOperationType.Set, 'C', 0x08);
                        done = true;
                        break;
                    case 0xDA:
                        executeBitOperation(BitOperationType.Set, 'D', 0x08);
                        done = true;
                        break;
                    case 0xDB:
                        executeBitOperation(BitOperationType.Set, 'E', 0x08);
                        done = true;
                        break;
                    case 0xDC:
                        executeBitOperation(BitOperationType.Set, 'H', 0x08);
                        done = true;
                        break;
                    case 0xDD:
                        executeBitOperation(BitOperationType.Set, 'L', 0x08);
                        done = true;
                        break;
                    case 0xDE:
                        executeBitOperation(BitOperationType.Set, 'M', 0x08);
                        done = true;
                        break;
                    case 0xDF:
                        executeBitOperation(BitOperationType.Set, 'A', 0x08);
                        done = true;
                        break;
                    case 0xE0:
                        executeBitOperation(BitOperationType.Set, 'B', 0x10);
                        done = true;
                        break;
                    case 0xE1:
                        executeBitOperation(BitOperationType.Set, 'C', 0x10);
                        done = true;
                        break;
                    case 0xE2:
                        executeBitOperation(BitOperationType.Set, 'D', 0x10);
                        done = true;
                        break;
                    case 0xE3:
                        executeBitOperation(BitOperationType.Set, 'E', 0x10);
                        done = true;
                        break;
                    case 0xE4:
                        executeBitOperation(BitOperationType.Set, 'H', 0x10);
                        done = true;
                        break;
                    case 0xE5:
                        executeBitOperation(BitOperationType.Set, 'L', 0x10);
                        done = true;
                        break;
                    case 0xE6:
                        executeBitOperation(BitOperationType.Set, 'M', 0x10);
                        done = true;
                        break;
                    case 0xE7:
                        executeBitOperation(BitOperationType.Set, 'A', 0x10);
                        done = true;
                        break;
                    case 0xE8:
                        executeBitOperation(BitOperationType.Set, 'B', 0x20);
                        done = true;
                        break;
                    case 0xE9:
                        executeBitOperation(BitOperationType.Set, 'C', 0x20);
                        done = true;
                        break;
                    case 0xEA:
                        executeBitOperation(BitOperationType.Set, 'D', 0x20);
                        done = true;
                        break;
                    case 0xEB:
                        executeBitOperation(BitOperationType.Set, 'E', 0x20);
                        done = true;
                        break;
                    case 0xEC:
                        executeBitOperation(BitOperationType.Set, 'H', 0x20);
                        done = true;
                        break;
                    case 0xED:
                        executeBitOperation(BitOperationType.Set, 'L', 0x20);
                        done = true;
                        break;
                    case 0xEE:
                        executeBitOperation(BitOperationType.Set, 'M', 0x20);
                        done = true;
                        break;
                    case 0xEF:
                        executeBitOperation(BitOperationType.Set, 'A', 0x20);
                        done = true;
                        break;
                    case 0xF0:
                        executeBitOperation(BitOperationType.Set, 'B', 0x40);
                        done = true;
                        break;
                    case 0xF1:
                        executeBitOperation(BitOperationType.Set, 'C', 0x40);
                        done = true;
                        break;
                    case 0xF2:
                        executeBitOperation(BitOperationType.Set, 'D', 0x40);
                        done = true;
                        break;
                    case 0xF3:
                        executeBitOperation(BitOperationType.Set, 'E', 0x40);
                        done = true;
                        break;
                    case 0xF4:
                        executeBitOperation(BitOperationType.Set, 'H', 0x40);
                        done = true;
                        break;
                    case 0xF5:
                        executeBitOperation(BitOperationType.Set, 'L', 0x40);
                        done = true;
                        break;
                    case 0xF6:
                        executeBitOperation(BitOperationType.Set, 'M', 0x40);
                        done = true;
                        break;
                    case 0xF7:
                        executeBitOperation(BitOperationType.Set, 'A', 0x40);
                        done = true;
                        break;
                    case 0xF8:
                        executeBitOperation(BitOperationType.Set, 'B', 0x80);
                        done = true;
                        break;
                    case 0xF9:
                        executeBitOperation(BitOperationType.Set, 'C', 0x80);
                        done = true;
                        break;
                    case 0xFA:
                        executeBitOperation(BitOperationType.Set, 'D', 0x80);
                        done = true;
                        break;
                    case 0xFB:
                        executeBitOperation(BitOperationType.Set, 'E', 0x80);
                        done = true;
                        break;
                    case 0xFC:
                        executeBitOperation(BitOperationType.Set, 'H', 0x80);
                        done = true;
                        break;
                    case 0xFD:
                        executeBitOperation(BitOperationType.Set, 'L', 0x80);
                        done = true;
                        break;
                    case 0xFE:
                        executeBitOperation(BitOperationType.Set, 'M', 0x80);
                        done = true;
                        break;
                    case 0xFF:
                        executeBitOperation(BitOperationType.Set, 'A', 0x80);
                        done = true;
                        break;
                }
            }
            return done;
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
                        set = ((getByte(getHL()) & mask) > 0);
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
                        setByte(val, (byte)((getByte(val) | mask)));
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
                        setByte(val, (byte)((getByte(val) & (~mask))));
                        break;
                }
            }
        }

        #region Interrupt Operations
        private bool isAnInterruptRequested()
        {
            return ((getByte(0xFF0F) & 0x01F) > 0);
        }

        private bool isInterruptRequested(InterruptType type)
        {
            return ((getByte(0xFF0F) & (int)type) > 0);
        }

        private bool isInterruptEnabled(InterruptType type)
        {
            bool rv = false;
            if (IME && ((getByte(0xFFFF) & (int)type) > 0))
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
                    setByte(0xFF0F, (Byte)((getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.LCDStat:
                    handlerAddress = 0x48;
                    setByte(0xFF0F, (Byte)((getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.Timer:
                    handlerAddress = 0x50;
                    setByte(0xFF0F, (Byte)((getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.Serial:
                    handlerAddress = 0x58;
                    setByte(0xFF0F, (Byte)((getByte(0xFF0F) & ~(int)type)));
                    break;
                case InterruptType.Joypad:
                    handlerAddress = 0x60;
                    setByte(0xFF0F, (Byte)((getByte(0xFF0F) & ~(int)type)));
                    break;
            }

            pushOnStack(getPC());
            setPC(handlerAddress);
            state = CPUState.Running;

            return rv;
        }
        #endregion


        #region Register Operations

        public Byte getA()
        {
            return A;
        }
        public void setA(Byte value)
        {
            A = value;
        }

        public Byte getB()
        {
            return B;
        }
        public void setB(Byte value)
        {
            B = value;
        }

        public Byte getC()
        {
            return C;
        }
        public void setC(Byte value)
        {
            C = value;
        }

        public Byte getD()
        {
            return D;
        }
        public void setD(Byte value)
        {
            D = value;
        }

        public Byte getE()
        {
            return E;
        }
        public void setE(Byte value)
        {
            E = value;
        }

        public Byte getH()
        {
            return H;
        }
        public void setH(Byte value)
        {
            H = value;
        }

        public Byte getL()
        {
            return L;
        }
        public void setL(Byte value)
        {
            L = value;
        }

        public Byte getF()
        {
            return F;
        }
        public void setF(Byte value)
        {
            Byte res = (Byte)(value & 0xF0); // cant Write to lower 4 bits of F
            F = res;
        }


        public ushort getSP()
        {
            return SP;
        }
        public void setSP(ushort value)
        {
            SP = value;
        }

        public ushort getPC()
        {
            return PC;
        }
        public void setPC(ushort value)
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

        public void setAF(ushort value)
        {
            A = (Byte)((value & 0xFF00) >> 8);
            F = (Byte)(value & 0x00F0); // Cant Write to lower 4 bits of F
        }

        public ushort getBC()
        {
            ushort rv = 0;
            rv = (ushort)((B << (ushort)8) | C);
            return rv;
        }

        public void setBC(ushort value)
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

        public void setDE(ushort value)
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

        public void setHL(ushort value)
        {
            H = (Byte)((value & 0xFF00) >> 8);
            L = (Byte)(value & 0x00FF);
        }

        public bool getCarryFlag()
        {

            return ((F & (Byte)CPUFlagsMask.Carry) > 0);
        }

        public void setCarryFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | (Byte)CPUFlagsMask.Carry);
            }
            else
            {
                F = (Byte)(F & (~(Byte)CPUFlagsMask.Carry));
            }
        }

        public bool getHalfCarryFlag()
        {

            return ((F & (Byte)CPUFlagsMask.HalfCarry) > 0);
        }

        private void setHalfCarryFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | (Byte)CPUFlagsMask.HalfCarry);
            }
            else
            {
                F = (Byte)(F & (~(Byte)CPUFlagsMask.HalfCarry));
            }
        }

        public bool getSubtractFlag()
        {

            return ((F & (Byte)CPUFlagsMask.Subtract) > 0);
        }

        private void setSubtractFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | (Byte)CPUFlagsMask.Subtract);
            }
            else
            {
                F = (Byte)(F & (~(Byte)CPUFlagsMask.Subtract));
            }
        }

        public bool getZeroFlag()
        {

            return ((F & (Byte)CPUFlagsMask.Zero) > 0);
        }

        private void setZeroFlag(bool on)
        {
            if (on)
            {
                F = (Byte)(F | (Byte)CPUFlagsMask.Zero);
            }
            else
            {
                F = (Byte)(F & (~(Byte)CPUFlagsMask.Zero));
            }
        }

        #endregion

        #region CPU Instructions
        private bool opNOP() // OP Code 0x00
        {
			bool done = true;
            return done;
        }

        private bool ldBC16() // 0x01
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                // load C
                Byte value = fetch();
                setC(value);
            }
            else if (currentInstruction.getCycleCount() == 12)
            {
                // load B
                Byte value = fetch();
                setB(value);
                done = true;
            }

            return done;
        }
        private bool ldAToMemBC() //0x02
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = getA();
                setByte(getBC(), value);
                done = true;
            }

            return done;
        }
        private bool incBC() //0x03
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                ushort value = getBC();

                currentInstruction.storage = increment16(value);

                setC(getLSB((ushort)currentInstruction.storage));
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setB(getMSB((ushort)currentInstruction.storage));
                done = true;
            }

            return done;
        }
        private bool incB() // 0x04
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                Byte value = getB();

                value = increment(value);

                setB(value);
                done = true;
            }

            return done;
        }
        private bool decB() // 0x05
        {
			bool done = false;
            Byte value = getB();

            value = decrement(value);

            setB(value);

            done = true;
            return done;
        }
        private bool ldB() // 0x06
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = fetch();
                setB(value);
                done = true;
            }

            return done;
        }
        private bool rlcA() // 0x07
        {
			bool done = false;
            setA(rotateLeftCarry(getA(), updateZeroFlag: false));
            done = true;
            return done;
        }
        private bool ldSPFromMem16() // 0x08
        {
			bool done = false;

            if (currentInstruction.getCycleCount() == 8)
            {
                currentInstruction.storage = (ushort)(fetch()); // lower
            }
            else if (currentInstruction.getCycleCount() == 12)
            {
                currentInstruction.storage = getUInt16ForBytes(getLSB((ushort)currentInstruction.storage), fetch());
            }
            else if (currentInstruction.getCycleCount() == 16)
            {
                ushort value = getUInt16ForBytes(getLSB((ushort)currentInstruction.storage), getMSB(getSP()));
                setSP(value);
            }
            else if (currentInstruction.getCycleCount() == 20)
            {
                setSP((ushort)currentInstruction.storage);
                done = true;
            }

            return done;
        }
        private bool addBCtoHL() //0x09
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                currentInstruction.storage = add16(getHL(), getBC(), Add16Type.HL);
                setL(getLSB((ushort)currentInstruction.storage)); 
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setH(getMSB((ushort)currentInstruction.storage));
                done = true;
            }

            return done;
        }
        private bool ldAMemBC() // 0x0A
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = getByte(getBC());
                setA(value);
                done = true;
            }

            return done;
        }
        private bool decBC() //0x0B
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                ushort value = getBC();

                currentInstruction.storage = decrement16(value);

                setC(getLSB((ushort)currentInstruction.storage));
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setB(getMSB((ushort)currentInstruction.storage));
                done = true;
            }

            return done;
        }
        private bool incC() // 0x0C
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                Byte value = getC();

                value = increment(value);

                setC(value);
                done = true;
            }

            return done;
        }
        private bool decC() // 0x0D
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                Byte value = getC();

                value = decrement(value);

                setC(value);
                done = true;
            }

            return done;
        }
        private bool ldC() // 0x0E
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = fetch();
                setC(value);
                done = true;
            }

            return done;
        }
        private bool rrcA() // 0x0F
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                setA(rotateRightCarry(getA(), updateZeroFlag: false));
                done = true;
            }

            return done;
        }
        // TODO: Trent You Stopped here. Cycle Accuracy Stops Here

        private bool stopCPU() // 0x10
        {
			bool done = false;
            Byte command = fetch();

            if (command == 0x00)
            {
                state = CPUState.Stopped;
            }
            else
            {
                //throw new NotImplementedException($"0x10 prefix Command 0x{command.ToString("X2")} is not implented.");
            }

            done = true;
            return done;
        }
        private bool ldDE16() // 0x11
        {
			bool done = false;
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setDE(rv);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldAIntoMemDE16() // 0x12
        {
			bool done = false;
            setByte(getDE(), getA());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool incDE() //0x13
        {
			bool done = false;
            ushort value = getDE();

            value = increment16(value);

            setDE(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool incD() // 0x14
        {
			bool done = false;
            Byte value = getD();

            value = increment(value);

            setD(value);
            done = true;
            return done;
        }
        private bool decD() // 0x15
        {
			bool done = false;
            Byte value = getD();

            value = decrement(value);

            setD(value);
            done = true;
            return done;
        }
        private bool ldD() // 0x16
        {
			bool done = false;
            Byte value = fetch();
            setD(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rlA() // 0x17
        {
			bool done = false;
            setA(rotateLeft(getA(), updateZeroFlag: false));
            done = true;
            return done;
        }
        private bool jumpPCPlusN() // 0x18
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                SByte offset = (SByte)fetch();
                currentInstruction.storage = add16IgnoreFlags(getPC(), offset);
            }
            else if (currentInstruction.getCycleCount() == 12)
            {
                setPC(currentInstruction.storage);
                done = true;
            }
            
            return done;
        }
        private bool addDEtoHL() //0x19
        {
			bool done = false;
            setHL(add16(getHL(), getDE(), Add16Type.HL));
            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldAMemDE() // 0x1A
        {
			bool done = false;
            Byte value = getByte(getDE());
            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool decDE() //0x1B
        {
			bool done = false;
            ushort value = getDE();

            value = decrement16(value);

            setDE(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool incE() // 0x1C
        {
			bool done = false;
            Byte value = getE();

            value = increment(value);

            setE(value);
            done = true;
            return done;
        }
        private bool decE() // 0x1D
        {
			bool done = false;
            Byte value = getE();

            value = decrement(value);

            setE(value);
            done = true;
            return done;
        }
        private bool ldE() // 0x1E
        {
			bool done = false;
            Byte value = fetch();
            setE(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rrA() // 0x1F
        {
			bool done = false;
            setA(rotateRight(getA(), updateZeroFlag: false));
            done = true;
            return done;
        }
        private bool jumpIfZeroFlagResetPlusN() // 0x20
        {
			bool done = false;
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (!getZeroFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHL16() // 0x21
        {
			bool done = false;
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setHL(rv);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldiMemHLWithA() // 0x22 8 Cycles
        {
			bool done = false;
            Byte value = getA();
            setByte(getHL(), value);

            incrementHL();

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool incHL() //0x23
        {
			bool done = false;
            ushort value = getHL();

            value = increment16(value);

            setHL(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool incH() // 0x24
        {
			bool done = false;
            Byte value = getH();

            value = increment(value);

            setH(value);
            done = true;
            return done;
        }
        private bool decH() // 0x25
        {
			bool done = false;
            Byte value = getH();

            value = decrement(value);

            setH(value);
            done = true;
            return done;
        }
        private bool ldH() // 0x26
        {
			bool done = false;
            Byte value = fetch();
            setH(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool daaRegA() // 0x27
        {
			bool done = false;
            Byte value = 0;

            value = daa(getA());

            setA(value);
            done = true;
            return done;
        }
        private bool jumpIfZeroFlagSetPlusN() // 0x28
        {
			bool done = false;
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (getZeroFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool addHLtoHL() //0x29
        {
			bool done = false;
            setHL(add16(getHL(), getHL(), Add16Type.HL));

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldiAMemHL() // 0x2A
        {
			bool done = false;
            Byte value = getByte(getHL());
            setA(value);

            incrementHL();

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool decHL() //0x2B
        {
			bool done = false;
            ushort value = getHL();

            value = decrement16(value);

            setHL(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool incL() // 0x2C
        {
			bool done = false;
            Byte value = getL();

            value = increment(value);

            setL(value);

            done = true;
            return done;
        }
        private bool decL() // 0x2D
        {
			bool done = false;
            Byte value = getL();

            value = decrement(value);

            setL(value);

            done = true;
            return done;
        }
        private bool ldL() // 0x2E
        {
			bool done = false;
            Byte value = fetch();
            setL(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool complementA() // 0x2F
        {
			bool done = false;
            Byte value = (Byte)((~getA()) & 0xFF);

            setSubtractFlag(true);
            setHalfCarryFlag(true);
            setA(value);

            done = true;
            return done;
        }
        private bool jumpIfCarryFlagResetPlusN() // 0x30
        {
			bool done = false;
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (!getCarryFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldSP16() // 0x31
        {
			bool done = false;
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setSP(rv);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool lddMemHLWithA() // 0x32
        {
			bool done = false;
            Byte value = getA();
            setByte(getHL(), value);

            decrementHL();

            done = true;
            return done;
        }
        private bool incSP() //0x33
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                currentInstruction.storage =increment16(getSP());
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.LSB, currentInstruction.storage), BytePlacement.LSB, getSP()));
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.MSB, currentInstruction.storage), BytePlacement.MSB, getSP()));
                done = true;
            }

            return done;
        }
        private bool incHLMem() // 0x34
        {
			bool done = false;
            Byte value = getByte(getHL());

            value = increment(value);

            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool decHLMem() // 0x35
        {
			bool done = false;
            Byte value = getByte(getHL());

            value = decrement(value);

            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLMem() // 0x36
        {
			bool done = false;
            Byte value = fetch();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool highCarryFlag() // 0x37
        {
			bool done = false;
            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag(true);
            done = true;
            return done;
        }
        private bool jumpIfCarryFlagSetPlusN() // 0x38
        {
			bool done = false;
            SByte offset = (SByte)fetch();
            ushort value = add16IgnoreFlags(getPC(), offset);
            if (getCarryFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool addSPtoHL() //0x39
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                currentInstruction.storage = add16(getHL(), getSP(), Add16Type.HL);
                setL(getByteInUInt16(BytePlacement.LSB, currentInstruction.storage));
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setH(getByteInUInt16(BytePlacement.MSB, currentInstruction.storage));
                done = true;
            }

            return done;
        }
        private bool lddAMemHL() // 0x3A
        {
			bool done = false;
            Byte value = getByte(getHL());
            setA(value);

            decrementHL();

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool decSP() //0x3B
        {
            bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                currentInstruction.storage = decrement16(getSP());
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.LSB, currentInstruction.storage), BytePlacement.LSB, getSP()));
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.MSB, currentInstruction.storage), BytePlacement.MSB, getSP()));
                done = true;
            }

            return done;
        }
        private bool incA() // 0x04
        {
			bool done = false;
            Byte value = getA();

            value = increment(value);

            setA(value);
            done = true;
            return done;
        }
        private bool decA() // 0x3D
        {
			bool done = false;
            Byte value = getA();

            value = decrement(value);

            setA(value);
            done = true;
            return done;
        }
        private bool ldA() //0x3E
        {
			bool done = false;
            Byte value = fetch();
            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool complementCarryFlag() // 0x3F
        {
			bool done = false;
            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag(!getCarryFlag());

            done = true;
            return done;
        }
        private bool ldrBB() // 0x40
        {
			bool done = false;
            Byte value = getB();
            setB(value);
            done = true;
            return done;
        }
        private bool ldrBC() // 0x41
        {
			bool done = false;
            Byte value = getC();
            setB(value);
            done = true;
            return done;
        }
        private bool ldrBD() // 0x42
        {
			bool done = false;
            Byte value = getD();
            setB(value);

            done = true;
            return done;
        }
        private bool ldrBE() // 0x43
        {
			bool done = false;
            Byte value = getE();
            setB(value);

            done = true;
            return done;
        }
        private bool ldrBH() // 0x44
        {
			bool done = false;
            Byte value = getH();
            setB(value);

            done = true;
            return done;
        }
        private bool ldrBL() // 0x45
        {
			bool done = false;
            Byte value = getL();
            setB(value);

            done = true;
            return done;
        }
        private bool ldrBFromMemHL() // 0x46
        {
			bool done = false;
            Byte value = getByte(getHL());
            setB(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrBA() // 0x47
        {
			bool done = false;
            Byte value = getA();
            setB(value);
            done = true;
            return done;

        }
        private bool ldrCB() // 0x48
        {
			bool done = false;
            Byte value = getB();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrCC() // 0x49
        {
			bool done = false;
            Byte value = getC();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrCD() // 0x4A
        {
			bool done = false;
            Byte value = getD();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrCE() // 0x4B
        {
			bool done = false;
            Byte value = getE();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrCH() // 0x4C
        {
			bool done = false;
            Byte value = getH();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrCL() // 0x4D
        {
			bool done = false;
            Byte value = getL();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrCFromMemHL() // 0x4E
        {
			bool done = false;
            Byte value = getByte(getHL());
            setC(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrCA() // 0x4F
        {
			bool done = false;
            Byte value = getA();
            setC(value);

            done = true;
            return done;
        }
        private bool ldrDB() // 0x50
        {
			bool done = false;
            Byte value = getB();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrDC() // 0x51
        {
			bool done = false;
            Byte value = getC();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrDD() // 0x52
        {
			bool done = false;
            Byte value = getD();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrDE() // 0x53
        {
			bool done = false;
            Byte value = getE();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrDH() // 0x54
        {
			bool done = false;
            Byte value = getH();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrDL() // 0x55
        {
			bool done = false;
            Byte value = getL();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrDFromMemHL() // 0x56
        {
			bool done = false;
            Byte value = getByte(getHL());
            setD(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrDA() // 0x57
        {
			bool done = false;
            Byte value = getA();
            setD(value);

            done = true;
            return done;
        }
        private bool ldrEB() // 0x58
        {
			bool done = false;
            Byte value = getB();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrEC() // 0x59
        {
			bool done = false;
            Byte value = getC();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrED() // 0x5A
        {
			bool done = false;
            Byte value = getD();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrEE() // 0x5B
        {
			bool done = false;
            Byte value = getE();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrEH() // 0x5C
        {
			bool done = false;
            Byte value = getH();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrEL() // 0x5D
        {
			bool done = false;
            Byte value = getL();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrEFromMemHL() // 0x5E
        {
			bool done = false;
            Byte value = getByte(getHL());
            setE(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrEA() // 0x5F
        {
			bool done = false;
            Byte value = getA();
            setE(value);

            done = true;
            return done;
        }
        private bool ldrHB() // 0x60
        {
			bool done = false;
            Byte value = getB();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrHC() // 0x61
        {
			bool done = false;
            Byte value = getC();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrHD() // 0x62
        {
			bool done = false;
            Byte value = getD();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrHE() // 0x63
        {
			bool done = false;
            Byte value = getE();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrHH() // 0x64
        {
			bool done = false;
            Byte value = getH();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrHL() // 0x65
        {
			bool done = false;
            Byte value = getL();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrHFromMemHL() // 0x66
        {
			bool done = false;
            Byte value = getByte(getHL());
            setH(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrHA() // 0x67
        {
			bool done = false;
            Byte value = getA();
            setH(value);

            done = true;
            return done;
        }
        private bool ldrLB() // 0x68
        {
			bool done = false;
            Byte value = getB();
            setL(value);

            done = true;
            return done;
        }
        private bool ldrLC() // 0x69
        {
			bool done = false;
            Byte value = getC();
            setL(value);

            done = true;
            return done;
        }
        private bool ldrLD() // 0x6A
        {
			bool done = false;
            Byte value = getD();
            setL(value);

            done = true;
            return done;
        }
        private bool ldrLE() // 0x6B
        {
			bool done = false;
            Byte value = getE();
            setL(value);

            done = true;
            return done;
        }
        private bool ldrLH() // 0x6C
        {
			bool done = false;
            Byte value = getH();
            setL(value);

            done = true;
            return done;
        }
        private bool ldrLL() // 0x6D
        {
			bool done = false;
            Byte value = getL();
            setL(value);

            done = true;
            return done;
        }
        private bool ldrLFromMemHL() // 0x6E
        {
			bool done = false;
            Byte value = getByte(getHL());
            setL(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrLA() // 0x6F
        {
			bool done = false;
            Byte value = getA();
            setL(value);

            done = true;
            return done;
        }
        private bool ldHLMemFromB() // 0x70
        {
			bool done = false;
            Byte value = getB();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLMemFromC() // 0x71
        {
			bool done = false;
            Byte value = getC();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLMemFromD() // 0x72
        {
			bool done = false;
            Byte value = getD();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLMemFromE() // 0x73
        {
			bool done = false;
            Byte value = getE();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLMemFromH() // 0x74
        {
			bool done = false;
            Byte value = getH();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLMemFromL() // 0x75
        {
			bool done = false;
            Byte value = getL();
            setByte(getHL(), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool haltCPU() // 0x76
        {
			bool done = false;
            state = CPUState.Halted;

            done = true;
            return done;
        }
        private bool ldAIntoMemHL16() // 0x77
        {
			bool done = false;
            setByte(getHL(), getA());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrAB() // 0x78
        {
			bool done = false;
            Byte value = getB();
            setA(value);

            done = true;
            return done;
        }
        private bool ldrAC() // 0x79
        {
			bool done = false;
            Byte value = getC();
            setA(value);

            done = true;
            return done;
        }
        private bool ldrAD() // 0x7A
        {
			bool done = false;
            Byte value = getD();
            setA(value);

            done = true;
            return done;
        }
        private bool ldrAE() // 0x7B
        {
			bool done = false;
            Byte value = getE();
            setA(value);

            done = true;
            return done;
        }
        private bool ldrAH() // 0x7C
        {
			bool done = false;
            Byte value = getH();
            setA(value);

            done = true;
            return done;
        }
        private bool ldrAL() // 0x7D
        {
			bool done = false;
            Byte value = getL();
            setA(value);

            done = true;
            return done;
        }
        private bool ldrAFromMemHL() // 0x7E
        {
			bool done = false;
            Byte value = getByte(getHL());
            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldrAA() // 0x7F
        {
			bool done = false;
            Byte value = getA();
            setA(value);

            done = true;
            return done;
        }
        private bool addBtoA() // 0x80
        {
			bool done = false;
            Byte value = 0;
            value = add(getA(), getB());

            setA(value);

            done = true;
            return done;
        }
        private bool addCtoA() // 0x81
        {
			bool done = false;
            Byte value = 0;
            value = add(getA(), getC());

            setA(value);

            done = true;
            return done;
        }
        private bool addDtoA() // 0x82
        {
			bool done = false;
            Byte value = 0;
            value = add(getA(), getD());

            setA(value);

            done = true;
            return done;
        }
        private bool addEtoA() // 0x83
        {
			bool done = false;
            Byte value = 0;
            value = add(getA(), getE());

            setA(value);

            done = true;
            return done;
        }
        private bool addHtoA() // 0x84
        {
			bool done = false;
            Byte value = 0;
            value = add(getA(), getH());

            setA(value);

            done = true;
            return done;
        }
        private bool addLtoA() // 0x85
        {
			bool done = false;
            Byte value = 0;
            value = add(getA(), getL());

            setA(value);

            done = true;
            return done;
        }
        private bool addMemAtHLtoA() // 0x86
        {
			bool done = false;
            Byte value = getByte(getHL());
            value = add(getA(), value);

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool addAtoA() // 0x87
        {
			bool done = false;
            Byte value = getA();
            value = add(value, value);

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryBtoA() // 0x88
        {
			bool done = false;
            Byte value = 0;
            value = addCarry(getA(), getB());

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryCtoA() // 0x89
        {
			bool done = false;
            Byte value = 0;
            value = addCarry(getA(), getC());

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryDtoA() // 0x8A
        {
			bool done = false;
            Byte value = 0;
            value = addCarry(getA(), getD());

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryEtoA() // 0x8B
        {
			bool done = false;
            Byte value = 0;
            value = addCarry(getA(), getE());

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryHtoA() // 0x8C
        {
			bool done = false;
            Byte value = 0;
            value = addCarry(getA(), getH());

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryLtoA() // 0x8D
        {
			bool done = false;
            Byte value = 0;
            value = addCarry(getA(), getL());

            setA(value);

            done = true;
            return done;
        }
        private bool addCarryMemAtHLtoA() // 0x8E
        {
			bool done = false;
            Byte value = getByte(getHL());
            value = addCarry(getA(), value);

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool addCarryAtoA() // 0x8F
        {
			bool done = false;
            Byte value = getA();
            value = addCarry(value, value);

            setA(value);

            done = true;
            return done;
        }
        private bool subBFromA() // 0x90
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getB(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCFromA() // 0x91
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getC(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subDFromA() // 0x92
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getD(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subEFromA() // 0x93
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getE(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subHFromA() // 0x94
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getH(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subLFromA() // 0x95
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getL(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subMemAtHLFromA() // 0x96
        {
			bool done = false;

            if (currentInstruction.getCycleCount() == 8)
            {
                done = true;
                // subtract(n, A)
                setA(subtract(getByte(getHL()), getA()));
            }
            
            return done;
        }
        private bool subAFromA() // 0x97
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtract(getA(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCarryBFromA() // 0x98
        {
			bool done = false;

            // subtractCarry(n, A)
            setA(subtractCarry(getB(), getA()));

            done = true;
            return done;
        }
        private bool subCarryCFromA() // 0x99
        {
			bool done = false;
            Byte value = 0;

            // subtract(n, A)
            value = subtractCarry(getC(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCarryDFromA() // 0x9A
        {
			bool done = false;
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getD(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCarryEFromA() // 0x9B
        {
			bool done = false;
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getE(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCarryHFromA() // 0x9C
        {
			bool done = false;
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getH(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCarryLFromA() // 0x9D
        {
			bool done = false;
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getL(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool subCarryMemAtHLFromA() // 0x9E
        {
			bool done = false;

            if (currentInstruction.getCycleCount() == 8)
            {
                setA(subtractCarry(getByte(getHL()), getA()));
                done = true;
            }

            return done;
        }
        private bool subCarryAFromA() // 0x9F
        {
			bool done = false;
            Byte value = 0;

            // subtractCarry(n, A)
            value = subtractCarry(getA(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andABinA() // 0xA0
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getB(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andACinA() // 0xA1
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getC(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andADinA() // 0xA2
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getD(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andAEinA() // 0xA3
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getE(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andAHinA() // 0xA4
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getH(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andALinA() // 0xA5
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getL(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool andAMemHLinA() // 0xA6
        {
			bool done = false;
            Byte value = getByte(getHL());
            // and(n,A)
            value = and(value, getA());

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool andAAinA() // 0xA7
        {
			bool done = false;
            Byte value = 0;
            // and(n,A)
            value = and(getA(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorABinA() // 0xA8
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getB(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorACinA() // 0xA9
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getC(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorADinA() // 0xAA
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getD(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorAEinA() // 0xAB
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getE(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorAHinA() // 0xAC
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getH(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorALinA() // 0xAD
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getL(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool xorAMemHLinA() // 0xAE
        {
			bool done = false;
            Byte value = getByte(getHL());
            // xor(n,A)
            value = xor(value, getA());

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool xorAAinA() // 0xAF
        {
			bool done = false;
            Byte value = 0;
            // xor(n,A)
            value = xor(getA(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orABinA() // 0xB0
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getB(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orACinA() // 0xB1
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getC(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orADinA() // 0xB2
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getD(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orAEinA() // 0xB3
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getE(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orAHinA() // 0xB4
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getH(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orALinA() // 0xB5
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getL(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool orAMemHLinA() // 0xB6
        {
			bool done = false;
            Byte value = getByte(getHL());
            // or(n,A)
            value = or(value, getA());

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool orAAinA() // 0xB7
        {
			bool done = false;
            Byte value = 0;
            // or(n,A)
            value = or(getA(), getA());

            setA(value);

            done = true;
            return done;
        }
        private bool cmpAB() // 0xB8
        {
			bool done = false;
            // cmp(n,A)
            cmp(getB(), getA());

            done = true;
            return done;
        }
        private bool cmpAC() // 0xB9
        {
			bool done = false;
            // cmp(n,A)
            cmp(getC(), getA());

            done = true;
            return done;
        }
        private bool cmpAD() // 0xBA
        {
			bool done = false;
            // cmp(n,A)
            cmp(getD(), getA());

            done = true;
            return done;
        }
        private bool cmpAE() // 0xBB
        {
			bool done = false;
            // cmp(n,A)
            cmp(getE(), getA());

            done = true;
            return done;
        }
        private bool cmpAH() // 0xBC
        {
			bool done = false;
            // cmp(n,A)
            cmp(getH(), getA());

            done = true;
            return done;
        }
        private bool cmpAL() // 0xBD
        {
			bool done = false;
            // cmp(n,A)
            cmp(getL(), getA());

            done = true;
            return done;
        }
        private bool cmpAMemHL() // 0xBE
        {
			bool done = false;

            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = getByte(getHL());
                // cmp(n,A)
                cmp(value, getA());

                done = true;
            }

            return done;
        }
        private bool cmpAA() // 0xBF
        {
			bool done = false;
            // cmp(n,A)
            cmp(getA(), getA());


            done = true;
            return done;
        }
        private bool retIfZReset() // 0xC0
        {
			bool done = false;
            if (!getZeroFlag())
            {
                setPC(pop16OffStack());
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool popIntoBC() //0xC1
        {
			bool done = false;

            ushort value = pop16OffStack();

            setBC(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool jumpIfZeroFlagReset() // 0xC2
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = getUInt16ForBytes(lsb, msb);
            if (!getZeroFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool jumpToNN() // 0xC3
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = getUInt16ForBytes(lsb, msb);
            setPC(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool callNNIfZReset() //0xC4
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            if (!getZeroFlag())
            {
                pushOnStack(getPC());
                setPC(getUInt16ForBytes(lsb, msb));
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool pushBCToStack() //0xC5
        {
			bool done = false;
            pushOnStack(getBC());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool addNtoA() // 0xC6
        {
			bool done = false;
            Byte value = fetch();
            value = add(getA(), value);

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rst00() // 0xC7
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0000);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool retIfZSet() // 0xC8
        {
			bool done = false;
            if (getZeroFlag())
            {
                setPC(pop16OffStack());
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ret() // 0xC9
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                currentInstruction.storage = setByteInUInt16(popOffStack(), BytePlacement.LSB, currentInstruction.storage);
            }
            if (currentInstruction.getCycleCount() == 12)
            {
                currentInstruction.storage = setByteInUInt16(popOffStack(), BytePlacement.MSB, currentInstruction.storage);
            }
            if (currentInstruction.getCycleCount() == 16)
            {
                setPC(currentInstruction.storage);
                done = true;
            }
            
            return done;
        }
        private bool jumpIfZeroFlagSet() // 0xCA
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = getUInt16ForBytes(lsb, msb);
            if (getZeroFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool executeCBPrefixedOpCode() // 0xCB
        {
			
            return executeCBOperation();
        }
        private bool callNNIfZSet() //0xCC
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            if (getZeroFlag())
            {
                pushOnStack(getPC());
                setPC(getUInt16ForBytes(lsb, msb));
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool callNN() //0xCD
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                currentInstruction.storage = setByteInUInt16(fetch(), BytePlacement.LSB, currentInstruction.storage); ;
            }
            else if (currentInstruction.getCycleCount() == 12)
            {
                currentInstruction.storage = setByteInUInt16(fetch(), BytePlacement.MSB, currentInstruction.storage);
            }
            else if (currentInstruction.getCycleCount() == 16)
            {
                // Internal Branch Decision ??
            }
            else if (currentInstruction.getCycleCount() == 20)
            {
                pushOnStack(getByteInUInt16(BytePlacement.MSB, getPC()));
            }
            else if (currentInstruction.getCycleCount() == 24)
            {
                pushOnStack(getByteInUInt16(BytePlacement.LSB, getPC()));
                setPC(currentInstruction.storage);
                done = true;
            }
            
            return done;
        }
        private bool addCarryNtoA() // 0xCE
        {
			bool done = false;
            Byte value = fetch();
            Byte rv = 0;
            rv = addCarry(getA(), value);

            setA(rv);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rst08() // 0xCF
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0008);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool retIfCarryReset() // 0xD0
        {
			bool done = false;
            if (!getCarryFlag())
            {
                setPC(pop16OffStack());
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool popIntoDE() //0xD1
        {
			bool done = false;
            ushort value = pop16OffStack();

            setDE(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool jumpIfCarryFlagReset() // 0xD2
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = getUInt16ForBytes(lsb, msb);
            if (!getCarryFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool unusedD3() // 0xD3
        {
            throw new NotImplementedException(" 0xD3 is Unused");
        }
        private bool callNNIfCReset() //0xD4
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            if (!getCarryFlag())
            {
                pushOnStack(getPC());
                setPC(getUInt16ForBytes(lsb, msb));
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool pushDEToStack() //0xD5
        {
			bool done = false;
            pushOnStack(getDE());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool subNFromA() // 0xD6
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = fetch();
                // subtract(n, A)
                setA(subtract(value, getA()));
                done = true;
            }
            
            return done;
        }
        private bool rst10() // 0xD7
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0010);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool retIfCarrySet() // 0xD8
        {
			bool done = false;
            if (getCarryFlag())
            {
                setPC(pop16OffStack());
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool retEnableInterrupts() // 0xD9
        {
			bool done = false;
            setPC(pop16OffStack());
            shouldEnableInterrupts = true;

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool jumpIfCarryFlagSet() // 0xDA
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            ushort value = getUInt16ForBytes(lsb, msb);
            if (getCarryFlag())
            {
                setPC(value);
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool unusedDB() // 0xDB
        {
            throw new NotImplementedException(" 0xDB is Unused");
        }
        private bool callNNIfCSet() //0xDC
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            if (getCarryFlag())
            {
                pushOnStack(getPC());
                setPC(getUInt16ForBytes(lsb, msb));
            }

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool unusedDD() // 0xDD
        {
            throw new NotImplementedException(" 0xDD is Unused");
        }
        private bool subCarryNFromA() // 0xDE
        {
			bool done = false;
            Byte value = fetch();

            // subtractCarry(n, A)
            value = subtractCarry(value, getA());

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rst18() // 0xDF
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0018);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool putAIntoIOPlusMem() // 0xE0
        {
			bool done = false;
            Byte value = getA();
            Byte offset = fetch();
            setByte((ushort)(0xFF00 + offset), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool popIntoHL() //0xE1
        {
			bool done = false;
            ushort value = pop16OffStack();

            setHL(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldAIntoIOPlusC() // 0xE2
        {
			bool done = false;
            Byte value = getA();
            setByte((ushort)(0xFF00 + getC()), value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool unusedE3() // 0xE3
        {
            throw new NotImplementedException(" 0xE3 is Unused");
        }
        private bool unusedE4() // 0xE4
        {
            throw new NotImplementedException(" 0xE4 is Unused");
        }
        private bool pushHLToStack() //0xE5
        {
            bool done = false;
            pushOnStack(getHL());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool andANinA() // 0xE6
        {
			bool done = false;
            Byte value = fetch();
            // and(n,A)
            value = and(value, getA());

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rst20() // 0xE7
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0020);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool addNtoSP() // 0xE8
        {
			bool done = false;
            

            if (currentInstruction.getCycleCount() == 8)
            {
                SByte offset = (SByte)fetch();
                currentInstruction.storage = addSP(getSP(), offset);
            }
            if (currentInstruction.getCycleCount() == 12)
            {
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.LSB, currentInstruction.storage), BytePlacement.LSB, getSP()));
            }
            else if (currentInstruction.getCycleCount() == 16)
            {
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.MSB, currentInstruction.storage), BytePlacement.MSB, getSP()));
                done = true;
            }

            return done;
        }
        private bool jumpHL() // 0xE9
        {
			bool done = false;
            if (getCurrentInstruction().getCycleCount() == 4)
            {
                setPC(getHL());
            }

            done = true;
            return done;

        }
        private bool ldAIntoNN16() // 0xEA
        {
			bool done = false;
            Byte lsb = fetch();
            Byte msb = fetch();
            setByte(getUInt16ForBytes(lsb, msb), getA());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool unusedEB() // 0xEB
        {
            throw new NotImplementedException(" 0xEB is Unused");
        }
        private bool unusedEC() // 0xEC
        {
            throw new NotImplementedException(" 0xEC is Unused");
        }
        private bool unusedED() // 0xED
        {
            throw new NotImplementedException(" 0xED is Unused");
        }
        private bool xorANinA() // 0xEE
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = fetch();
                // xor(n,A)
                value = xor(value, getA());

                setA(value);
                done = true;
            }

            return done;
        }
        private bool rst28() // 0xEF
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0028);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool putIOPlusMemIntoA() // 0xF0
        {
			bool done = false;
            Byte offset = fetch();
            Byte value = getByte((ushort)(0xFF00 + offset));
            
            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool popIntoAF() //0xF1
        {
			bool done = false;
            ushort value = pop16OffStack();

            setAF(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldIOPlusCToA() // 0xF2
        {
			bool done = false;
            Byte value = getByte((ushort)(0xFF00 + getC()));
            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool disableInterruptsAfterNextIns() // 0xF3
        {
			bool done = false;
            shouldDisableInterrupts = true;

            done = true;
            return done;
        }
        private bool unusedF4() // 0xF4
        {
            throw new NotImplementedException(" 0xF4 is Unused");
        }
        private bool pushAFToStack() //0xF5
        {
			bool done = false;
            pushOnStack(getAF());

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool orANinA() // 0xF6
        {
			bool done = false;
            Byte value = fetch();
            // add(n,A)
            value = or(value, getA());

            setA(value);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool rst30() // 0xF7
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0030);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }
        private bool ldHLFromSPPlusN() // 0xF8
        {
			bool done = false;

            if (currentInstruction.getCycleCount() == 8)
            {
                SByte offset = (SByte)fetch();
                currentInstruction.storage = addSP(getSP(), offset);
                setL(getByteInUInt16(BytePlacement.LSB, currentInstruction.storage));
            }
            else if (currentInstruction.getCycleCount() == 12)
            {
                setH(getByteInUInt16(BytePlacement.MSB, currentInstruction.storage));
                done = true;
            }

            return done;
        }
        private bool ldSPFromHL() // 0xF9
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 4)
            {
                currentInstruction.storage = getHL();
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.LSB, currentInstruction.storage), BytePlacement.LSB, getSP()));
            }
            else if (currentInstruction.getCycleCount() == 8)
            {
                setSP(setByteInUInt16(getByteInUInt16(BytePlacement.MSB, currentInstruction.storage), BytePlacement.MSB, getSP()));
                done = true;
            }
            return done;
        }
        private bool ld16A() // 0xFA
        {
			bool done = false;
            if (getCurrentInstruction().getCycleCount() == 8)
            {
                currentInstruction.storage = setByteInUInt16(fetch(), BytePlacement.LSB, currentInstruction.storage);
            }
            else if(getCurrentInstruction().getCycleCount() == 12)
            {
                currentInstruction.storage = setByteInUInt16(fetch(), BytePlacement.MSB, currentInstruction.storage);
            }
            else if (getCurrentInstruction().getCycleCount() == 16)
            {
                setA(getByte(getCurrentInstruction().storage));
                done = true;
            }

            return done;
        }
        private bool enableInterruptsAfterNextIns() // 0xFB
        {
			bool done = false;
            shouldEnableInterrupts = true;

            done = true;
            return done;
        }
        private bool unusedFC() // 0xFC
        {
            throw new NotImplementedException(" 0xFC is Unused");
        }
        private bool unusedFD() // 0xFD
        {
            throw new NotImplementedException(" 0xFD is Unused");
        }
        private bool cmpAN() // 0xFE
        {
			bool done = false;
            if (currentInstruction.getCycleCount() == 8)
            {
                Byte value = fetch();
                // cmp(n,A)
                cmp(value, getA());
                done = true;
            }
            
            return done;
        }
        private bool rst38() // 0xFF
        {
			bool done = false;
            pushOnStack(getPC());
            setPC(0x0038);

            // This needs to be updated when updating for cycle accurracy
            done = true;
            return done;
        }





        #endregion

        #region Instruction Helper Functions

        #region 8-Bit Math Functions
        public Byte increment(Byte value)
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
            if ((value & 0x0F) == 0x0F)
            {
                setHalfCarryFlag(true);
            }

            return result;
        }

        public Byte decrement(Byte value)
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

        public Byte add(Byte op1, Byte op2)
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
            if (((op1 & 0x0F) + (op2 & 0x0F)) > 0x0F)
            {
                // Adding LSB Nibble > 0x0F (15)
                setHalfCarryFlag(true);
            }
            if (((op1 & 0xFF) + (op2 & 0xFF)) > 0xFF)
            {
                // Overflow Detected result > 0xFF (255)
                setCarryFlag(true);
            }

            return value;
        }

        public Byte addCarry(Byte op1, Byte op2)
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

        public Byte subtract(Byte op1, Byte op2)
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
            if (((op1 & 0xF) > (op2 & 0xF))) // Lower Nible of op2 is higher we need to borrow
            {
                setHalfCarryFlag(true);
            }
            if (op1 > op2) // Lower Nible of op2 is higher we need to borrow
            {
                setCarryFlag(true);
            }

            return value;
        }
        
        public Byte subtractCarry(Byte op1, Byte op2)
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

        public Byte and(Byte op1, Byte op2)
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

        public Byte or(Byte op1, Byte op2)
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

        public Byte xor(Byte op1, Byte op2)
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

        public Byte cmp(Byte op1, Byte a)
        {
            return subtract(op1, a);
        }

        public Byte daa(byte arg)
        {
            int result = arg;
            Byte rv = 0;
            bool carryFlag = getCarryFlag();
            bool halfCarryFlag = getHalfCarryFlag();
            bool subFlag = getSubtractFlag();
            bool zeroFlag = getZeroFlag();

            setHalfCarryFlag(false);
            setZeroFlag(false);
            //setCarryFlag(false); // Pass thorugh Carry Flag

            if (subFlag)
            {
                if (halfCarryFlag)
                {
                    result = ((result - 6) & 0xff);
                }
                if (carryFlag)
                {
                    result = ((result - 0x60) & 0xff);
                }
            }
            else
            {
                if (halfCarryFlag || ((result & 0xf) > 9))
                {
                    result += 0x06;
                }
                if (carryFlag || (result > 0x99))
                {
                    result += 0x60;
                    setCarryFlag(true);
                }
            }
            if (result > 0xff)
            {
                setCarryFlag(true);
            }
            rv = (Byte)(result & 0xff);

            setZeroFlag(rv == 0);


            return rv;
        }

        //        // note: assumes a is a uint8_t and wraps from 0xff to 0
        //if (!n_flag) {  // after an addition, adjust if (half-)carry occurred or if result is out of bounds
        //  if (c_flag || a > 0x99) { a += 0x60; c_flag = 1; }
        //  if (h_flag || (a & 0x0f) > 0x09) { a += 0x6; }
        //} else
        //{  // after a subtraction, only adjust if (half-)carry occurred
        //    if (c_flag) { a -= 0x60; }
        //    if (h_flag) { a -= 0x6; }
        //}
        //// these flags are always updated
        //z_flag = (a == 0); // the usual z flag
        //h_flag = 0; // h flag is always cleared

        public Byte rotateLeftCarry(Byte op1, bool updateZeroFlag = true)
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
            if (updateZeroFlag)
            {
                setZeroFlag((result == 0));
            }
            else
            {
                setZeroFlag(false);
            }
            
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        public Byte rotateLeft(Byte op1, bool updateZeroFlag = true)
        {
            int carryFlag = (getCarryFlag()) ? 1 : 0;
            Byte result = (byte)((op1 << 1) & 0xFF);
            result = (byte)(result | carryFlag);
            setCarryFlag((op1 & 0x80) != 0);

            if (updateZeroFlag)
            {
                setZeroFlag((result == 0));
            }
            else
            {
                setZeroFlag(false);
            }
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        public Byte rotateRightCarry(Byte op1, bool updateZeroFlag = true)
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
            if (updateZeroFlag)
            {
                setZeroFlag((result == 0));
            }
            else
            {
                setZeroFlag(false);
            }
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        public Byte rotateRight(Byte op1, bool updateZeroFlag = true)
        {
            Byte carryFlagAdj =(Byte) ((getCarryFlag()) ? 0x80 : 0);
            Byte result = (byte)((op1 >> 1));
            result = (byte)(result | carryFlagAdj);
            setCarryFlag((op1 & 0x01) != 0);

            if (updateZeroFlag)
            {
                setZeroFlag((result == 0));
            }
            else
            {
                setZeroFlag(false);
            }
            setSubtractFlag(false);
            setHalfCarryFlag(false);
            return result;
        }

        public Byte swapNibbles(byte value)
        {
            Byte msn =(Byte) ((value & 0x0F) << 4);
            Byte lsn = (Byte) ((value & 0xF0) >> 4);
            Byte res = (Byte)(msn | lsn);

            setCarryFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            setZeroFlag(res == 0);

            return res;
        }

        public Byte sla(byte value)
        {
            Byte rv = (byte)((((int)value) << 1) & 0xFF);

            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag((value & 0x80) != 0);
            setZeroFlag(rv == 0);


            return rv;
        }

        public Byte sra(byte value)
        {
            Byte rv = (Byte)(((((int)value) >> 1) | (((int)value) & 0x80)) & 0xFF);

            setSubtractFlag(false);
            setHalfCarryFlag(false);

            setCarryFlag((value & 0x01) != 0);
            setZeroFlag(rv == 0);

            return rv;
        }

        public Byte srl(byte value)
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

        public ushort add16(ushort op1, ushort op2, Add16Type type)
        {
            ushort value = (ushort)(((int)op1 + (int)op2) & 0xFFFF);


            if (type == Add16Type.HL)
            {
                setHalfCarryFlag(false);
                setSubtractFlag(false);
                setCarryFlag(false);

                if (((op1 & 0x0FFF) + (op2 & 0x0FFF)) > 0x0FFF)
                {
                    setHalfCarryFlag(true);
                }
                if (((op1 & 0xFFFF) + (op2 & 0xFFFF)) > 0xFFFF)
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


        public ushort addSP(ushort sp, SByte n)
        {
            ushort value = (ushort)(((int)sp + (int)n) & 0xFFFF);
            setZeroFlag(false);
            setSubtractFlag(false);

            setHalfCarryFlag(false);
            setCarryFlag(false);

            if ((((sp & 0xff) + (n & 0xff)) & 0x100) > 0)
            {
                // overflow
                setCarryFlag(true);
            }
            if ((((sp & 0x0f) + (n & 0x0f)) & 0x10) > 0)
            {
                setHalfCarryFlag(true);
            }

            return value;
        }


        public ushort increment16(ushort value)
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



        public ushort decrement16(ushort value)
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

        public void decrementHL()
        {
            ushort valueToDecrement = getHL();
            valueToDecrement = decrement16(valueToDecrement);
            setHL(valueToDecrement);
        }

        public void incrementHL()
        {
            ushort valueToIncrement = getHL();
            valueToIncrement = increment16(valueToIncrement);
            setHL(valueToIncrement);
        }

        public void pushOnStack(Byte value)
        {
            decrementSP();
            setByte(getSP(), value);
        }

        public void pushOnStack(ushort value)
        {
            byte[] tmp = getByteArrayForUInt16(value);
            pushOnStack(tmp[1]);
            pushOnStack(tmp[0]);
            
        }
        public void pushOnStack(byte[] value)
        {
            pushOnStack(value[1]);
            pushOnStack(value[0]);
        }

        public Byte popOffStack()
        {
            Byte value = getByte(getSP());
            incrementSP();

            return value;
        }
        public ushort pop16OffStack()
        {
            Byte lsb = popOffStack();
            Byte msb = popOffStack();
            return getUInt16ForBytes(lsb, msb);
        }
        #endregion

        #region C# Data Rollover Helper Functions

        // Essentially same as CPU Helpers above but no flags are affected

        public Byte decrementIgnoreFlags(Byte value)
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
        public enum BytePlacement
        {
            MSB,
            LSB
        }

        public static Byte getByteInUInt16(BytePlacement pos, ushort storage)
        {
            Byte MSB = (Byte)((storage & 0xFF00) >> 8);
            Byte LSB = (Byte)(storage & 0x00FF);

            Byte rv = 0;

            if (pos == BytePlacement.MSB)
            {
                rv = MSB;
            }
            else if (pos == BytePlacement.LSB)
            {
                rv = LSB;
            }
            else
            {
                throw new NotImplementedException("Invalid Byte Placement Type");
            }

            return rv;
        }
        public static ushort setByteInUInt16( Byte set, BytePlacement pos, ushort storage)
        {
            Byte MSB = (Byte)((storage & 0xFF00) >> 8);
            Byte LSB = (Byte)(storage & 0x00FF);

            ushort rv = 0;

            if (pos == BytePlacement.MSB)
            {
                int setVal = (int)set;
                rv = (ushort)((setVal << 8) + LSB);
            }
            else if(pos == BytePlacement.LSB)
            {
                int setVal = (int)MSB;
                rv = (ushort)((setVal << 8) + set);
            }
            else
            {
                throw new NotImplementedException("Invalid Byte Placement Type");
            }

            return rv;
        }

        public Byte incrementIgnoreFlags(Byte value)
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

        public Byte addIgnoreFlags(Byte value1, Byte value2)
        {
            return (Byte)((value1 + value2) & 0xFF);
        }

        public Byte subIgnoreFlags(Byte value1, Byte value2)
        {
            return (Byte)((value2 - value1) & 0xFF);
        }

        public ushort add16IgnoreFlags(Byte value1, ushort value2)
        {
            return add16IgnoreFlags((ushort)value1, value2);
        }

        public ushort add16IgnoreFlags(ushort value1, Byte value2)
        {
            return add16IgnoreFlags(value1, (ushort) value2);
        }

        public ushort add16IgnoreFlags(ushort value1, SByte value2)
        {
            return (ushort)((value1 + value2) & 0xFFFF);
        }

        public ushort add16IgnoreFlags(ushort value1, ushort value2)
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

        public static Byte getMSB(ushort value)
        {
            return (Byte)((value >> 8) & 0xFF);
        }

        public static Byte getLSB(ushort value)
        {
            return (Byte)((value) & 0x00FF);
        }


        #endregion
    }
}
