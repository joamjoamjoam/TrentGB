using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{

    
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
        private Memory mem = null;

        public CPU(Memory m)
        {
            mem = m;
        }

        public void reset()
        {
            setAF(0x11B0); // Set this based on ROMs GB Mode. For Now Hardcode for GBC A = 0x11
            setBC(0x0013);
            setDE(0x00D8);
            setHL(0x014D);
            setSP(0xFFFE);
            setPC(0x0100);

            // Set Other memory Addresses from CPU Manual???
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
            SP+=1;
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

        #region CPU Operations


        #endregion

    }
}
