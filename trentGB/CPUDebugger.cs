using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace trentGB
{
    public partial class CPUDebugger : Form
    {
        public enum DebugType
        {
            Address = 0,
            InstrCount = 1,
            StopNextCall = 2,
            MemoryWrite = 3,
            MemoryRead = 4,
            MemoryAccess = 5,
            StopNext = 0xFFFE,
            None = 0xFFFF
        }


        private List<ushort> watchList = new List<ushort>();
        private BindingList<ushort> watchListBinded = new BindingList<ushort>(); 
        private Dictionary<string, string> currentStatusDict = new Dictionary<String, String>();
        private Dictionary<string, string> oldStatusDict = new Dictionary<String, String>();
        private Dictionary<string, string> specialRegistersMap = new Dictionary<string, string>();
        private List<Instruction> disRom = null;
        public int currentAddress = 0;
        public bool wait = true;

        private Color changedColor = Color.FromArgb(255, 255, 126, 94);


        public CPUDebugger(List<Instruction> disassembledRom)
        {
            InitializeComponent();
            watchListBinded = new BindingList<ushort>(watchList);
            
            watchListBinded.RaiseListChangedEvents = true;
            watchListBinded.AllowNew = true;
            watchListBinded.AllowRemove = true;
            watchListBinded.ListChanged += new ListChangedEventHandler(watchListboxDataSourceChanged);
            watchAddrListBox.HeaderStyle = ColumnHeaderStyle.None;
            memoryListBox.HeaderStyle = ColumnHeaderStyle.None;
            romView.HeaderStyle = ColumnHeaderStyle.None;

            debugTypeCB.SelectedIndex = 0;      

            loadSpecialRegistersMap();
            disRom = disassembledRom;
        }

        private void loadSpecialRegistersMap()
        {
            specialRegistersMap.Add("FF00", "(P1)");
            specialRegistersMap.Add("FF01", "(SB)");
            specialRegistersMap.Add("FF02", "(SC)");
            specialRegistersMap.Add("FF04", "(DIV)");
            specialRegistersMap.Add("FF05", "(TIMA)");
            specialRegistersMap.Add("FF06", "(TMA)");
            specialRegistersMap.Add("FF07", "(TAC)");
            specialRegistersMap.Add("FF0F", "(IF)");
            specialRegistersMap.Add("FF10", "(NR 10)");
            specialRegistersMap.Add("FF11", "(NR 11)");
            specialRegistersMap.Add("FF12", "(NR12)");
            specialRegistersMap.Add("FF13", "(NR 13)");
            specialRegistersMap.Add("FF14", "(NR 14)");
            specialRegistersMap.Add("FF16", "(NR 21)");
            specialRegistersMap.Add("FF17", "(NR 22)");
            specialRegistersMap.Add("FF18", "(NR 23)");
            specialRegistersMap.Add("FF19", "(NR 24)");
            specialRegistersMap.Add("FF1A", "(NR 30)");
            specialRegistersMap.Add("FF1B", "(NR 31)");
            specialRegistersMap.Add("FF1C", "(NR 32)");
            specialRegistersMap.Add("FF1D", "(NR 33)");
            specialRegistersMap.Add("FF1E", "(NR 34)");
            specialRegistersMap.Add("FF20", "(NR 41)");
            specialRegistersMap.Add("FF21", "(NR 42)");
            specialRegistersMap.Add("FF22", "(NR 43)");
            specialRegistersMap.Add("FF23", "(NR 44)");
            specialRegistersMap.Add("FF24", "(NR 50)");
            specialRegistersMap.Add("FF25", "(NR 51)");
            specialRegistersMap.Add("FF26", "(NR 52)");
            specialRegistersMap.Add("FF30", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF31", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF32", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF33", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF34", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF35", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF36", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF37", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF38", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF39", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF3A", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF3B", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF3C", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF3D", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF3E", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF3F", " (Wave Pattern RAM)");
            specialRegistersMap.Add("FF40", "(LCDC)");
            specialRegistersMap.Add("FF41", "(STAT)");
            specialRegistersMap.Add("FF42", "(SCY)");
            specialRegistersMap.Add("FF43", "(SCX)");
            specialRegistersMap.Add("FF44", "(LY)");
            specialRegistersMap.Add("FF45", "(LYC)");
            specialRegistersMap.Add("FF46", "(DMA)");
            specialRegistersMap.Add("FF47", "(BGP)");
            specialRegistersMap.Add("FF48", "(OBP0)");
            specialRegistersMap.Add("FF49", "(OBP1)");
            specialRegistersMap.Add("FF4A", "(WY)");
            specialRegistersMap.Add("FF4B", "(WX)");
            specialRegistersMap.Add("FFFF", "(IE)");
        }

        private void watchListboxDataSourceChanged(object sender, ListChangedEventArgs e)
        {
            watchAddrListBox.BeginUpdate();
            watchAddrListBox.Items.Clear();
            watchList.Sort();
            foreach (ushort addr in watchList)
            {
                addKeyToListView(watchAddrListBox, addr.ToString("X4"));
            }

            watchAddrListBox.EndUpdate();
        }

        private void continueBtn_Click(object sender, EventArgs e)
        {
            wait = false;
            DebugType type = (DebugType)debugTypeCB.SelectedIndex;

            switch (type)
            {
                case DebugType.MemoryAccess:
                case DebugType.MemoryRead:
                case DebugType.MemoryWrite:
                    addToWatchList(getContinueAddr()[1]);
                    break;
                case DebugType.StopNextCall:
                    if (!Regex.IsMatch(contAddrTxtBox.Text, "[ ]*[0-9A-F][0-9A-F][ ]*") && !Regex.IsMatch(contAddrTxtBox.Text, "[ ]*CB[ ]*[0-9A-F][0-9A-F][ ]*"))
                    {
                        MessageBox.Show("Invalid Opcode");
                        this.DialogResult = DialogResult.None;
                    }
                    else
                    {
                        // Send this to form as ushort
                        contAddrTxtBox.Text = Convert.ToUInt16(contAddrTxtBox.Text.Replace(" ", ""), 16).ToString("X4");
                    }
                    break;

                default:
                    break;
            }
        }

        private void showDisassembledRom()
        {
            romView.Items.Clear();

            foreach (Instruction ins in disRom)
            {
                Color textColor = romView.ForeColor;

                ListViewItem c = new ListViewItem(ins.ToString());
                c.ForeColor = (!ins.isCompleted()) ? Color.LightGreen : romView.ForeColor;
                //c.Font = new Font(c.Font, FontStyle.Bold);
                romView.Items.Add(c);
            }
            if (romView.Items.Count > 0)
            {
                romView.Items[0].EnsureVisible();
            }


            //Decode current Instruction
        }

        public void updateDisassembledRom(List<Instruction> insList)
        {
            this.disRom = insList;

            //Decode current Instruction
        }

        public void printText(String text)
        {
            romView.Items.Clear();

            Color textColor = romView.ForeColor;

            List<String> arr = text.Split(new char[] { '\n' }).ToList();

            foreach (String str in arr)
            {
                ListViewItem c = new ListViewItem(str);
                c.ForeColor = romView.ForeColor;
                //c.Font = new Font(c.Font, FontStyle.Bold);
                romView.Items.Add(c);
            }
            if (romView.Items.Count > 0)
            {
                romView.Items[0].EnsureVisible();
            }
        }

        private void addKeyToListView(ListView view, String key)
        {
            Color textColor = (oldStatusDict.Keys.Count() == 0 || (currentStatusDict[key]) != (oldStatusDict[key])) ? changedColor : romView.ForeColor;
            String specInfo = (specialRegistersMap.ContainsKey(key)) ? $" {specialRegistersMap[key]}" : "";

            String[] strArr = new String[] { $"{key}{specInfo}", $"{((currentStatusDict.Count >= 0xFFFF) ? currentStatusDict[key] : "NULL")}" };
            ListViewItem c = new ListViewItem(strArr);
            c.ForeColor = textColor;
            c.Font = new Font(c.Font, FontStyle.Bold);
            view.Items.Add(c);
        }

        public void updateMemoryWindow(Dictionary<String, String> dict)
        {
            memoryListBox.BeginUpdate();
            memoryListBox.Items.Clear();
            oldStatusDict = currentStatusDict;
            currentStatusDict = dict;
            if (dict != null && dict.Count >= 17)
            {
                int cpuStateDictLength = 16;
                for (int i = 0; i <= cpuStateDictLength; i++)
                {
                    KeyValuePair<String, String> kp = dict.GetEntry<String, String>(dict.Keys.ToList()[i]);
                    addKeyToListView(memoryListBox, kp.Key);
                }

                foreach (KeyValuePair<String, String> addr in specialRegistersMap)
                {
                    addKeyToListView(memoryListBox, addr.Key);
                }

                // get Everything after memory
                for (int i = cpuStateDictLength + 0xFFFF; i < dict.Count; i++)
                {
                    KeyValuePair<String, String> kp = dict.GetEntry<String, String>(dict.Keys.ToList()[i]);
                    addKeyToListView(memoryListBox, kp.Key);
                }
            }
            memoryListBox.EndUpdate();
            watchListBinded.ResetBindings();
        }

        public List<ushort> getContinueAddr()
        {
            List<ushort> rv = new List<ushort>();

            rv.Add((ushort) debugTypeCB.SelectedIndex);
            rv.Add(Convert.ToUInt16(contAddrTxtBox.Text, 16));

            return rv;
        }
        
        private void selectInstructionForRAMAddress(ushort PC)
        {
            for(int i = 0; i < disRom.Count; i++)
            {
                if (disRom[i].address == PC)
                {
                    romView.Items[i].Selected = true;
                    romView.EnsureVisible(i);
                    break;
                }
            }
        }

        public DialogResult ShowDialog(ushort PC)
        {
            // get Address for PC in AddressSpace from mapper
            
            this.DialogResult = DialogResult.None;

            currentAddress = PC;

            return ShowDialog();
        }

        public void Show(ushort PC)
        {
            // get Address for PC in AddressSpace from mapper

            this.DialogResult = DialogResult.None;

            currentAddress = PC;

            Show();
        }

        public void addToWatchList(ushort addr)
        {
            if (!watchList.Contains(addr))
            {
                watchListBinded.Add(addr);
                updateMemoryWindow(currentStatusDict);
            }
        }

        public void setContinueAddr(ushort nextAddr)
        {
            contAddrTxtBox.Text = nextAddr.ToString("X4");
        }

        private void Watch_Click(object sender, EventArgs e)
        {
            if (watchAddrTxtBox.Text.Length > 0)
            {
                addToWatchList(Convert.ToUInt16(watchAddrTxtBox.Text, 16));
            }
        }

        private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in watchAddrListBox.SelectedItems)
            {
                try
                {
                    ushort key = Convert.ToUInt16(Regex.Match(item.Text, $"^[0-9A-F][0-9A-F][0-9A-F][0-9A-F]").Value, 16);
                    watchList.Remove(key);
                }
                catch
                {

                }
            }

            watchListBinded.ResetBindings();
        }

        private void CPUDebugger_Shown(object sender, EventArgs e)
        {
            
            //selectInstructionForRAMAddress((ushort)currentAddress);
            
        }

        private void yesBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            wait = false;
        }

        private void noBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            wait = false;
        }

        private void CPUDebugger_Load(object sender, EventArgs e)
        {
            showDisassembledRom();
        }

        private void CPUDebugger_SizeChanged(object sender, EventArgs e)
        {
            // Format All Boxes 1/3 the screen
        }

        private void debugTypeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            contAddrTxtBox.Text = "";
            switch ((DebugType)debugTypeCB.SelectedIndex)
            {
                case DebugType.InstrCount:
                case DebugType.StopNextCall:
                    contAddrTxtBox.MaxLength = 5;
                    break;
                default:
                    contAddrTxtBox.MaxLength = 4;
                    break;
            }
        }

        public bool getShowAfterState()
        {
            return showAfterStateCB.Checked;
        }

        private void contAddrTxtBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (e.KeyChar != '\b')
            {
                String newText = $"{tb.Text}{e.KeyChar.ToString().ToUpper()}";

                if (tb.Name == "contAddrTxtBox")
                {
                    switch ((DebugType)debugTypeCB.SelectedIndex)
                    {
                        case DebugType.Address:
                        case DebugType.MemoryAccess:
                        case DebugType.MemoryWrite:
                        case DebugType.MemoryRead:
                            if (Regex.IsMatch(newText, $"[^0-9A-F]+") || newText.Length > 4)
                            {
                                e.Handled = true;
                            }
                            break;
                        case DebugType.InstrCount:
                            try
                            {
                                if (Regex.IsMatch(newText, $"[^0-9]+"))
                                {
                                    throw new ArgumentException("Value is not a decimal integer");
                                }
                            }
                            catch
                            {
                                e.Handled = true;
                            }

                            break;
                        case DebugType.StopNextCall:
                            if (Regex.IsMatch(newText, $"[^0-9A-F ]+"))
                            {
                                e.Handled = true;
                            }
                            break;
                    }
                }
                else
                {
                    if (Regex.IsMatch(newText, $"[^0-9A-F]+") || newText.Length > 4)
                    {
                        e.Handled = true;
                    }
                }
            }
            
        }

        private void watchAddrTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Watch_Click(sender, new EventArgs());
            }
        }

        private void contAddrTxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.DialogResult = DialogResult.Ignore;
                continueBtn_Click(sender, new EventArgs());
            }
        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }

    public static class IDictionary
    {
        public static KeyValuePair<TKey, TValue> GetEntry<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
        }
    }


}
