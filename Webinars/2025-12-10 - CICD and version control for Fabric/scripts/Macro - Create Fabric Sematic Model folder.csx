using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

// ---------------------------------------------------------
// Step 1: Show custom info dialog
// ---------------------------------------------------------

var form = new Form()
{
    Text = "Fabric Semantic Model Setup",
    Width = 600,
    Height = 360,
    FormBorderStyle = FormBorderStyle.FixedDialog,
    StartPosition = FormStartPosition.Manual,
    MaximizeBox = false,
    MinimizeBox = false
};

// Center horizontally, 400px from top
var screen = Screen.PrimaryScreen.WorkingArea;
form.Left = (screen.Width - form.Width) / 2;
form.Top = 400;

var lbl = new Label()
{
    Text = "This will create the Fabric Git folder structure for:\n" +
           $"{Model.Name}.SemanticModel\n\n" +
           "Click the button below to choose where the semantic model folder should be created.",
    AutoSize = false,
    Width = 540,
    Height = 200,
    Left = 15,
    Top = 15
};

var btn = new Button()
{
    Text = "Choose Model Folder",
    Width = 300,
    Height = 40,
    Left = 100,
    Top = 220,
    DialogResult = DialogResult.OK
};

var cancelBtn = new Button()
{
    Text = "Cancel",
    Width = 125,
    Height = 40,
    Left = 425,
    Top = 220,
    DialogResult = DialogResult.Cancel
};

form.Controls.Add(lbl);
form.Controls.Add(btn);
form.Controls.Add(cancelBtn);

if (form.ShowDialog() != DialogResult.OK)
    return;

// ---------------------------------------------------------
// Step 2: Ask user to select root folder
// ---------------------------------------------------------

var dlg = new System.Windows.Forms.FolderBrowserDialog();
if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

var root = dlg.SelectedPath;

// ---------------------------------------------------------
// Step 3: Create folder structure
// ---------------------------------------------------------

var modelFolder = Path.Combine(root, Model.Name + ".SemanticModel");
var defFolder = Path.Combine(modelFolder, "definition");

Directory.CreateDirectory(defFolder);

// ---------------------------------------------------------
// Step 4: Write files
// ---------------------------------------------------------

Guid guid = Guid.NewGuid();

var platform = new JObject
{
    ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/gitIntegration/platformProperties/2.0.0/schema.json",
    ["metadata"] = new JObject
    {
        ["type"] = "SemanticModel",
        ["displayName"] = Model.Name
    },
    ["config"] = new JObject
    {
        ["version"] = "2.0",
        ["logicalId"] = guid.ToString()
    }
};

File.WriteAllText(
    Path.Combine(modelFolder, ".platform"),
    platform.ToString()
);

var pbism = new JObject
{
    ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/semanticModel/definitionProperties/1.0.0/schema.json",
    ["version"] = "4.2",
    ["settings"] = new JObject()
};

File.WriteAllText(
    Path.Combine(modelFolder, "definition.pbism"),
    pbism.ToString()
);

// ---------------------------------------------------------
// Step 5: Inform user
// ---------------------------------------------------------

Info($"Fabric Git folder for model '{Model.Name}' was successfully created at:\n{modelFolder}\n\nRemember to save your model in the definition folder.");