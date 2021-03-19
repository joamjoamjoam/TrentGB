using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace trentGB
{
    public partial class CPUDebugger : Form
    {
        public CPUDebugger()
        {
            InitializeComponent();
        }

        public void setDisplayText(String text)
        {
            displayBox.Text = "";
            displayBox.AppendText(text, Color.Black);
        }

        public void setDisplayText(String beforeText, String afterText)
        {
            displayBox.Text = "";
            int minLength = (beforeText.Length > afterText.Length) ? afterText.Length : beforeText.Length;
            int maxLength = (beforeText.Length > afterText.Length) ? beforeText.Length : afterText.Length;

            for (int i = 0; i < maxLength; i++)
            {
                Color letterColor = Color.Black;
                if (i < minLength)
                {
                    if (beforeText[i] == afterText[i])
                    {
                        letterColor = Color.Black;
                    }
                    else
                    {
                        letterColor = Color.Red;
                    }
                }
                else
                {
                    letterColor = Color.Red;
                }

                // Write Letter to Box
                displayBox.AppendText($"{afterText[i]}", letterColor);
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
}
