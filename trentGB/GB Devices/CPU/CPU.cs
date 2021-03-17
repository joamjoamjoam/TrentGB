using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    // OP Code Dict is Below.

    class CPU
    {
        enum Add16Type
        {
            Normal,
            HL
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
        private AddressSpace mem = null;
        private Dictionary<Byte, Action> opCodeTranslationDict = new Dictionary<byte, Action>();

        public bool done = false;
        public ROM rom;



        public CPU(AddressSpace m, ROM rom)
        {
            mem = m;
            this.rom = rom;
            loadOpCodeMap();
        }


        public void reset()
        {
            done = false;
            setAF(0x11B0); // Set this based on ROMs GB Mode. For Now Hardcode for GBC A = 0x11
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
            return rom.getByte(PC++);
        }

        public void decodeAndExecute(Byte opCode)
        {
            opCodeTranslationDict[opCode].Invoke();
        }

        private void loadOpCodeMap()
        {
            opCodeTranslationDict.Add(0x00, opNOP);
            opCodeTranslationDict.Add(0x01, ldBC16);
            opCodeTranslationDict.Add(0x02, ldAToMemBC);
            opCodeTranslationDict.Add(0x03, incBC);
            opCodeTranslationDict.Add(0x04, incB);
            opCodeTranslationDict.Add(0x05, decB);
            opCodeTranslationDict.Add(0x06, ldB);
            opCodeTranslationDict.Add(0x07, implementOpCode07);
            opCodeTranslationDict.Add(0x08, ldSPFromMem16);
            opCodeTranslationDict.Add(0x09, implementOpCode09);
            opCodeTranslationDict.Add(0x0A, ldAMemBC);
            opCodeTranslationDict.Add(0x0B, decBC);
            opCodeTranslationDict.Add(0x0C, incC);
            opCodeTranslationDict.Add(0x0D, decC);
            opCodeTranslationDict.Add(0x0E, ldC);
            opCodeTranslationDict.Add(0x0F, implementOpCode0F);
            opCodeTranslationDict.Add(0x10, implementOpCode10);
            opCodeTranslationDict.Add(0x11, ldDE16);
            opCodeTranslationDict.Add(0x12, implementOpCode12);
            opCodeTranslationDict.Add(0x13, incDE);
            opCodeTranslationDict.Add(0x14, incD);
            opCodeTranslationDict.Add(0x15, decD);
            opCodeTranslationDict.Add(0x16, ldD);
            opCodeTranslationDict.Add(0x17, implementOpCode17);
            opCodeTranslationDict.Add(0x18, implementOpCode18);
            opCodeTranslationDict.Add(0x19, implementOpCode19);
            opCodeTranslationDict.Add(0x1A, ldAMemDE);
            opCodeTranslationDict.Add(0x1B, decDE);
            opCodeTranslationDict.Add(0x1C, incE);
            opCodeTranslationDict.Add(0x1D, decE);
            opCodeTranslationDict.Add(0x1E, ldE);
            opCodeTranslationDict.Add(0x1F, implementOpCode1F);
            opCodeTranslationDict.Add(0x20, implementOpCode20);
            opCodeTranslationDict.Add(0x21, ldHL16);
            opCodeTranslationDict.Add(0x22, ldiMemHLWithA);
            opCodeTranslationDict.Add(0x23, incHL);
            opCodeTranslationDict.Add(0x24, incH);
            opCodeTranslationDict.Add(0x25, decH);
            opCodeTranslationDict.Add(0x26, ldH);
            opCodeTranslationDict.Add(0x27, implementOpCode27);
            opCodeTranslationDict.Add(0x28, implementOpCode28);
            opCodeTranslationDict.Add(0x29, implementOpCode29);
            opCodeTranslationDict.Add(0x2A, ldiAMemHL);
            opCodeTranslationDict.Add(0x2B, decHL);
            opCodeTranslationDict.Add(0x2C, incL);
            opCodeTranslationDict.Add(0x2D, decL);
            opCodeTranslationDict.Add(0x2E, ldL);
            opCodeTranslationDict.Add(0x2F, implementOpCode2F);
            opCodeTranslationDict.Add(0x30, implementOpCode30);
            opCodeTranslationDict.Add(0x31, ldSP16);
            opCodeTranslationDict.Add(0x32, lddMemHLWithA);
            opCodeTranslationDict.Add(0x33, incSP);
            opCodeTranslationDict.Add(0x34, incHLMem);
            opCodeTranslationDict.Add(0x35, decHLMem);
            opCodeTranslationDict.Add(0x36, ldHLMem);
            opCodeTranslationDict.Add(0x37, implementOpCode37);
            opCodeTranslationDict.Add(0x38, implementOpCode38);
            opCodeTranslationDict.Add(0x39, implementOpCode39);
            opCodeTranslationDict.Add(0x3A, lddAMemHL);
            opCodeTranslationDict.Add(0x3B, decSP);
            opCodeTranslationDict.Add(0x3C, incA);
            opCodeTranslationDict.Add(0x3D, decA);
            opCodeTranslationDict.Add(0x3E, ldA);
            opCodeTranslationDict.Add(0x3F, implementOpCode3F);
            opCodeTranslationDict.Add(0x40, ldrBB);
            opCodeTranslationDict.Add(0x41, ldrBC);
            opCodeTranslationDict.Add(0x42, ldrBD);
            opCodeTranslationDict.Add(0x43, ldrBE);
            opCodeTranslationDict.Add(0x44, ldrBH);
            opCodeTranslationDict.Add(0x45, ldrBL);
            opCodeTranslationDict.Add(0x46, ldrBFromMemHL);
            opCodeTranslationDict.Add(0x47, ldrBA);
            opCodeTranslationDict.Add(0x48, ldrCB);
            opCodeTranslationDict.Add(0x49, ldrCC);
            opCodeTranslationDict.Add(0x4A, ldrCD);
            opCodeTranslationDict.Add(0x4B, ldrCE);
            opCodeTranslationDict.Add(0x4C, ldrCH);
            opCodeTranslationDict.Add(0x4D, ldrCL);
            opCodeTranslationDict.Add(0x4E, ldrCFromMemHL);
            opCodeTranslationDict.Add(0x4F, ldrCA);
            opCodeTranslationDict.Add(0x50, ldrDB);
            opCodeTranslationDict.Add(0x51, ldrDC);
            opCodeTranslationDict.Add(0x52, ldrDD);
            opCodeTranslationDict.Add(0x53, ldrDE);
            opCodeTranslationDict.Add(0x54, ldrDH);
            opCodeTranslationDict.Add(0x55, ldrDL);
            opCodeTranslationDict.Add(0x56, ldrDFromMemHL);
            opCodeTranslationDict.Add(0x57, ldrDA);
            opCodeTranslationDict.Add(0x58, ldrEB);
            opCodeTranslationDict.Add(0x59, ldrEC);
            opCodeTranslationDict.Add(0x5A, ldrED);
            opCodeTranslationDict.Add(0x5B, ldrEE);
            opCodeTranslationDict.Add(0x5C, ldrEH);
            opCodeTranslationDict.Add(0x5D, ldrEL);
            opCodeTranslationDict.Add(0x5E, ldrEFromMemHL);
            opCodeTranslationDict.Add(0x5F, ldrEA);
            opCodeTranslationDict.Add(0x60, ldrHB);
            opCodeTranslationDict.Add(0x61, ldrHC);
            opCodeTranslationDict.Add(0x62, ldrHD);
            opCodeTranslationDict.Add(0x63, ldrHE);
            opCodeTranslationDict.Add(0x64, ldrHH);
            opCodeTranslationDict.Add(0x65, ldrHL);
            opCodeTranslationDict.Add(0x66, ldrHFromMemHL);
            opCodeTranslationDict.Add(0x67, ldrHA);
            opCodeTranslationDict.Add(0x68, ldrLB);
            opCodeTranslationDict.Add(0x69, ldrLC);
            opCodeTranslationDict.Add(0x6A, ldrLD);
            opCodeTranslationDict.Add(0x6B, ldrLE);
            opCodeTranslationDict.Add(0x6C, ldrLH);
            opCodeTranslationDict.Add(0x6D, ldrLL);
            opCodeTranslationDict.Add(0x6E, ldrLFromMemHL);
            opCodeTranslationDict.Add(0x6F, ldrLA);
            opCodeTranslationDict.Add(0x70, ldHLMemFromB);
            opCodeTranslationDict.Add(0x71, ldHLMemFromC);
            opCodeTranslationDict.Add(0x72, ldHLMemFromD);
            opCodeTranslationDict.Add(0x73, ldHLMemFromE);
            opCodeTranslationDict.Add(0x74, ldHLMemFromH);
            opCodeTranslationDict.Add(0x75, ldHLMemFromL);
            opCodeTranslationDict.Add(0x76, implementOpCode76);
            opCodeTranslationDict.Add(0x77, implementOpCode77);
            opCodeTranslationDict.Add(0x78, ldrAB);
            opCodeTranslationDict.Add(0x79, ldrAC);
            opCodeTranslationDict.Add(0x7A, ldrAD);
            opCodeTranslationDict.Add(0x7B, ldrAE);
            opCodeTranslationDict.Add(0x7C, ldrAH);
            opCodeTranslationDict.Add(0x7D, ldrAL);
            opCodeTranslationDict.Add(0x7E, ldrAFromMemHL);
            opCodeTranslationDict.Add(0x7F, ldrAA);
            opCodeTranslationDict.Add(0x80, addBtoA);
            opCodeTranslationDict.Add(0x81, addCtoA);
            opCodeTranslationDict.Add(0x82, addDtoA);
            opCodeTranslationDict.Add(0x83, addEtoA);
            opCodeTranslationDict.Add(0x84, addHtoA);
            opCodeTranslationDict.Add(0x85, addLtoA);
            opCodeTranslationDict.Add(0x86, addMemAtHLtoA);
            opCodeTranslationDict.Add(0x87, addAtoA);
            opCodeTranslationDict.Add(0x88, addCarryBtoA);
            opCodeTranslationDict.Add(0x89, addCarryCtoA);
            opCodeTranslationDict.Add(0x8A, addCarryDtoA);
            opCodeTranslationDict.Add(0x8B, addCarryEtoA);
            opCodeTranslationDict.Add(0x8C, addCarryHtoA);
            opCodeTranslationDict.Add(0x8D, addCarryLtoA);
            opCodeTranslationDict.Add(0x8E, addCarryBtoA);
            opCodeTranslationDict.Add(0x8F, addCarryAtoA);
            opCodeTranslationDict.Add(0x90, subBFromA);
            opCodeTranslationDict.Add(0x91, subCFromA);
            opCodeTranslationDict.Add(0x92, subDFromA);
            opCodeTranslationDict.Add(0x93, subEFromA);
            opCodeTranslationDict.Add(0x94, subHFromA);
            opCodeTranslationDict.Add(0x95, subLFromA);
            opCodeTranslationDict.Add(0x96, subMemAtHLFromA);
            opCodeTranslationDict.Add(0x97, subAFromA);
            opCodeTranslationDict.Add(0x98, subCarryBFromA);
            opCodeTranslationDict.Add(0x99, subCarryCFromA);
            opCodeTranslationDict.Add(0x9A, subCarryDFromA);
            opCodeTranslationDict.Add(0x9B, subCarryEFromA);
            opCodeTranslationDict.Add(0x9C, subCarryHFromA);
            opCodeTranslationDict.Add(0x9D, subCarryLFromA);
            opCodeTranslationDict.Add(0x9E, subCarryMemAtHLFromA);
            opCodeTranslationDict.Add(0x9F, subCarryAFromA);
            opCodeTranslationDict.Add(0xA0, implementOpCodeA0);
            opCodeTranslationDict.Add(0xA1, implementOpCodeA1);
            opCodeTranslationDict.Add(0xA2, implementOpCodeA2);
            opCodeTranslationDict.Add(0xA3, implementOpCodeA3);
            opCodeTranslationDict.Add(0xA4, implementOpCodeA4);
            opCodeTranslationDict.Add(0xA5, implementOpCodeA5);
            opCodeTranslationDict.Add(0xA6, implementOpCodeA6);
            opCodeTranslationDict.Add(0xA7, implementOpCodeA7);
            opCodeTranslationDict.Add(0xA8, implementOpCodeA8);
            opCodeTranslationDict.Add(0xA9, implementOpCodeA9);
            opCodeTranslationDict.Add(0xAA, implementOpCodeAA);
            opCodeTranslationDict.Add(0xAB, implementOpCodeAB);
            opCodeTranslationDict.Add(0xAC, implementOpCodeAC);
            opCodeTranslationDict.Add(0xAD, implementOpCodeAD);
            opCodeTranslationDict.Add(0xAE, implementOpCodeAE);
            opCodeTranslationDict.Add(0xAF, implementOpCodeAF);
            opCodeTranslationDict.Add(0xB0, implementOpCodeB0);
            opCodeTranslationDict.Add(0xB1, implementOpCodeB1);
            opCodeTranslationDict.Add(0xB2, implementOpCodeB2);
            opCodeTranslationDict.Add(0xB3, implementOpCodeB3);
            opCodeTranslationDict.Add(0xB4, implementOpCodeB4);
            opCodeTranslationDict.Add(0xB5, implementOpCodeB5);
            opCodeTranslationDict.Add(0xB6, implementOpCodeB6);
            opCodeTranslationDict.Add(0xB7, implementOpCodeB7);
            opCodeTranslationDict.Add(0xB8, implementOpCodeB8);
            opCodeTranslationDict.Add(0xB9, implementOpCodeB9);
            opCodeTranslationDict.Add(0xBA, implementOpCodeBA);
            opCodeTranslationDict.Add(0xBB, implementOpCodeBB);
            opCodeTranslationDict.Add(0xBC, implementOpCodeBC);
            opCodeTranslationDict.Add(0xBD, implementOpCodeBD);
            opCodeTranslationDict.Add(0xBE, implementOpCodeBE);
            opCodeTranslationDict.Add(0xBF, implementOpCodeBF);
            opCodeTranslationDict.Add(0xC0, implementOpCodeC0);
            opCodeTranslationDict.Add(0xC1, popIntoBC);
            opCodeTranslationDict.Add(0xC2, implementOpCodeC2);
            opCodeTranslationDict.Add(0xC3, implementOpCodeC3);
            opCodeTranslationDict.Add(0xC4, implementOpCodeC4);
            opCodeTranslationDict.Add(0xC5, pushBCToStack);
            opCodeTranslationDict.Add(0xC6, addNtoA);
            opCodeTranslationDict.Add(0xC7, implementOpCodeC7);
            opCodeTranslationDict.Add(0xC8, implementOpCodeC8);
            opCodeTranslationDict.Add(0xC9, implementOpCodeC9);
            opCodeTranslationDict.Add(0xCA, implementOpCodeCA);
            opCodeTranslationDict.Add(0xCB, implementOpCodeCB);
            opCodeTranslationDict.Add(0xCC, implementOpCodeCC);
            opCodeTranslationDict.Add(0xCD, implementOpCodeCD);
            opCodeTranslationDict.Add(0xCE, implementOpCodeCE);
            opCodeTranslationDict.Add(0xCF, implementOpCodeCF);
            opCodeTranslationDict.Add(0xD0, implementOpCodeD0);
            opCodeTranslationDict.Add(0xD1, popIntoDE);
            opCodeTranslationDict.Add(0xD2, implementOpCodeD2);
            opCodeTranslationDict.Add(0xD3, unusedD3);
            opCodeTranslationDict.Add(0xD4, implementOpCodeD4);
            opCodeTranslationDict.Add(0xD5, pushDEToStack);
            opCodeTranslationDict.Add(0xD6, subNFromA);
            opCodeTranslationDict.Add(0xD7, implementOpCodeD7);
            opCodeTranslationDict.Add(0xD8, implementOpCodeD8);
            opCodeTranslationDict.Add(0xD9, implementOpCodeD9);
            opCodeTranslationDict.Add(0xDA, implementOpCodeDA);
            opCodeTranslationDict.Add(0xDB, unusedDB); 
            opCodeTranslationDict.Add(0xDC, implementOpCodeDC);
            opCodeTranslationDict.Add(0xDD, unusedDD);
            opCodeTranslationDict.Add(0xDE, subCarryNFromA);  // SBC A,n
            opCodeTranslationDict.Add(0xDF, implementOpCodeDF);
            opCodeTranslationDict.Add(0xE0, putAIntoIOPlusMem);
            opCodeTranslationDict.Add(0xE1, popIntoHL);
            opCodeTranslationDict.Add(0xE2, ldAIntoIOPlusC);
            opCodeTranslationDict.Add(0xE3, unusedE3);
            opCodeTranslationDict.Add(0xE4, unusedE4);
            opCodeTranslationDict.Add(0xE5, pushHLToStack);
            opCodeTranslationDict.Add(0xE6, implementOpCodeE6);
            opCodeTranslationDict.Add(0xE7, implementOpCodeE7);
            opCodeTranslationDict.Add(0xE8, implementOpCodeE8);
            opCodeTranslationDict.Add(0xE9, implementOpCodeE9);
            opCodeTranslationDict.Add(0xEA, implementOpCodeEA);
            opCodeTranslationDict.Add(0xEB, unusedEB);
            opCodeTranslationDict.Add(0xEC, unusedEC);
            opCodeTranslationDict.Add(0xED, unusedED);
            opCodeTranslationDict.Add(0xEE, implementOpCodeEE);
            opCodeTranslationDict.Add(0xEF, implementOpCodeEF);
            opCodeTranslationDict.Add(0xF0, putIOPlusMemIntoA);
            opCodeTranslationDict.Add(0xF1, popIntoAF);
            opCodeTranslationDict.Add(0xF2, ldIOPlusCToA);
            opCodeTranslationDict.Add(0xF3, implementOpCodeF3);
            opCodeTranslationDict.Add(0xF4, unusedF4);
            opCodeTranslationDict.Add(0xF5, pushAFToStack);
            opCodeTranslationDict.Add(0xF6, implementOpCodeF6);
            opCodeTranslationDict.Add(0xF7, implementOpCodeF7);
            opCodeTranslationDict.Add(0xF8, ldHLFromSPPlusN);
            opCodeTranslationDict.Add(0xF9, ldSPFromHL);
            opCodeTranslationDict.Add(0xFA, ld16A);
            opCodeTranslationDict.Add(0xFB, implementOpCodeFB);
            opCodeTranslationDict.Add(0xFC, unusedFC);
            opCodeTranslationDict.Add(0xFD, unusedFD);
            opCodeTranslationDict.Add(0xFE, implementOpCodeFE);
            opCodeTranslationDict.Add(0xFF, implementOpCodeFF);
        }


        public new String ToString()
        {
            return $"AF={getAF().ToString("X4")}, BC={getBC().ToString("X4")}, DE={getDE().ToString("X4")}, HL={getHL().ToString("X4")}, SP={getSP().ToString("X4")}, PC={getPC().ToString("X4")}, F={getF().ToString("X2")}";
        }

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
        private void implementOpCode07()
        {
            throw new NotImplementedException("Implement Op Code 0x07");
        }
        private void ldSPFromMem16()
        {
            Byte value1 = fetch(); // lower
            Byte value2 = fetch(); // upper
            ushort value = (ushort) ((value2 << 8) + value1);

            setSP(value);
        }
        private void implementOpCode09()
        {
            throw new NotImplementedException("Implement Op Code 0x09");
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
        private void implementOpCode0F()
        {
            throw new NotImplementedException("Implement Op Code 0x0F");
        }
        private void implementOpCode10()
        {
            throw new NotImplementedException("Implement Op Code 0x10");
        }
        private void ldDE16() // 0x11
        {
            int value = fetch();
            int value2 = fetch();
            ushort rv = (ushort)((value2 << 8) + value);

            setDE(rv);
        }
        private void implementOpCode12()
        {
            throw new NotImplementedException("Implement Op Code 0x12");
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
        private void implementOpCode17()
        {
            throw new NotImplementedException("Implement Op Code 0x17");
        }
        private void implementOpCode18()
        {
            throw new NotImplementedException("Implement Op Code 0x18");
        }
        private void implementOpCode19()
        {
            throw new NotImplementedException("Implement Op Code 0x19");
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
        private void implementOpCode1F()
        {
            throw new NotImplementedException("Implement Op Code 0x1F");
        }
        private void implementOpCode20()
        {
            throw new NotImplementedException("Implement Op Code 0x20");
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
        private void implementOpCode27()
        {
            throw new NotImplementedException("Implement Op Code 0x27");
        }
        private void implementOpCode28()
        {
            throw new NotImplementedException("Implement Op Code 0x28");
        }
        private void implementOpCode29()
        {
            throw new NotImplementedException("Implement Op Code 0x29");
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
        private void implementOpCode2F()
        {
            throw new NotImplementedException("Implement Op Code 0x2F");
        }
        private void implementOpCode30()
        {
            throw new NotImplementedException("Implement Op Code 0x30");
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
        private void implementOpCode37()
        {
            throw new NotImplementedException("Implement Op Code 0x37");
        }
        private void implementOpCode38()
        {
            throw new NotImplementedException("Implement Op Code 0x38");
        }
        private void implementOpCode39()
        {
            throw new NotImplementedException("Implement Op Code 0x39");
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
        private void implementOpCode3F()
        {
            throw new NotImplementedException("Implement Op Code 0x3F");
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
        private void implementOpCode76()
        {
            throw new NotImplementedException("Implement Op Code 0x76");
        }
        private void implementOpCode77()
        {
            throw new NotImplementedException("Implement Op Code 0x77");
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
        private void implementOpCodeA0()
        {
            throw new NotImplementedException("Implement Op Code 0xA0");
        }
        private void implementOpCodeA1()
        {
            throw new NotImplementedException("Implement Op Code 0xA1");
        }
        private void implementOpCodeA2()
        {
            throw new NotImplementedException("Implement Op Code 0xA2");
        }
        private void implementOpCodeA3()
        {
            throw new NotImplementedException("Implement Op Code 0xA3");
        }
        private void implementOpCodeA4()
        {
            throw new NotImplementedException("Implement Op Code 0xA4");
        }
        private void implementOpCodeA5()
        {
            throw new NotImplementedException("Implement Op Code 0xA5");
        }
        private void implementOpCodeA6()
        {
            throw new NotImplementedException("Implement Op Code 0xA6");
        }
        private void implementOpCodeA7()
        {
            throw new NotImplementedException("Implement Op Code 0xA7");
        }
        private void implementOpCodeA8()
        {
            throw new NotImplementedException("Implement Op Code 0xA8");
        }
        private void implementOpCodeA9()
        {
            throw new NotImplementedException("Implement Op Code 0xA9");
        }
        private void implementOpCodeAA()
        {
            throw new NotImplementedException("Implement Op Code 0xAA");
        }
        private void implementOpCodeAB()
        {
            throw new NotImplementedException("Implement Op Code 0xAB");
        }
        private void implementOpCodeAC()
        {
            throw new NotImplementedException("Implement Op Code 0xAC");
        }
        private void implementOpCodeAD()
        {
            throw new NotImplementedException("Implement Op Code 0xAD");
        }
        private void implementOpCodeAE()
        {
            throw new NotImplementedException("Implement Op Code 0xAE");
        }
        private void implementOpCodeAF()
        {
            throw new NotImplementedException("Implement Op Code 0xAF");
        }
        private void implementOpCodeB0()
        {
            throw new NotImplementedException("Implement Op Code 0xB0");
        }
        private void implementOpCodeB1()
        {
            throw new NotImplementedException("Implement Op Code 0xB1");
        }
        private void implementOpCodeB2()
        {
            throw new NotImplementedException("Implement Op Code 0xB2");
        }
        private void implementOpCodeB3()
        {
            throw new NotImplementedException("Implement Op Code 0xB3");
        }
        private void implementOpCodeB4()
        {
            throw new NotImplementedException("Implement Op Code 0xB4");
        }
        private void implementOpCodeB5()
        {
            throw new NotImplementedException("Implement Op Code 0xB5");
        }
        private void implementOpCodeB6()
        {
            throw new NotImplementedException("Implement Op Code 0xB6");
        }
        private void implementOpCodeB7()
        {
            throw new NotImplementedException("Implement Op Code 0xB7");
        }
        private void implementOpCodeB8()
        {
            throw new NotImplementedException("Implement Op Code 0xB8");
        }
        private void implementOpCodeB9()
        {
            throw new NotImplementedException("Implement Op Code 0xB9");
        }
        private void implementOpCodeBA()
        {
            throw new NotImplementedException("Implement Op Code 0xBA");
        }
        private void implementOpCodeBB()
        {
            throw new NotImplementedException("Implement Op Code 0xBB");
        }
        private void implementOpCodeBC()
        {
            throw new NotImplementedException("Implement Op Code 0xBC");
        }
        private void implementOpCodeBD()
        {
            throw new NotImplementedException("Implement Op Code 0xBD");
        }
        private void implementOpCodeBE()
        {
            throw new NotImplementedException("Implement Op Code 0xBE");
        }
        private void implementOpCodeBF()
        {
            throw new NotImplementedException("Implement Op Code 0xBF");
        }
        private void implementOpCodeC0()
        {
            throw new NotImplementedException("Implement Op Code 0xC0");
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
        private void implementOpCodeC2()
        {
            throw new NotImplementedException("Implement Op Code 0xC2");
        }
        private void implementOpCodeC3()
        {
            throw new NotImplementedException("Implement Op Code 0xC3");
        }
        private void implementOpCodeC4()
        {
            throw new NotImplementedException("Implement Op Code 0xC4");
        }
        private void pushBCToStack() //0xC5
        {
            byte[] value = getByteArrayForUInt16(getBC());
            mem.setBytes(getSP(), value);
            decrementSP();
            decrementSP();
        }
        private void addNtoA() // 0xC6
        {
            Byte value = fetch();
            value = add(getA(), value);

            setA(value);
        }
        private void implementOpCodeC7()
        {
            throw new NotImplementedException("Implement Op Code 0xC7");
        }
        private void implementOpCodeC8()
        {
            throw new NotImplementedException("Implement Op Code 0xC8");
        }
        private void implementOpCodeC9()
        {
            throw new NotImplementedException("Implement Op Code 0xC9");
        }
        private void implementOpCodeCA()
        {
            throw new NotImplementedException("Implement Op Code 0xCA");
        }
        private void implementOpCodeCB()
        {
            throw new NotImplementedException("Implement Op Code 0xCB");
        }
        private void implementOpCodeCC()
        {
            throw new NotImplementedException("Implement Op Code 0xCC");
        }
        private void implementOpCodeCD()
        {
            throw new NotImplementedException("Implement Op Code 0xCD");
        }
        private void implementOpCodeCE()
        {
            throw new NotImplementedException("Implement Op Code 0xCE");
        }
        private void implementOpCodeCF()
        {
            throw new NotImplementedException("Implement Op Code 0xCF");
        }
        private void implementOpCodeD0()
        {
            throw new NotImplementedException("Implement Op Code 0xD0");
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
        private void implementOpCodeD2()
        {
            throw new NotImplementedException("Implement Op Code 0xD2");
        }
        private void unusedD3() // 0xD3
        {
            throw new NotImplementedException(" 0xD3 is Unused");
        }
        private void implementOpCodeD4()
        {
            throw new NotImplementedException("Implement Op Code 0xD4");
        }
        private void pushDEToStack() //0xD5
        {
            byte[] value = getByteArrayForUInt16(getDE());
            mem.setBytes(getSP(), value);
            decrementSP();
            decrementSP();
        }
        private void subNFromA() // 0xD6
        {
            Byte value = fetch();

            // subtract(n, A)
            value = subtract(value, getA());

            setA(value);
        }
        private void implementOpCodeD7()
        {
            throw new NotImplementedException("Implement Op Code 0xD7");
        }
        private void implementOpCodeD8()
        {
            throw new NotImplementedException("Implement Op Code 0xD8");
        }
        private void implementOpCodeD9()
        {
            throw new NotImplementedException("Implement Op Code 0xD9");
        }
        private void implementOpCodeDA()
        {
            throw new NotImplementedException("Implement Op Code 0xDA");
        }
        private void unusedDB() // 0xDB
        {
            throw new NotImplementedException(" 0xDB is Unused");
        }
        private void implementOpCodeDC()
        {
            throw new NotImplementedException("Implement Op Code 0xDC");
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
        private void implementOpCodeDF()
        {
            throw new NotImplementedException("Implement Op Code 0xDF");
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
            mem.setBytes(getSP(), value);
            decrementSP();
            decrementSP();
        }
        private void implementOpCodeE6()
        {
            throw new NotImplementedException("Implement Op Code 0xE6");
        }
        private void implementOpCodeE7()
        {
            throw new NotImplementedException("Implement Op Code 0xE7");
        }
        private void implementOpCodeE8()
        {
            throw new NotImplementedException("Implement Op Code 0xE8");
        }
        private void implementOpCodeE9()
        {
            throw new NotImplementedException("Implement Op Code 0xE9");
        }
        private void implementOpCodeEA()
        {
            throw new NotImplementedException("Implement Op Code 0xEA");
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
        private void implementOpCodeEE()
        {
            throw new NotImplementedException("Implement Op Code 0xEE");
        }
        private void implementOpCodeEF()
        {
            throw new NotImplementedException("Implement Op Code 0xEF");
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
        private void implementOpCodeF3()
        {
            throw new NotImplementedException("Implement Op Code 0xF3");
        }
        private void unusedF4() // 0xF4
        {
            throw new NotImplementedException(" 0xF4 is Unused");
        }
        private void pushAFToStack() //0xF5
        {
            byte[] value = getByteArrayForUInt16(getAF());
            mem.setByte(getSP(),value[0]);
            decrementSP();
            mem.setByte(getSP(), value[1]);
            decrementSP();
        }
        private void implementOpCodeF6()
        {
            throw new NotImplementedException("Implement Op Code 0xF6");
        }
        private void implementOpCodeF7()
        {
            throw new NotImplementedException("Implement Op Code 0xF7");
        }
        private void ldHLFromSPPlusN() // 0xF8
        {
            Byte value = fetch();
            ushort compValue = addSP(getSP(), value);

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
        private void implementOpCodeFB()
        {
            throw new NotImplementedException("Implement Op Code 0xFB");
        }
        private void unusedFC() // 0xFC
        {
            throw new NotImplementedException(" 0xFC is Unused");
        }
        private void unusedFD() // 0xFD
        {
            throw new NotImplementedException(" 0xFD is Unused");
        }
        private void implementOpCodeFE()
        {
            throw new NotImplementedException("Implement Op Code 0xFE");
        }
        private void implementOpCodeFF()
        {
            throw new NotImplementedException("Implement Op Code 0xFF");
        }





        #endregion

        #region Instruction Helper Functions

        #region 8-Bit Math Functions
        private Byte increment(Byte value)
        {
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(false);
            // Ignore Carry Flag
            if (value == 0xFF)
            {
                // Rollover
                value = 0;
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

            return value;
        }

        private Byte decrement(Byte value)
        {
            setZeroFlag(false);
            setHalfCarryFlag(false);
            setSubtractFlag(true);
            // Ignore Carry Flag
            if (value == 0)
            {
                value = 255;
            }
            else if (value == 1)
            {
                setZeroFlag(true);
                value--;
            }
            else
            {
                value--;
            }

            if ((value & 0x0F) == 0x0)
            {
                setHalfCarryFlag(true);
            }

            return value;
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

                if ((op1 & 0x0fff) + (op2 & 0x0fff) > 0x0fff)
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

        public byte[] getByteArrayForUInt16(ushort value)
        {
            // LSB First
            byte[] rv = new byte[2];
            rv[0] = (Byte)(value & 0x00FF);
            rv[1] = (Byte)(((value & 0xFF00) >> 8) & 0xFF);

            return rv;
        }
        #endregion

        #endregion
    }
}
