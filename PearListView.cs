using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace enzo.PopupForms
{
    /// <summary>
    /// Extension of the .NET WinForms ListView to draw insertion lines to signify where an item, once dragged, will be dropped
    /// </summary>
    public class PearListView : ListView
    {
        private const int WM_PAINT = 0x000F;

        public PearListView()
        {
            // Double buffer for graphics optimizations, primarily to reduce flickering
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }


        #region PRIVATE VARIABLES

        private int _LineBefore = -1;

        private int _LineAfter = -1;

        private SolidBrush pearBlueBrush = new SolidBrush(Color.FromArgb(26, 96, 182));

        private Pen pearBluePen = new Pen(Color.FromArgb(26, 96, 182), 1);

        #endregion

        #region PUBLIC VARIABLES

        public int LineBefore { get { return _LineBefore; } set { _LineBefore = value; } }

        public int LineAfter { get { return _LineAfter; } set { _LineAfter = value; } }

        #endregion


        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                if (LineBefore >= 0 && LineBefore < Items.Count)
                {
                    Rectangle rc = Items[LineBefore].GetBounds(ItemBoundsPortion.Entire);
                    DrawInsertionLine(rc.Left, rc.Right, rc.Top);
                }
                if (LineAfter >= 0 && LineBefore < Items.Count)
                {
                    Rectangle rc = Items[LineAfter].GetBounds(ItemBoundsPortion.Entire);
                    DrawInsertionLine(rc.Left, rc.Right, rc.Bottom);
                }
            }
        }

        private void DrawInsertionLine(int x1, int x2, int y)
        {
            using (Graphics g = this.CreateGraphics())
            { 

                g.DrawLine(pearBluePen, x1, y, x2 - 1, y);

                Point[] leftTriangle = new Point[3]
                {
                    new Point(x1,     y - 4),
                    new Point(x1 + 7, y),
                    new Point(x1,     y + 4)
                };

                Point[] rightTriangle = new Point[3]
                {
                    new Point(x2,     y - 4),
                    new Point(x2 - 8, y),
                    new Point(x2,     y + 4)
                };

                g.FillPolygon(pearBlueBrush, leftTriangle);
                g.FillPolygon(pearBlueBrush, rightTriangle);
            }
        }
    }
}
