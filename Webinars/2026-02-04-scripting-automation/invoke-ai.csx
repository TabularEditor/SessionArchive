// Use AI to sample data
// =====================
// You will need an OpenAI API key, which can be hardcoded into the script below, or added to the
// TE_OpenAI_APIKey environment variable.
// 
// * * * * * * *
// W A R N I N G
// * * * * * * *
// This script will extract a sample (default 100 rows) of data from whatever table is selected in
// the TOM Explorer, and embed that data in a prompt to the OpenAI LLM. DO NOT use this technique
// if your model contains sensitive data.

#r "System.Net.Http"
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

// You need to signin to https://platform.openai.com/ and create an API key for your profile then paste that key 
// into the apiKey constant below
var apiKey = Environment.GetEnvironmentVariable("TE_OpenAI_APIKey");
if(string.IsNullOrEmpty(apiKey)) {
    Info("Please specify your OpenAI API key in an environment variable named: 'TE_OpenAI_APIKey'");
    return;
}
if(Selected.Direct.Count != 1 || Selected.Direct.FirstOrDefault() is not Table) {
    Info("Select exactly 1 table in the TOM Explorer");
    return;
}

var tableName = Selected.Table.Name;

const int numberOfRows = 100;

EvaluateDax("TOPN(" + numberOfRows + ", '" + tableName + "')").Output();
var mr = MessageBox.Show("Proceed with submitting the previously shown sample data to the LLM?", "Submit sample data to LLM?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
if(mr == DialogResult.Cancel) return;


const string uri = "https://api.openai.com/v1/chat/completions";

const string model = "gpt-4o-mini";
var systemPrompt = "In the context of a Power BI semantic model, the user will provide a JSON object containing basic properties of a table. Analyze the data sample to determine if this is a fact table, a dimensional table, a bridge, a parameter table, or something else, and if any of the columns on the table seem out of place (for example, dimensional attributes on a fact table, or columns containing redundant data). Provide a brief description of the table and its purpose. If any columns should be removed, output a Tabular Editor C# script that deletes them, e.g.: ```csharp\nModel.Tables[\"Sales\"].Columns[\"Product Color\"].Delete();\nModel.Tables[\"Sales\"].Columns[\"Customer Name\"].Delete();```";

const string template = "{{\"model\": \"{0}\",\"messages\": [{{\"role\": \"system\",\"content\":[{{\"type\":\"text\",\"text\":\"{1}\"}}]}},{{\"role\": \"user\",\"content\":[{{\"type\":\"text\",\"text\":\"{2}\"}}]}}],\"temperature\": 1,\"max_tokens\": 4000}}";

using (var client = new HttpClient()) {
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

    var sampleAsJson = EvaluateDax("TOJSON('" + Selected.Table.Name + "', " + numberOfRows + ")") as string;
    var userPrompt = "Table name: " + tableName;
    userPrompt += "\nTable type: " + Selected.Table.ObjectTypeName;
    if(!string.IsNullOrEmpty(Selected.Table.Description)) userPrompt += "\nTable description: " + Selected.Table.Description;
    userPrompt += "\nSample data:\n";
    userPrompt += sampleAsJson;

    var requestBody = new JObject
    {
        ["model"] = model,
        ["messages"] = new JArray
        {
            new JObject
            {
                ["role"] = "system",
                ["content"] = new JArray
                {
                    new JObject { ["type"] = "text", ["text"] = systemPrompt }
                }
            },
            new JObject
            {
                ["role"] = "user",
                ["content"] = new JArray
                {
                    new JObject { ["type"] = "text", ["text"] = userPrompt }
                }
            }
        },
        ["temperature"] = 1,
        ["max_tokens"] = 4000
    };

    var body = requestBody.ToString(Newtonsoft.Json.Formatting.None);

    var res = client.PostAsync(uri, new StringContent(body, Encoding.UTF8,"application/json"));
    res.Result.EnsureSuccessStatusCode();
    var result = res.Result.Content.ReadAsStringAsync().Result;

    var obj = JObject.Parse(result);
    var content = obj["choices"][0]["message"]["content"];
    var response = content.ToString();

    response.ReplaceLineEndings().Output();
}