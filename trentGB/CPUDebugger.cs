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
        private List<ushort> watchList = new List<ushort>();
        private BindingList<ushort> watchListBinded = new BindingList<ushort>(); 
        private Dictionary<string, string> currentStatusDict = new Dictionary<String, String>();
        private Dictionary<string, string> oldStatusDict = new Dictionary<String, String>();
        private Dictionary<string, string> specialRegistersMap = new Dictionary<string, string>();

        private Color changedColor = Color.FromArgb(255, 255, 126, 94);


        public CPUDebugger()
        {
            InitializeComponent();
            watchListBinded = new BindingList<ushort>(watchList);
            
            watchListBinded.RaiseListChangedEvents = true;
            watchListBinded.AllowNew = true;
            watchListBinded.AllowRemove = true;
            watchListBinded.ListChanged += new ListChangedEventHandler(watchListboxDataSourceChanged);
            watchAddrListBox.HeaderStyle = ColumnHeaderStyle.None;
            memoryListBox.HeaderStyle = ColumnHeaderStyle.None;



            loadSpecialRegistersMap();

            // Add IO/Special Registers
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

        public void setDisplayText(String text)
        {
            displayBox.Text = "";
            displayBox.AppendText(text, displayBox.ForeColor);
        }

        private void continueBtn_Click(object sender, EventArgs e)
        {

        }

        private void addKeyToListView(ListView view, String key)
        {
            Color textColor = (oldStatusDict.Keys.Count() == 0 || (currentStatusDict[key]) != (oldStatusDict[key])) ? changedColor : Color.White;
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
                for (int i = 0; i < 17; i++)
                {
                    KeyValuePair<String, String> kp = dict.GetEntry<String, String>(dict.Keys.ToList()[i]);
                    addKeyToListView(memoryListBox, kp.Key);
                }

                foreach (KeyValuePair<String, String> addr in specialRegistersMap)
                {
                    addKeyToListView(memoryListBox, addr.Key);
                }
            }
            memoryListBox.EndUpdate();
            watchListBinded.ResetBindings();
        }

        public ushort getContinueAddr()
        {
            ushort rv = 0;

            rv = Convert.ToUInt16(contAddrTxtBox.Text, 16);

            return rv;
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

        private void contAddrTxtBox_Validating(object sender, CancelEventArgs e)
        {
            if (!Regex.IsMatch(contAddrTxtBox.Text, $"[0-9A-F]*") || contAddrTxtBox.Text.Length > 4)
            {
                e.Cancel = true;
            }
        }

        private void watchAddrTxtBox_Validating(object sender, CancelEventArgs e)
        {
            if (!Regex.IsMatch(contAddrTxtBox.Text, $"[0-9A-F]*") || contAddrTxtBox.Text.Length > 4)
            {
                e.Cancel = true;
            }
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
