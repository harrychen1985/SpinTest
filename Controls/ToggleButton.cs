using System.Windows.Forms;

namespace MODBUS_Control_Software.Controls
{
    public class ToggleButton : Button
    {
        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Tag = true;
        }

        protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Tag = false;
        }
    }
}
