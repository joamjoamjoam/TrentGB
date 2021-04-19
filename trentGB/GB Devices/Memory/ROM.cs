using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    public class ROM
    {
        public enum CartridgeType
        {
            ROM_ONLY = 0x0,
            ROM_MBC1 = 0x1,
            ROM_MBC1_RAM = 0x2,
            ROM_MBC1_RAM_BATT = 0x3,
            ROM_MBC2 = 0x5,
            ROM_MBC2_BATTERY = 0x6,
            ROM_RAM = 0x8,
            ROM_RAM_BATTERY = 0x9,
            ROM_MMM01 = 0xB,
            ROM_MMM01_SRAM = 0xC,
            ROM_MMM01_SRAM_BATT = 0xD,
            ROM_MBC3_TIMER_BATT = 0xF,
            ROM_MBC3_TIMER_RAM_BATT = 0x10,
            ROM_MBC3 = 0x11,
            ROM_MBC3_RAM = 0x12,
            ROM_MBC3_RAM_BATT = 0x13,
            ROM_MBC5 = 0x19,
            ROM_MBC5_RAM = 0x1A,
            ROM_MBC5_RAM_BATT = 0x1B,
            ROM_MBC5_RUMBLE = 0x1C,
            ROM_MBC5_RUMBLE_SRAM = 0x1D,
            ROM_MBC5_RUMBLE_SRAM_BATT = 0x1E,
            Pocket_Camera = 0x1F,
            Pocket_Camera_1 = 0xFC,
            Bandai_TAMA5 = 0xFD,
            Hudson_HuC_3 = 0xFE,
            Hudson_HuC_1 = 0xFF
        }

        public enum RAMSize
        {
            None = 0x00,
            KB2 = 0x01,
            KB8 = 0x02,
            KB32 = 0x03,
            KB128 = 0x04
        }
        
        public enum MBC1_Mode
        {
            ROM_MODE,
            RAM_MODE
        }


        public enum ROMSize
        {
            KB32 = 0x00,
            KB64 = 0x01,
            KB128 = 0x02,
            KB256 = 0x03,
            KB512 = 0x04,
            MB1 = 0x05,
            MB2 = 0x06,
            MB1_1 = 0x52,
            MB1_2 = 0x53,
            MB1_5 = 0x54
        }


        class ROMHeaderInfo
        {
            public readonly String description = "";
            public readonly byte[] bytes = null;
            public readonly Type valueType = null;
            public readonly object value = null;

            public ROMHeaderInfo(String desc, byte[] bytes, Type type = null, object value = null)
            {
                description = desc;
                this.bytes = bytes;
                this.valueType = (type == null) ? typeof(Byte) : type;
                this.value = value;
            }

            public new String ToString()
            {
                String rv = "";
                if (valueType != null && valueType != typeof(Byte) && value != null)
                {
                    rv = value.ToString();
                }
                else
                {
                    // Dump Raw Bytes
                    if (bytes != null)
                    {
                        rv = String.Join(" ", bytes.Select(b => "0x" + b.ToString("X2")));
                    }
                    else
                    {
                        rv = "NULL";
                    }
                    
                }



                return $"{description}: {rv} -> {((bytes == null) ? "NULL" :  String.Join(" ", bytes.Select(b => "0x" + b.ToString("X2"))))}";
                
            }

            public String ToStringState()
            {
                String rv = "";
                if (valueType != null && valueType != typeof(Byte) && value != null)
                {
                    rv = value.ToString();
                }
                else
                {
                    // Dump Raw Bytes
                    if (bytes != null)
                    {
                        rv = String.Join(" ", bytes.Select(b => "0x" + b.ToString("X2")));
                    }
                    else
                    {
                        rv = "NULL";
                    }

                }



                return $"{((rv != "") ? $"{rv} -> " : "")}{((bytes == null) ? "NULL" : String.Join(" ", bytes.Select(b => "0x" + b.ToString("X2"))))}";

            }
        }

        public readonly int size = 0;
        private byte[] bytes = null;
        private readonly byte[] validBootLogo = null;
        private List<ROMHeaderInfo> headerInfoDict = new List<ROMHeaderInfo>();
        private List<byte[]> banks = new List<byte[]>();
        private ushort selectedRomBank = 1;
        private Byte selectedRamBank = 0;
        private MBC1_Mode memContMode = MBC1_Mode.ROM_MODE;

        public ROM(String filePath)
        {
            // Load New Rom

            validBootLogo = new Byte[48] { 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E };

            // Validate File is a GB Rom (Check for GB Header at 0x100)

            byte[] tmpBytes = File.ReadAllBytes(filePath);
            bytes = new byte[tmpBytes.Length + 1];
            Array.Copy(tmpBytes, bytes, tmpBytes.Length);
            size = tmpBytes.Length;

            byte[] headerInfo = new byte[0x14F - 0x100 + 1];
            Array.ConstrainedCopy(tmpBytes, 0x130, headerInfo, 0, headerInfo.Length);

            Debug.WriteLine($"Byte[] headerInfo = new byte [] {{{String.Join(", ", headerInfo.Select(by => "0x" + by.ToString("X2")))}}}");

            if (!validateROMFile(bytes))
            {
                throw new Exception($"{filePath} is not a Valid GB ROM File. Failed Boot Logo Check.");
            }
            else
            {
                // Load Rom Header Info
                loadRomHeaderData();

                // load Banks
                loadBanks();
            }
        }

        // Used for Unit Testing
        public ROM(byte[] parameters)
        {
            // Load New Rom

            validBootLogo = new Byte[48] { 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E };

            // Validate File is a GB Rom (Check for GB Header at 0x100)

            bytes = new byte[0x8000+ 1];
            size = bytes.Length-1;

            // set up dummy header info
            Byte[] headerInfo = new byte[] { 0xBB, 0xB9, 0x33, 0x3E, 0x54, 0x45, 0x54, 0x52, 0x49, 0x53, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x0A, 0x16, 0xBF, 0xC3, 0x0C, 0x02, 0xCD, 0xE3, 0x29, 0xF0, 0x41, 0xE6, 0x03, 0x20, 0xFA, 0x46, 0xF0, 0x41, 0xE6, 0x03, 0x20, 0xFA, 0x7E, 0xA0, 0xC9, 0x7B, 0x86, 0x27, 0x22, 0x7A, 0x8E, 0x27, 0x22, 0x3E, 0x00, 0x8E, 0x27, 0x77, 0x3E, 0x01, 0xE0, 0xE0, 0xD0, 0x3E, 0x99, 0x32, 0x32, 0x77, 0xC9, 0xF5, 0xC5 };
            Array.ConstrainedCopy(headerInfo, 0, bytes, 0x130, headerInfo.Length);
            Array.ConstrainedCopy(parameters, 0, bytes, 0x100, parameters.Length);

            byte[] fileBytes = new byte[8000];
            Array.ConstrainedCopy(bytes, 0, fileBytes, 0, fileBytes.Length);
            File.WriteAllBytes("testRom.gb", bytes);

            // Load Rom Header Info
            loadRomHeaderData();

            // load Banks
            loadBanks();
       }

        public byte[] getBytesDirect()
        {
            return bytes;
        }
        public byte getByteDirect(int address)
        {
            return bytes[address];
        }

        public Byte getByte(ushort address)
        {
            Byte rv = 0;

            if (address >= 0x0000 && address <= 0x3FFF)
            {
                // ROM Bank 0
                rv = banks[0][address];
            }
            else if (address >= 0x4000 && address <= 0x7FFF)
            {
                rv = banks[selectedRomBank][(address - 0x4000)];
            }
            else
            {
                // Invalid Access Reading Past ROM Space
                throw new ArgumentException($"Attempted to read Address 0x{address.ToString("X4")} from ROM Space. Address is not in ROM Space");
            }

            return rv;
        }

        public Dictionary<String, String> getState()
        {
            Dictionary<String, String> state = new Dictionary<string, string>();

            foreach (ROMHeaderInfo kp in headerInfoDict)
            {
                state.Add(kp.description, kp.ToStringState());
            }

            state.Add("MBC1 Mode Selected", memContMode.ToString());
            state.Add("ROM Banks", banks.Count.ToString());
            state.Add("ROM Bank Selected", selectedRomBank.ToString());
            state.Add("RAM Bank Selected", selectedRamBank.ToString());

            return state;

        }

        public void setByte(ushort address, Byte Value)
        {
            CartridgeType type = (CartridgeType)this.headerInfoDict.Where(fi => fi.description == "Cartridge Type").First().value;
            switch (type)
            {
                case CartridgeType.ROM_ONLY:
                    break;
                case CartridgeType.ROM_MBC1:
                case CartridgeType.ROM_MBC1_RAM:
                case CartridgeType.ROM_MBC1_RAM_BATT:
                    if (address >= 0x0000 && address <= 0x1FFF)
                    {
                        // RAM Enable

                    }
                    else if (address >= 0x2000 && address <= 0x3FFF)
                    {

                        // ROM Select
                        Byte romBankNum = Value;

                        // ROM Address MBC1 Bug
                        if ((romBankNum & 0x1F) == 0)
                        {
                            romBankNum += 1;
                        }
                        romBankNum = (Byte)(romBankNum & 0x1F);

                        // Set ROM Bank Num
                        if (memContMode == MBC1_Mode.ROM_MODE)
                        {
                            // Set only 5 lsb bits
                            selectedRomBank = (ushort)((selectedRomBank & 0xC0) | (romBankNum & 0x1F));
                        }

                    }
                    else if (address >= 0x4000 && address <= 0x5FFF)
                    {
                        Byte tmp = (Byte)(Value & 0x03);

                        if (memContMode == MBC1_Mode.ROM_MODE)
                        {
                            // Upper ROM Select (Set upper 2 bits 5-6)
                            selectedRomBank = (ushort)((selectedRomBank & 0x1F) | (tmp << 5));
                        }
                        else
                        {
                            // RAM Mode
                            selectedRamBank = tmp;
                        }


                    }
                    else if (address >= 0x6000 && address <= 0x7FFF)
                    {
                        // ROM/RAM mode select
                        if (Value == 0)
                        {
                            memContMode = MBC1_Mode.ROM_MODE;
                        }
                        else if (Value == 1)
                        {
                            memContMode = MBC1_Mode.RAM_MODE;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid Write to ROM Address 0x{address.ToString("X4")} value 0x{Value.ToString("X2")}");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Attempted Write to Address Which is in ROM Space");
                    }
                    break;


                default:
                    throw new NotImplementedException($"Cartridge Type {type.ToString()} MMC Operation write at {address.ToString("X4")} not Implemented");

            }
            
        }

        private bool validateROMFile(byte[] fileBytes)
        {
            bool isValid = false;
            if (validBootLogo.Length == 48)
            {
                isValid = true;
                for (int i = 0x0104; i < 0x0133; i++)
                {
                    if (i >= bytes.Length ||  bytes[i] != validBootLogo[i - 0x0104])
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    // Check Header Checksum at 0x014B
                    Byte checksum = 0;
                    for (int i = 0x0134; i <= 0x014C; i++)
                    {
                        checksum = (Byte) (checksum - bytes[i] - (byte)0x01);
                    }

                    isValid = (checksum == bytes[0x014D]);
                }

            }

            Debug.WriteLine($"ROM Validation {((isValid) ? "was Successful" : "Failed")}");

            return isValid;
        }

        private void loadBanks()
        {
            // Every Rom is a Multiple of 16kB Rom Banks

            for (int i = 0; i < bytes.Length-1; i+=0x4000)
            {
                byte[] arr = new byte[0x3FFF + 1];
                Array.ConstrainedCopy(bytes, i, arr, 0, 0x3FFF);
                banks.Add(arr);
            }

        }

        private String getLicensee(byte[] licenseeCodeArr)
        {
            
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(licenseeCodeArr);
            }

            String licenseeCodeStr = (licenseeCodeArr.Count() == 2) ? $"{(char)licenseeCodeArr[1]}{(char)licenseeCodeArr[0]}" : $"0{(char)licenseeCodeArr[0]}";
            String rv = $"Unknown Licensee {licenseeCodeStr} (0x{((licenseeCodeArr.Count() == 2) ? licenseeCodeArr[1] : 0)}{licenseeCodeArr[0]})";
            switch (licenseeCodeStr)
            {
                case "00":
                    rv = "none" + $" ({licenseeCodeStr}) ";
                    break;
                case "01":
                    rv = "Nintendo R&D1" + $" ({licenseeCodeStr}) ";
                    break;
                case "08":
                    rv = "Capcom" + $" ({licenseeCodeStr}) ";
                    break;
                case "13":
                    rv = "Electronic Arts" + $" ({licenseeCodeStr}) ";
                    break;
                case "18":
                    rv = "Hudson Soft" + $" ({licenseeCodeStr}) ";
                    break;
                case "19":
                    rv = "b-ai" + $" ({licenseeCodeStr}) ";
                    break;
                case "20":
                    rv = "kss" + $" ({licenseeCodeStr}) ";
                    break;
                case "22":
                    rv = "pow" + $" ({licenseeCodeStr}) ";
                    break;
                case "24":
                    rv = "PCM Complete" + $" ({licenseeCodeStr}) ";
                    break;
                case "25":
                    rv = "san-x" + $" ({licenseeCodeStr}) ";
                    break;
                case "28":
                    rv = "Kemco Japan" + $" ({licenseeCodeStr}) ";
                    break;
                case "29":
                    rv = "seta" + $" ({licenseeCodeStr}) ";
                    break;
                case "30":
                    rv = "Viacom" + $" ({licenseeCodeStr}) ";
                    break;
                case "31":
                    rv = "Nintendo" + $" ({licenseeCodeStr}) ";
                    break;
                case "32":
                    rv = "Bandai" + $" ({licenseeCodeStr}) ";
                    break;
                case "33":
                    rv = "Ocean/Acclaim" + $" ({licenseeCodeStr}) ";
                    break;
                case "34":
                    rv = "Konami" + $" ({licenseeCodeStr}) ";
                    break;
                case "35":
                    rv = "Hector" + $" ({licenseeCodeStr}) ";
                    break;
                case "37":
                    rv = "Taito" + $" ({licenseeCodeStr}) ";
                    break;
                case "38":
                    rv = "Hudson" + $" ({licenseeCodeStr}) ";
                    break;
                case "39":
                    rv = "Banpresto" + $" ({licenseeCodeStr}) ";
                    break;
                case "41":
                    rv = "Ubi Soft" + $" ({licenseeCodeStr}) ";
                    break;
                case "42":
                    rv = "Atlus" + $" ({licenseeCodeStr}) ";
                    break;
                case "44":
                    rv = "Malibu" + $" ({licenseeCodeStr}) ";
                    break;
                case "46":
                    rv = "angel" + $" ({licenseeCodeStr}) ";
                    break;
                case "47":
                    rv = "Bullet-Proof" + $" ({licenseeCodeStr}) ";
                    break;
                case "49":
                    rv = "irem" + $" ({licenseeCodeStr}) ";
                    break;
                case "50":
                    rv = "Absolute" + $" ({licenseeCodeStr}) ";
                    break;
                case "51":
                    rv = "Acclaim" + $" ({licenseeCodeStr}) ";
                    break;
                case "52":
                    rv = "Activision" + $" ({licenseeCodeStr}) ";
                    break;
                case "53":
                    rv = "American sammy" + $" ({licenseeCodeStr}) ";
                    break;
                case "54":
                    rv = "Konami" + $" ({licenseeCodeStr}) ";
                    break;
                case "55":
                    rv = "Hi tech entertainment" + $" ({licenseeCodeStr}) ";
                    break;
                case "56":
                    rv = "LJN" + $" ({licenseeCodeStr}) ";
                    break;
                case "57":
                    rv = "Matchbox" + $" ({licenseeCodeStr}) ";
                    break;
                case "58":
                    rv = "Mattel" + $" ({licenseeCodeStr}) ";
                    break;
                case "59":
                    rv = "Milton Bradley" + $" ({licenseeCodeStr}) ";
                    break;
                case "60":
                    rv = "Titus" + $" ({licenseeCodeStr}) ";
                    break;
                case "61":
                    rv = "Virgin" + $" ({licenseeCodeStr}) ";
                    break;
                case "64":
                    rv = "LucasArts" + $" ({licenseeCodeStr}) ";
                    break;
                case "67":
                    rv = "Ocean" + $" ({licenseeCodeStr}) ";
                    break;
                case "69":
                    rv = "Electronic Arts" + $" ({licenseeCodeStr}) ";
                    break;
                case "70":
                    rv = "Infogrames" + $" ({licenseeCodeStr}) ";
                    break;
                case "71":
                    rv = "Interplay" + $" ({licenseeCodeStr}) ";
                    break;
                case "72":
                    rv = "Broderbund" + $" ({licenseeCodeStr}) ";
                    break;
                case "73":
                    rv = "sculptured" + $" ({licenseeCodeStr}) ";
                    break;
                case "75":
                    rv = "sci" + $" ({licenseeCodeStr}) ";
                    break;
                case "78":
                    rv = "THQ" + $" ({licenseeCodeStr}) ";
                    break;
                case "79":
                    rv = "Accolade" + $" ({licenseeCodeStr}) ";
                    break;
                case "80":
                    rv = "misawa" + $" ({licenseeCodeStr}) ";
                    break;
                case "83":
                    rv = "lozc" + $" ({licenseeCodeStr}) ";
                    break;
                case "86":
                    rv = "tokuma shoten i*" + $" ({licenseeCodeStr}) ";
                    break;
                case "87":
                    rv = "tsukuda ori*" + $" ({licenseeCodeStr}) ";
                    break;
                case "91":
                    rv = "Chunsoft" + $" ({licenseeCodeStr}) ";
                    break;
                case "92":
                    rv = "Video system" + $" ({licenseeCodeStr}) ";
                    break;
                case "93":
                    rv = "Ocean/Acclaim" + $" ({licenseeCodeStr}) ";
                    break;
                case "95":
                    rv = "Varie" + $" ({licenseeCodeStr}) ";
                    break;
                case "96":
                    rv = "Yonezawa/s'pal" + $" ({licenseeCodeStr}) ";
                    break;
                case "97":
                    rv = "Kaneko" + $" ({licenseeCodeStr}) ";
                    break;
                case "99":
                    rv = "Pack in soft" + $" ({licenseeCodeStr}) ";
                    break;
                case "A4":
                    rv = "Konami (Yu-Gi-Oh!)" + $" ({licenseeCodeStr}) ";
                    break;
            }

            return rv;
        }

        private void loadRomHeaderData()
        {
            headerInfoDict.Clear();
            String tmp = "";
            List<Byte> byteList = new List<byte>();
            for (int i = 0x0134; i <= 0x0142; i++)
            {
                if (bytes[i] > 0)
                {
                    tmp += (Char)bytes[i];

                }
                byteList.Add(bytes[i]);
            }
            headerInfoDict.Add(new ROMHeaderInfo("Title", byteList.ToArray(), typeof(String), tmp));

            byteList.Clear();
            tmp = "";
            for (int i = 0x013F; i <= 0x0142; i++)
            {
                if (bytes[i] > 0)
                {
                    tmp += (Char)bytes[i];

                }
                byteList.Add(bytes[i]);
            }
            headerInfoDict.Add(new ROMHeaderInfo("Manufacturer Code", byteList.ToArray(), typeof(String), tmp));

            headerInfoDict.Add(new ROMHeaderInfo("Gameboy Type", new byte[] { bytes[0x0143]}, typeof(String), (bytes[0x0143] == 0x80) ? "GBC" : (bytes[0x0143] == 0x80) ? "GBC Only" : "GB"));
            if (bytes[0x014B] == 0x33)
            {
                byteList.Clear();
                byteList.Add(bytes[0x144]);
                byteList.Add(bytes[0x145]);
                headerInfoDict.Add(new ROMHeaderInfo("Licensee Code", byteList.ToArray(), typeof(String), getLicensee(byteList.ToArray())));
            }
            else
            {
                byteList.Clear();
                byteList.Add(bytes[0x014B]);
                headerInfoDict.Add(new ROMHeaderInfo("Licensee Code", byteList.ToArray(), typeof(String), getLicensee(byteList.ToArray())));
            }

            headerInfoDict.Add(new ROMHeaderInfo("Supports SGB Functions", null, typeof(bool), ((bytes[0x0146] == 0x03) && (bytes[0x014B] == 0x33))));

            headerInfoDict.Add(new ROMHeaderInfo("Cartridge Type", new byte[] { bytes[0x0147] }, typeof(CartridgeType), (CartridgeType)bytes[0x0147]));
            //headerInfoDict.Add(new ROMHeaderInfo("Cartridge Type", new byte[] { 0x00 }, typeof(CartridgeType), (CartridgeType)0x00));
            headerInfoDict.Add(new ROMHeaderInfo("ROM Size", new byte[] { bytes[0x0148] }, typeof(ROMSize), Enum.GetName(typeof(ROMSize), bytes[0x0148])));
            headerInfoDict.Add(new ROMHeaderInfo("RAM Size", new byte[] { bytes[0x0149] }, typeof(RAMSize), Enum.GetName(typeof(RAMSize), bytes[0x0149])));
            headerInfoDict.Add(new ROMHeaderInfo("Destination Code", new byte[] { bytes[0x014A] }, typeof(String), (bytes[0x014A] == 0) ? "Japanese" : "Non - Japanese"));
            headerInfoDict.Add(new ROMHeaderInfo("Mask Rom Number", new byte[] { bytes[0x014C] }));
            headerInfoDict.Add(new ROMHeaderInfo("Checksum", new byte[] { bytes[0x014E], bytes[0x014F] }));
        }

        public new String ToString()
        {
            String rv = "";

            if (headerInfoDict.Where(r => r.description == "Title").Count() == 1)
            {
                rv = (String)headerInfoDict.Where(r => r.description == "Title").ToArray()[0].value;
            }
            else
            {
                rv = $"ROM[{bytes.Length}]";
            }

            return rv;
        }

        public List<Instruction> disassemble(Dictionary<Byte, Instruction> model)
        {
            List<Instruction> rv = new List<Instruction>();
            for (int i = 0; i < bytes.Length; i++)
            {
                Byte opCode = bytes[i];
                Instruction tmp = new Instruction(model[opCode], i, this);
                rv.Add(tmp);

                i += tmp.length - 1;
            }

            return rv;
        }

        public void printRomHeader()
        {
            foreach (ROMHeaderInfo info in headerInfoDict)
            {
                Debug.WriteLine(info.ToString());
            }
        }
    }
}
