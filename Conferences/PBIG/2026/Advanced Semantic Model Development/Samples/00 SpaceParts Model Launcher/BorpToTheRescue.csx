#r "System.Drawing"
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using System.IO;

// --------------------------------------------------------------------------------------------------------
// Tabular Editor C# script to create the SpaceParts semantic model.
// Run against a new empty model in Tabular Editor 2 or 3.
// Author: Peer Grønnerup, Tabular Editor
// -------------------------------------------------------------------------------------------------------- 

Application.UseWaitCursor = false;

var imageUrl = "https://tabulareditor.com/hs-fs/hubfs/BorpWeb3.png?width=400&height=636&name=BorpWeb3.png";

Image borpImage;
using (var webClient = new WebClient())
using (var stream = webClient.OpenRead(imageUrl))
{
    borpImage = Image.FromStream(stream);
}

// Layout constants
int padding = 20;
int imgWidth = 300;
int imgHeight = (int)(imgWidth * ((double)borpImage.Height / borpImage.Width));
int headerHeight = 50;
int buttonHeight = 50;
int formWidth = imgWidth + padding * 2;
int formHeight = padding + headerHeight + imgHeight + 40 + buttonHeight + padding;

// Create the form
var form = new Form()
{
    Text = "Borp Launcher",
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog,
    MaximizeBox = false,
    MinimizeBox = false,
    ClientSize = new Size(formWidth, formHeight),
    BackColor = Color.White
};

// Header label
var header = new Label()
{
    Text = "Borp to the rescue!",
    Font = new Font("Segoe UI", 16f, FontStyle.Bold),
    ForeColor = Color.FromArgb(40, 40, 80),
    TextAlign = ContentAlignment.MiddleCenter,
    AutoSize = false,
    Width = formWidth,
    Height = headerHeight,
    Location = new Point(0, padding)
};
form.Controls.Add(header);

// Image
var pictureBox = new PictureBox()
{
    Image = borpImage,
    SizeMode = PictureBoxSizeMode.Zoom,
    Width = imgWidth,
    Height = imgHeight,
    Location = new Point(padding, padding + headerHeight)
};
form.Controls.Add(pictureBox);

var cbSemanticModel = new CheckBox
{
    AutoSize = true,
    Checked = true,
    Location = new Point(padding +70, padding + headerHeight + imgHeight + 15)
}; 

var lblSemanticModel = new Label
{
    Text = "Is Power BI Semantic Model",
    AutoSize = true,
    Location = new Point(padding + 92 , padding + headerHeight + imgHeight + 15) // space to the right of checkbox
};

form.Controls.Add(cbSemanticModel);
form.Controls.Add(lblSemanticModel);

// Launch button
var launchButton = new Button()
{
    Text = "GO",
    Font = new Font("Segoe UI", 14f, FontStyle.Bold),
    FlatStyle = FlatStyle.Flat,
    BackColor = Color.Red,
    ForeColor = Color.White,
    Cursor = Cursors.Hand,
    Height = buttonHeight,
    Width = formWidth - padding * 2,
    Location = new Point(padding, padding + headerHeight + imgHeight + 40)
};
launchButton.FlatAppearance.BorderSize = 0;
launchButton.Click += (sender, evnt) =>
{
    form.DialogResult = DialogResult.OK;
    
    // ============================================================
    // CONFIGURATION
    // ============================================================
    // true  = Power BI  (M partitions + shared M expressions)
    // false = Analysis Services  (SQL partitions + legacy datasource)

    var isPowerBI = cbSemanticModel.Checked;

    // ============================================================
    // 1. MODEL SETTINGS
    // ============================================================
    Model.DiscourageImplicitMeasures = true;

    if (!isPowerBI && Model.Database.CompatibilityLevel > 1600) Model.Database.CompatibilityLevel = 1600;

    // ============================================================
    // ============================================================
    // 2. SHARED M EXPRESSIONS
    // ============================================================
    if (isPowerBI) 
    {
        if (!Model.Expressions.Contains("SqlEndpoint"))
        {
            var e = Model.AddExpression("SqlEndpoint");
            e.Kind = ExpressionKind.M;
            e.Expression = @"""te3-training-eu.database.windows.net"" meta [IsParameterQuery=true, Type=""Text"", IsParameterQueryRequired=true]";
        }
        if (!Model.Expressions.Contains("Database"))
        {
            var e = Model.AddExpression("Database");
            e.Kind = ExpressionKind.M;
            e.Expression = @"""SpacePartsCoDW"" meta [IsParameterQuery=true, Type=""Text"", IsParameterQueryRequired=true]";
        }
        if (!Model.Expressions.Contains("RangeStart"))
        {
            var e = Model.AddExpression("RangeStart");
            e.Kind = ExpressionKind.M;
            e.Expression = @"#datetime(2010, 1, 1, 0, 0, 0) meta [IsParameterQuery=true, Type=""DateTime"", IsParameterQueryRequired=true]";
        }
        if (!Model.Expressions.Contains("RangeEnd"))
        {
            var e = Model.AddExpression("RangeEnd");
            e.Kind = ExpressionKind.M;
            e.Expression = @"#datetime(2030, 1, 1, 0, 0, 0) meta [IsParameterQuery=true, Type=""DateTime"", IsParameterQueryRequired=true]";
        }
    // ============================================================
    } else {
        {
            var ds = Model.AddDataSource("SpacePartsCoDW");
            ds.ConnectionString = @"Data Source=te3-training-eu.database.windows.net;Initial Catalog=SpacePartsCoDW;User ID=dwreader;Password=TE3#reader!;Encrypt=True";
            ds.Provider = "System.Data.SqlClient";
            ds.ImpersonationMode = ImpersonationMode.ImpersonateServiceAccount;
            ds.SetAnnotation("ConnectionEditUISource", "SqlServer");
        }
    }
    
    // 3. REGULAR (M) TABLES
    // ============================================================

    // --- Brands ---
    {
        var t = Model.AddTable("Brands");
        if (isPowerBI) {
            t.AddMPartition("Brands_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Brands""]}[Data]
    in
        Data");
            t.Partitions["Brands"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Brands]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
            t.Partitions[0].Mode = ModeType.Import;
        }
        {
            var c = t.AddDataColumn("Flagship", "Flagship", "1. Brand Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Class", "Class", "1. Brand Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Type", "Type", "2. Brand Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Brand", "Brand", "1. Brand Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Sub Brand", "Sub Brand", "1. Brand Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Product Brand VP", "Product Brand VP", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var h = t.AddHierarchy("Brand Hierarchy");
            h.DisplayFolder = "1. Brand Hierarchy";
            h.AddLevel(t.Columns["Class"], "Class");
            h.AddLevel(t.Columns["Flagship"], "Flagship");
            h.AddLevel(t.Columns["Brand"], "Brand");
            h.AddLevel(t.Columns["Sub Brand"], "Sub Brand");
        }
    }

    // --- Budget Rate ---
    {
        var t = Model.AddTable("Budget Rate");
        t.IsHidden = true;
        if (isPowerBI) {
            t.AddMPartition("Budget Rate_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Budget Rate""]}[Data]
    in
        Data");
            t.Partitions["Budget Rate"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Budget Rate]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
            t.Partitions[0].Mode = ModeType.Import;
        }
        {
            var c = t.AddDataColumn("Rate", "Rate", "1. Facts", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("From Currency", "From Currency", "2. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("To Currency", "To Currency", "3. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Currency System", "Currency System", "3. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
    }

    // --- Customers ---
    {
        var t = Model.AddTable("Customers");
        if (isPowerBI) {
            t.AddMPartition("Customers_M",
    @"let

      // Data Source
      Source = Sql.Database(
        #""SqlEndpoint"", 
        #""Database""
      ), 

      // Step
      Data = Source
        {
          [
            Schema = ""Dimview"", 
            Item   = ""Customers""
          ]
        }
        [Data]

    // Result
    in
      Data");
            t.Partitions["Customers"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Customers]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
            t.Partitions[0].Mode = ModeType.Import;
        }
        {
            var c = t.AddDataColumn("Customer Key", "Customer Key", "4. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Customer Sold-To Name", "Customer Sold-To Name", "1. Customer Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Account Name", "Account Name", "1. Customer Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Key Account Name", "Key Account Name", "1. Customer Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Transaction Type", "Transaction Type", "2. Other Customer Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Account Type", "Account Type", "1. Customer Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Station", "Station", "4. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Account Manager", "Account Manager", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Key Account Manager", "Key Account Manager", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var m = t.AddMeasure("# Customers",
                @"
    COUNTROWS (
        VALUES ( 'Customers'[Account Name] )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "Measures";
        }
        {
            var m = t.AddMeasure("# Key Accounts",
                @"
    COUNTROWS (
        VALUES ( 'Customers'[Key Account Name] )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "Measures";
        }
        {
            var m = t.AddMeasure("Customer Label",
                @"SELECTEDVALUE (
        Customers[Account Name],
        ""All customers""
    )");
        }
        {
            var h = t.AddHierarchy("Customer Hierarchy");
            h.DisplayFolder = "1. Customer Hierarchy";
            h.AddLevel(t.Columns["Account Type"], "Account Type");
            h.AddLevel(t.Columns["Key Account Name"], "Key Account Name");
            h.AddLevel(t.Columns["Account Name"], "Account Name");
            h.AddLevel(t.Columns["Customer Sold-To Name"], "Customer Sold-To Name");
        }
    }

    // --- Employees ---
    {
        var t = Model.AddTable("Employees");
        t.IsHidden = true;
        if (isPowerBI) {
            t.AddMPartition("Employees_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Employees""]}[Data]
    in
        Data");
            t.Partitions["Employees"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Employees]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
            t.Partitions[0].Mode = ModeType.Import;
        }
        {
            var c = t.AddDataColumn("Role", "Role", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Employee Name", "Employee Name", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Employee Email", "Employee Email", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Data Security Rule", "Data Security Rule", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
    }

    // --- Exchange Rate ---
    {
        var t = Model.AddTable("Exchange Rate");
        if (isPowerBI) {
            t.AddMPartition("Exchange Rate_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Exchange Rate""]}[Data]
    in
        Data");
            t.Partitions["Exchange Rate"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Exchange Rate]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Rate Type", "Rate Type", "2. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("From Currency", "From Currency", "1. Select a Currency", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("To Currency", "To Currency", "2. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Currency System", "Currency System", "2. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Rate", "Rate", "2. Other Currency Fields", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Date", "Date", "2. Other Currency Fields", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Month", "Month", "2. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Exchange Rate Composite Key", "Exchange Rate Composite Key", "2. Other Currency Fields", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
    }

    // --- Invoice Document Type ---
    {
        var t = Model.AddTable("Invoice Document Type");
        if (isPowerBI) {
            t.AddMPartition("Invoice Document Type_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Invoice Document Type""]}[Data]
    in
        Data");
            t.Partitions["Invoice Document Type"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Invoice Document Type]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Billing Document Type Code", "Billing Document Type Code", "2. Keys", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Text", "Text", "1. Billing Doc. Type", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Doc. Type Ordinal", "Doc. Type Ordinal", "3. Ordinal", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Group", "Group", "1. Billing Doc. Type", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Group Ordinal", "Group Ordinal", "3. Ordinal", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        t.Columns["Billing Document Type Code"].SortByColumn = t.Columns["Doc. Type Ordinal"];
        t.Columns["Text"].SortByColumn = t.Columns["Doc. Type Ordinal"];
        t.Columns["Group"].SortByColumn = t.Columns["Group Ordinal"];
    }

    // --- Products ---
    {
        var t = Model.AddTable("Products");
        if (isPowerBI) {
            t.AddMPartition("Products_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Products""]}[Data]
    in
        Data");
            t.Partitions["Products"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Products]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Sub Brand Name", "Sub Brand Name", "5. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Ship Class for Part", "Ship Class for Part", "2. Product Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Product Name", "Product Name", "1. Product Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Part Fit Grading", "Part Fit Grading", "2. Product Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Product Key", "Product Key", "5. Keys", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Subtype", "Subtype", "1. Product Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Type", "Type", "1. Product Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("MK", "MK", "2. Product Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Color", "Color", "2. Product Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Production Series", "Production Series", "2. Product Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Nameplate", "Nameplate", "2. Product Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Material", "Material", "1. Product Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Maximum Temperature (K)", "Maximum Temperature (K)", "4. Product Facts", DataType.Int64);
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Product Business Line Leader", "Product Business Line Leader", "3. Product Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Tolerance (g)", "Tolerance (g)", "4. Product Facts", DataType.Int64);
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Velocity Tolerance (Meters / Second)", "Velocity Tolerance (Meters / Second)", "4. Product Facts", DataType.Int64);
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Weight (Tonnes)", "Weight (Tonnes)", "4. Product Facts", DataType.Double);
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var m = t.AddMeasure("# Products",
                @"
    COUNTROWS ( 
        VALUES ( 'Products'[Product Name] )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "Measures";
        }
        {
            var h = t.AddHierarchy("Product Hierarchy");
            h.DisplayFolder = "1. Product Hierarchy";
            h.AddLevel(t.Columns["Type"], "Type");
            h.AddLevel(t.Columns["Subtype"], "Subtype");
            h.AddLevel(t.Columns["Product Name"], "Product Name");
            h.AddLevel(t.Columns["Material"], "Material");
        }
    }

    // --- Regions ---
    {
        var t = Model.AddTable("Regions");
        if (isPowerBI) {
            t.AddMPartition("Regions_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Regions""]}[Data]
    in
        Data");
            t.Partitions["Regions"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Regions]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("System", "System", "1. Region Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Interplanetary Region", "Interplanetary Region", "1. Region Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Territory", "Territory", "1. Region Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Station", "Station", "1. Region Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Station Type", "Station Type", "2. Region Attributes", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Territory Directors", "Territory Directors", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("All", "All", "1. Region Hierarchy", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Station Sales Managers", "Station Sales Managers", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("System Regional Managers", "System Regional Managers", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("System Sales Directors", "System Sales Directors", "3. Managers", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Tax Rate", "Tax Rate", "2. Region Attributes", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
            c.FormatString = "#,##0.000";
        }
        {
            var h = t.AddHierarchy("Regional Hierarchy");
            h.DisplayFolder = "1. Region Hierarchy";
            h.AddLevel(t.Columns["All"], "All");
            h.AddLevel(t.Columns["System"], "System");
            h.AddLevel(t.Columns["Interplanetary Region"], "Interplanetary Region");
            h.AddLevel(t.Columns["Territory"], "Territory");
            h.AddLevel(t.Columns["Station"], "Station");
        }
    }

    // --- Budget ---
    {
        var t = Model.AddTable("Budget");
        if (isPowerBI) {
            t.AddMPartition("Budget_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Factview"",Item=""Budget""]}[Data],
        #""Select Columns"" = Table.SelectColumns ( Data, {""Customer Key"", ""Month"", ""Product Key"", ""Total Budget""} )
    in
        #""Select Columns""");
            t.Partitions["Budget"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Factview].[Budget]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Month", "Month", "1. Keys", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Budget (EUR)", "Total Budget", "2. Facts", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Customer Key", "Customer Key", "1. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Product Key", "Product Key", "1. Keys", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var m = t.AddMeasure("2022 Budget Achievement %",
                @"
    VAR _Sales =
        CALCULATE (
            [Gross Sales],
            'Date'[Calendar Year Number (ie 2021)]
                = 2022,
            'Date'[IsDateInScope] = TRUE ( ),
            DATESYTD ( 'Date'[Date] )
        )
    VAR _Budget =
        CALCULATE (
            [Total Budget],
            'Date'[Calendar Year Number (ie 2021)]
                = 2022,
            'Date'[IsDateInScope] = TRUE ( ),
            DATESYTD ( 'Date'[Date] )
        )
    VAR _Delta = _Sales - _Budget
    VAR _Perc = DIVIDE ( _Delta, _Budget )
    RETURN
        _Perc");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Budget MTD",
                @"
    -- Calculates the target for the current month/year selection
    VAR _CURRENT_MONTH_VALUE =
        CALCULATE (
            [Budget],
            ALL ( 'Date' ),
            VALUES ( 'Date'[Calendar Month Year (ie Jan 21)] )
        ) 

    -- Computes rate at which Budget increases through the month
    VAR _CURRENT_MONTH_PERC_WORKDAYS_MTD =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE
            ),
            1.035
        )

    VAR _CURRENT_MONTH_PERC_WORKDAYS =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] )
            ),
            1.035
        )

    VAR _PERIOD =
        SELECTEDVALUE (
            '4) Selected Period'[Period]
        )
    VAR _HASONEMONTH =
        HASONEVALUE (
            'Date'[Calendar Month Year (ie Jan 21)]
        )
    RETURN
        -- return MTD value if a month is selected
        IF.EAGER (
            _HASONEMONTH
                && _PERIOD = ""Full Period"",
            _CURRENT_MONTH_PERC_WORKDAYS
                * _CURRENT_MONTH_VALUE,

            IF (
                _HASONEMONTH,
                _CURRENT_MONTH_PERC_WORKDAYS_MTD
                    * _CURRENT_MONTH_VALUE
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\ii. MTD";
        }
        {
            var m = t.AddMeasure("Budget MTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Budget MTD]
    VAR _DELTA  = [MTD Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\ii. MTD";
        }
        {
            var m = t.AddMeasure("Budget MTD vs. Gross Sales (Δ)",
                @"
    [MTD Gross Sales] - [Budget MTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\ii. MTD";
        }
        {
            var m = t.AddMeasure("Budget QTD",
                @"
    VAR _DATES_QTD =
        CALCULATETABLE (
            DATESQTD ('Date'[Date]),
            'Date'[IsThisMonth] = FALSE
        )
    VAR _CURRENT_MONTH_Budget = [Budget MTD]
    VAR QTD_BUDGET_BEFORE_THIS_MONTH = CALCULATE ([Budget], _DATES_QTD)
    VAR _RESULT = _CURRENT_MONTH_Budget + QTD_BUDGET_BEFORE_THIS_MONTH
    RETURN
        _RESULT");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\iii. QTD";
        }
        {
            var m = t.AddMeasure("Budget QTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Budget QTD]
    VAR _DELTA  = [Gross Sales] - [Budget QTD]
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\iii. QTD";
        }
        {
            var m = t.AddMeasure("Budget QTD vs. Gross Sales (Δ)",
                @"
    [Gross Sales] - [Budget QTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\iii. QTD";
        }
        {
            var m = t.AddMeasure("Budget YTD",
                @"
    VAR _DATES_YTD =
        CALCULATETABLE (
            DATESYTD ( 'Date'[Date] ),
            'Date'[IsThisMonth] = FALSE
        )
    VAR _CURRENT_MONTH_Budget = [Budget MTD]
    VAR QTD_Budget_BEFORE_THIS_MONTH =
        CALCULATE ( [Budget], _DATES_YTD )
    VAR _RESULT =
        _CURRENT_MONTH_Budget
            + QTD_Budget_BEFORE_THIS_MONTH
    RETURN
        _RESULT");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\iv. YTD";
        }
        {
            var m = t.AddMeasure("Budget YTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Budget YTD]
    VAR _DELTA  = [Gross Sales] - [Budget YTD]
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\iv. YTD";
        }
        {
            var m = t.AddMeasure("Budget YTD vs. Gross Sales (Δ)",
                @"
    [Gross Sales] - [Budget YTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\iv. YTD";
        }
        {
            var m = t.AddMeasure("Total Budget",
                @"
        VAR _SelectedRate = MAX ( 'Exchange Rate'[Rate] )
    RETURN
        SUM ( 'Budget'[Budget (EUR)] ) * _SelectedRate");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Budget",
                @"
    IF.EAGER(
        HASONEVALUE( '2) Selected Unit'[Select a Unit] )
            && 
        HASONEVALUE( 'Exchange Rate'[From Currency] ),

        VAR _SELECTED_CURRENCY = 
            SELECTEDVALUE( 'Exchange Rate'[From Currency] )

        VAR _SELECTED_RATE_TYPE = 
            SELECTEDVALUE( '2) Selected Unit'[Select a Unit] )
        
        VAR _SELECTED_BUDGET_RATE =
            MAX( 'Exchange Rate'[Rate] )
            
        VAR _SELECTED_MONTHLY_RATE =
            ADDCOLUMNS(
                SUMMARIZE(
                    'Exchange Rate',
                    'Date'[Calendar Month Year (ie Jan 21)]
                ),
                ""Curr"", 
                CALCULATE ( 
                    MAX( 'Exchange Rate'[Rate] ) 
                )
            )
            
        RETURN

        IF(
            _SELECTED_RATE_TYPE = ""Value (Budget Rate)"",
            SUM ( 'Budget'[Budget (EUR)]) 
            * 
            _SELECTED_BUDGET_RATE,
            
            IF(
                _SELECTED_RATE_TYPE = ""Value (Monthly Rate)"",
                SUMX(
                    _SELECTED_MONTHLY_RATE,
                    CALCULATE( 
                        SUM ( 'Budget'[Budget (EUR)])
                    )
                    * [Curr]
                )
            )
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Budget vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Budget]
    VAR _DELTA  = [Gross Sales] - [Budget]
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Budget vs. Gross Sales (Δ)",
                @"
    [Gross Sales] - [Budget]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\i. Total";
        }
    }

    // --- Orders ---
    {
        var t = Model.AddTable("Orders");
        if (isPowerBI) {
            t.AddMPartition("Orders_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Factview"",Item=""Orders""]}[Data],
        #""Select Columns"" = Table.RemoveColumns ( Data, ""DWCreatedDate"" ),
        #""Added Custom"" = Table.AddColumn(#""Select Columns"", ""RandomDays"", each Number.RandomBetween(7, 90), Int64.Type),
        #""Changed Type"" = Table.TransformColumnTypes(#""Added Custom"",{{""RandomDays"", Int64.Type}}),
        #""Added Custom1"" = Table.AddColumn(#""Changed Type"", ""Fixed Order Date"", each if [Order Date] = null then Date.AddDays([Request Goods Receipt Date], -[RandomDays]) else [Order Date]),
        #""Changed Type1"" = Table.TransformColumnTypes(#""Added Custom1"",{{""Fixed Order Date"", type date}}),
        #""Removed Columns"" = Table.RemoveColumns(#""Changed Type1"",{""Order Date"", ""RandomDays""}),
        #""Renamed Columns"" = Table.RenameColumns(#""Removed Columns"",{{""Fixed Order Date"", ""Order Date""}})
    in
        #""Renamed Columns""");
            t.Partitions["Orders"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Factview].[Orders]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Sales Order Document Number", "Sales Order Document Number", @"Columns\2. Document Fields", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Order Date", "Order Date", @"Columns\3. Keys\Dates", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Customer Key", "Customer Key", @"Columns\3. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Sales Order Document Line Item Number", "Sales Order Document Line Item Number", @"Columns\2. Document Fields", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Product Key", "Product Key", @"Columns\3. Keys", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Billing Date", "Billing Date", @"Columns\3. Keys\Dates", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Ship Date", "Ship Date", @"Columns\3. Keys\Dates", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Request Goods Receipt Date", "Request Goods Receipt Date", @"Columns\3. Keys\Dates", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Confirm Goods Receipt Date", "Confirm Goods Receipt Date", @"Columns\3. Keys\Dates", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Sales Order Document Type Code", "Sales Order Document Type Code", @"Columns\3. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Sales Order Document Line Item Status", "Sales Order Document Line Item Status", @"Columns\3. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Local Currency", "Local Currency", @"Columns\3. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Net Order Value", "Net Order Value", @"Columns\1. Facts", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Net Order Quantity", "Net Order Quantity", @"Columns\1. Facts", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var m = t.AddMeasure("Net Orders",
                @"
    CALCULATE (
        [Total Net Order Value],
        'Order Document Type'[Group]
            <> ""Cancellation""
    ) ");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\i. Total";
            m.Description = "Net orders exclude cancellation document types";
        }
        {
            var m = t.AddMeasure("Net Orders (Quantity)",
                @"
    CALCULATE (
        [Total Net Order Quantity],
        'Order Document Type'[Group]
            <> ""Cancellation""
    ) ");
            m.FormatString = "#,##0";
            m.DisplayFolder = "2. Quantity";
            m.Description = "Net orders exclude cancellation document types";
        }
        {
            var m = t.AddMeasure("Net Orders 1YP",
                @"
    CALCULATE ( [Net Orders], DATEADD('Date'[Date], -1, YEAR ))");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\ii. 1YP";
            m.Description = "Net orders exclude cancellation document types";
        }
        {
            var m = t.AddMeasure("Net Orders 2YP",
                @"
    CALCULATE ( [Net Orders], DATEADD('Date'[Date], -2, YEAR ))");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\iii. 2YP";
            m.Description = "Net orders exclude cancellation document types";
        }
        {
            var m = t.AddMeasure("Total Net Order Value",
                @"
    VAR _RATE = MAX ('Exchange Rate'[Rate])
    RETURN

        -- Convert to common currency (WAT)
        SUMX (
            'Orders',
            'Orders'[Net Order Value]
                / RELATED ('Budget Rate'[Rate])
        )

            -- Convert to selected currency
            * _RATE ");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\i. Total";
            m.Description = "The net order value is the sum of all order intake including cancellations (also known as Gross Orders)";
        }
        {
            var m = t.AddMeasure("Order Lines",
                @"
    COUNTROWS ( 'Orders' )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "3. Lines";
        }
        {
            var m = t.AddMeasure("Total Net Order Quantity",
                @"
    SUM ( 'Orders'[Net Order Quantity] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = "2. Quantity";
        }
    }

    // --- Forecast ---
    {
        var t = Model.AddTable("Forecast");
        if (isPowerBI) {
            t.AddMPartition("Forecast_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Factview"",Item=""Forecast""]}[Data],
        #""Select Columns"" = Table.SelectColumns ( Data, {""Forecast Month"", ""Forecast (EUR)"", ""Region Territory"", ""Product Type""})
    in
        #""Select Columns""");
            t.Partitions["Forecast"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Factview].[Forecast]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Forecast Month", "Forecast Month", "2. Keys", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Region Territory", "Region Territory", "2. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Product Type", "Product Type", "2. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Forecast (EUR)", "Forecast (EUR)", "1. Facts", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var m = t.AddMeasure("Forecast MTD",
                @"
    -- Calculates the target for the current month/year selection
    VAR _CURRENT_MONTH_VALUE =
        CALCULATE (
            [Forecast],
            ALL ( 'Date' ),
            VALUES ( 'Date'[Calendar Month Year (ie Jan 21)] )
        ) 

    -- Computes rate at which Forecast increases through the month
    VAR _CURRENT_MONTH_PERC_WORKDAYS_MTD =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE
            ),
            1.035
        )

    VAR _CURRENT_MONTH_PERC_WORKDAYS =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] )
            ),
            1.035
        )

    VAR _PERIOD =
        SELECTEDVALUE (
            '4) Selected Period'[Period]
        )
    VAR _HASONEMONTH =
        HASONEVALUE (
            'Date'[Calendar Month Year (ie Jan 21)]
        )
    RETURN
        -- return MTD value if a month is selected
        IF.EAGER (
            _HASONEMONTH
                && _PERIOD = ""Full Period"",
            _CURRENT_MONTH_PERC_WORKDAYS
                * _CURRENT_MONTH_VALUE,

            IF (
                _HASONEMONTH,
                _CURRENT_MONTH_PERC_WORKDAYS_MTD
                    * _CURRENT_MONTH_VALUE
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\ii. MTD";
        }
        {
            var m = t.AddMeasure("Forecast QTD",
                @"
    VAR _DATES_QTD =
        CALCULATETABLE (
            DATESQTD ('Date'[Date]),
            'Date'[IsThisMonth] = FALSE
        )
    VAR _CURRENT_MONTH_Forecast = [Forecast MTD]
    VAR _QTD_Forecast_BEFORE_THIS_MONTH = CALCULATE ([Forecast], _DATES_QTD)
    VAR _RESULT = _CURRENT_MONTH_Forecast + _QTD_Forecast_BEFORE_THIS_MONTH
    RETURN
        _RESULT");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\iii. QTD";
        }
        {
            var m = t.AddMeasure("Forecast QTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Forecast QTD]
    VAR _DELTA  = [Gross Sales] - [Forecast QTD]
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\iii. QTD";
        }
        {
            var m = t.AddMeasure("Forecast QTD vs. Gross Sales (Δ)",
                @"
    [Gross Sales] - [Forecast QTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\iii. QTD";
        }
        {
            var m = t.AddMeasure("Forecast YTD",
                @"
    VAR _DATES_YTD =
        CALCULATETABLE (
            DATESYTD ('Date'[Date]),
            'Date'[IsThisMonth] = FALSE
        )
    VAR _CURRENT_MONTH_Forecast = [Forecast MTD]
    VAR _QTD_Forecast_BEFORE_THIS_MONTH = CALCULATE ([Forecast], _DATES_YTD)
    VAR _RESULT = _CURRENT_MONTH_Forecast + _QTD_Forecast_BEFORE_THIS_MONTH
    RETURN
        _RESULT");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\iv. YTD";
        }
        {
            var m = t.AddMeasure("Forecast YTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Forecast YTD]
    VAR _DELTA  = [Gross Sales] - [Forecast YTD]
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\iv. YTD";
        }
        {
            var m = t.AddMeasure("Forecast YTD vs. Gross Sales (Δ)",
                @"
    [Gross Sales] - [Forecast YTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\iv. YTD";
        }
        {
            var m = t.AddMeasure("Total Forecast",
                @"
        VAR _SelectedRate = MAX ( 'Exchange Rate'[Rate] )
    RETURN
        SUM ( 'Forecast'[Forecast (EUR)] ) * _SelectedRate");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Forecast",
                @"
    IF.EAGER(
        HASONEVALUE( '2) Selected Unit'[Select a Unit] )
            && 
        HASONEVALUE( 'Exchange Rate'[From Currency] ),

        VAR _SELECTED_CURRENCY = 
            SELECTEDVALUE( 'Exchange Rate'[From Currency] )

        VAR _SELECTED_RATE_TYPE = 
            SELECTEDVALUE( '2) Selected Unit'[Select a Unit] )
        
        VAR _SELECTED_BUDGET_RATE =
            MAX( 'Exchange Rate'[Rate] )
            
        VAR _SELECTED_MONTHLY_RATE =
            ADDCOLUMNS(
                SUMMARIZE(
                    'Exchange Rate',
                    'Date'[Calendar Month Year (ie Jan 21)]
                ),
                ""Curr"", 
                CALCULATE ( 
                    MAX( 'Exchange Rate'[Rate] ) 
                )
            )
            
        RETURN

        IF(
            _SELECTED_RATE_TYPE = ""Value (Budget Rate)"",
            SUM ( 'Forecast'[Forecast (EUR)]) 
            * 
            _SELECTED_BUDGET_RATE,
            
            IF(
                _SELECTED_RATE_TYPE = ""Value (Monthly Rate)"",
                SUMX(
                    _SELECTED_MONTHLY_RATE,
                    CALCULATE( 
                        SUM ( 'Forecast'[Forecast (EUR)])
                    )
                    * [Curr]
                )
            )
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Forecast MTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Forecast MTD]
    VAR _DELTA  = [MTD Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\ii. MTD";
        }
        {
            var m = t.AddMeasure("Forecast MTD vs. Gross Sales (Δ)",
                @"
    [MTD Gross Sales] - [Forecast MTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\ii. MTD";
        }
        {
            var m = t.AddMeasure("Forecast vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Forecast]
    VAR _DELTA  = [Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"Measures\i. Total";
        }
        {
            var m = t.AddMeasure("Forecast vs. Gross Sales (Δ)",
                @"
    [Gross Sales] - [Forecast]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"Measures\i. Total";
        }
    }

    // --- Order Document Type ---
    {
        var t = Model.AddTable("Order Document Type");
        if (isPowerBI) {
            t.AddMPartition("Order Document Type_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Order Document Type""]}[Data]
    in
        Data");
            t.Partitions["Order Document Type"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Order Document Type]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Sales Order Document Type Code", "Sales Order Document Type Code", "2. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Text", "Text", "1. Order Doc. Type", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Doc. Type Ordinal", "Doc. Type Ordinal", "3. Ordinal", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Group", "Group", "1. Order Doc. Type", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Group Ordinal", "Group Ordinal", "3. Ordinal", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
    }

    // --- Order Status ---
    {
        var t = Model.AddTable("Order Status");
        if (isPowerBI) {
            t.AddMPartition("Order Status_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Dimview"",Item=""Order Status""]}[Data]
    in
        Data");
            t.Partitions["Order Status"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Dimview].[Order Status]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Order Status Code", "Order Status Code", "2. Keys", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Order Status Text", "Order Status Text", "1. Order Status", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Order Status Ordinal", "Order Status Ordinal", "3. Ordinal", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Order Status Group", "Order Status Group", "1. Order Status", DataType.String);
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Order Status Grouping Ordinal", "Order Status Grouping Ordinal", "3. Ordinal", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        t.Columns["Order Status Code"].SortByColumn = t.Columns["Order Status Ordinal"];
        t.Columns["Order Status Text"].SortByColumn = t.Columns["Order Status Ordinal"];
        t.Columns["Order Status Group"].SortByColumn = t.Columns["Order Status Grouping Ordinal"];
    }

    // --- Last Refresh ---
    {
        var t = Model.AddTable("Last Refresh");
        if (isPowerBI) {
            t.AddMPartition("Last Refresh_M",
    @"let
        Source = DateTimeZone.FixedLocalNow()
    in
        Source");
            t.Partitions["Last Refresh"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT GETDATE() AS [Last Refresh]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Last Refresh", "Last Refresh", "", DataType.DateTime);
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "General Date";
        }
    }

    // --- Invoices ---
    {
        var t = Model.AddTable("Invoices");
        if (isPowerBI) {
            t.AddMPartition("Invoices_M",
    @"let
        Source = Sql.Database(#""SqlEndpoint"",#""Database""),
        Data = Source{[Schema=""Factview"",Item=""Invoices""]}[Data],
        #""Select Columns"" = Table.RemoveColumns ( Data, {""DWCreatedDate"", ""Net Invoice Cost""} )
    in
        #""Select Columns""");
            t.Partitions["Invoices"].Delete();
        } else {
            t.Partitions[0].Query = "SELECT * FROM [Factview].[Invoices]";
            t.Partitions[0].DataSource = Model.DataSources["SpacePartsCoDW"];
        }
        {
            var c = t.AddDataColumn("Billing Document Number", "Billing Document Number", "Columns", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
            c.FormatString = "0";
        }
        {
            var c = t.AddDataColumn("Billing Date", "Billing Date", "Columns", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("Customer Key", "Customer Key", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Billing Document Line Item Number", "Billing Document Line Item Number", "Columns", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
            c.FormatString = "0";
        }
        {
            var c = t.AddDataColumn("Product Key", "Product Key", "Columns", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "0";
        }
        {
            var c = t.AddDataColumn("Ship Date", "Ship Date", "Columns", DataType.DateTime);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = "Long Date";
        }
        {
            var c = t.AddDataColumn("OTD Indicator", "OTD Indicator", "Columns", DataType.Boolean);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
            c.FormatString = @"""TRUE"";""TRUE"";""FALSE""";
        }
        {
            var c = t.AddDataColumn("Billing Document Type Code", "Billing Document Type Code", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Local Currency", "Local Currency", "Columns", DataType.String);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.None;
        }
        {
            var c = t.AddDataColumn("Delivery Cost", "Delivery Cost", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Net Invoice COGS", "Net Invoice COGS", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Late Delivery Penalties", "Late Delivery Penalties", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Overdue Payment Penalties", "Overdue Payment Penalties", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Taxes & Commercial Fees", "Taxes & Commercial Fees", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Freight", "Freight", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Net Invoice Value", "Net Invoice Value", "Columns", DataType.Double);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
        }
        {
            var c = t.AddDataColumn("Net Invoice Quantity", "Net Invoice Quantity", "Columns", DataType.Int64);
            c.IsHidden = true;
            c.SummarizeBy = AggregateFunction.Sum;
            c.FormatString = "0";
        }
        {
            var m = t.AddMeasure("Delivery Margin (%)",
                @"
    VAR _TOTAL =
        CALCULATE (
            [Total Freight Surcharge],
            'Invoice Document Type'[Billing Document Type Code] IN {""invoice"", ""express""}
        )
    VAR _MARGIN = [Delivery Margin (Value)]
    RETURN
        DIVIDE (_MARGIN, _TOTAL)");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\III. Delivery Margin\%";
            m.Description = "The delivery margin only considers the revenue and costs associated with freights, delivery and shipments.";
        }
        {
            var m = t.AddMeasure("Delivery Margin 1YP (%)",
                @"CALCULATE (
        [Delivery Margin (%)],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\III. Delivery Margin\%";
            m.Description = "The delivery margin only considers the revenue and costs associated with freights, delivery and shipments.";
        }
        {
            var m = t.AddMeasure("Delivery Margin 2YP (%)",
                @"CALCULATE (
        [Delivery Margin (%)],
        DATEADD ('Date'[Date], -2, YEAR)
    )");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\III. Delivery Margin\%";
            m.Description = "The delivery margin only considers the revenue and costs associated with freights, delivery and shipments.";
        }
        {
            var m = t.AddMeasure("Delivery Margin (Value)",
                @"CALCULATE (
        SUMX (
            'Invoices',
            'Invoices'[Freight] - 'Invoices'[Delivery Cost]
        ),
        'Invoice Document Type'[Billing Document Type Code] IN { ""invoice"", ""express"" }
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\III. Delivery Margin\$";
            m.Description = "The delivery margin only considers the revenue and costs associated with freights, delivery and shipments.";
        }
        {
            var m = t.AddMeasure("Delivery Margin 1YP (Value)",
                @"CALCULATE (
        [Delivery Margin (Value)],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\III. Delivery Margin\$";
            m.Description = "The delivery margin only considers the revenue and costs associated with freights, delivery and shipments.";
        }
        {
            var m = t.AddMeasure("Delivery Margin 2YP (Value)",
                @"CALCULATE (
        [Delivery Margin (Value)],
        DATEADD ('Date'[Date], -2, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\III. Delivery Margin\$";
            m.Description = "The delivery margin only considers the revenue and costs associated with freights, delivery and shipments.";
        }
        {
            var m = t.AddMeasure("Selling Margin (%)",
                @"VAR _TOTAL =
        CALCULATE (
            [Total Net Invoice Value],
            'Invoice Document Type'[Billing Document Type Code] = ""invoice""
        )
    VAR _MARGIN = [Selling Margin (Value)]
    RETURN
        DIVIDE (_MARGIN, _TOTAL)");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\II. Selling Margin\%";
            m.Description = "The selling margin is the margin earned when considering only the revenue and costs associated with the product, before delivery";
        }
        {
            var m = t.AddMeasure("Selling Margin 1YP (%)",
                @"CALCULATE (
        [Selling Margin (%)],
        DATEADD ('Date'[Date], -1, YEAR)
    ) ");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\II. Selling Margin\%";
            m.Description = "The selling margin is the margin earned when considering only the revenue and costs associated with the product, before delivery";
        }
        {
            var m = t.AddMeasure("Selling Margin 2YP (%)",
                @"CALCULATE (
        [Selling Margin (%)],
        DATEADD ('Date'[Date], -2, YEAR)
    ) ");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\II. Selling Margin\%";
            m.Description = "The selling margin is the margin earned when considering only the revenue and costs associated with the product, before delivery";
        }
        {
            var m = t.AddMeasure("Selling Margin (Value)",
                @"
    CALCULATE (
        SUMX (
            'Invoices',
            'Invoices'[Net Invoice Value] - 'Invoices'[Net Invoice COGS]
        ),
        'Invoice Document Type'[Billing Document Type Code] = ""invoice""
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\II. Selling Margin\$";
            m.Description = "The selling margin is the margin earned when considering only the revenue and costs associated with the product, before delivery";
        }
        {
            var m = t.AddMeasure("Selling Margin 1YP (Value)",
                @"CALCULATE (
        [Selling Margin (Value)],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\II. Selling Margin\$";
            m.Description = "The selling margin is the margin earned when considering only the revenue and costs associated with the product, before delivery";
        }
        {
            var m = t.AddMeasure("Selling Margin 2YP (Value)",
                @"CALCULATE (
        [Selling Margin (Value)],
        DATEADD ('Date'[Date], -2, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\II. Selling Margin\$";
            m.Description = "The selling margin is the margin earned when considering only the revenue and costs associated with the product, before delivery";
        }
        {
            var m = t.AddMeasure("Standard Margin (%)",
                @"
    DIVIDE ( [Standard Margin (Value)], [Net Sales] ) ");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\I. Std. Margin\%";
            m.Description = "System.Object[]";
        }
        {
            var m = t.AddMeasure("Standard Margin 1YP (%)",
                @"CALCULATE (
        [Standard Margin (%)],
        DATEADD ('Date'[Date], -1, YEAR)
    ) ");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\I. Std. Margin\%";
            m.Description = "System.Object[]";
        }
        {
            var m = t.AddMeasure("Standard Margin 2YP (%)",
                @"CALCULATE (
        [Standard Margin (%)],
        DATEADD ('Date'[Date], -2, YEAR)
    ) ");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = @"1. Value\d. Margins\I. Std. Margin\%";
            m.Description = "System.Object[]";
        }
        {
            var m = t.AddMeasure("Standard Margin (Value)",
                @"
    VAR _RATE = MAX ('Exchange Rate'[Rate])
    RETURN
        CALCULATE (

            -- Convert to common currency (WAT)
            SUMX (
                'Invoices',

                -- Net Sales
                VAR _NET = 
                ('Invoices'[Net Invoice Value]
                    + 'Invoices'[Freight]
                    - 'Invoices'[Taxes & Commercial Fees]
                    + 'Invoices'[Late Delivery Penalties]
                    + 'Invoices'[Overdue Payment Penalties] )

                -- COGS
                VAR _COST =
                ('Invoices'[Net Invoice COGS] 
                    + 'Invoices'[Delivery Cost] )

                RETURN

                ( _NET - _COST ) / RELATED ('Budget Rate'[Rate])
            )

                -- Convert to selected rate
                * _RATE,

            -- Limit to dates in scope
            'Date'[IsDateInScope] = TRUE
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\I. Std. Margin\$";
            m.Description = "System.Object[]";
        }
        {
            var m = t.AddMeasure("Standard Margin 1YP (Value)",
                @"CALCULATE (
        [Standard Margin (Value)],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\I. Std. Margin\$";
            m.Description = "System.Object[]";
        }
        {
            var m = t.AddMeasure("Standard Margin 2YP (Value)",
                @"CALCULATE (
        [Standard Margin (Value)],
        DATEADD ('Date'[Date], -2, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\d. Margins\I. Std. Margin\$";
            m.Description = "System.Object[]";
        }
        {
            var m = t.AddMeasure("Gross Sales",
                @"
    [Total Net Invoice Value]
        + [Total Freight Surcharge]
        + [Total Late Delivery Penalties]
        + [Total Overdue Payment Penalties]
        - [Total Taxes & Commercial Fees]");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\I. Total";
        }
        {
            var m = t.AddMeasure("Gross Sales (Selected Unit)",
                @"
    -- Sideways recursion
    CALCULATE (
        [Actuals],
        ALL ( '1) Selected Metric'[Select a Measure] ),
        '1) Selected Metric'[Select a Measure] = ""Gross Sales""
    )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\I. Total";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales",
                @"
    CALCULATE (
        [Gross Sales],
        CALCULATETABLE (
            DATESMTD ('Date'[Date]),
            'Date'[IsDateInScope]
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\I. Total";
        }
        {
            var m = t.AddMeasure("Total Net Invoice Value",
                @"SUMX (
        'Invoices',
        'Invoices'[Net Invoice Value]
            / RELATED ( 'Budget Rate'[Rate] )
    )
        * MAX ( 'Exchange Rate'[Rate] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\I. Total";
            m.Description = "This measure is theSumof column 'Invoices'[Net Invoice Value]";
        }
        {
            var m = t.AddMeasure("Gross Sales 1YP",
                @"
    CALCULATE (
        [Gross Sales],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\II. 1YP\Daily";
        }
        {
            var m = t.AddMeasure("Gross Sales vs 1YP (%)",
                @"
    VAR _TARGET = [Gross Sales 1YP]
    VAR _DELTA  = [Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\II. 1YP\Daily";
        }
        {
            var m = t.AddMeasure("Gross Sales vs 1YP (Δ)",
                @"
    [Gross Sales] - [Gross Sales 1YP]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\II. 1YP\Daily";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales 1YP",
                @"
    -- Calculates Target for the current month/year selection
    VAR _CurrentMonth =
        CALCULATE (
            [Gross Sales 1YP],
            ALL ( 'Date' ),
            VALUES (
                'Date'[Calendar Month Year (ie Jan 21)]
            )
        )

    -- Computes rate at which Target increases through the month
    VAR _CurrentMonthWDMTD =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE
            ),
            0.93
        )
    VAR _CurrentMonthWDTotal =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] )
            ),
            0.93
        )
    VAR _Period =
        SELECTEDVALUE (
            '4) Selected Period'[Period]
        )
    VAR _HasOneMonth =
        HASONEVALUE (
            'Date'[Calendar Month Year (ie Jan 21)]
        )
    RETURN
        -- return MTD value if a month is selected
        IF.EAGER (
            _HasOneMonth && _Period = ""Full Period"",
            _CurrentMonthWDTotal
                * _CurrentMonth,
            IF (
                _HasOneMonth,
                _CurrentMonthWDMTD
                    * _CurrentMonth
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\II. 1YP\MTD";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales vs 1YP (%)",
                @"
    VAR _TARGET = [MTD Gross Sales 1YP]
    VAR _DELTA  = [MTD Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\II. 1YP\MTD";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales vs 1YP (Δ)",
                @"
    [MTD Gross Sales] - [MTD Gross Sales 1YP]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\II. 1YP\MTD";
        }
        {
            var m = t.AddMeasure("Gross Sales 2YP",
                @"
    CALCULATE (
        [Gross Sales],
        DATEADD ('Date'[Date], -2, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\III. 2YP\Daily";
        }
        {
            var m = t.AddMeasure("Gross Sales vs 2YP (%)",
                @"
    VAR _TARGET = [Gross Sales 2YP]
    VAR _DELTA  = [Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\III. 2YP\Daily";
        }
        {
            var m = t.AddMeasure("Gross Sales vs 2YP (Δ)",
                @"
    [Gross Sales] - [Gross Sales 2YP]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\III. 2YP\Daily";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales 2YP",
                @"
    -- Calculates Target for the current month/year selection
    VAR _CurrentMonth =
        CALCULATE (
            [Gross Sales 2YP],
            ALL ( 'Date' ),
            VALUES (
                'Date'[Calendar Month Year (ie Jan 21)]
            )
        )

    -- Computes rate at which Target increases through the month
    VAR _CurrentMonthWDMTD =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE
            ),
            0.93
        )
    VAR _CurrentMonthWDTotal =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] )
            ),
            0.93
        )
    VAR _Period =
        SELECTEDVALUE (
            '4) Selected Period'[Period]
        )
    VAR _HasOneMonth =
        HASONEVALUE (
            'Date'[Calendar Month Year (ie Jan 21)]
        )
    RETURN
        -- return MTD value if a month is selected
        IF.EAGER (
            _HasOneMonth && _Period = ""Full Period"",
            _CurrentMonthWDTotal
                * _CurrentMonth,
            IF (
                _HasOneMonth,
                _CurrentMonthWDMTD
                    * _CurrentMonth
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\III. 2YP\MTD";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales vs 2YP (%)",
                @"
    VAR _TARGET = [MTD Gross Sales 1YP]
    VAR _DELTA  = [MTD Gross Sales] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\III. 2YP\MTD";
        }
        {
            var m = t.AddMeasure("MTD Gross Sales vs. 2YP (Δ)",
                @"
    [MTD Gross Sales] - [MTD Gross Sales 1YP]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"1. Value\a. Gross Sales Actuals\III. 2YP\MTD";
        }
        {
            var m = t.AddMeasure("Net Sales",
                @"
    VAR _RATE = MAX ('Exchange Rate'[Rate])
    RETURN
        CALCULATE (

            -- Convert to common currency (WAT)
            SUMX (
                'Invoices',

                -- Net sales
                ('Invoices'[Net Invoice Value]
                    + 'Invoices'[Freight]
                    - 'Invoices'[Taxes & Commercial Fees]
                    + 'Invoices'[Late Delivery Penalties]
                    + 'Invoices'[Overdue Payment Penalties] )

                    / RELATED ('Budget Rate'[Rate])
            )

                -- Convert to selected currency
                * _RATE,

            -- Limit to dates in scope
            'Date'[IsDateInScope] = TRUE
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\c. Net Sales";
        }
        {
            var m = t.AddMeasure("Net Sales 1YP",
                @"
    CALCULATE (
        [Net Sales],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\c. Net Sales";
        }
        {
            var m = t.AddMeasure("Net Sales 2YP",
                @"
    CALCULATE (
        [Net Sales],
        DATEADD ('Date'[Date], -2, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\c. Net Sales";
        }
        {
            var m = t.AddMeasure("Total Cost",
                @"
    VAR _RATE = MAX ('Exchange Rate'[Rate])
    RETURN
        CALCULATE (

            -- Convert to common currency (WAT)
            SUMX (
                'Invoices',

                -- COGS
                ('Invoices'[Net Invoice COGS] + 'Invoices'[Delivery Cost])
                    / RELATED ('Budget Rate'[Rate])
            )

                -- Convert to selected currency
                * _RATE,

            -- Limit to dates in scope
            'Date'[IsDateInScope] = TRUE
        )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\I. Cost";
            m.Description = "The cost of the product sold and the shipment / delivery";
        }
        {
            var m = t.AddMeasure("Total Delivery Cost",
                @"SUMX (
        'Invoices',
        'Invoices'[Delivery Cost]
            / RELATED ( 'Budget Rate'[Rate] )
    )
        * MAX ( 'Exchange Rate'[Rate] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\I. Cost";
            m.Description = "The cost of the delivery only";
        }
        {
            var m = t.AddMeasure("Total Net Invoice COGS",
                @"SUMX (
        'Invoices',
        'Invoices'[Net Invoice COGS]
            / RELATED ( 'Budget Rate'[Rate] )
    )
        * MAX ( 'Exchange Rate'[Rate] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\I. Cost";
            m.Description = "All costs associated with the sold product";
        }
        {
            var m = t.AddMeasure("Total Fees & Penalties",
                @"
    VAR _RATE = MAX ('Exchange Rate'[Rate])
    RETURN
        CALCULATE (

            -- Convert to common currency (WAT)
            SUMX (
                'Invoices',

                -- Total fees & penalties
                ('Invoices'[Late Delivery Penalties]
                    + 'Invoices'[Overdue Payment Penalties]
                    - 'Invoices'[Taxes & Commercial Fees])

                    / RELATED ('Budget Rate'[Rate])
            )

                -- Convert to selected currency
                * _RATE,

            -- Limit to dates in scope
            'Date'[IsDateInScope] = TRUE
        )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\II. Penalties and Adjustments";
        }
        {
            var m = t.AddMeasure("Total Late Delivery Penalties",
                @"SUMX (
        'Invoices',
        'Invoices'[Late Delivery Penalties]
            / RELATED ( 'Budget Rate'[Rate] )
    )
        * MAX ( 'Exchange Rate'[Rate] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\II. Penalties and Adjustments";
            m.Description = "Late Delivery penalties occur when we fail to meet the SLA for deliveries. If the products take longer than a certain number of days to deliver after the requested delivery date, a Late Delivery Penalty is levied.";
        }
        {
            var m = t.AddMeasure("Total Overdue Payment Penalties",
                @"SUMX (
        'Invoices',
        'Invoices'[Overdue Payment Penalties]
            / RELATED ( 'Budget Rate'[Rate] )
    )
        * MAX ( 'Exchange Rate'[Rate] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\II. Penalties and Adjustments";
            m.Description = "Overdue Payments result when the customer hasn't paid their invoice within a certain number of days since the product was shipped.";
        }
        {
            var m = t.AddMeasure("Total Taxes & Commercial Fees",
                @"SUMX (
        'Invoices',
        'Invoices'[Taxes & Commercial Fees]
            / RELATED ( 'Budget Rate'[Rate] )
    )
        * MAX ( 'Exchange Rate'[Rate] )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\II. Penalties and Adjustments";
            m.Description = "This measure is theSumof column 'Invoices'[Taxes & Commercial Fees]";
        }
        {
            var m = t.AddMeasure("Total Freight Surcharge",
                @"
    VAR _RATE = MAX ('Exchange Rate'[Rate])
    RETURN
        CALCULATE (

            -- Convert to common currency (WAT)
            SUMX (
                'Invoices',
                'Invoices'[Freight]
                    / RELATED ('Budget Rate'[Rate])
            )

                -- Convert to selected currency
                * _RATE,

            -- Limit to dates in scope
            'Date'[IsDateInScope] = TRUE
        )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Value\b. Cost & Penalties\III. Freight";
            m.Description = "The freight surcharge is what we charge for delivery. It is one of our lines of revenue.";
        }
        {
            var m = t.AddMeasure("Invoice Lines",
                @"
    COUNTROWS ( 'Invoices' )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "3. Lines";
        }
        {
            var m = t.AddMeasure("Gross Sales (Quantity)",
                @"
    [Total Net Invoice Quantity]");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"2. Quantity\b. Gross Sales Actuals";
        }
        {
            var m = t.AddMeasure("Gross Sales 1YP (Quantity)",
                @"
    CALCULATE (
        [Gross Sales (Quantity)],
        DATEADD ('Date'[Date], -1, YEAR)
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"2. Quantity\b. Gross Sales Actuals";
        }
        {
            var m = t.AddMeasure("Total Net Invoice Quantity",
                @"SUM ( 'Invoices'[Net Invoice Quantity] )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"2. Quantity\b. Gross Sales Actuals";
            m.Description = "This measure is theSumof column 'Invoices'[Net Invoice Quantity]";
        }
    }

    // Remove dummy created data source as this is not needed for Power BI using a structured data source (PowerQuery)
    if (isPowerBI && Model.DataSources.Count > 0) Model.DataSources[0].Delete();
    
    // ============================================================
    // 4. CALCULATION GROUPS
    // ============================================================

    // --- Z01CG1 - Quantity (precedence 0) ---
    {
        var cg = Model.AddCalculationGroup("Z01CG1 - Quantity");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 0;
        cg.Columns[0].Name = "Measure";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("Gross Sales",
                @"
    [Total Net Invoice Quantity]");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("Orders",
                @"
    [Net Orders (Quantity)]");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("Invoices",
                @"
    [Total Net Invoice Quantity]");
            item.Ordinal = 2;
        }
        {
            var item = cg.AddCalculationItem("Invoice Cost",
                @"
    BLANK()");
            item.Ordinal = 3;
            item.Description = "The Net Invoice Cost is";
        }
        {
            var item = cg.AddCalculationItem("Delivery Cost",
                @"
    BLANK()");
            item.Ordinal = 4;
            item.Description = "The cost of the delivery only";
        }
        {
            var item = cg.AddCalculationItem("Delivery Surcharge",
                @"
    BLANK()");
            item.Ordinal = 5;
        }
        {
            var item = cg.AddCalculationItem("Taxes",
                @"
    BLANK()");
            item.Ordinal = 6;
        }
        {
            var item = cg.AddCalculationItem("Overdue Payment Penalties",
                @"
    BLANK()");
            item.Ordinal = 7;
            item.Description = "Overdue Payments result when the customer hasn't paid their invoice within a certain number of days since the product was shipped.";
        }
        {
            var item = cg.AddCalculationItem("Late Delivery Penalties",
                @"
    BLANK()");
            item.Ordinal = 8;
            item.Description = "Late Delivery penalties occur when we fail to meet the SLA for deliveries. If the products take longer than a certain number of days to deliver after the requested delivery date, a Late Delivery Penalty is levied.";
        }
    }

    // --- Z01CG2 - Value (Budget Rate) (precedence 1) ---
    {
        var cg = Model.AddCalculationGroup("Z01CG2 - Value (Budget Rate)");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 1;
        cg.Columns[0].Name = "Measure";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("Gross Sales",
                @"
    [Gross Sales]");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("Orders",
                @"
    [Net Orders]");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("Invoices",
                @"
    [Total Net Invoice Value]");
            item.Ordinal = 2;
        }
        {
            var item = cg.AddCalculationItem("Invoice Cost",
                @"
    [Total Net Invoice COGS]");
            item.Ordinal = 3;
            item.Description = "The Net Invoice Cost is";
        }
        {
            var item = cg.AddCalculationItem("Delivery Cost",
                @"
    [Total Delivery Cost]");
            item.Ordinal = 4;
            item.Description = "The cost of the delivery only";
        }
        {
            var item = cg.AddCalculationItem("Delivery Surcharge",
                @"
    [Total Freight Surcharge]");
            item.Ordinal = 5;
        }
        {
            var item = cg.AddCalculationItem("Taxes",
                @"
    [Total Taxes & Commercial Fees]");
            item.Ordinal = 6;
        }
        {
            var item = cg.AddCalculationItem("Overdue Payment Penalties",
                @"
    [Total Overdue Payment Penalties]");
            item.Ordinal = 7;
            item.Description = "Overdue Payments result when the customer hasn't paid their invoice within a certain number of days since the product was shipped.";
        }
        {
            var item = cg.AddCalculationItem("Late Delivery Penalties",
                @"
    [Total Late Delivery Penalties]");
            item.Ordinal = 8;
            item.Description = "Late Delivery penalties occur when we fail to meet the SLA for deliveries. If the products take longer than a certain number of days to deliver after the requested delivery date, a Late Delivery Penalty is levied.";
        }
    }

    // --- Z01CG4 - Lines (precedence 3) ---
    {
        var cg = Model.AddCalculationGroup("Z01CG4 - Lines");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 3;
        cg.Columns[0].Name = "Measure";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("Gross Sales",
                @"
    [Invoice Lines]");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("Net Sales",
                @"
    BLANK()");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("Orders",
                @"
    [Net Orders]");
            item.Ordinal = 2;
        }
        {
            var item = cg.AddCalculationItem("Invoices",
                @"
    [Lines]");
            item.Ordinal = 3;
        }
        {
            var item = cg.AddCalculationItem("Invoice Cost",
                @"
    BLANK()");
            item.Ordinal = 4;
            item.Description = "The Net Invoice Cost is";
        }
        {
            var item = cg.AddCalculationItem("Delivery Cost",
                @"
    BLANK()");
            item.Ordinal = 5;
            item.Description = "The cost of the delivery only";
        }
        {
            var item = cg.AddCalculationItem("Delivery Surcharge",
                @"
    BLANK()");
            item.Ordinal = 6;
        }
        {
            var item = cg.AddCalculationItem("Taxes",
                @"
    BLANK()");
            item.Ordinal = 7;
        }
        {
            var item = cg.AddCalculationItem("Overdue Payment Penalties",
                @"
    BLANK()");
            item.Ordinal = 8;
            item.Description = "Overdue Payments result when the customer hasn't paid their invoice within a certain number of days since the product was shipped.";
        }
        {
            var item = cg.AddCalculationItem("Late Delivery Penalties",
                @"
    BLANK()");
            item.Ordinal = 9;
            item.Description = "Late Delivery penalties occur when we fail to meet the SLA for deliveries. If the products take longer than a certain number of days to deliver after the requested delivery date, a Late Delivery Penalty is levied.";
        }
    }

    // --- Z02CG1 - Unit (precedence 2) ---
    {
        var cg = Model.AddCalculationGroup("Z02CG1 - Unit");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 2;
        cg.Columns[0].Name = "Unit";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("Value (Budget Rate)",
                @"
    [Value]");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("Quantity",
                @"
    [Quantity]");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("Lines",
                @"
    [Lines]");
            item.Ordinal = 2;
        }
    }

    // --- Z03CG1 - Sales Target (precedence 6) ---
    {
        var cg = Model.AddCalculationGroup("Z03CG1 - Sales Target");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 6;
        cg.Columns[0].Name = "Target";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("Budget",
                @"
    [Total Budget]");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("Forecast",
                @"
    [Forecast]");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("1 Year Prior",
                @"
    [Gross Sales 1YP (Selected Unit)]");
            item.Ordinal = 2;
        }
        {
            var item = cg.AddCalculationItem("2 Years Prior",
                @"
    [Gross Sales 2YP (Selected Unit)]");
            item.Ordinal = 3;
        }
    }

    // --- Z03CG2 - Orders Target (precedence 7) ---
    {
        var cg = Model.AddCalculationGroup("Z03CG2 - Orders Target");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 7;
        cg.Columns[0].Name = "Target";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("1 Year Prior",
                @"
    [Net Orders 1YP]");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("2 Years Prior",
                @"
    [Net Orders 2YP]");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("Budget",
                @"
    BLANK()");
            item.Ordinal = 2;
        }
        {
            var item = cg.AddCalculationItem("Forecast",
                @"
    BLANK()");
            item.Ordinal = 3;
        }
    }

    // --- Z04CG1 - Time Intelligence (precedence 4) ---
    {
        var cg = Model.AddCalculationGroup("Z04CG1 - Time Intelligence");
        cg.IsHidden = true;
        cg.CalculationGroup.Precedence = 4;
        cg.Columns[0].Name = "Aggregation";
        cg.Columns[0].IsHidden = true;
        cg.Columns[0].SortByColumn = cg.Columns["Ordinal"];
        if (cg.Columns.Count > 1) cg.Columns[1].IsHidden = true;
        {
            var item = cg.AddCalculationItem("Daily",
                @"
    SELECTEDMEASURE()");
            item.Ordinal = 0;
        }
        {
            var item = cg.AddCalculationItem("MTD",
                @"
    CALCULATE (
        SELECTEDMEASURE (), 
        CALCULATETABLE (
            DATESMTD ('Date'[Date]),
            'Date'[IsDateInScope]
        )
    )");
            item.Ordinal = 1;
        }
        {
            var item = cg.AddCalculationItem("QTD",
                @"
    CALCULATE (
        SELECTEDMEASURE (), 
        CALCULATETABLE (
            DATESQTD ('Date'[Date]),
            'Date'[IsDateInScope]
        )
    )");
            item.Ordinal = 2;
        }
        {
            var item = cg.AddCalculationItem("YTD",
                @"
    VAR _YTD =
        CALCULATE (
            SELECTEDMEASURE (),
            CALCULATETABLE (
                DATESYTD ('Date'[Date]),
                'Date'[IsDateInScope]
            )
        )
    VAR _FULL_YTD =
        CALCULATE (SELECTEDMEASURE (), DATESYTD ('Date'[Date]))
    RETURN
        IF (
            SEARCH (""Actuals"", SELECTEDMEASURENAME (), , 999) <> 999,
            _YTD,
            _FULL_YTD
        )");
            item.Ordinal = 3;
        }
    }
    
    // ============================================================
    // 5. CALCULATED TABLES
    // ============================================================

    // --- Date ---
    {
        var t = Model.AddCalculatedTable("Date",
            @"-- Reference date for the latest date in the report
    -- Until when the business wants to see data in reports
    VAR _Refdate_Measure = [RefDate]
    VAR _Today = TODAY ( )

    -- Replace with ""Today"" if [RefDate] evaluates blank
    VAR _Refdate = IF ( ISBLANK ( _Refdate_Measure ), _Today, _Refdate_Measure )
        VAR _RefYear        = YEAR ( _Refdate )
        VAR _RefQuarter     = _RefYear * 100 + QUARTER(_Refdate)
        VAR _RefMonth       = _RefYear * 100 + MONTH(_Refdate)
        VAR _RefWeek_EU     = _RefYear * 100 + WEEKNUM(_Refdate, 2)

    -- Fiscal calendar settings (July start)
    VAR _FiscalYearStartMonth = 7
     
    -- Earliest date in the model scope
    VAR _EarliestDate       = DATE ( YEAR ( MIN ( 'Orders'[Order Date] ) ) - 2, 1, 1 )
    VAR _EarliestDate_Safe  = MIN ( _EarliestDate, DATE ( YEAR ( _Today ) + 1, 1, 1 ) )

    -- Latest date in the model scope
    VAR _LatestDate_Safe    = DATE ( YEAR ( _Refdate ) + 2, 12, 1 )

    ------------------------------------------
    -- Base calendar table
    VAR _Base_Calendar      = CALENDAR ( _EarliestDate_Safe, _LatestDate_Safe )
    ------------------------------------------



    ------------------------------------------
    VAR _IntermediateResult = 
        ADDCOLUMNS ( _Base_Calendar,

                ------------------------------------------
            ""Calendar Year Number (ie 2021)"",           --|
                YEAR ([Date]),                          --|-- Year
                                                        --|
            ""Calendar Year (ie 2021)"",                  --|
                FORMAT ([Date], ""YYYY""),                --|
                ------------------------------------------

                ------------------------------------------
            ""Calendar Quarter Year (ie Q1 2021)"",       --|
                ""Q"" &                                   --|-- Quarter
                CONVERT(QUARTER([Date]), STRING) &      --|
                "" "" &                                   --|
                CONVERT(YEAR([Date]), STRING),          --|
                                                        --|
            ""Calendar Year Quarter (ie 202101)"",        --|
                YEAR([Date]) * 100 + QUARTER([Date]),   --|
                                                        --| 
            ""Calendar Quarter (ie Q1)"",                 --|
                ""Q"" &                                   --|
                CONVERT(QUARTER([Date]), STRING),       --|  
                ------------------------------------------

                ------------------------------------------
            ""Calendar Month Year (ie Jan 21)"",          --|
                FORMAT ( [Date], ""MMM YY"" ),            --|-- Month
                                                        --|
            ""Calendar Year Month (ie 202101)"",          --|
                YEAR([Date]) * 100 + MONTH([Date]),     --|
                                                        --|
            ""Calendar Month (ie Jan)"",                  --|
                FORMAT ( [Date], ""MMM"" ),               --|
                                                        --|
            ""Calendar Month # (ie 1)"",                  --|
                MONTH ( [Date] ),                       --|
                ------------------------------------------
                
                ------------------------------------------
            ""Calendar Week EU (ie WK25)"",               --|
                ""WK"" & WEEKNUM( [Date], 2 ),            --|-- Week
                                                        --|
            ""Calendar Week Number EU (ie 25)"",          --|
                WEEKNUM( [Date], 2 ),                   --|
                                                        --|
            ""Calendar Year Week Number EU (ie 202125)"", --|
                YEAR ( [Date] ) * 100                   --|
                +                                       --|
                WEEKNUM( [Date], 2 ),                   --|
                                                        --|
            ""Calendar Week US (ie WK25)"",               --|
                ""WK"" & WEEKNUM( [Date], 1 ),            --|
                                                        --|
            ""Calendar Week Number US (ie 25)"",          --|
                WEEKNUM( [Date], 1 ),                   --|
                                                        --|
            ""Calendar Year Week Number US (ie 202125)"", --|
                YEAR ( [Date] ) * 100                   --|
                +                                       --|
                WEEKNUM( [Date], 1 ),                   --|
                                                        --|
            ""Calendar Week ISO (ie WK25)"",              --|
                ""WK"" & WEEKNUM( [Date], 21 ),           --|
                                                        --|
            ""Calendar Week Number ISO (ie 25)"",         --|
                WEEKNUM( [Date], 21 ),                  --|
                                                        --|
            ""Calendar Year Week Number ISO (ie 202125)"",--|
                YEAR ( [Date] ) * 100                   --|
                +                                       --|
                WEEKNUM( [Date], 21 ),                  --|
                ------------------------------------------

                ------------------------------------------
            ""Weekday Short (i.e. Mon)"",                 --|
                FORMAT ( [Date], ""DDD"" ),               --|-- Weekday
                                                        --|
            ""Weekday Name (i.e. Monday)"",               --|
                FORMAT ( [Date], ""DDDD"" ),              --|
                                                        --|
            ""Weekday Number EU (i.e. 1)"",               --|
                WEEKDAY ( [Date], 2 ),                  --|
                ------------------------------------------
                
                ------------------------------------------
            ""Calendar Month Day (i.e. Jan 05)"",         --|
                FORMAT ( [Date], ""MMM DD"" ),            --|-- Day
                                                        --|
            ""Calendar Month Day (i.e. 0105)"",           --|
                MONTH([Date]) * 100                     --|
                +                                       --|
                DAY([Date]),                            --|
                                                        --|
            ""YYYYMMDD"",                                 --|
                YEAR ( [Date] ) * 10000                 --|
                +                                       --|
                MONTH ( [Date] ) * 100                  --|
                +                                       --|
                DAY ( [Date] ),                         --|
                ------------------------------------------

                ------------------------------------------
            ""Fiscal Year Number (ie 2025)"",             --|
                IF (                                    --|-- Fiscal Year
                    MONTH([Date]) >= _FiscalYearStartMonth,
                    YEAR([Date]) + 1,                   --|
                    YEAR([Date])                        --|
                ),                                      --|
                                                        --|
            ""Fiscal Year (ie FY25)"",                    --|
                ""FY"" &                                  --|
                RIGHT(                                  --|
                    IF (                                --|
                        MONTH([Date]) >= _FiscalYearStartMonth,
                        YEAR([Date]) + 1,               --|
                        YEAR([Date])                    --|
                    ),                                  --|
                    2                                   --|
                ),                                      --|
                ------------------------------------------

                ------------------------------------------
            ""Fiscal Quarter (ie FQ1)"",                  --|
                ""FQ"" &                                  --|-- Fiscal Quarter
                SWITCH (                                --|
                    TRUE(),                             --|
                    MONTH([Date]) IN {7, 8, 9}, 1,      --|
                    MONTH([Date]) IN {10, 11, 12}, 2,   --|
                    MONTH([Date]) IN {1, 2, 3}, 3,      --|
                    4                                   --|
                ),                                      --|
                                                        --|
            ""Fiscal Quarter Number (ie 1)"",             --|
                SWITCH (                                --|
                    TRUE(),                             --|
                    MONTH([Date]) IN {7, 8, 9}, 1,      --|
                    MONTH([Date]) IN {10, 11, 12}, 2,   --|
                    MONTH([Date]) IN {1, 2, 3}, 3,      --|
                    4                                   --|
                ),                                      --|
                                                        --|
            ""Fiscal Year Quarter (ie FY25 Q1)"",         --|
                ""FY"" &                                  --|
                RIGHT(                                  --|
                    IF (                                --|
                        MONTH([Date]) >= _FiscalYearStartMonth,
                        YEAR([Date]) + 1,               --|
                        YEAR([Date])                    --|
                    ),                                  --|
                    2                                   --|
                ) &                                     --|
                "" Q"" &                                  --|
                SWITCH (                                --|
                    TRUE(),                             --|
                    MONTH([Date]) IN {7, 8, 9}, 1,      --|
                    MONTH([Date]) IN {10, 11, 12}, 2,   --|
                    MONTH([Date]) IN {1, 2, 3}, 3,      --|
                    4                                   --|
                ),                                      --|
                ------------------------------------------

                ------------------------------------------
            ""Fiscal Month Number (ie 1)"",               --|
                IF (                                    --|-- Fiscal Month
                    MONTH([Date]) >= _FiscalYearStartMonth,
                    MONTH([Date]) - _FiscalYearStartMonth + 1,
                    MONTH([Date]) + 12 - _FiscalYearStartMonth + 1
                ),                                      --|
                                                        --|
            ""Fiscal Month (ie FM01)"",                   --|
                ""FM"" &                                  --|
                FORMAT (                                --|
                    IF (                                --|
                        MONTH([Date]) >= _FiscalYearStartMonth,
                        MONTH([Date]) - _FiscalYearStartMonth + 1,
                        MONTH([Date]) + 12 - _FiscalYearStartMonth + 1
                    ),                                  --|
                    ""00""                                --|
                ),                                      --|
                ------------------------------------------


                ------------------------------------------
            ""IsDateInScope"",                            --|
                [Date] <= _Refdate                      --|-- Boolean
                &&                                      --|
                YEAR([Date]) > YEAR(_EarliestDate),     --|
                                                        --|
            ""IsBeforeThisMonth"",                        --|
                [Date] <= EOMONTH ( _Refdate, -1 ),     --|
                                                        --|
            ""IsLastMonth"",                              --|
                [Date] <= EOMONTH ( _Refdate, 0 )       --|
                &&                                      --|
                [Date] > EOMONTH ( _Refdate, -1 ),      --|
                                                        --|
            ""IsYTD"",                                    --|
                MONTH([Date])                           --|
                <=                                      --|
                MONTH(EOMONTH ( _Refdate, 0 )),         --|
                                                        --|
            ""IsActualToday"",                            --|
                [Date] = _Today,                        --|
                                                        --|
            ""IsRefDate"",                                --|
                [Date] = _Refdate,                      --|
                                                        --|
            ""IsHoliday"",                                --|
                MONTH([Date]) * 100                     --|
                +                                       --|
                DAY([Date])                             --|
                    IN {0101, 0501, 1111, 1225},        --|
                                                        --|
            ""IsWeekday"",                                --|
                WEEKDAY([Date], 2)                      --|
                    IN {1, 2, 3, 4, 5})                 --|
                ------------------------------------------

    VAR _Result = 
        
                --------------------------------------------
        ADDCOLUMNS (                                      --|
            _IntermediateResult,                          --|-- Boolean #2
            ""IsThisYear"",                                 --|
                [Calendar Year Number (ie 2021)]          --|
                    = _RefYear,                           --|
                                                          --|
            ""IsThisMonth"",                                --|
                [Calendar Year Month (ie 202101)]         --|
                    = _RefMonth,                          --|
                                                          --|
            ""IsThisQuarter"",                              --|
                [Calendar Year Quarter (ie 202101)]       --|
                    = _RefQuarter,                        --|
                                                          --|
            ""IsThisWeek"",                                 --|
                [Calendar Year Week Number EU (ie 202125)]--|
                    = _RefWeek_EU                         --|
        )                                                 --|
                --------------------------------------------
                
    RETURN 
        _Result");
        if (!t.Columns.Contains("Date")) t.AddCalculatedTableColumn("Date", "[Date]");
        if (!t.Columns.Contains("Calendar Year Number (ie 2021)")) t.AddCalculatedTableColumn("Calendar Year Number (ie 2021)", "[Calendar Year Number (ie 2021)]");
        if (!t.Columns.Contains("Calendar Year (ie 2021)")) t.AddCalculatedTableColumn("Calendar Year (ie 2021)", "[Calendar Year (ie 2021)]");
        if (!t.Columns.Contains("Calendar Quarter Year (ie Q1 2021)")) t.AddCalculatedTableColumn("Calendar Quarter Year (ie Q1 2021)", "[Calendar Quarter Year (ie Q1 2021)]");
        if (!t.Columns.Contains("Calendar Year Quarter (ie 202101)")) t.AddCalculatedTableColumn("Calendar Year Quarter (ie 202101)", "[Calendar Year Quarter (ie 202101)]");
        if (!t.Columns.Contains("Calendar Month Year (ie Jan 21)")) t.AddCalculatedTableColumn("Calendar Month Year (ie Jan 21)", "[Calendar Month Year (ie Jan 21)]");
        if (!t.Columns.Contains("Calendar Year Month (ie 202101)")) t.AddCalculatedTableColumn("Calendar Year Month (ie 202101)", "[Calendar Year Month (ie 202101)]");
        if (!t.Columns.Contains("Calendar Month (ie Jan)")) t.AddCalculatedTableColumn("Calendar Month (ie Jan)", "[Calendar Month (ie Jan)]");
        if (!t.Columns.Contains("Calendar Month # (ie 1)")) t.AddCalculatedTableColumn("Calendar Month # (ie 1)", "[Calendar Month # (ie 1)]");
        if (!t.Columns.Contains("Calendar Week EU (ie WK25)")) t.AddCalculatedTableColumn("Calendar Week EU (ie WK25)", "[Calendar Week EU (ie WK25)]");
        if (!t.Columns.Contains("Calendar Week Number EU (ie 25)")) t.AddCalculatedTableColumn("Calendar Week Number EU (ie 25)", "[Calendar Week Number EU (ie 25)]");
        if (!t.Columns.Contains("Calendar Year Week Number EU (ie 202125)")) t.AddCalculatedTableColumn("Calendar Year Week Number EU (ie 202125)", "[Calendar Year Week Number EU (ie 202125)]");
        if (!t.Columns.Contains("Calendar Week US (ie WK25)")) t.AddCalculatedTableColumn("Calendar Week US (ie WK25)", "[Calendar Week US (ie WK25)]");
        if (!t.Columns.Contains("Calendar Week Number US (ie 25)")) t.AddCalculatedTableColumn("Calendar Week Number US (ie 25)", "[Calendar Week Number US (ie 25)]");
        if (!t.Columns.Contains("Calendar Year Week Number US (ie 202125)")) t.AddCalculatedTableColumn("Calendar Year Week Number US (ie 202125)", "[Calendar Year Week Number US (ie 202125)]");
        if (!t.Columns.Contains("Calendar Week ISO (ie WK25)")) t.AddCalculatedTableColumn("Calendar Week ISO (ie WK25)", "[Calendar Week ISO (ie WK25)]");
        if (!t.Columns.Contains("Calendar Week Number ISO (ie 25)")) t.AddCalculatedTableColumn("Calendar Week Number ISO (ie 25)", "[Calendar Week Number ISO (ie 25)]");
        if (!t.Columns.Contains("Calendar Year Week Number ISO (ie 202125)")) t.AddCalculatedTableColumn("Calendar Year Week Number ISO (ie 202125)", "[Calendar Year Week Number ISO (ie 202125)]");
        if (!t.Columns.Contains("Weekday Short (i.e. Mon)")) t.AddCalculatedTableColumn("Weekday Short (i.e. Mon)", "[Weekday Short (i.e. Mon)]");
        if (!t.Columns.Contains("Weekday Name (i.e. Monday)")) t.AddCalculatedTableColumn("Weekday Name (i.e. Monday)", "[Weekday Name (i.e. Monday)]");
        if (!t.Columns.Contains("Weekday Number EU (i.e. 1)")) t.AddCalculatedTableColumn("Weekday Number EU (i.e. 1)", "[Weekday Number EU (i.e. 1)]");
        if (!t.Columns.Contains("Calendar Month Day (i.e. Jan 05)")) t.AddCalculatedTableColumn("Calendar Month Day (i.e. Jan 05)", "[Calendar Month Day (i.e. Jan 05)]");
        if (!t.Columns.Contains("Calendar Month Day (i.e. 0105)")) t.AddCalculatedTableColumn("Calendar Month Day (i.e. 0105)", "[Calendar Month Day (i.e. 0105)]");
        if (!t.Columns.Contains("YYYYMMDD")) t.AddCalculatedTableColumn("YYYYMMDD", "[YYYYMMDD]");
        if (!t.Columns.Contains("IsDateInScope")) t.AddCalculatedTableColumn("IsDateInScope", "[IsDateInScope]");
        if (!t.Columns.Contains("IsBeforeThisMonth")) t.AddCalculatedTableColumn("IsBeforeThisMonth", "[IsBeforeThisMonth]");
        if (!t.Columns.Contains("IsLastMonth")) t.AddCalculatedTableColumn("IsLastMonth", "[IsLastMonth]");
        if (!t.Columns.Contains("IsYTD")) t.AddCalculatedTableColumn("IsYTD", "[IsYTD]");
        if (!t.Columns.Contains("IsActualToday")) t.AddCalculatedTableColumn("IsActualToday", "[IsActualToday]");
        if (!t.Columns.Contains("IsRefDate")) t.AddCalculatedTableColumn("IsRefDate", "[IsRefDate]");
        if (!t.Columns.Contains("IsHoliday")) t.AddCalculatedTableColumn("IsHoliday", "[IsHoliday]");
        if (!t.Columns.Contains("IsWeekday")) t.AddCalculatedTableColumn("IsWeekday", "[IsWeekday]");
        if (!t.Columns.Contains("IsThisYear")) t.AddCalculatedTableColumn("IsThisYear", "[IsThisYear]");
        if (!t.Columns.Contains("IsThisMonth")) t.AddCalculatedTableColumn("IsThisMonth", "[IsThisMonth]");
        if (!t.Columns.Contains("IsThisQuarter")) t.AddCalculatedTableColumn("IsThisQuarter", "[IsThisQuarter]");
        if (!t.Columns.Contains("IsThisWeek")) t.AddCalculatedTableColumn("IsThisWeek", "[IsThisWeek]");
        if (!t.Columns.Contains("Calendar Quarter (ie Q1)")) t.AddCalculatedTableColumn("Calendar Quarter (ie Q1)", "[Calendar Quarter (ie Q1)]");
        if (!t.Columns.Contains("Fiscal Year Number (ie 2025)")) t.AddCalculatedTableColumn("Fiscal Year Number (ie 2025)", "[Fiscal Year Number (ie 2025)]");
        if (!t.Columns.Contains("Fiscal Year (ie FY25)")) t.AddCalculatedTableColumn("Fiscal Year (ie FY25)", "[Fiscal Year (ie FY25)]");
        if (!t.Columns.Contains("Fiscal Quarter (ie FQ1)")) t.AddCalculatedTableColumn("Fiscal Quarter (ie FQ1)", "[Fiscal Quarter (ie FQ1)]");
        if (!t.Columns.Contains("Fiscal Quarter Number (ie 1)")) t.AddCalculatedTableColumn("Fiscal Quarter Number (ie 1)", "[Fiscal Quarter Number (ie 1)]");
        if (!t.Columns.Contains("Fiscal Year Quarter (ie FY25 Q1)")) t.AddCalculatedTableColumn("Fiscal Year Quarter (ie FY25 Q1)", "[Fiscal Year Quarter (ie FY25 Q1)]");
        if (!t.Columns.Contains("Fiscal Month Number (ie 1)")) t.AddCalculatedTableColumn("Fiscal Month Number (ie 1)", "[Fiscal Month Number (ie 1)]");
        if (!t.Columns.Contains("Fiscal Month (ie FM01)")) t.AddCalculatedTableColumn("Fiscal Month (ie FM01)", "[Fiscal Month (ie FM01)]");
        
        t.Columns["Date"].IsDataTypeInferred = true;
        t.Columns["Date"].DisplayFolder = "6. Calendar Date";
        t.Columns["Date"].IsKey = true;
        t.Columns["Date"].SummarizeBy = AggregateFunction.None;
        t.Columns["Date"].DataType = DataType.DateTime;
        t.Columns["Calendar Year Number (ie 2021)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Number (ie 2021)"].DataType = DataType.Int64;
        t.Columns["Calendar Year (ie 2021)"].IsDataTypeInferred = true;
        t.Columns["Calendar Quarter Year (ie Q1 2021)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Quarter (ie 202101)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Quarter (ie 202101)"].DataType = DataType.Int64;
        t.Columns["Calendar Month Year (ie Jan 21)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Month (ie 202101)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Month (ie 202101)"].DataType = DataType.Int64;
        t.Columns["Calendar Month (ie Jan)"].IsDataTypeInferred = true;
        t.Columns["Calendar Month # (ie 1)"].IsDataTypeInferred = true;
        t.Columns["Calendar Month # (ie 1)"].DataType = DataType.Int64;
        t.Columns["Calendar Week EU (ie WK25)"].IsDataTypeInferred = true;
        t.Columns["Calendar Week Number EU (ie 25)"].IsDataTypeInferred = true;
        t.Columns["Calendar Week Number EU (ie 25)"].DataType = DataType.Int64;
        t.Columns["Calendar Year Week Number EU (ie 202125)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Week Number EU (ie 202125)"].DataType = DataType.Int64;
        t.Columns["Calendar Week US (ie WK25)"].IsDataTypeInferred = true;
        t.Columns["Calendar Week Number US (ie 25)"].IsDataTypeInferred = true;
        t.Columns["Calendar Week Number US (ie 25)"].DataType = DataType.Int64;
        t.Columns["Calendar Year Week Number US (ie 202125)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Week Number US (ie 202125)"].DataType = DataType.Int64;
        t.Columns["Calendar Week ISO (ie WK25)"].IsDataTypeInferred = true;
        t.Columns["Calendar Week Number ISO (ie 25)"].IsDataTypeInferred = true;
        t.Columns["Calendar Week Number ISO (ie 25)"].DataType = DataType.Int64;
        t.Columns["Calendar Year Week Number ISO (ie 202125)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Week Number ISO (ie 202125)"].DataType = DataType.Int64;
        t.Columns["Weekday Short (i.e. Mon)"].IsDataTypeInferred = true;
        t.Columns["Weekday Name (i.e. Monday)"].IsDataTypeInferred = true;
        t.Columns["Weekday Number EU (i.e. 1)"].IsDataTypeInferred = true;
        t.Columns["Weekday Number EU (i.e. 1)"].DataType = DataType.Int64;
        t.Columns["Calendar Month Day (i.e. Jan 05)"].IsDataTypeInferred = true;
        t.Columns["Calendar Month Day (i.e. 0105)"].IsDataTypeInferred = true;
        t.Columns["YYYYMMDD"].IsDataTypeInferred = true;
        t.Columns["IsDateInScope"].IsDataTypeInferred = true;
        t.Columns["IsBeforeThisMonth"].IsDataTypeInferred = true;
        t.Columns["IsLastMonth"].IsDataTypeInferred = true;
        t.Columns["IsYTD"].IsDataTypeInferred = true;
        t.Columns["IsActualToday"].IsDataTypeInferred = true;
        t.Columns["IsRefDate"].IsDataTypeInferred = true;
        t.Columns["IsHoliday"].IsDataTypeInferred = true;
        t.Columns["IsWeekday"].IsDataTypeInferred = true;
        t.Columns["IsThisYear"].IsDataTypeInferred = true;
        t.Columns["IsThisMonth"].IsDataTypeInferred = true;
        t.Columns["IsThisQuarter"].IsDataTypeInferred = true;
        t.Columns["IsThisWeek"].IsDataTypeInferred = true;
        t.Columns["Calendar Quarter (ie Q1)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Year Number (ie 2025)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Year (ie FY25)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Quarter (ie FQ1)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Quarter Number (ie 1)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Year Quarter (ie FY25 Q1)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Month Number (ie 1)"].IsDataTypeInferred = true;
        t.Columns["Fiscal Month (ie FM01)"].IsDataTypeInferred = true;
        t.Columns["Calendar Year Number (ie 2021)"].DisplayFolder = "1. Year";
        t.Columns["Calendar Year Number (ie 2021)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Year (ie 2021)"].DisplayFolder = "1. Year";
        t.Columns["Calendar Year (ie 2021)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Quarter Year (ie Q1 2021)"].DisplayFolder = "2. Quarter";
        t.Columns["Calendar Quarter Year (ie Q1 2021)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Year Quarter (ie 202101)"].DisplayFolder = "2. Quarter";
        t.Columns["Calendar Year Quarter (ie 202101)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Month Year (ie Jan 21)"].DisplayFolder = "3. Month";
        t.Columns["Calendar Month Year (ie Jan 21)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Year Month (ie 202101)"].DisplayFolder = "3. Month";
        t.Columns["Calendar Year Month (ie 202101)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Month (ie Jan)"].DisplayFolder = "3. Month";
        t.Columns["Calendar Month (ie Jan)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Month # (ie 1)"].DisplayFolder = "3. Month";
        t.Columns["Calendar Month # (ie 1)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Week EU (ie WK25)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Week EU (ie WK25)"].IsHidden = true;
        t.Columns["Calendar Week EU (ie WK25)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Week Number EU (ie 25)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Week Number EU (ie 25)"].IsHidden = true;
        t.Columns["Calendar Week Number EU (ie 25)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Year Week Number EU (ie 202125)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Year Week Number EU (ie 202125)"].IsHidden = true;
        t.Columns["Calendar Year Week Number EU (ie 202125)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Week US (ie WK25)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Week US (ie WK25)"].IsHidden = true;
        t.Columns["Calendar Week US (ie WK25)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Week Number US (ie 25)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Week Number US (ie 25)"].IsHidden = true;
        t.Columns["Calendar Week Number US (ie 25)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Year Week Number US (ie 202125)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Year Week Number US (ie 202125)"].IsHidden = true;
        t.Columns["Calendar Year Week Number US (ie 202125)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Week ISO (ie WK25)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Week ISO (ie WK25)"].IsHidden = true;
        t.Columns["Calendar Week ISO (ie WK25)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Week Number ISO (ie 25)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Week Number ISO (ie 25)"].IsHidden = true;
        t.Columns["Calendar Week Number ISO (ie 25)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Year Week Number ISO (ie 202125)"].DisplayFolder = "4. Week";
        t.Columns["Calendar Year Week Number ISO (ie 202125)"].IsHidden = true;
        t.Columns["Calendar Year Week Number ISO (ie 202125)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Weekday Short (i.e. Mon)"].DisplayFolder = @"5. Weekday / Workday\Weekday";
        t.Columns["Weekday Short (i.e. Mon)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Weekday Name (i.e. Monday)"].DisplayFolder = @"5. Weekday / Workday\Weekday";
        t.Columns["Weekday Name (i.e. Monday)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Weekday Number EU (i.e. 1)"].DisplayFolder = @"5. Weekday / Workday\Weekday";
        t.Columns["Weekday Number EU (i.e. 1)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["Calendar Month Day (i.e. Jan 05)"].DisplayFolder = "3. Month";
        t.Columns["Calendar Month Day (i.e. Jan 05)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Month Day (i.e. 0105)"].DisplayFolder = "3. Month";
        t.Columns["Calendar Month Day (i.e. 0105)"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["YYYYMMDD"].DisplayFolder = "6. Calendar Date";
        t.Columns["YYYYMMDD"].IsHidden = true;
        t.Columns["YYYYMMDD"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["IsDateInScope"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsDateInScope"].IsHidden = true;
        t.Columns["IsDateInScope"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsBeforeThisMonth"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsBeforeThisMonth"].IsHidden = true;
        t.Columns["IsBeforeThisMonth"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsLastMonth"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsLastMonth"].IsHidden = true;
        t.Columns["IsLastMonth"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsYTD"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsYTD"].IsHidden = true;
        t.Columns["IsYTD"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsActualToday"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsActualToday"].IsHidden = true;
        t.Columns["IsActualToday"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsRefDate"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsRefDate"].IsHidden = true;
        t.Columns["IsRefDate"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsHoliday"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsHoliday"].IsHidden = true;
        t.Columns["IsHoliday"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsWeekday"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsWeekday"].IsHidden = true;
        t.Columns["IsWeekday"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsThisYear"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsThisYear"].IsHidden = true;
        t.Columns["IsThisYear"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsThisMonth"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsThisMonth"].IsHidden = true;
        t.Columns["IsThisMonth"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsThisQuarter"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsThisQuarter"].IsHidden = true;
        t.Columns["IsThisQuarter"].SummarizeBy = AggregateFunction.None;
        t.Columns["IsThisWeek"].DisplayFolder = "7. Boolean Fields";
        t.Columns["IsThisWeek"].IsHidden = true;
        t.Columns["IsThisWeek"].SummarizeBy = AggregateFunction.None;
        {
            var cc = t.AddCalculatedColumn("Workdays MTD",
                @"VAR _Holidays =
        CALCULATETABLE (
            DISTINCT ('Date'[Date]),
            'Date'[IsHoliday] <> TRUE
        )
    VAR _WeekdayName = CALCULATE ( SELECTEDVALUE ( 'Date'[Weekday Short (i.e. Mon)] ) )
    VAR _WeekendDays = SWITCH (
            _WeekdayName,
            ""Sat"", 2,
            ""Sun"", 3,
            0
        )
    VAR _WorkdaysMTD =
        CALCULATE (
            NETWORKDAYS (
                CALCULATE (
                    MIN ('Date'[Date]),
                    ALLEXCEPT ('Date', 'Date'[Calendar Month Year (ie Jan 21)])
                ),
                CALCULATE (MAX ('Date'[Date]) - _WeekendDays),
                1,
                _Holidays
            )
        )
            + 1
    RETURN
        IF (_WorkdaysMTD < 1, 1, _WorkdaysMTD)", @"5. Weekday / Workday\Workdays");
            cc.SummarizeBy = AggregateFunction.Sum;
            cc.IsDataTypeInferred = true;
            cc.DataType = DataType.Int64;
            
        }
        {
            var cc = t.AddCalculatedColumn("Workdays QTD",
                @"VAR _Holidays =
        CALCULATETABLE (
            DISTINCT ('Date'[Date]),
            'Date'[IsHoliday] <> TRUE
        )
    VAR _WeekdayName = CALCULATE ( SELECTEDVALUE ( 'Date'[Weekday Short (i.e. Mon)] ) )
    VAR _WeekendDays = SWITCH (
            _WeekdayName,
            ""Sat"", 2,
            ""Sun"", 3,
            0
        )
    VAR _WorkdaysMTD =
        CALCULATE (
            NETWORKDAYS (
                CALCULATE (
                    MIN ('Date'[Date]),
                    ALLEXCEPT ('Date', 'Date'[Calendar Quarter Year (ie Q1 2021)])
                ),
                CALCULATE (MAX ('Date'[Date]) - _WeekendDays),
                1,
                _Holidays
            )
        )
            + 1
    RETURN
        IF (_WorkdaysMTD < 1, 1, _WorkdaysMTD)", @"5. Weekday / Workday\Workdays");
            cc.SummarizeBy = AggregateFunction.Sum;
            cc.IsDataTypeInferred = true;
            cc.DataType = DataType.Int64;
        }
        {
            var cc = t.AddCalculatedColumn("Workdays YTD",
                @"VAR _Holidays =
        CALCULATETABLE (
            DISTINCT ('Date'[Date]),
            'Date'[IsHoliday] <> TRUE
        )
    VAR _WeekdayName = CALCULATE ( SELECTEDVALUE ( 'Date'[Weekday Short (i.e. Mon)] ) )
    VAR _WeekendDays = SWITCH (
            _WeekdayName,
            ""Sat"", 2,
            ""Sun"", 3,
            0
        )
    VAR _WorkdaysMTD =
        CALCULATE (
            NETWORKDAYS (
                CALCULATE (
                    MIN ('Date'[Date]),
                    ALLEXCEPT ('Date', 'Date'[Calendar Year (ie 2021)])
                ),
                CALCULATE (MAX ('Date'[Date]) - _WeekendDays),
                1,
                _Holidays
            )
        )
            + 1
    RETURN
        IF (_WorkdaysMTD < 1, 1, _WorkdaysMTD)", @"5. Weekday / Workday\Workdays");
            cc.SummarizeBy = AggregateFunction.Sum;
            cc.IsDataTypeInferred = true;
            cc.DataType = DataType.Int64;
        }
        {
            var cc = t.AddCalculatedColumn("Workdays MTD %",
                @"DIVIDE (
        'Date'[Workdays MTD],
        /* Number of weekdays MTD for selected month */
        CALCULATE (
            [# Workdays in Selected Month],
            ALLEXCEPT ( 'Date', 'Date'[Calendar Month Year (ie Jan 21)] )
        ),
        /* Total number of weekdays for selected month */
        0
    )", @"5. Weekday / Workday\Workdays");
            cc.SummarizeBy = AggregateFunction.Sum;
            cc.FormatString = "0.00%";
            cc.IsDataTypeInferred = true;
            cc.DataType = DataType.Double;
        }
        t.Columns["Calendar Quarter (ie Q1)"].DisplayFolder = "2. Quarter";
        t.Columns["Calendar Quarter (ie Q1)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Year Number (ie 2025)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Year Number (ie 2025)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Year (ie FY25)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Year (ie FY25)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Quarter (ie FQ1)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Quarter (ie FQ1)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Quarter Number (ie 1)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Quarter Number (ie 1)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Year Quarter (ie FY25 Q1)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Year Quarter (ie FY25 Q1)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Month Number (ie 1)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Month Number (ie 1)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Fiscal Month (ie FM01)"].DisplayFolder = "8. Fiscal";
        t.Columns["Fiscal Month (ie FM01)"].SummarizeBy = AggregateFunction.None;
        t.Columns["Calendar Year (ie 2021)"].SortByColumn = t.Columns["Calendar Year Number (ie 2021)"];
        t.Columns["Calendar Quarter Year (ie Q1 2021)"].SortByColumn = t.Columns["Calendar Year Quarter (ie 202101)"];
        t.Columns["Calendar Month Year (ie Jan 21)"].SortByColumn = t.Columns["Calendar Year Month (ie 202101)"];
        t.Columns["Calendar Month (ie Jan)"].SortByColumn = t.Columns["Calendar Month # (ie 1)"];
        t.Columns["Calendar Week EU (ie WK25)"].SortByColumn = t.Columns["Calendar Week Number EU (ie 25)"];
        t.Columns["Calendar Week US (ie WK25)"].SortByColumn = t.Columns["Calendar Week Number US (ie 25)"];
        t.Columns["Calendar Week ISO (ie WK25)"].SortByColumn = t.Columns["Calendar Week Number ISO (ie 25)"];
        t.Columns["Weekday Short (i.e. Mon)"].SortByColumn = t.Columns["Weekday Number EU (i.e. 1)"];
        t.Columns["Weekday Name (i.e. Monday)"].SortByColumn = t.Columns["Weekday Number EU (i.e. 1)"];
        t.Columns["Calendar Month Day (i.e. Jan 05)"].SortByColumn = t.Columns["Calendar Month Day (i.e. 0105)"];
        {
            var m = t.AddMeasure("# Workdays MTD",
                @"CALCULATE(
        MAX( 'Date'[Workdays MTD] ),
        'Date'[IsDateInScope] = TRUE
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("# Workdays QTD",
                @"CALCULATE(
        MAX( 'Date'[Workdays QTD] ),
        'Date'[IsDateInScope] = TRUE
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("# Workdays YTD",
                @"CALCULATE(
        MAX( 'Date'[Workdays YTD] ),
        'Date'[IsDateInScope] = TRUE
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("# Workdays in Selected Month",
                @"IF (
        HASONEVALUE ('Date'[Calendar Month Year (ie Jan 21)]),
        CALCULATE (
            MAX ('Date'[Workdays MTD]),
            VALUES ('Date'[Calendar Month Year (ie Jan 21)])
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("# Workdays in Selected Quarter",
                @"IF (
        HASONEVALUE ('Date'[Calendar Quarter Year (ie Q1 2021)]),
        CALCULATE (
            MAX ('Date'[Workdays QTD]),
            VALUES ('Date'[Calendar Quarter Year (ie Q1 2021)])
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("# Workdays in Selected Year",
                @"IF (
        HASONEVALUE ('Date'[Calendar Year (ie 2021)]),
        CALCULATE (
            MAX ('Date'[Workdays YTD]),
            VALUES ('Date'[Calendar Year (ie 2021)])
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("% Workdays MTD",
                @"IF (
        HASONEVALUE ('Date'[Calendar Month Year (ie Jan 21)]),
        MROUND (
            DIVIDE ([# Workdays MTD], [# Workdays in Selected Month]),
            0.01
        )
    )");
            m.FormatString = "#,##0%";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("% Workdays QTD",
                @"IF (
        HASONEVALUE ('Date'[Calendar Quarter Year (ie Q1 2021)]),
        MROUND (
            DIVIDE ([# Workdays QTD], [# Workdays in Selected Quarter]),
            0.01
        )
    )");
            m.FormatString = "#,##0%";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("% Workdays YTD",
                @"IF (
        HASONEVALUE ('Date'[Calendar Year (ie 2021)]),
        MROUND (
            DIVIDE ([# Workdays YTD], [# Workdays in Selected Year]),
            0.01
        )
    )");
            m.FormatString = "#,##0%";
            m.DisplayFolder = @"5. Weekday / Workday\Measures\# Workdays";
        }
        {
            var m = t.AddMeasure("RefDate",
                @"CALCULATE ( MAX ( 'Invoices'[Billing Date] ), REMOVEFILTERS ( ) )");
            m.IsHidden = true;
            m.DisplayFolder = "Measures";
        }
        {
            var h = t.AddHierarchy("Date Hierarchy");
            h.AddLevel(t.Columns["Calendar Year (ie 2021)"], "Calendar Year (ie 2021)");
            h.AddLevel(t.Columns["Calendar Quarter (ie Q1)"], "Calendar Quarter (ie Q1)");
            h.AddLevel(t.Columns["Calendar Month (ie Jan)"], "Calendar Month (ie Jan)");
            h.AddLevel(t.Columns["Calendar Week EU (ie WK25)"], "Calendar Week EU (ie WK25)");
            h.AddLevel(t.Columns["Weekday Short (i.e. Mon)"], "Weekday Short (i.e. Mon)");
            h.AddLevel(t.Columns["Date"], "Date");
        }
    }

    // --- On-Time Delivery ---
    {
        var t = Model.AddCalculatedTable("On-Time Delivery",
            @"
    ADDCOLUMNS (
        FILTER (
            DISTINCT ('Invoices'[OTD Indicator]),
            NOT ISBLANK ('Invoices'[OTD Indicator])
        ),
        ""OTD Status"",
            SWITCH (
                TRUE (),
                'Invoices'[OTD Indicator] = TRUE, ""On Time"",
                ""Late""
            ),
        ""OTD Sort Order"",
            SWITCH (
                TRUE (),
                'Invoices'[OTD Indicator] = TRUE, 1,
                2
            )
    )");
        {
            if (!t.Columns.Contains("OTD Indicator")) t.AddCalculatedTableColumn("OTD Indicator", "Invoices[OTD Indicator]");
            if (!t.Columns.Contains("OTD Status")) t.AddCalculatedTableColumn("OTD Status", "[OTD Status]");
            if (!t.Columns.Contains("OTD Sort Order")) t.AddCalculatedTableColumn("OTD Sort Order","[OTD Sort Order]");
            
            t.Columns["OTD Indicator"].IsHidden = true;
            t.Columns["OTD Indicator"].DataType = DataType.Boolean;
            t.Columns["OTD Indicator"].SummarizeBy = AggregateFunction.None;
            t.Columns["OTD Indicator"].FormatString = @"""TRUE"";""TRUE"";""FALSE""";
            t.Columns["OTD Indicator"].SortByColumn = t.Columns["OTD Sort Order"];
            t.Columns["OTD Indicator"].IsDataTypeInferred = true;
            t.Columns["OTD Status"].IsHidden = true;
            t.Columns["OTD Status"].SummarizeBy = AggregateFunction.None;
            t.Columns["OTD Status"].SortByColumn = t.Columns["OTD Sort Order"];
            t.Columns["OTD Status"].IsDataTypeInferred = true;
            t.Columns["OTD Sort Order"].SummarizeBy = AggregateFunction.Sum;
            t.Columns["OTD Sort Order"].IsHidden = true;
            t.Columns["OTD Sort Order"].DataType = DataType.Int64;
            t.Columns["OTD Sort Order"].IsDataTypeInferred = true;
        }
        
        {
            var m = t.AddMeasure("OTD % (Lines)",
                @"
    VAR _OnTime = [OTD (Lines)]
    VAR _Total = CALCULATE (
        [Invoice Lines],
        'Invoice Document Type'[Group] = ""Invoice""
    )
    RETURN

    DIVIDE ( _OnTime, _Total )");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = "3. Lines";
        }
        {
            var m = t.AddMeasure("OTD % (Quantity)",
                @"
    VAR _OnTime = [OTD (Quantity)]
    VAR _Total = 
        CALCULATE (
            [Total Net Invoice Quantity],
            'Invoice Document Type'[Group] = ""Invoice""
        )
    RETURN

    DIVIDE ( _OnTime, _Total )");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = "2. Quantity";
        }
        {
            var m = t.AddMeasure("OTD % (Value)",
                @"
    VAR _OnTime = [OTD (Value)]
    VAR _Total = 
        CALCULATE (
            [Total Net Invoice Value],
            'Invoice Document Type'[Group] = ""Invoice""
        )
    RETURN

    DIVIDE ( _OnTime, _Total )");
            m.FormatString = "#,##0.0%";
            m.DisplayFolder = "1. Value";
        }
        {
            var m = t.AddMeasure("OTD (Lines)",
                @"
    CALCULATE (
        [Invoice Lines],
        'On-Time Delivery'[OTD Indicator] = TRUE,
        'Invoice Document Type'[Group] = ""Invoice""
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "3. Lines";
        }
        {
            var m = t.AddMeasure("OTD (Quantity)",
                @"
    CALCULATE (
        [Total Net Invoice Quantity],
        'On-Time Delivery'[OTD Indicator] = TRUE,
        'Invoice Document Type'[Group] = ""Invoice""
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "2. Quantity";
        }
        {
            var m = t.AddMeasure("OTD (Value)",
                @"
    CALCULATE (
        [Total Net Invoice Value],
        'On-Time Delivery'[OTD Indicator] = TRUE,
        'Invoice Document Type'[Group] = ""Invoice""
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "1. Value";
        }
    }

    // --- 1) Selected Metric ---
    {
        var t = Model.AddCalculatedTable("1) Selected Metric",
            @"
    SELECTCOLUMNS(
        'Z01CG2 - Value (Budget Rate)',
            ""Select a Measure"", 'Z01CG2 - Value (Budget Rate)'[Measure],
            ""Order"", 'Z01CG2 - Value (Budget Rate)'[Ordinal]
    )");
        t.Description = "It lets you select a Metric.";
        {        
            if (!t.Columns.Contains("Select a Measure")) t.AddCalculatedTableColumn("Select a Measure", "[Select a Measure]");
            if (!t.Columns.Contains("Order")) t.AddCalculatedTableColumn("Order", "[Order]");
            
            t.Columns["Order"].IsHidden = true;
            t.Columns["Order"].SummarizeBy = AggregateFunction.Sum;
            t.Columns["Order"].DataType = DataType.Int64;
            t.Columns["Order"].IsDataTypeInferred = true;
            t.Columns["Select a Measure"].SummarizeBy = AggregateFunction.None;
            t.Columns["Select a Measure"].SortByColumn = t.Columns["Order"];
            t.Columns["Select a Measure"].IsDataTypeInferred = true;
        }
        
    }

    // --- 2) Selected Unit ---
    {
        var t = Model.AddCalculatedTable("2) Selected Unit",
            @"
    SELECTCOLUMNS(
        'Z02CG1 - Unit',
            ""Select a Unit"", 'Z02CG1 - Unit'[Unit],
            ""Order"", 'Z02CG1 - Unit'[Ordinal]
    ) ");
        t.Description = "System.Object[]";
        {
            if (!t.Columns.Contains("Select a Unit")) t.AddCalculatedTableColumn("Select a Unit","[Select a Unit]");
            if (!t.Columns.Contains("Order")) t.AddCalculatedTableColumn("Order", "[Order]");
            
            t.Columns["Order"].IsHidden = true;
            t.Columns["Order"].SummarizeBy = AggregateFunction.Sum;
            t.Columns["Order"].DataType = DataType.Int64;
            t.Columns["Order"].IsDataTypeInferred = true;
            t.Columns["Select a Unit"].SummarizeBy = AggregateFunction.None;
            t.Columns["Select a Unit"].SortByColumn = t.Columns["Order"];
            t.Columns["Select a Unit"].IsDataTypeInferred = true;
        }
    }

    // --- 3) Selected Target ---
    {
        var t = Model.AddCalculatedTable("3) Selected Target",
            @"
    SELECTCOLUMNS (
        'Z03CG1 - Sales Target',
        ""Select a Target"", 'Z03CG1 - Sales Target'[Target],
        ""Order"", 'Z03CG1 - Sales Target'[Ordinal],
        ""MTD Slicer"",
            VAR _TARGET = 'Z03CG1 - Sales Target'[Target]
            RETURN
                SWITCH (
                    TRUE (),
                    _TARGET = ""Budget"",         ""Gross Sales MTD vs. Budget"",
                    _TARGET = ""Forecast"",       ""Gross Sales MTD vs. FCST"",
                    _TARGET = ""1 Year Prior"",   ""Gross Sales MTD vs. 1YP"",
                    _TARGET = ""2 Years Prior"",  ""Gross Sales MTD vs. 2YP""
                )
    )");
        t.Description = "System.Object[]";
        {
            if (!t.Columns.Contains("Select a Target")) t.AddCalculatedTableColumn("Select a Target", "[Select a Target]");
            if (!t.Columns.Contains("Order")) t.AddCalculatedTableColumn("Order", "[Order]");
            if (!t.Columns.Contains("MTD Slicer")) t.AddCalculatedTableColumn("MTD Slicer", "[MTD Slicer]");
            
            t.Columns["MTD Slicer"].IsHidden = true;
            t.Columns["MTD Slicer"].SummarizeBy = AggregateFunction.None;
            t.Columns["MTD Slicer"].SortByColumn = t.Columns["Order"];
            t.Columns["MTD Slicer"].IsDataTypeInferred = true;
            t.Columns["Order"].IsHidden = true;
            t.Columns["Order"].SummarizeBy = AggregateFunction.Sum;
            t.Columns["Order"].DataType = DataType.Int64;
            t.Columns["Order"].IsDataTypeInferred = true;
            t.Columns["Select a Target"].SummarizeBy = AggregateFunction.None;
            t.Columns["Select a Target"].SortByColumn = t.Columns["Order"];   
            t.Columns["Select a Target"].IsDataTypeInferred = true;
        }
    }

    // --- 4) Selected Period ---
    {
        var t = Model.AddCalculatedTable("4) Selected Period",
            @"
    UNION (
        ROW (
            ""Period"", {""Full Period""},
            ""Order"", {0},
            ""MTD"", {""Full Month""},
            ""QTD"", {""Full Quarter""},
            ""YTD"", {""Full Year""}
        ),
        ROW (
            ""Period"", {""Period-to-Date""},
            ""Order"", {1},
            ""MTD"", {""MTD""},
            ""QTD"", {""QTD""},
            ""YTD"", {""YTD""}
        )
    )");
        t.Description = "System.Object[]";
        {
            if (!t.Columns.Contains("MTD")) t.AddCalculatedTableColumn("MTD", "[MTD]");
            if (!t.Columns.Contains("QTD")) t.AddCalculatedTableColumn("QTD", "[QTD]");
            if (!t.Columns.Contains("YTD")) t.AddCalculatedTableColumn("YTD", "[YTD]");
            if (!t.Columns.Contains("Order")) t.AddCalculatedTableColumn("Order", "[Order]");
            if (!t.Columns.Contains("Period")) t.AddCalculatedTableColumn("Period", "[Period]");
            
            t.Columns["MTD"].IsHidden = true;
            t.Columns["MTD"].SummarizeBy = AggregateFunction.None;
            t.Columns["MTD"].IsDataTypeInferred = true;
            t.Columns["QTD"].IsHidden = true;
            t.Columns["QTD"].SummarizeBy = AggregateFunction.None;
            t.Columns["QTD"].IsDataTypeInferred = true;
            t.Columns["YTD"].IsHidden = true;
            t.Columns["YTD"].SummarizeBy = AggregateFunction.None;
            t.Columns["YTD"].IsDataTypeInferred = true;
            t.Columns["Order"].IsHidden = true;
            t.Columns["Order"].SummarizeBy = AggregateFunction.Sum;
            t.Columns["Order"].DataType = DataType.Int64;
            t.Columns["Order"].IsDataTypeInferred = true;
            t.Columns["Period"].SummarizeBy = AggregateFunction.None;
            t.Columns["Period"].IsDataTypeInferred = true;
        }
    }

    // --- __Measures ---
    {
        var t = Model.AddCalculatedTable("__Measures",
            @"
    SELECTCOLUMNS (
        { 1 },
        ""__Measures"", ''[Value]
    )");
        t.IsHidden = true;
        t.Description = "System.Object[]";
        
        if (!t.Columns.Contains("__Measures")) t.AddCalculatedTableColumn("__Measures", "[__Measures]");
        
        t.Columns["__Measures"].IsHidden = true;
        t.Columns["__Measures"].SummarizeBy = AggregateFunction.Sum;
        t.Columns["__Measures"].IsDataTypeInferred = true;
        t.Columns["__Measures"].DataType = DataType.Int64;
        {
            var m = t.AddMeasure("Actuals",
                @"
    CALCULATE (
        [Blank],
        TREATAS (
            DISTINCT ('2) Selected Unit'[Select a Unit]),
            'Z02CG1 - Unit'[Unit]
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Actuals";
        }
        {
            var m = t.AddMeasure("Actuals MTD",
                @"
    CALCULATE (
        [Actuals],
        CALCULATETABLE (
            DATESMTD ('Date'[Date]),
            'Date'[IsDateInScope]
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"2. MTD\Actuals";
        }
        {
            var m = t.AddMeasure("Actuals QTD",
                @"
    CALCULATE (
        [Actuals],
        CALCULATETABLE (
            DATESQTD ('Date'[Date]),
            'Date'[IsDateInScope]
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"3. QTD\Actuals";
        }
        {
            var m = t.AddMeasure("Actuals YTD",
                @"
    CALCULATE (
        [Actuals],
        CALCULATETABLE (
            DATESYTD ('Date'[Date]),
            'Date'[IsDateInScope]
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "4. YTD";
        }
        {
            var m = t.AddMeasure("Lines",
                @"
    CALCULATE (
        [Blank],
        TREATAS (
            DISTINCT ('1) Selected Metric'[Select a Measure]),
            'Z01CG4 - Lines'[Measure]
        )
    )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Actuals";
        }
        {
            var m = t.AddMeasure("Quantity",
                @"
    CALCULATE (
        [Blank],
        TREATAS (
            DISTINCT ('1) Selected Metric'[Select a Measure]),
            'Z01CG1 - Quantity'[Measure])
    )");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Actuals";
        }
        {
            var m = t.AddMeasure("Sales Target YTD vs. Gross Sales (%)",
                @"
    VAR _TARGET = [Sales Target YTD]
    VAR _DELTA  = [Actuals YTD] -  _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = "4. YTD";
        }
        {
            var m = t.AddMeasure("Sales Target YTD vs. Gross Sales (Δ)",
                @"
    [Actuals YTD] - [Sales Target YTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = "4. YTD";
        }
        {
            var m = t.AddMeasure("Value",
                @"
    CALCULATE (
        [Blank],
        TREATAS (
            DISTINCT ('1) Selected Metric'[Select a Measure]),
            'Z01CG2 - Value (Budget Rate)'[Measure]
        )
    ) ");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Actuals";
        }
        {
            var m = t.AddMeasure("Sales Target YTD",
                @"
    VAR YRTD =
        CALCULATE(
            [Sales Target],
            CALCULATETABLE(
                DATESYTD( 'Date'[Date] ),
                'Date'[IsDateInScope] = TRUE
            ),
            'Date'[IsBeforeThisMonth] = TRUE
        ) // Calculates the target for the current month/year selection
    VAR _CurrentMonth_value =
        CALCULATE( [Sales Target], 'Date'[IsThisMonth] = TRUE ) // Calculates the % workdays MTD together with the dynamic parameter to set the exponential rate of increase MTD
    VAR _currentMonthWorkDay =
        POWER(
            CALCULATE(
                MAX( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE,
                'Date'[IsThisMonth] = TRUE
            ),
            0.93
        ) // If viewing units, hide the FCST (as there is no units FCST as of July 09, 2020)
    VAR MOTD =
        IF(
            _CurrentMonth_value > 0,
            _currentMonthWorkDay * _CurrentMonth_value,
            BLANK( )
        )
            + YRTD
    VAR _PERIOD = SELECTEDVALUE( '4) Selected Period'[Period] )
    VAR _YEAR = MAX( 'Date'[Calendar Year Number (ie 2021)] )
    VAR _MONTH = MAX( 'Date'[Calendar Year Month (ie 202101)] )
    RETURN
        IF(
            _PERIOD = ""Full Period"",
            CALCULATE(
                [Sales Target],
                ALL( 'Date' ),
                'Date'[Calendar Year Number (ie 2021)] = _YEAR,
                'Date'[Calendar Year Month (ie 202101)] <= _MONTH
            ),
            // If date is in the visual context and YTD is blank, show MTD, otherwise add YTD + MTD together
            MOTD
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "4. YTD";
        }
        {
            var m = t.AddMeasure("Sales Target QTD",
                @"
    VAR _QTD =
        CALCULATE(
            [Sales Target],
            CALCULATETABLE(
                DATESQTD( 'Date'[Date] ),
                'Date'[IsDateInScope] = TRUE
            ),
            'Date'[IsThisMonth] = FALSE
        )
    VAR _MTD =
        CALCULATE( [Sales Target MTD], 'Date'[IsThisMonth] = TRUE )
    VAR _PERIOD = SELECTEDVALUE( '4) Selected Period'[Period] )
    VAR _QUARTER = MAX( 'Date'[Calendar Year Quarter (ie 202101)] )
    VAR _MONTH = MAX( 'Date'[Calendar Year Month (ie 202101)] )
    RETURN
        IF(
            _PERIOD = ""Full Period"",
            CALCULATE(
                [Sales Target],
                ALL( 'Date' ),
                'Date'[Calendar Year Quarter (ie 202101)] = _QUARTER,
                'Date'[Calendar Year Month (ie 202101)] <= _MONTH
            ),
            IF(
                ISBLANK( _QTD ),
                _MTD,
                // If date is in the visual context and QTD is blank, show MTD, otherwise add QTD + MTD together
                IF( MAX ( 'Date'[Date] ) >= MAX ('Last Refresh'[Last Refresh]), _QTD + _MTD, _QTD )
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"3. QTD\Sales Target";
        }
        {
            var m = t.AddMeasure("Sales Target MTD",
                @"
    -- Calculates the target for the current month/year selection
    VAR _CurrentMonth =
        CALCULATE (
            [Sales Target],
            ALL ( 'Date' ),
            VALUES (
                'Date'[Calendar Month Year (ie Jan 21)]
            )
        )

    -- Computes rate at which Budget increases through the month
    VAR _CurrentMonthWDMTD =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE
            ),
            1.035
        )
    VAR CurrentMonthWDTotal =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] )
            ),
            1.035
        )
    VAR _Period =
        SELECTEDVALUE (
            '4) Selected Period'[Period]
        )
    VAR _HasOneMonth =
        HASONEVALUE (
            'Date'[Calendar Month Year (ie Jan 21)]
        )
    RETURN
        -- return MTD value if a month is selected
        IF.EAGER (
            _HasOneMonth && _Period = ""Full Period"",
            CurrentMonthWDTotal
                * _CurrentMonth,
            IF (
                _HasOneMonth,
                _CurrentMonthWDMTD
                    * _CurrentMonth
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"2. MTD\Sales Target";
        }
        {
            var m = t.AddMeasure("Sales Target",
                @"
    CALCULATE (
        [Blank],
        TREATAS (
            DISTINCT (
                '3) Selected Target'[Select a Target]
            ),
            'Z03CG1 - Sales Target'[Target]
        )
    )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Sales Target";
        }
        {
            var m = t.AddMeasure("Sales Target vs. Actuals (Δ)",
                @"
    [Actuals] - [Sales Target]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"1. Total\Sales Target";
        }
        {
            var m = t.AddMeasure("Sales Target vs. Actuals (%)",
                @"
    VAR _TARGET = [Sales Target]
    VAR _ACTUAL = [Actuals]
    VAR _DELTA  = _ACTUAL - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )

    RETURN

    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"1. Total\Sales Target";
        }
        {
            var m = t.AddMeasure("Sales Target MTD vs. Actuals (Δ)",
                @"
    [Actuals MTD] - [Sales Target MTD]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"2. MTD\Sales Target";
        }
        {
            var m = t.AddMeasure("Sales Target MTD vs. Actuals (%)",
                @"
    VAR _TARGET = [Sales Target MTD]
    VAR _ACTUAL = [Actuals MTD]
    VAR _DELTA  = _ACTUAL - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )

    RETURN

    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"2. MTD\Sales Target";
        }
        {
            var m = t.AddMeasure("Actuals MTD, Latest Data Point",
                @"
    -- Max workday in scope
    VAR _MAXWD =
        CALCULATE (
            MAX ( 'Date'[Workdays MTD] ),
            'Date'[IsDateInScope],
            ALLEXCEPT ('Date', 'Date'[Calendar Month Year (ie Jan 21)])
        )

    -- Max date in scope
    VAR _MAXDT =
        CALCULATE ( MAX ('Date'[Date] ),
            'Date'[IsDateInScope],
            ALLEXCEPT ('Date', 'Date'[Calendar Month Year (ie Jan 21)]) )
    RETURN

        -- If the selected workday = the max workday in scope, return the value
        -- Otherwise return blank (don't show the marker / value)
        IF (
            MAX ('Date'[Workdays MTD]) = _MAXWD
                || MAX ('Date'[Date]) = _MAXDT,
            [Actuals MTD]
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "9. Technical Measures";
        }
        {
            var m = t.AddMeasure("Actuals MTD, Data Label",
                @"
    -- Max workday in scope
    VAR _MAXWD =
        CALCULATE (
            MAX ( 'Date'[Workdays MTD] ),
            'Date'[IsDateInScope],
            ALLEXCEPT ('Date', 'Date'[Calendar Month Year (ie Jan 21)])
        )

    -- Max date in scope
    VAR _MAXDT =
        CALCULATE ( MAX ('Date'[Date] ),
            'Date'[IsDateInScope],
            ALLEXCEPT ('Date', 'Date'[Calendar Month Year (ie Jan 21)]) )
    RETURN

        -- If the selected workday = the max workday in scope, return the value
        -- Otherwise return blank (don't show the marker / value)
        IF (
            MAX ('Date'[Workdays MTD]) = _MAXWD
                || MAX ('Date'[Date]) = _MAXDT,
            ""#6e8c95"",
            ""#FFFFFF00""
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = "9. Technical Measures";
        }
        {
            var m = t.AddMeasure("Blank",
                @"
    BLANK ()");
            m.IsHidden = true;
            m.DisplayFolder = "9. Technical Measures";
        }
        {
            var m = t.AddMeasure("Orders Target",
                @"
    CALCULATE (
        [Blank],
        TREATAS (
            DISTINCT ( '3) Selected Target'[Select a Target] ), 'Z03CG2 - Orders Target'[Target] )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Orders Target";
        }
        {
            var m = t.AddMeasure("Orders Target vs. Net Orders (%)",
                @"
    VAR _TARGET = [Orders Target]
    VAR _DELTA  = [Net Orders] - _TARGET
    VAR _PERC   = DIVIDE ( _DELTA, _TARGET )
    RETURN
    _PERC");
            m.FormatString = "#,##0.0% ↑;#,##0.0% ↓;#,##0.0%";
            m.DisplayFolder = @"1. Total\Orders Target";
            m.Description = "Net orders exclude cancellation document types";
        }
        {
            var m = t.AddMeasure("Orders Target vs. Net Orders (Δ)",
                @"
    [Net Orders] - [Orders Target]");
            m.FormatString = "#,##0 ↑;#,##0 ↓;#,##0";
            m.DisplayFolder = @"1. Total\Orders Target";
            m.Description = "Net orders exclude cancellation document types";
        }
        {
            var m = t.AddMeasure("Orders Target MTD",
                @"
    -- Calculates the target for the current month/year selection
    VAR _CurrentMonth =
        CALCULATE (
            [Orders Target],
            ALL ( 'Date' ),
            VALUES (
                'Date'[Calendar Month Year (ie Jan 21)]
            )
        )

    -- Computes rate at which Budget increases through the month
    VAR _CurrentMonthWDMTD =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] ),
                'Date'[IsDateInScope] = TRUE
            ),
            0.93
        )
    VAR _CurrentMonthWDTotal =
        POWER (
            CALCULATE (
                MAX ( 'Date'[Workdays MTD %] )
            ),
            0.93
        )
    VAR _Period =
        SELECTEDVALUE (
            '4) Selected Period'[Period]
        )
        
    VAR __HasOneMonth =
        HASONEVALUE (
            'Date'[Calendar Month Year (ie Jan 21)]
        )
    RETURN
        -- return MTD value if a month is selected
        IF.EAGER (
            __HasOneMonth && _Period = ""Full Period"",
            _CurrentMonthWDTotal
                * _CurrentMonth,
            IF (
                __HasOneMonth,
                _CurrentMonthWDMTD
                    * _CurrentMonth
            )
        )");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"2. MTD\Orders Target";
        }
        {
            var m = t.AddMeasure("Orders Target QTD",
                @"
    VAR _DATES_QTD =
        CALCULATETABLE (
            DATESQTD ('Date'[Date]),
            'Date'[IsThisMonth] = FALSE
        )
    VAR _CURRENT_MONTH_Forecast = [Orders Target MTD]
    VAR _QTD_Forecast_BEFORE_THIS_MONTH = CALCULATE ([Orders Target], _DATES_QTD)
    VAR _RESULT = _CURRENT_MONTH_Forecast + _QTD_Forecast_BEFORE_THIS_MONTH
    RETURN
        _RESULT");
            m.FormatString = "#,##0";
            m.DisplayFolder = @"3. QTD\Orders Target";
        }
        {
            var m = t.AddMeasure("Orders Target YTD",
                @"
    VAR _CURRENT_MONTH_TARGET = [Orders Target MTD]
    VAR _YTD_TARGET_BEFORE_THIS_MONTH =
        CALCULATE (
            [Orders Target],
            CALCULATETABLE (
                DATESYTD ('Date'[Date]),
                'Date'[IsThisMonth] = FALSE,
                'Date'[IsDateInScope] = TRUE
            )
        )
    VAR _RESULT = _CURRENT_MONTH_TARGET + _YTD_TARGET_BEFORE_THIS_MONTH
    RETURN
        _RESULT");
            m.FormatString = "#,##0";
            m.DisplayFolder = "4. YTD";
        }
        {
            var m = t.AddMeasure("Gross Sales 2YP (Selected Unit)",
                @"
    CALCULATE ( [Gross Sales (Selected Unit)], DATEADD('Date'[Date], -2, YEAR ))");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Sales Target";
        }
        {
            var m = t.AddMeasure("Gross Sales 1YP (Selected Unit)",
                @"
    CALCULATE ( [Gross Sales (Selected Unit)], DATEADD('Date'[Date], -1, YEAR ))");
            m.IsHidden = true;
            m.FormatString = "#,##0";
            m.DisplayFolder = @"1. Total\Sales Target";
        }
    }

    // --- __Demo Measures ---
    {
        var t = Model.AddCalculatedTable("__Demo Measures",
            @"
    SELECTCOLUMNS (
        { 1 },
        ""__Measures"", ''[Value]
    )");
        t.Description = "5 measures usable without any selection; quick-start";
        {
            if (!t.Columns.Contains("__Measures")) t.AddCalculatedTableColumn("__Measures", "[__Measures]");
            
            t.Columns["__Measures"].IsHidden = true;
            t.Columns["__Measures"].SummarizeBy = AggregateFunction.Sum;
            t.Columns["__Measures"].IsDataTypeInferred = true;
            t.Columns["__Measures"].DataType = DataType.Int64;
        }
        {
            var m = t.AddMeasure("Sales",
                @"
    CALCULATE (
        [Gross Sales],
        'Exchange Rate'[From Currency] = ""EUR""
    )");
            m.FormatString = "#,##0.00";
            m.DisplayFolder = @"Measures\Numbers";
        }
        {
            var m = t.AddMeasure("Sales Budget",
                @"
    CALCULATE (
        [Budget],
        'Exchange Rate'[From Currency] = ""EUR""
    )");
            m.FormatString = "#,##0.00";
            m.DisplayFolder = @"Measures\Numbers";
        }
        {
            var m = t.AddMeasure("Sales Forecast",
                @"
    CALCULATE (
        [Forecast],
        'Exchange Rate'[From Currency] = ""EUR""
    )");
            m.FormatString = "#,##0.00";
            m.DisplayFolder = @"Measures\Numbers";
        }
        {
            var m = t.AddMeasure("Billing Document Lines",
                @"[Invoice Lines]");
            m.FormatString = "#,##0.00";
            m.DisplayFolder = @"Measures\Numbers";
        }
        {
            var m = t.AddMeasure("# Accounts",
                @"1");
            m.FormatString = "#,##0.00";
            m.DisplayFolder = @"Measures\Numbers";
        }
        {
            var m = t.AddMeasure("ActualVsTargetColor",
                @"
    IF (
        [Gross Sales (Quantity)]
            > [Sales Target],
        ""good"", -- Good color (blue)
        ""bad""  -- Bad color (yellow)
    )");
        }
        {
            var m = t.AddMeasure("No. Products",
                @"COUNTROWS ( 'Products' )");
            m.FormatString = "#,##0 Products";
            m.DisplayFolder = @"Measures\Numbers";
        }
        {
            var m = t.AddMeasure("CustomData",
                @"CUSTOMDATA()");
        }
    }
    
    // ============================================================
    // 6. RELATIONSHIPS
    // ============================================================
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Customers"].Columns["Station"];
        r.ToColumn = Model.Tables["Regions"].Columns["Station"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Products"].Columns["Sub Brand Name"];
        r.ToColumn = Model.Tables["Brands"].Columns["Sub Brand"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Budget"].Columns["Month"];
        r.ToColumn = Model.Tables["Date"].Columns["Date"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Budget"].Columns["Customer Key"];
        r.ToColumn = Model.Tables["Customers"].Columns["Customer Key"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Budget"].Columns["Product Key"];
        r.ToColumn = Model.Tables["Products"].Columns["Product Key"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Exchange Rate"].Columns["Date"];
        r.ToColumn = Model.Tables["Date"].Columns["Date"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Order Date"];
        r.ToColumn = Model.Tables["Date"].Columns["Date"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Request Goods Receipt Date"];
        r.ToColumn = Model.Tables["Date"].Columns["Date"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
        r.IsActive = false;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Sales Order Document Line Item Status"];
        r.ToColumn = Model.Tables["Order Status"].Columns["Order Status Code"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Sales Order Document Type Code"];
        r.ToColumn = Model.Tables["Order Document Type"].Columns["Sales Order Document Type Code"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Product Key"];
        r.ToColumn = Model.Tables["Products"].Columns["Product Key"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Customer Key"];
        r.ToColumn = Model.Tables["Customers"].Columns["Customer Key"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Forecast"].Columns["Forecast Month"];
        r.ToColumn = Model.Tables["Date"].Columns["Date"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Forecast"].Columns["Product Type"];
        r.ToColumn = Model.Tables["Products"].Columns["Type"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.Many;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Forecast"].Columns["Region Territory"];
        r.ToColumn = Model.Tables["Regions"].Columns["Territory"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.Many;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Orders"].Columns["Local Currency"];
        r.ToColumn = Model.Tables["Budget Rate"].Columns["From Currency"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Invoices"].Columns["Billing Date"];
        r.ToColumn = Model.Tables["Date"].Columns["Date"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Invoices"].Columns["Billing Document Type Code"];
        r.ToColumn = Model.Tables["Invoice Document Type"].Columns["Billing Document Type Code"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Invoices"].Columns["Local Currency"];
        r.ToColumn = Model.Tables["Budget Rate"].Columns["From Currency"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Invoices"].Columns["OTD Indicator"];
        r.ToColumn = Model.Tables["On-Time Delivery"].Columns["OTD Indicator"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Invoices"].Columns["Product Key"];
        r.ToColumn = Model.Tables["Products"].Columns["Product Key"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }
    {
        var r = Model.AddRelationship();
        r.FromColumn = Model.Tables["Invoices"].Columns["Customer Key"];
        r.ToColumn = Model.Tables["Customers"].Columns["Customer Key"];
        r.FromCardinality = RelationshipEndCardinality.Many;
        r.ToCardinality = RelationshipEndCardinality.One;
        r.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
    }

    // ============================================================
    // 7. MODEL-LEVEL DAX FUNCTIONS
    // ============================================================
    if(Model.Database.CompatibilityLevel >= 1702)
    {
        var f = Function.CreateNew(Model, "TE.Comparison.RelativToTarget");
        f.Description =
            @"Compare to a target and return the relative percent";
        f.Expression =
            @"//Compare to a target and return the relative percent
    //Example usage: Comparing Sales to budget or forecast to get relative performance
    //Examples:
    // - 9/10 is -10%
    // - 11/10 is +10%
    (
        // The actual value
        actual: SCALAR NUMERIC VAL,
     
        // The target that you are comparing to
        target: SCALAR NUMERIC VAL
    ) =>
    VAR _target = target
    VAR _actual = actual
    VAR _delta  = _actual - _target
    VAR _percent  = DIVIDE ( _delta, _target )

    RETURN
        _percent";
    }     
        
    form.Close();
};
form.Controls.Add(launchButton);

// Show the dialog
form.ShowDialog();
