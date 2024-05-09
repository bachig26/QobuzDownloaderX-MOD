using QobuzApiSharp.Exceptions;
using QobuzApiSharp.Models.Content;
using QobuzDownloaderX.Models.UI;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.View;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace QobuzDownloaderX
{
    public partial class CoverForm : HeadlessForm
    {
        public CoverForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None; // no borders
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true); // this is to avoid visual artifacts
        }

        private const int
            //HTLEFT = 10,
            //HTRIGHT = 11,
            //HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            //HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17;

        const int _ = 10; // you can rename this variable if you like

        /*new Rectangle Top { get { return new Rectangle(0, 0, this.ClientSize.Width, _); } }
        new Rectangle Left { get { return new Rectangle(0, 0, _, this.ClientSize.Height); } }
        new Rectangle Bottom { get { return new Rectangle(0, this.ClientSize.Height - _, this.ClientSize.Width, _); } }
        new Rectangle Right { get { return new Rectangle(this.ClientSize.Width - _, 0, _, this.ClientSize.Height); } }*/

        Rectangle TopLeft { get { return new Rectangle(0, 0, _, _); } }
        Rectangle TopRight { get { return new Rectangle(this.ClientSize.Width - _, 0, _, _); } }
        Rectangle BottomLeft { get { return new Rectangle(0, this.ClientSize.Height - _, _, _); } }
        Rectangle BottomRight { get { return new Rectangle(this.ClientSize.Width - _, this.ClientSize.Height - _, _, _); } }


        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg == 0x84) // WM_NCHITTEST
            {
                var cursor = this.PointToClient(Cursor.Position);

                if (TopLeft.Contains(cursor)) message.Result = (IntPtr)HTTOPLEFT;
                else if (TopRight.Contains(cursor)) message.Result = (IntPtr)HTTOPRIGHT;
                else if (BottomLeft.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMLEFT;
                else if (BottomRight.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMRIGHT;

                /*else if (Top.Contains(cursor)) message.Result = (IntPtr)HTTOP;
                else if (Left.Contains(cursor)) message.Result = (IntPtr)HTLEFT;
                else if (Right.Contains(cursor)) message.Result = (IntPtr)HTRIGHT;
                else if (Bottom.Contains(cursor)) message.Result = (IntPtr)HTBOTTOM;*/
            }
        }

        public void SetPicture(string thumbnailUrl)
        {
            coverPictureBox.ImageLocation = thumbnailUrl;
        }

        private void ClearPicture()
        {
            coverPictureBox.Image = null;
        }

        private void ExitLabel_Click(object sender, EventArgs e)
        {
            this.Hide();
            ClearPicture();
        }

        // Enable moving Form with mouse in absence of titlebar
        private void CoverForm_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void CoverForm_SizeChanged(object sender, EventArgs e)
        {
            // To make form size W = H
            if (this.Width > this.Height)
            {
                int coverWidth = this.Width - 12;
                this.Height = coverWidth + 40;
            }
            else
            {
                int coverHeight = this.Height - 40;
                this.Width = coverHeight + 12;
            }

            sizeLabel.Text = $"({coverPictureBox.Width}x{coverPictureBox.Height})";
        }


    }
}
