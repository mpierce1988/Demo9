using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBProgrammingDemo9
{
    public partial class frmProductMaintenance : Form
    {
        public frmProductMaintenance()
        {
            InitializeComponent();
        }

        int currentRecord = 0;
        int currentProductId = 0;
        int firstProductId = 0;
        int lastProductId = 0;
        int? previousProductId;
        int? nextProductId;
        int totalProductsCount;

        #region [Form Events]

        /// <summary>
        /// Form load event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmProductMaintenance_Load(object sender, EventArgs e)
        {
            LoadSuppliers();
            LoadCategories();

            LoadFirstProduct();
        }

        /// <summary>
        /// Add buton click event handler. Places the form in a creation mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdd_Click(object sender, EventArgs e)
        {
            // set status labels
            toolStripStatusLabel1.Text = "Adding a new Product";
            toolStripStatusLabel2.Text = "";
            toolStripStatusLabel3.Text = "";
            ClearControls(grpProducts.Controls);

            LoadCategories();
            LoadSuppliers();

            // save Save btn text to Creat
            btnSave.Text = "Create";

            // disable Add button to prevent double clicks
            btnAdd.Enabled = false;

            // disable Delete button as well
            btnDelete.Enabled = false;

            // disable navigation buttons
            NavigationState(false);
        }

        /// <summary>
        /// Cancel any changes to an existin selected product or the beginnings of the newly created product
        /// We will reload the last active product
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            // re-enable navigation buttons
            NavigationState(true);

            // change btnAdd text back to "Add"
            btnAdd.Text = "Add";

            // enable Add button
            btnAdd.Enabled = true;

            // enable Delete button
            btnDelete.Enabled = true;

            // populate form fields with current record
            LoadProductDetails();

            // reset first last nav buttons
            NextPreviousButtonManagement();
        }

        /// <summary>
        /// Save click event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (ValidateChildren(ValidationConstraints.Enabled))
            {
                ProgressBar();
                if(txtProductId.Text == String.Empty)
                {
                    // if product ID is empty, this is inserting a new record
                    CreateProduct();
                } 
                else
                {
                    // if product id has value, this is an update
                    ProductChanges();
                }
            }
            else
            {
                MessageBox.Show("Check if Data is Valid");
            }
                
        }

        /// <summary>
        /// Delete button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you wish to delete?", "Are you sure?", MessageBoxButtons.OKCancel);

            if(result == DialogResult.Cancel)
            {
                return;
            }

            string sqlDeleteQuery = $"DELETE from Products WHERE ProductId = {txtProductId.Text}";

            int rowsAffected = DataAccess.SendData(sqlDeleteQuery);

            if (rowsAffected == 1)
            {
                // successful
                MessageBox.Show("Product deleted successfully");
                LoadFirstProduct();
            }
            else
            {
                MessageBox.Show("Product not deleted successfully");
            }
        }

        #endregion

        #region [Retrieves]
        
        /// <summary>
        /// Load the Suppliers and bind the combobox
        /// </summary>
        private void LoadSuppliers()
        {
            string sqlSuppliers = "SELECT SupplierId, CompanyName FROM Suppliers ORDER BY CompanyName";

            UIUtilities.BindComboBox(cmbSuppliers, DataAccess.GetData(sqlSuppliers), "CompanyName", "SupplierId");
        }

        /// <summary>
        /// Load the Categories and bind the ComboBox
        /// </summary>
        private void LoadCategories()
        {
            UIUtilities.BindComboBox(cmbCategories, DataAccess.GetData("SELECT CategoryId, CategoryName FROM Categories ORDER BY CategoryName"), "CategoryName", "CategoryId");
        }

        /// <summary>
        /// Load the product details into the form when using navigation buttons
        /// </summary>
        private void LoadProductDetails()
        {
            //Clear any errors in the error provider
            errProvider.Clear();

            string[] sqlStatements = new string[]
            {
                $"SELECT * FROM Products WHERE ProductId = {currentProductId}",
                $@"
                SELECT 
                (
                    SELECT TOP(1) ProductId as FirstProductId FROM Products ORDER BY ProductName
                ) as FirstProductId,
                q.PreviousProductId,
                q.NextProductId,
                (
                    SELECT TOP(1) ProductId as LastProductId FROM Products ORDER BY ProductName Desc
                ) as LastProductId,
                q.RowNumber
                FROM
                (
                    SELECT ProductId, ProductName,
                    LEAD(ProductId) OVER(ORDER BY ProductName) AS NextProductId,
                    LAG(ProductId) OVER(ORDER BY ProductName) AS PreviousProductId,
                    ROW_NUMBER() OVER(ORDER BY ProductName) AS 'RowNumber'
                    FROM Products
                ) AS q
                WHERE q.ProductId = {currentProductId}
                ORDER BY q.ProductName".Replace(System.Environment.NewLine," "),
                "SELECT COUNT(ProductId) as ProductCount FROM Products"
            };

            DataSet ds = new DataSet();
            ds = DataAccess.GetData(sqlStatements);

            if(ds.Tables[0].Rows.Count == 1)
            {
                DataRow selectedProduct = ds.Tables[0].Rows[0];

                txtProductId.Text = selectedProduct["ProductId"].ToString();
                cmbSuppliers.SelectedValue = selectedProduct["SupplierId"];
                cmbCategories.SelectedValue = selectedProduct["CategoryId"];
                txtProductName.Text = selectedProduct["ProductName"].ToString();
                txtQtyPerUnit.Text = selectedProduct["QuantityPerUnit"].ToString();
                txtUnitPrice.Text = Convert.ToDouble(selectedProduct["UnitPrice"]).ToString("n2");
                txtStock.Text = selectedProduct["UnitsInStock"].ToString();
                txtOnOrder.Text = selectedProduct["UnitsOnOrder"].ToString();
                txtReorder.Text = selectedProduct["ReorderLevel"].ToString();
                chkDiscontinued.Checked = Convert.ToBoolean(selectedProduct["Discontinued"]);

                firstProductId = Convert.ToInt32(ds.Tables[1].Rows[0]["FirstProductId"]);
                previousProductId = ds.Tables[1].Rows[0]["PreviousProductId"] != DBNull.Value ? Convert.ToInt32(ds.Tables["Table1"].Rows[0]["PreviousProductId"]) : (int?)null;
                nextProductId = ds.Tables[1].Rows[0]["NextProductId"] != DBNull.Value ? Convert.ToInt32(ds.Tables["Table1"].Rows[0]["NextProductId"]) : (int?)null;
                lastProductId = Convert.ToInt32(ds.Tables[1].Rows[0]["LastProductId"]);
                currentRecord = Convert.ToInt32(ds.Tables[1].Rows[0]["RowNumber"]);

                // get total product count from table 3
                totalProductsCount = Convert.ToInt32(ds.Tables[2].Rows[0]["ProductCount"]);

                //Which item we are on in the count
                toolStripStatusLabel1.Text = $"Displaying product {currentRecord} of {totalProductsCount}";
            }
            else
            {
                MessageBox.Show("The product no longer exists");
                LoadFirstProduct();
            }

            
            NextPreviousButtonManagement();
        }

        #endregion

        #region [Navigation Helpers]

        /// <summary>
        /// Helps manage the enable state of our next and previous navigation buttons
        /// Depending on where we are in products we may need to set enable state based on position
        /// navigation through product records
        /// </summary>
        private void NextPreviousButtonManagement()
        {
            btnPrevious.Enabled = previousProductId != null;
            btnNext.Enabled = nextProductId != null;
        }

        /// <summary>
        /// Helper method to set state of all nav buttons
        /// </summary>
        /// <param name="enableState"></param>
        private void NavigationState(bool enableState)
        {
            btnFirst.Enabled = enableState;
            btnLast.Enabled = enableState;
            btnNext.Enabled = enableState;
            btnPrevious.Enabled = enableState;
        }

        /// <summary>
        /// Handle navigation button interaction
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Navigation_Handler(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            toolStripStatusLabel2.Text = string.Empty;

            switch (b.Name)
            {
                case "btnFirst":
                    currentProductId = firstProductId;
                    toolStripStatusLabel2.Text = "The first product is currently displayed";
                    break;
                case "btnLast":
                    currentProductId = lastProductId;
                    toolStripStatusLabel2.Text = "The last product is currently displayed";
                    break;
                case "btnPrevious":
                    currentProductId = previousProductId.Value;

                    if (currentRecord == 1)
                        toolStripStatusLabel2.Text = "The first product is currently displayed";
                    break;
                case "btnNext":
                    currentProductId = nextProductId.Value;
                    
                    break;
            }

            LoadProductDetails();
        }

        #endregion

        #region [Validation Events and Methods]

        /// <summary>
        
       
        /// <summary>
        /// Numeric validation 
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>The result of the validation</returns>
        private bool IsNumeric(string value)
        {
            return Double.TryParse(value, out double a);
        }

        #endregion

        #region [Form Helpers]
                
        /// <summary>
        /// Clear the form inputs and set checkbox unchecked
        /// </summary>
        /// <param name="controls">Controls collection to clear</param>
        private void ClearControls(Control.ControlCollection controls)
        {
            foreach (Control ctl in controls)
            {
                switch (ctl)
                {
                    case TextBox txt:
                        txt.Clear();
                        break;
                    case CheckBox chk:
                        chk.Checked = false;
                        break;
                    case GroupBox gB:
                        ClearControls(gB.Controls);
                        break;
                }
            }
        }

        /// <summary>
        /// Animate the progress bar
        /// This is ui thread blocking. Ok for this application.
        /// </summary>
        private void ProgressBar()
        {
            this.toolStripStatusLabel3.Text = "Processing...";
            prgBar.Value = 0;
            this.statusStrip1.Refresh();

            while (prgBar.Value < prgBar.Maximum)
            {
                Thread.Sleep(15);
                prgBar.Value += 1;
                prgBar.Value -= 1;
                prgBar.Value += 1;
            }

            prgBar.Value = 100;
            if(prgBar.Value == 100)
            this.toolStripStatusLabel3.Text = "Processed";
        }

        /// <summary>
        /// Allow an invalid form to close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmProductMaintenance_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;
        }

        #endregion

        private void LoadFirstProduct()
        {
            int productID = Convert.ToInt32(DataAccess.GetValue("SELECT TOP(1) ProductID FROM Products ORDER BY ProductName"));
            currentProductId = productID;

            LoadProductDetails();
        }

        private void cmb_Validating(object sender, CancelEventArgs e)
        {
            // typecast sender into combobox
            ComboBox cmb = sender as ComboBox;

            if(cmb == null)
            {
                // type cast did not work
                return;
            }

            string errorMsg = null;

            if(cmb.SelectedIndex < 1 || string.IsNullOrEmpty(cmb.SelectedValue.ToString()))
            {
                // user has not selected a value, or has selected empty row
                errorMsg = $"{cmb.Tag} is required";
                e.Cancel = true;

            }
            else
            {
                e.Cancel = false;
            }

            errProvider.SetError(cmb, errorMsg);

            
        }

        private void txt_Validating(object sender, CancelEventArgs e)
        {
            // typecast sender into textbox
            TextBox textBox = sender as TextBox;

            // return out of method of textBox is null
            if(textBox == null)
            {
                return;
            }

            string errorMsg = null;

            // ensure fields are not empty
            if (textBox.Text == String.Empty) {
                errorMsg = $"{textBox.Tag} is required";
                e.Cancel = true;
            }
            else if(
                (textBox.Name == "txtUnitPrice" 
                || textBox.Name == "txtStock" 
                || textBox.Name == "txtOnOrder" 
                || textBox.Name == "txtReorder") 
                && !IsNumeric(textBox.Text))
            {
                errorMsg = $"{textBox.Tag} is not numeric";
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }

            errProvider.SetError(textBox, errorMsg);

      
        }

        private void CreateProduct()
        {
            string sqlInsertQuery =
                $@"INSERT INTO Products(ProductName, SupplierID, CategoryID, QuantityPerUnit, UnitPrice, UnitsInStock, UnitsOnOrder, ReorderLevel, Discontinued) 
VALUES('{txtProductName.Text.Trim()}', {cmbSuppliers.SelectedValue}, {cmbCategories.SelectedValue}, '{txtQtyPerUnit.Text.Trim()}', {txtUnitPrice.Text.Trim()}, {txtStock.Text.Trim()}, {txtOnOrder.Text.Trim()}, {txtReorder.Text.Trim()}, {(chkDiscontinued.Checked ? 1 : 0)});";

            int rowsAffected = DataAccess.SendData(sqlInsertQuery);
            if(rowsAffected == 1)
            {
                MessageBox.Show("Product created successfully");
            }
            else
            {
                MessageBox.Show("Insert product was not successful");
            }

            // enable add button
            btnAdd.Enabled = true;
            // disable delete button
            btnDelete.Enabled = true;

            // change save button back to save
            btnSave.Text = "Save";

            LoadFirstProduct();
            NavigationState(true);
        }

        private void ProductChanges()
        {
            string sql =
                $@"UPDATE Products SET ProductName = '{txtProductName.Text.Trim()}', supplierID = {cmbSuppliers.SelectedValue}, categoryId = {cmbCategories.SelectedValue}, QuantityPerUnit = '{txtQtyPerUnit.Text.Trim()}', UnitPrice = {txtUnitPrice.Text.Trim()},UnitsInStock = {txtStock.Text.Trim()}, UnitsOnOrder = {txtOnOrder.Text.Trim()}, ReorderLevel = {txtReorder.Text.Trim()}, Discontinued = {(chkDiscontinued.Checked ? 1 : 0)}  WHERE ProductId = {txtProductId.Text}";
        
            int rowsAffected = DataAccess.SendData(sql);

            if(rowsAffected == 1)
            {
                MessageBox.Show("Product successfully updated");
            }
            else if(rowsAffected == 0)
            {
                MessageBox.Show("No Rows Updated");
            }
        }
    }
}
