using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Windows.Forms
{
    public class MyTreeView : TreeView
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public MyTreeView()
        {
            // sets double buffering for flickerfree treeview
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            // like the one in Windows Explorer
            //ShowLines = false;
            HideSelection = false;
        }

        /// <summary>
        /// OnPaint-Method, modified for UserPaint-Flag at Style
        /// </summary>
        /// <param name="e">base PaintEventArgs</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (GetStyle(ControlStyles.UserPaint))
            {
                // create new message
                Message m = new Message();

                // content handle
                m.HWnd = Handle;

                // message itself
                m.Msg = 0x0318; // WM_PRINTCLIENT message

                // params
                m.WParam = e.Graphics.GetHdc();
                m.LParam = (IntPtr)4; // PRF_CLIENT message

                // send this message
                DefWndProc(ref m);

                // release hdc
                e.Graphics.ReleaseHdc(m.WParam);
            }

            // do the basics
            base.OnPaint(e);
        }

        /// <summary>
        /// OnHandleCreated-Method
        /// </summary>
        /// <param name="e">base EventArgs</param>
        protected override void OnHandleCreated(EventArgs e)
        {
            // do the basics
            base.OnHandleCreated(e);

            // set the theme of this treeview from "explorer" (application name)
            SetWindowTheme(Handle, "explorer", null);

            if (Environment.OSVersion.Version.Major > 5) // greater than xp
            {
                if (!Scrollable)
                {
                    // send some messages for the stylish effects
                    IntPtr lParam = (IntPtr)(SendMessage(Handle, 0x112d, IntPtr.Zero, IntPtr.Zero).ToInt32() | 0x60);
                    SendMessage(Handle, 0x112c, IntPtr.Zero, lParam);
                }
            }
        }

        /// <summary>
        /// CreateParams-Property
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                // get base params
                CreateParams createParams = base.CreateParams;

                if (Environment.OSVersion.Version.Major > 5) // greater than xp
                {
                    if (!Scrollable)
                        createParams.Style |= 0x8000;
                }

                // return the modified value
                return createParams;
            }
        }
    }
}