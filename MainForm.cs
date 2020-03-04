using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Runtime.Serialization;


namespace enzo.PopupForms
{
    /// <summary>
    /// Dialogue responsible for adding/editing attribute columns
    /// </summary>
    public partial class AttributeDialogue : MasterForm, IPopupForm
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AttributeDialogue()
        {
            InitializeComponent();
        }
        
        #region LIST VIEW STUFF

        private ListViewItem _itemToDnD = null;

        #region event listeners

        private void AttrOrderListView_MouseDown_1(object sender, MouseEventArgs e)
        {
            _itemToDnD = attrOrderListView.GetItemAt(e.X, e.Y);
        }

        private void AttrOrderListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (_itemToDnD == null)
                return;

            Cursor = Cursors.Hand;

            // Get the bottom of the last item to stop the drag.
            int lastItemBottom = Math.Min(e.Y, attrOrderListView.Items[attrOrderListView.Items.Count - 1].GetBounds(ItemBoundsPortion.Entire).Bottom - 1);

            ListViewItem itemOver = attrOrderListView.GetItemAt(0, lastItemBottom);

            if (itemOver == null)
                return;

            Rectangle rc = itemOver.GetBounds(ItemBoundsPortion.Entire);
            if (e.Y < rc.Top + (rc.Height / 2))
            {
                attrOrderListView.LineBefore = itemOver.Index;
                attrOrderListView.LineAfter = -1;
            }
            else
            {
                attrOrderListView.LineBefore = -1;
                attrOrderListView.LineAfter = itemOver.Index;
            }

            attrOrderListView.Invalidate();
        }

        private void AttrOrderListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (_itemToDnD == null)
                return;

            try
            {
                // calculate the bottom of the last item in the LV so that you don't have to stop your drag at the last item
                int lastItemBottom = Math.Min(e.Y, attrOrderListView.Items[attrOrderListView.Items.Count - 1].GetBounds(ItemBoundsPortion.Entire).Bottom - 1);

                // use 0 instead of e.X so that you don't have to keep inside the columns while dragging
                ListViewItem itemOver = attrOrderListView.GetItemAt(0, lastItemBottom);

                if (itemOver == null)
                    return;

                Rectangle rc = itemOver.GetBounds(ItemBoundsPortion.Entire);

                // find out if we insert before or after the item the mouse is over
                bool insertBefore;
                if (e.Y < rc.Top + (rc.Height / 2))
                {
                    insertBefore = true;
                }
                else
                {
                    insertBefore = false;
                }

                if (_itemToDnD != itemOver) // if we dropped the item on itself, nothing is to be done
                {
                    if (insertBefore)
                    {
                        attrOrderListView.Items.Remove(_itemToDnD);
                        attrOrderListView.Items.Insert(itemOver.Index, _itemToDnD);
                    }
                    else
                    {
                        attrOrderListView.Items.Remove(_itemToDnD);
                        attrOrderListView.Items.Insert(itemOver.Index + 1, _itemToDnD);
                    }
                }

                // clear the insertion line
                attrOrderListView.LineAfter =
                attrOrderListView.LineBefore = -1;

                attrOrderListView.Invalidate();
            }
            finally
            {
                // finish drag&drop operation
                _itemToDnD = null;
                Cursor = Cursors.Default;
            }
        }


        #endregion

        #endregion
        
    }
}
