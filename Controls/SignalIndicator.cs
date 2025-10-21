using System.Drawing;
using System.Windows.Forms;

namespace MODBUS_Control_Software.Controls
{
    public class SignalIndicator : Panel
    {
        public SignalIndicator()
        {
            Width = Height = 20;
            BackColor = Color.Transparent;
            BorderStyle = BorderStyle.FixedSingle;
        }

        public void SetState(bool isActive)
        {
            BackColor = isActive ? Color.Green : Parent.BackColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(new SolidBrush(BackColor), 0, 0, Width-1, Height-1);
        }
    }
}
