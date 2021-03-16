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

        public void loadOpCodeMap()
        {
            opCodeTranslationDict.Add(0x00, opNOP);
            opCodeTranslationDict.Add(0x01, ldBC16);
            opCodeTranslationDict.Add(0x02, ldAToMemBC);
            opCodeTranslationDict.Add(0x03, incBC);
            opCodeTranslationDict.Add(0x04, incB);
            opCodeTranslationDict.Add(0x05, decB);
            opCodeTranslationDict.Add(0x06, ldB);
            opCodeTranslationDict.Add(0x07, implementOpCode07);
            opCodeTranslationDict.Add(0x08, implementOpCode08);
            opCodeTranslationDict.Add(0x09, implementOpCode09);
            opCodeTranslationDict.Add(0x0A, implementOpCode0A);
            opCodeTranslationDict.Add(0x0B, decBC);
            opCodeTranslationDict.Add(0x0C, incC);
            opCodeTranslationDict.Add(0x0D, decC);
            opCodeTranslationDict.Add(0x0E, ldC);
            opCodeTranslationDict.Add(0x0F, implementOpCode0F);
            opCodeTranslationDict.Add(0x10, implementOpCode10);
            opCodeTranslationDict.Add(0x11, implementOpCode11);
            opCodeTranslationDict.Add(0x12, implementOpCode12);
            opCodeTranslationDict.Add(0x13, incDE);
            opCodeTranslationDict.Add(0x14, incD);
            opCodeTranslationDict.Add(0x15, decD);
            opCodeTranslationDict.Add(0x16, ldD);
            opCodeTranslationDict.Add(0x17, implementOpCode17);
            opCodeTranslationDict.Add(0x18, implementOpCode18);
            opCodeTranslationDict.Add(0x19, implementOpCode19);
            opCodeTranslationDict.Add(0x1A, implementOpCode1A);
            opCodeTranslationDict.Add(0x1B, decDE);
            opCodeTranslationDict.Add(0x1C, incE);
            opCodeTranslationDict.Add(0x1D, decE);
            opCodeTranslationDict.Add(0x1E, ldE);
            opCodeTranslationDict.Add(0x1F, implementOpCode1F);
            opCodeTranslationDict.Add(0x20, implementOpCode20);
            opCodeTranslationDict.Add(0x21, implementOpCode21);
            opCodeTranslationDict.Add(0x22, implementOpCode22);
            opCodeTranslationDict.Add(0x23, incHL);
            opCodeTranslationDict.Add(0x24, incH);
            opCodeTranslationDict.Add(0x25, decH);
            opCodeTranslationDict.Add(0x26, ldH);
            opCodeTranslationDict.Add(0x27, implementOpCode27);
            opCodeTranslationDict.Add(0x28, implementOpCode28);
            opCodeTranslationDict.Add(0x29, implementOpCode29);
            opCodeTranslationDict.Add(0x2A, implementOpCode2A);
            opCodeTranslationDict.Add(0x2B, decHL);
            opCodeTranslationDict.Add(0x2C, incL);
            opCodeTranslationDict.Add(0x2D, decL);
            opCodeTranslationDict.Add(0x2E, ldL);
            opCodeTranslationDict.Add(0x2F, implementOpCode2F);
            opCodeTranslationDict.Add(0x30, implementOpCode30);
            opCodeTranslationDict.Add(0x31, implementOpCode31);
            opCodeTranslationDict.Add(0x32, implementOpCode32);
            opCodeTranslationDict.Add(0x33, incSP);
            opCodeTranslationDict.Add(0x34, incHLMem);
            opCodeTranslationDict.Add(0x35, decHLMem);
            opCodeTranslationDict.Add(0x36, implementOpCode36);
            opCodeTranslationDict.Add(0x37, implementOpCode37);
            opCodeTranslationDict.Add(0x38, implementOpCode38);
            opCodeTranslationDict.Add(0x39, implementOpCode39);
            opCodeTranslationDict.Add(0x3A, implementOpCode3A);
            opCodeTranslationDict.Add(0x3B, decSP);
            opCodeTranslationDict.Add(0x3C, incA);
            opCodeTranslationDict.Add(0x3D, decA);
            opCodeTranslationDict.Add(0x3E, implementOpCode3E);
            opCodeTranslationDict.Add(0x3F, implementOpCode3F);
            opCodeTranslationDict.Add(0x40, ldrBB);
            opCodeTranslationDict.Add(0x41, ldrBC);
            opCodeTranslationDict.Add(0x42, ldrBD);
            opCodeTranslationDict.Add(0x43, ldrBE);
            opCodeTranslationDict.Add(0x44, ldrBH);
            opCodeTranslationDict.Add(0x45, ldrBL);
            opCodeTranslationDict.Add(0x46, ldrBFromMemHL);
            opCodeTranslationDict.Add(0x47, implementOpCode47);
            opCodeTranslationDict.Add(0x48, ldrCB);
            opCodeTranslationDict.Add(0x49, ldrCC);
            opCodeTranslationDict.Add(0x4A, ldrCD);
            opCodeTranslationDict.Add(0x4B, ldrCE);
            opCodeTranslationDict.Add(0x4C, ldrCH);
            opCodeTranslationDict.Add(0x4D, ldrCL);
            opCodeTranslationDict.Add(0x4E, ldrCFromMemHL);
            opCodeTranslationDict.Add(0x4F, implementOpCode4F);
            opCodeTranslationDict.Add(0x50, ldrDB);
            opCodeTranslationDict.Add(0x51, ldrDC);
            opCodeTranslationDict.Add(0x52, ldrDD);
            opCodeTranslationDict.Add(0x53, ldrDE);
            opCodeTranslationDict.Add(0x54, ldrDH);
            opCodeTranslationDict.Add(0x55, ldrDL);
            opCodeTranslationDict.Add(0x56, ldrDFromMemHL);
            opCodeTranslationDict.Add(0x57, implementOpCode57);
            opCodeTranslationDict.Add(0x58, ldrEB);
            opCodeTranslationDict.Add(0x59, ldrEC);
            opCodeTranslationDict.Add(0x5A, ldrED);
            opCodeTranslationDict.Add(0x5B, ldrEE);
            opCodeTranslationDict.Add(0x5C, ldrEH);
            opCodeTranslationDict.Add(0x5D, ldrEL);
            opCodeTranslationDict.Add(0x5E, ldrEFromMemHL);
            opCodeTranslationDict.Add(0x5F, implementOpCode5F);
            opCodeTranslationDict.Add(0x60, ldrHB);
            opCodeTranslationDict.Add(0x61, ldrHC);
            opCodeTranslationDict.Add(0x62, ldrHD);
            opCodeTranslationDict.Add(0x63, ldrHE);
            opCodeTranslationDict.Add(0x64, ldrHH);
            opCodeTranslationDict.Add(0x65, ldrHL);
            opCodeTranslationDict.Add(0x66, ldrHFromMemHL);
            opCodeTranslationDict.Add(0x67, implementOpCode67);
            opCodeTranslationDict.Add(0x68, ldrLB);
            opCodeTranslationDict.Add(0x69, ldrLC);
            opCodeTranslationDict.Add(0x6A, ldrLD);
            opCodeTranslationDict.Add(0x6B, ldrLE);
            opCodeTranslationDict.Add(0x6C, ldrLH);
            opCodeTranslationDict.Add(0x6D, ldrLL);
            opCodeTranslationDict.Add(0x6E, ldrLFromMemHL);
            opCodeTranslationDict.Add(0x6F, implementOpCode6F);
            opCodeTranslationDict.Add(0x70, implementOpCode70);
            opCodeTranslationDict.Add(0x71, implementOpCode71);
            opCodeTranslationDict.Add(0x72, implementOpCode72);
            opCodeTranslationDict.Add(0x73, implementOpCode73);
            opCodeTranslationDict.Add(0x74, implementOpCode74);
            opCodeTranslationDict.Add(0x75, implementOpCode75);
            opCodeTranslationDict.Add(0x76, implementOpCode76);
            opCodeTranslationDict.Add(0x77, implementOpCode77);
            opCodeTranslationDict.Add(0x78, implementOpCode78);
            opCodeTranslationDict.Add(0x79, implementOpCode79);
            opCodeTranslationDict.Add(0x7A, implementOpCode7A);
            opCodeTranslationDict.Add(0x7B, implementOpCode7B);
            opCodeTranslationDict.Add(0x7C, implementOpCode7C);
            opCodeTranslationDict.Add(0x7D, implementOpCode7D);
            opCodeTranslationDict.Add(0x7E, implementOpCode7E);
            opCodeTranslationDict.Add(0x7F, implementOpCode7F);
            opCodeTranslationDict.Add(0x80, implementOpCode80);
            opCodeTranslationDict.Add(0x81, implementOpCode81);
            opCodeTranslationDict.Add(0x82, implementOpCode82);
            opCodeTranslationDict.Add(0x83, implementOpCode83);
            opCodeTranslationDict.Add(0x84, implementOpCode84);
            opCodeTranslationDict.Add(0x85, implementOpCode85);
            opCodeTranslationDict.Add(0x86, implementOpCode86);
            opCodeTranslationDict.Add(0x87, implementOpCode87);
            opCodeTranslationDict.Add(0x88, implementOpCode88);
            opCodeTranslationDict.Add(0x89, implementOpCode89);
            opCodeTranslationDict.Add(0x8A, implementOpCode8A);
            opCodeTranslationDict.Add(0x8B, implementOpCode8B);
            opCodeTranslationDict.Add(0x8C, implementOpCode8C);
            opCodeTranslationDict.Add(0x8D, implementOpCode8D);
            opCodeTranslationDict.Add(0x8E, implementOpCode8E);
            opCodeTranslationDict.Add(0x8F, implementOpCode8F);
            opCodeTranslationDict.Add(0x90, implementOpCode90);
            opCodeTranslationDict.Add(0x91, implementOpCode91);
            opCodeTranslationDict.Add(0x92, implementOpCode92);
            opCodeTranslationDict.Add(0x93, implementOpCode93);
            opCodeTranslationDict.Add(0x94, implementOpCode94);
            opCodeTranslationDict.Add(0x95, implementOpCode95);
            opCodeTranslationDict.Add(0x96, implementOpCode96);
            opCodeTranslationDict.Add(0x97, implementOpCode97);
            opCodeTranslationDict.Add(0x98, implementOpCode98);
            opCodeTranslationDict.Add(0x99, implementOpCode99);
            opCodeTranslationDict.Add(0x9A, implementOpCode9A);
            opCodeTranslationDict.Add(0x9B, implementOpCode9B);
            opCodeTranslationDict.Add(0x9C, implementOpCode9C);
            opCodeTranslationDict.Add(0x9D, implementOpCode9D);
            opCodeTranslationDict.Add(0x9E, implementOpCode9E);
            opCodeTranslationDict.Add(0x9F, implementOpCode9F);
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
            opCodeTranslationDict.Add(0xC1, implementOpCodeC1);
            opCodeTranslationDict.Add(0xC2, implementOpCodeC2);
            opCodeTranslationDict.Add(0xC3, implementOpCodeC3);
            opCodeTranslationDict.Add(0xC4, implementOpCodeC4);
            opCodeTranslationDict.Add(0xC5, implementOpCodeC5);
            opCodeTranslationDict.Add(0xC6, implementOpCodeC6);
            opCodeTranslationDict.Add(0xC7, implementOpCodeC7);
            opCodeTranslationDict.Add(0xC8, implementOpCodeC8);
            opCodeTranslationDict.Add(0xC9, implementOpCodeC9);
            opCodeTranslationDict.Add(0xCA, implementOpCodeCA);
            //opCodeTranslationDict.Add(0xCB, implementOpCodeCB);  OpCode Not Used
            opCodeTranslationDict.Add(0xCC, implementOpCodeCC);
            opCodeTranslationDict.Add(0xCD, implementOpCodeCD);
            opCodeTranslationDict.Add(0xCE, implementOpCodeCE);
            opCodeTranslationDict.Add(0xCF, implementOpCodeCF);
            opCodeTranslationDict.Add(0xD0, implementOpCodeD0);
            opCodeTranslationDict.Add(0xD1, implementOpCodeD1);
            opCodeTranslationDict.Add(0xD2, implementOpCodeD2);
            //opCodeTranslationDict.Add(0xD3, implementOpCodeD3);  OpCode Not Used
            opCodeTranslationDict.Add(0xD4, implementOpCodeD4);
            opCodeTranslationDict.Add(0xD5, implementOpCodeD5);
            opCodeTranslationDict.Add(0xD6, implementOpCodeD6);
            opCodeTranslationDict.Add(0xD7, implementOpCodeD7);
            opCodeTranslationDict.Add(0xD8, implementOpCodeD8);
            opCodeTranslationDict.Add(0xD9, implementOpCodeD9);
            opCodeTranslationDict.Add(0xDA, implementOpCodeDA);
            //opCodeTranslationDict.Add(0xDB, implementOpCodeDB);  OpCode Not Used
            opCodeTranslationDict.Add(0xDC, implementOpCodeDC);
            //opCodeTranslationDict.Add(0xDD, implementOpCodeDD);  OpCode Not Used
            opCodeTranslationDict.Add(0xDE, implementOpCodeDE);
            opCodeTranslationDict.Add(0xDF, implementOpCodeDF);
            opCodeTranslationDict.Add(0xE0, implementOpCodeE0);
            opCodeTranslationDict.Add(0xE1, implementOpCodeE1);
            opCodeTranslationDict.Add(0xE2, implementOpCodeE2);
            //opCodeTranslationDict.Add(0xE3, implementOpCodeE3);  OpCode Not Used
            //opCodeTranslationDict.Add(0xE4, implementOpCodeE4);  OpCode Not Used
            opCodeTranslationDict.Add(0xE5, implementOpCodeE5);
            opCodeTranslationDict.Add(0xE6, implementOpCodeE6);
            opCodeTranslationDict.Add(0xE7, implementOpCodeE7);
            opCodeTranslationDict.Add(0xE8, implementOpCodeE8);
            opCodeTranslationDict.Add(0xE9, implementOpCodeE9);
            opCodeTranslationDict.Add(0xEA, implementOpCodeEA);
            //opCodeTranslationDict.Add(0xEB, implementOpCodeEB);  OpCode Not Used
            //opCodeTranslationDict.Add(0xEC, implementOpCodeEC);  OpCode Not Used
            //opCodeTranslationDict.Add(0xED, implementOpCodeED);  OpCode Not Used
            opCodeTranslationDict.Add(0xEE, implementOpCodeEE);
            opCodeTranslationDict.Add(0xEF, implementOpCodeEF);
            opCodeTranslationDict.Add(0xF0, implementOpCodeF0);
            opCodeTranslationDict.Add(0xF1, implementOpCodeF1);
            opCodeTranslationDict.Add(0xF2, implementOpCodeF2);
            opCodeTranslationDict.Add(0xF3, implementOpCodeF3);
            //opCodeTranslationDict.Add(0xF4, implementOpCodeF4);  OpCode Not Used
            opCodeTranslationDict.Add(0xF5, implementOpCodeF5);
            opCodeTranslationDict.Add(0xF6, implementOpCodeF6);
            opCodeTranslationDict.Add(0xF7, implementOpCodeF7);
            opCodeTranslationDict.Add(0xF8, implementOpCodeF8);
            opCodeTranslationDict.Add(0xF9, implementOpCodeF9);
            opCodeTranslationDict.Add(0xFA, implementOpCodeFA);
            opCodeTranslationDict.Add(0xFB, implementOpCodeFB);
            //opCodeTranslationDict.Add(0xFC, implementOpCodeFC);  OpCode Not Used
            //opCodeTranslationDict.Add(0xFD, implementOpCodeFD);  OpCode Not Used
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
            F = value;
        }

        public ushort getSP()
        {
            return SP;
        }
        public void setSP(ushort value)
        {
            SP = value;
        }
        public void incrementSP()
        {
            SP += 1;
        }
        public void decrementSP()
        {
            SP -= 1;
        }

        public ushort getPC()
        {
            return PC;
        }
        public void setPC(ushort value)
        {
            PC = value;
        }
        public void incrementPC()
        {
            PC += 1;
        }
        public void decrementPC()
        {
            PC -= 1;
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
            F = (Byte)(value & 0x00FF);
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

            return ((F & 0x10) > 0);
        }

        public void setCarryFlag(bool on)
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

        public void setHalfCarryFlag(bool on)
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

        public void setSubtractFlag(bool on)
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

        public void setZeroFlag(bool on)
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

        public void resetFlagsNibble()
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
        private void implementOpCode08()
        {
            throw new NotImplementedException("Implement Op Code 0x08");
        }
        private void implementOpCode09()
        {
            throw new NotImplementedException("Implement Op Code 0x09");
        }
        private void implementOpCode0A()
        {
            throw new NotImplementedException("Implement Op Code 0x0A");
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
        private void implementOpCode11()
        {
            throw new NotImplementedException("Implement Op Code 0x11");
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
        private void implementOpCode1A()
        {
            throw new NotImplementedException("Implement Op Code 0x1A");
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
        private void ldE() // 0x06
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
        private void implementOpCode21()
        {
            throw new NotImplementedException("Implement Op Code 0x21");
        }
        private void implementOpCode22()
        {
            throw new NotImplementedException("Implement Op Code 0x22");
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
        private void implementOpCode2A()
        {
            throw new NotImplementedException("Implement Op Code 0x2A");
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
        private void implementOpCode31()
        {
            throw new NotImplementedException("Implement Op Code 0x31");
        }
        private void implementOpCode32()
        {
            throw new NotImplementedException("Implement Op Code 0x32");
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
        private void implementOpCode36()
        {
            throw new NotImplementedException("Implement Op Code 0x36");
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
        private void implementOpCode3A()
        {
            throw new NotImplementedException("Implement Op Code 0x3A");
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
        private void implementOpCode3E()
        {
            throw new NotImplementedException("Implement Op Code 0x3E");
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
        private void implementOpCode47() // 0x47
        {
            throw new NotImplementedException("Implement Op Code 0x47");
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
        private void implementOpCode4F()
        {
            throw new NotImplementedException("Implement Op Code 0x4F");
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
        private void implementOpCode57()
        {
            throw new NotImplementedException("Implement Op Code 0x57");
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
        private void implementOpCode5F()
        {
            throw new NotImplementedException("Implement Op Code 0x5F");
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
        private void implementOpCode67()
        {
            throw new NotImplementedException("Implement Op Code 0x67");
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
        private void implementOpCode6F()
        {
            throw new NotImplementedException("Implement Op Code 0x6F");
        }
        private void implementOpCode70()
        {
            throw new NotImplementedException("Implement Op Code 0x70");
        }
        private void implementOpCode71()
        {
            throw new NotImplementedException("Implement Op Code 0x71");
        }
        private void implementOpCode72()
        {
            throw new NotImplementedException("Implement Op Code 0x72");
        }
        private void implementOpCode73()
        {
            throw new NotImplementedException("Implement Op Code 0x73");
        }
        private void implementOpCode74()
        {
            throw new NotImplementedException("Implement Op Code 0x74");
        }
        private void implementOpCode75()
        {
            throw new NotImplementedException("Implement Op Code 0x75");
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
        private void implementOpCode80()
        {
            throw new NotImplementedException("Implement Op Code 0x80");
        }
        private void implementOpCode81()
        {
            throw new NotImplementedException("Implement Op Code 0x81");
        }
        private void implementOpCode82()
        {
            throw new NotImplementedException("Implement Op Code 0x82");
        }
        private void implementOpCode83()
        {
            throw new NotImplementedException("Implement Op Code 0x83");
        }
        private void implementOpCode84()
        {
            throw new NotImplementedException("Implement Op Code 0x84");
        }
        private void implementOpCode85()
        {
            throw new NotImplementedException("Implement Op Code 0x85");
        }
        private void implementOpCode86()
        {
            throw new NotImplementedException("Implement Op Code 0x86");
        }
        private void implementOpCode87()
        {
            throw new NotImplementedException("Implement Op Code 0x87");
        }
        private void implementOpCode88()
        {
            throw new NotImplementedException("Implement Op Code 0x88");
        }
        private void implementOpCode89()
        {
            throw new NotImplementedException("Implement Op Code 0x89");
        }
        private void implementOpCode8A()
        {
            throw new NotImplementedException("Implement Op Code 0x8A");
        }
        private void implementOpCode8B()
        {
            throw new NotImplementedException("Implement Op Code 0x8B");
        }
        private void implementOpCode8C()
        {
            throw new NotImplementedException("Implement Op Code 0x8C");
        }
        private void implementOpCode8D()
        {
            throw new NotImplementedException("Implement Op Code 0x8D");
        }
        private void implementOpCode8E()
        {
            throw new NotImplementedException("Implement Op Code 0x8E");
        }
        private void implementOpCode8F()
        {
            throw new NotImplementedException("Implement Op Code 0x8F");
        }
        private void implementOpCode90()
        {
            throw new NotImplementedException("Implement Op Code 0x90");
        }
        private void implementOpCode91()
        {
            throw new NotImplementedException("Implement Op Code 0x91");
        }
        private void implementOpCode92()
        {
            throw new NotImplementedException("Implement Op Code 0x92");
        }
        private void implementOpCode93()
        {
            throw new NotImplementedException("Implement Op Code 0x93");
        }
        private void implementOpCode94()
        {
            throw new NotImplementedException("Implement Op Code 0x94");
        }
        private void implementOpCode95()
        {
            throw new NotImplementedException("Implement Op Code 0x95");
        }
        private void implementOpCode96()
        {
            throw new NotImplementedException("Implement Op Code 0x96");
        }
        private void implementOpCode97()
        {
            throw new NotImplementedException("Implement Op Code 0x97");
        }
        private void implementOpCode98()
        {
            throw new NotImplementedException("Implement Op Code 0x98");
        }
        private void implementOpCode99()
        {
            throw new NotImplementedException("Implement Op Code 0x99");
        }
        private void implementOpCode9A()
        {
            throw new NotImplementedException("Implement Op Code 0x9A");
        }
        private void implementOpCode9B()
        {
            throw new NotImplementedException("Implement Op Code 0x9B");
        }
        private void implementOpCode9C()
        {
            throw new NotImplementedException("Implement Op Code 0x9C");
        }
        private void implementOpCode9D()
        {
            throw new NotImplementedException("Implement Op Code 0x9D");
        }
        private void implementOpCode9E()
        {
            throw new NotImplementedException("Implement Op Code 0x9E");
        }
        private void implementOpCode9F()
        {
            throw new NotImplementedException("Implement Op Code 0x9F");
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
        private void implementOpCodeC1()
        {
            throw new NotImplementedException("Implement Op Code 0xC1");
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
        private void implementOpCodeC5()
        {
            throw new NotImplementedException("Implement Op Code 0xC5");
        }
        private void implementOpCodeC6()
        {
            throw new NotImplementedException("Implement Op Code 0xC6");
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
        private void implementOpCodeD1()
        {
            throw new NotImplementedException("Implement Op Code 0xD1");
        }
        private void implementOpCodeD2()
        {
            throw new NotImplementedException("Implement Op Code 0xD2");
        }
        private void implementOpCodeD4()
        {
            throw new NotImplementedException("Implement Op Code 0xD4");
        }
        private void implementOpCodeD5()
        {
            throw new NotImplementedException("Implement Op Code 0xD5");
        }
        private void implementOpCodeD6()
        {
            throw new NotImplementedException("Implement Op Code 0xD6");
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
        private void implementOpCodeDC()
        {
            throw new NotImplementedException("Implement Op Code 0xDC");
        }
        private void implementOpCodeDE()
        {
            throw new NotImplementedException("Implement Op Code 0xDE");
        }
        private void implementOpCodeDF()
        {
            throw new NotImplementedException("Implement Op Code 0xDF");
        }
        private void implementOpCodeE0()
        {
            throw new NotImplementedException("Implement Op Code 0xE0");
        }
        private void implementOpCodeE1()
        {
            throw new NotImplementedException("Implement Op Code 0xE1");
        }
        private void implementOpCodeE2()
        {
            throw new NotImplementedException("Implement Op Code 0xE2");
        }
        private void implementOpCodeE5()
        {
            throw new NotImplementedException("Implement Op Code 0xE5");
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
        private void implementOpCodeEE()
        {
            throw new NotImplementedException("Implement Op Code 0xEE");
        }
        private void implementOpCodeEF()
        {
            throw new NotImplementedException("Implement Op Code 0xEF");
        }
        private void implementOpCodeF0()
        {
            throw new NotImplementedException("Implement Op Code 0xF0");
        }
        private void implementOpCodeF1()
        {
            throw new NotImplementedException("Implement Op Code 0xF1");
        }
        private void implementOpCodeF2()
        {
            throw new NotImplementedException("Implement Op Code 0xF2");
        }
        private void implementOpCodeF3()
        {
            throw new NotImplementedException("Implement Op Code 0xF3");
        }
        private void implementOpCodeF5()
        {
            throw new NotImplementedException("Implement Op Code 0xF5");
        }
        private void implementOpCodeF6()
        {
            throw new NotImplementedException("Implement Op Code 0xF6");
        }
        private void implementOpCodeF7()
        {
            throw new NotImplementedException("Implement Op Code 0xF7");
        }
        private void implementOpCodeF8()
        {
            throw new NotImplementedException("Implement Op Code 0xF8");
        }
        private void implementOpCodeF9()
        {
            throw new NotImplementedException("Implement Op Code 0xF9");
        }
        private void implementOpCodeFA()
        {
            throw new NotImplementedException("Implement Op Code 0xFA");
        }
        private void implementOpCodeFB()
        {
            throw new NotImplementedException("Implement Op Code 0xFB");
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
            else if (value == 0x0F)
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

        private ushort increment16(ushort value)
        {
            // Ignore Carry Flag
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

        private ushort decrement16(ushort value)
        {
            // Ignore Carry Flag
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

        #region OP Code Translation Map

        private Dictionary<Byte, Action> opCodeTranslationDict = new Dictionary<byte, Action>()
        {
            
        };

        #endregion

    }
}
