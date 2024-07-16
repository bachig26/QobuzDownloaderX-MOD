using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace QobuzDownloaderX.View
{
    public partial class HeadlessForm : Form
    {
        public HeadlessForm()
        {
            InitializeComponent();
			
			// Get the screen that the form is currently on
			Screen currentScreen = Screen.FromControl(this);
			
			// Set the form's start position to manual
			this.StartPosition = FormStartPosition.Manual;
			
			// Calculate the top-right corner position
			this.Location = new Point(currentScreen.Bounds.Width - this.Width, 0);
		}

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}