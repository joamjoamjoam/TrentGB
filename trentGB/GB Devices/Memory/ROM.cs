using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trentGB
{
    class ROM
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
        }

        public readonly int size = 0;
        private byte[] bytes = null;
        private readonly byte[] validBootLogo = null;
        private List<ROMHeaderInfo> headerInfoDict = new List<ROMHeaderInfo>();

        public ROM(String filePath)
        {
            // Load New Rom

            validBootLogo = new Byte[48] { 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E };

            // Validate File is a GB Rom (Check for GB Header at 0x100)

            byte[] tmpBytes = File.ReadAllBytes(filePath);
            bytes = new byte[tmpBytes.Length + 1];
            Array.Copy(tmpBytes, bytes, tmpBytes.Length);
            size = tmpBytes.Length;

            if (!validateROMFile(bytes))
            {
                throw new Exception($"{filePath} is not a Valid GB ROM File. Failed Boot Logo Check.");
            }
            else
            {
                // Load Rom Header Info
                loadRomHeaderData();

            }
        }

        public byte[] getBytes()
        {
            return bytes;
        }
        public byte getByte(ushort address)
        {
            return bytes[address];
        }
        public byte[] getBytes(ushort address, ushort count)
        {
            byte[] rv = new Byte[count];
            if (((UInt32)address + (UInt32)count) <= 0xFFFF)
            {
                Array.ConstrainedCopy(bytes, address, rv, 0, (int)count);
            }
            else
            {
                rv = null;
            }

            return rv;
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

            headerInfoDict.Add(new ROMHeaderInfo("Cartridge Type", new byte[] { bytes[0x0147] }, typeof(CartridgeType), Enum.GetName(typeof(CartridgeType), bytes[0x0147])));
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

        public void printRomHeader()
        {
            foreach (ROMHeaderInfo info in headerInfoDict)
            {
                Debug.WriteLine(info.ToString());
            }
        }
    }
}
