using System.Collections;
using System.IO;
using System.Text.Json;

// Replace the prefix of selected function names to be "My.Package"
F.ReCategorize("My.Package", Selected.Functions);

// Display a text box showing whether the currently selected function references model-specific objects
F.IsModelDependent(Selected.Function).Output();

// Exports a daxlib folder structure for selected functions as a measure
F.ExportPackage("C:/Users/GregBaldini/tmp/packages/", "My.Package", "0.0.1", "greggyb", "A demo package", Selected.Functions);

public static class F
{
    public static List<IDaxObject> GetFunctionDependencies(IEnumerable<Function> functions) =>
        Selected.Functions.SelectMany(f => f.DependsOn.Deep()).ToList();

    public static bool IsModelDependent(Function function) =>
        GetFunctionDependencies([function]).Any(dep => dep is not Function);

    public static bool AreModelDependent(IEnumerable<Function> functions) =>
        functions.Any(IsModelDependent);

    public static void ReCategorize(string prefix, IEnumerable<Function> functions)
    {
        foreach (var f in functions)
        {
            var lastName = f.Name.Split('.')[^1];
            f.Name = $"{prefix}.{lastName}";
        }
    }

    /// <summary>
    /// Checks that functions are valid to create a DAXLib package, then creates directory structure and required files
    /// </summary>
    public static void ExportPackage(string exportPath, string id, string version, string authors, string description, IEnumerable<Function> functions)
    {
        var sb = new System.Text.StringBuilder();
        var functionList = functions.ToList();
        // guard against 
        if (AreModelDependent(functionList))
        {
            sb.Append("Cannot make a DAXLib package with model-dependent functions");
            Output(sb);
            return;
        }

        var packagePath = Path.Combine(exportPath, id.ToLower());
        var versionPath = Path.Combine(packagePath, version);
        var libPath = Path.Combine(versionPath, "lib");
        foreach (var path in (string[])[exportPath, packagePath, versionPath, libPath])
        {
            Directory.CreateDirectory(path);
            sb.AppendLine($"Ensured directory: {path}");
        }

        var manifest = new {id, version, authors, description};
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(manifest, opts);
        var manifestPath = Path.Combine(versionPath, "manifest.daxlib");
        File.WriteAllText(manifestPath, json);
        sb.AppendLine($"Wrote manifest at: {manifestPath}");

        // ensure we include the selected functions and everything they depend on
        // already ensured above that these are model *in*dependent
        var allDeps = functionList.Union(GetFunctionDependencies(functionList).Select(f => f as Function)).Distinct();
        var tmdl = Scripter.ScriptTmdl(allDeps);
        var tmdlPath = Path.Combine(libPath, "functions.tmdl");
        File.WriteAllText(tmdlPath, tmdl);
        sb.AppendLine($"Wrote functions at: {tmdlPath}");

        Output(sb);
        return;
    }
}