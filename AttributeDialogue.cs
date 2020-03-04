using enzo.LayerTypes;
using enzo.LayerTypes.SQLiteHelper;
using enzo.MessageManagement;
using enzo.PearHelper;
using enzo.TooManagement;
using enzo.XMLFiles.XMLFilesProject;
using enzo.XMLFiles.XMLFilesProject.v_0_03;

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Runtime.Serialization;

using ThinkGeo.MapSuite.Layers;

using Infragistics.Win.UltraWinListView;

namespace enzo.PopupForms
{
    /// <summary>
    /// Dialogue responsible for adding/editing attribute columns
    /// </summary>
    public partial class AttributeDialogue : MasterForm, IPopupForm
    {
        // Name of layer
        private string layerName;
        // Reference to layer.
        private FeatureLayer layer;

        // True if called for new table
        private bool isNewLayer = false;

        // Queue up actions ready to process
        private AttributeActionStack actionStack = new AttributeActionStack();

        // Current (edited) set of attributes and associated control
        List<AttributeControl> attributeControls = new List<AttributeControl>();

        /// <summary>
        /// Interface implementation
        /// </summary>
        public MessageManager InternalMessages { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public AttributeDialogue()
        {
            InitializeComponent();
            if (ProjectHelper.GetAllDataSources().Count == 0) { ubtnManageLinkedDataSources.Enabled = false; }
        }

        // Load up controls for each layer attribute
        private void SetAttributeControls()
        {
            // Add appropriate message
            if (isNewLayer)
            { lblWarning.Text = "Add attibutes or click 'Cancel' to exit."; }
            else
            { lblWarning.Text = "NOTE: Changes to the attribute list cannot be reversed using Undo."; }
            
            // Collection of all columns currently attached to the layer
            Collection<FeatureSourceColumn> currCols = new Collection<FeatureSourceColumn>();

            // Get connection path and table / layer name
            string dbPath = ProjectHelper.GetProjectMapData().path + @"\" + ProjectHelper.GetProjectName() + PearTechHelper.GetPearMapExtension();

            // Does the layer exist
            if (!ProjectHelper.IsLayerDuplicate(layerName))
            {
                string msg = PearMessageBox.FormattedString("Unknown Layer", string.Format("Could not find any details for the layer: {0}.", layerName));
                PearMessageBox.Show(PearMessageBox.mBoxType.simpleNotification, msg);
                return;
            }

            // Get unique attribute names.
            List<string> uniqueAttributes = SQLiteHelper.GetUniqueIndexAttributes(dbPath, layerName);

            // Can set up the set of controls
            upAttributes.ClientArea.Controls.Clear();

            // Get the layer to work with
            PearFeatureLayer layer = new PearFeatureLayer(dbPath, layerName);

            try
            {
                // Get the column information
                layer.Open();
                currCols = layer.QueryTools.GetColumns();
                layer.Close();

                // Add populated controls to panel
                int locationCounter = 0;
                foreach (FeatureSourceColumn column in currCols)
                {
                    if (column.ColumnName == PearLabelLayer.LabelColumnName) continue;
                    string foundType = ((PearFeatureSource)layer.FeatureSource).GetColumnType(column);
                    SetEditAttributControl(locationCounter++, column.ColumnName,
                        foundType, uniqueAttributes.Contains(column.ColumnName));
                }

                // Add extra unpoulated control to allow for new attributes
                SetNewAttributeControl(locationCounter);
            }
            catch (Exception ex)
            { PearMessageBox.Show(PearMessageBox.mBoxType.errorNotification, "Error setting up list", ex.Message); }
            finally
            { if (layer.IsOpen) { layer.Close(); } }
        }

        // Load up controls for each layer attribute
        private void RefeshAttributeControls()
        {
            int locationCounter = 0;
            foreach (AttributeControl attrib in attributeControls)
            {
                attrib.Left = (upAttributes.Width - attrib.Width) / 2;
                attrib.Top = (attrib.Height) * locationCounter++;
                upAttributes.ClientArea.Controls.Add(attrib);
            }
        }

        // Add a populated control (for edit)
        // This method is called when adding a new attribute to the dialogue before commiting the attribute to the PearFeatureSource columns
        private void SetNewAttributeControl(int locationCounter)
        {
            AttributeControl attrib = new AttributeControl(actionStack, layerName);
            attrib.AttributeAdded += attrib_AttributeAdded;
            attrib.AttributeRemoved += Attrib_AttributeRemoved;
            attrib.Left = (upAttributes.Width - attrib.Width) / 2;
            attrib.Top = (attrib.Height) * locationCounter;
            upAttributes.ClientArea.Controls.Add(attrib);

            //Lists
            attributeControls.Add(attrib);
        }

        // Add an unpopulated control (for new)
        // This method is called when repopulating the list of controls from the PearFeatureSoruce of the layer
        private void SetEditAttributControl(int locationCounter, string columnName, string columnType, bool uniqueValues)
        {
            AttributeControl attrib = new AttributeControl(actionStack, layerName, columnName,
                ColumnDefinition.GetSQLTypeFromSystemType(columnType), uniqueValues);
            attrib.AttributeAdded += attrib_AttributeAdded;
            attrib.AttributeRemoved += Attrib_AttributeRemoved;
            attrib.Left = (upAttributes.Width - attrib.Width) / 2;
            attrib.Top = (attrib.Height) * locationCounter;

            upAttributes.ClientArea.Controls.Add(attrib);
            attributeControls.Add(attrib);
        }

        // Handle a delete
        void Attrib_AttributeRemoved(object sender, EventArgs e)
        {
            // Can set up the set of controls
            upAttributes.ClientArea.Controls.Clear();
            attributeControls.Remove((AttributeControl)sender);
            RefeshAttributeControls();
        }

        // Event handler fired when an attribute control changes
        void attrib_AttributeAdded(object sender, EventArgs e)
        { SetNewAttributeControl(attributeControls.Count); }

        /// <summary>
        /// Populates the attribute dialogue so the layer is known, and any columns can be retreived
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="isNewLayer"></param>
        public void Populate(ref FeatureLayer layer, bool isNewLayer)
        {
            // Save the layer name
            this.layerName = layer.Name;
            // Save the layer.
            this.layer = layer;

            // Define calling mode
            this.isNewLayer = isNewLayer;

            // Fill in the attribute list
            SetAttributeControls();
        }

        // Handle the OK button by closing with a result of OK
        private void ubtnOK_Click(object sender, EventArgs e)
        {
            // Process any actions and refresh if needed
            if (actionStack.processedCompleteQueue())
            { InternalMessages.Send(new InternalMessage(Cmd.Route.REFRESH_ALL)); }

            

            // Generate project path to work with
            string projectPath = ProjectHelper.GetProjectName(true) + PearTechHelper.GetPearMapExtension();
            //Delete previous entries to the table as entries will be rewritten 
            SQLiteHelper.DeleteAttributeOrderData(projectPath, layerName);
            // Define collection of columns 
            Collection<FeatureSourceColumn> currCols;
            // Get layer to work with
            PearFeatureLayer layer = new PearFeatureLayer(projectPath, layerName);
            //Get columns assoaciated with the layer
            layer.Open();
            currCols = layer.QueryTools.GetColumns();
            layer.Close();


            for (int i = 0; i < currCols.Count; i++)
            {
                AttributeControl attrCtrl = attributeControls[i];
                SQLiteHelper.WriteAttributeOrderData(projectPath, layerName, currCols[i].ColumnName, i);
            }


            // Complete
            this.DialogResult = DialogResult.OK; 
        }

        // Handle the cancel button
        private void ubCancel_Click(object sender, EventArgs e)
        { this.DialogResult = DialogResult.Cancel; }



        // Button for opening advanced attribute options form.
        private void ubtnOpenAttributeDialogAdvanced_Click(object sender, EventArgs e)
        {
            // Send a message to open advanced attribute dialog.
            InternalMessages.Send(
                new Message_OpenAttributeDialog(Cmd.Popup.ADD_ATTRIBUTES_ADVANCED, ref layer));
        }

        private void ubtnManageLinkedDataSources_Click(object sender, EventArgs e)
        {
            LayerOrderRecord layerRecord = ProjectHelper.GetLayerRecord(ProjectHelper.GetProjectName(), layer.Name);
            if (layerRecord != null)
            {
                InternalMessages.Send(new Message_ManageLayerDatasources(
                    Cmd.Popup.MANAGE_LAYER_DATASOURCES, layer, layerRecord));
            }
            else
            {
                PearMessageBox.Show(PearMessageBox.mBoxType.errorNotification,
                    PearMessageBox.FormattedString("Something went wrong.",
                    "Unable to get layer record. Please contact Pear Technology."));
                return;
            }
        }

        private void AttributeDialogue_Load(object sender, EventArgs e)
        {
            this.BringToFront();
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

        #region AttributeOrderClass DEFUNCT can delete
        [Serializable]
        class LayerAttributeOrder
        {
            public string LayerName { get; set; }
            public Dictionary<string, int> Attributes { get; set; }

            public LayerAttributeOrder(string ln, string attr, int pos)
            {
                LayerName = ln;
                Attributes.Add(attr, pos);
            }
        }



        #endregion


    }
}