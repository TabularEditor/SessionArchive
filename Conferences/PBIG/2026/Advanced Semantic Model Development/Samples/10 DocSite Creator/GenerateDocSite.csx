// Tabular Editor C# Script — generates a single-file static documentation site
// Covers: Tables, Columns, Measures, Relationships (with live search)
// Author: Peer Grønnerup, Tabular Editor
var docPath = @"C:\PBIG2026\";

var sb = new System.Text.StringBuilder();

// ── Collect relationship data ───────────────────────────────────────────────
var relRows = new System.Text.StringBuilder();
foreach (var r in Model.Relationships.OfType<SingleColumnRelationship>())
{
    var active = r.IsActive ? "Active" : "Inactive";
    var cross  = r.CrossFilteringBehavior.ToString();
    var fromCard = r.FromCardinality.ToString();
    var toCard   = r.ToCardinality.ToString();
    relRows.AppendLine("<tr>"
        + "<td>" + Esc(r.FromTable.Name) + "</td>"
        + "<td>" + Esc(r.FromColumn.Name) + "</td>"
        + "<td>" + fromCard + "</td>"
        + "<td>" + Esc(r.ToTable.Name) + "</td>"
        + "<td>" + Esc(r.ToColumn.Name) + "</td>"
        + "<td>" + toCard + "</td>"
        + "<td>" + cross + "</td>"
        + "<td class='tag " + (r.IsActive ? "tag-active" : "tag-inactive") + "'>" + active + "</td>"
        + "</tr>");
}

// ── Collect table data ──────────────────────────────────────────────────────
var tableCards = new System.Text.StringBuilder();
foreach (var t in Model.Tables.OrderBy(t => t.Name))
{
    // Related tables
    var related = new System.Collections.Generic.List<string>();
    foreach (var r in Model.Relationships.OfType<SingleColumnRelationship>())
    {
        if (r.FromTable.Name == t.Name) related.Add(r.ToTable.Name);
        if (r.ToTable.Name == t.Name) related.Add(r.FromTable.Name);
    }
    var relatedHtml = related.Count > 0
        ? string.Join(" ", related.Distinct().OrderBy(x => x).Select(x => "<span class='rel-chip' onclick=\"scrollToTable('" + EscJs(x) + "')\">" + Esc(x) + "</span>"))
        : "<span class='muted'>None</span>";

    tableCards.AppendLine("<div class='card' data-table='" + EscAttr(t.Name) + "' id='table-" + EscAttr(t.Name) + "'>");
    tableCards.AppendLine("  <div class='card-header'>");
    tableCards.AppendLine("    <h2>" + Esc(t.Name) + (t.IsHidden ? " <span class='tag tag-hidden'>Hidden</span>" : "") + "</h2>");
    if (!string.IsNullOrWhiteSpace(t.Description))
        tableCards.AppendLine("    <p class='desc'>" + Esc(t.Description) + "</p>");
    tableCards.AppendLine("    <div class='related'>Related: " + relatedHtml + "</div>");
    tableCards.AppendLine("  </div>");

    // Columns
    var cols = t.Columns.OrderBy(c => c.Name).ToList();
    if (cols.Count > 0)
    {
        tableCards.AppendLine("  <details open><summary>Columns (" + cols.Count + ")</summary>");
        tableCards.AppendLine("  <table><thead><tr><th>Column</th><th>Data Type</th><th>Type</th><th>Hidden</th><th>Description</th></tr></thead><tbody>");
        foreach (var c in cols)
        {
            var colType = c.Type.ToString();
            tableCards.AppendLine("  <tr>"
                + "<td class='col-name'>" + Esc(c.Name) + (c.IsKey ? " <span class='tag tag-key'>PK</span>" : "") + "</td>"
                + "<td>" + c.DataType + "</td>"
                + "<td>" + colType + "</td>"
                + "<td>" + (c.IsHidden ? "Yes" : "") + "</td>"
                + "<td class='desc-cell'>" + Esc(c.Description) + "</td>"
                + "</tr>");
        }
        tableCards.AppendLine("  </tbody></table></details>");
    }

    // Measures
    var measures = t.Measures.OrderBy(m => m.Name).ToList();
    if (measures.Count > 0)
    {
        tableCards.AppendLine("  <details open><summary>Measures (" + measures.Count + ")</summary>");
        tableCards.AppendLine("  <table><thead><tr><th>Measure</th><th>Data Type</th><th>Format</th><th>Hidden</th><th>Folder</th><th>Expression</th></tr></thead><tbody>");
        foreach (var m in measures)
        {
            var expr = Esc(m.Expression).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
            tableCards.AppendLine("  <tr>"
                + "<td class='col-name'>" + Esc(m.Name) + "</td>"
                + "<td>" + m.DataType + "</td>"
                + "<td>" + Esc(m.FormatString) + "</td>"
                + "<td>" + (m.IsHidden ? "Yes" : "") + "</td>"
                + "<td>" + Esc(m.DisplayFolder) + "</td>"
                + "<td class='expr-cell'><code>" + expr + "</code></td>"
                + "</tr>");
        }
        tableCards.AppendLine("  </tbody></table></details>");
    }

    tableCards.AppendLine("</div>");
}

// ── Stats ───────────────────────────────────────────────────────────────────
var totalTables   = Model.Tables.Count();
var totalColumns  = Model.AllColumns.Count();
var totalMeasures = Model.AllMeasures.Count();
var totalRels     = Model.Relationships.Count();

// ── Build HTML ──────────────────────────────────────────────────────────────
sb.AppendLine(@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>" + Esc(Model.Database.Name) + @" — Data Model Docs</title>
<style>
:root {
  --bg: #f5f5f7; --bg2: #fff; --fg: #1d1d1f; --fg2: #6e6e73;
  --border: #d2d2d7; --accent: #0071e3; --accent-bg: #e8f2fe;
  --code-bg: #f0f0f2; --card-shadow: 0 1px 3px rgba(0,0,0,.08);
  --radius: 8px;
}
@media(prefers-color-scheme:dark){
  :root {
    --bg: #1c1c1e; --bg2: #2c2c2e; --fg: #f5f5f7; --fg2: #98989d;
    --border: #3a3a3c; --accent: #64a8ff; --accent-bg: #1a3050;
    --code-bg: #38383a; --card-shadow: 0 1px 3px rgba(0,0,0,.3);
  }
}
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
  background:var(--bg);color:var(--fg);line-height:1.5;padding:0}
header{background:var(--bg2);border-bottom:1px solid var(--border);padding:24px 32px;
  position:sticky;top:0;z-index:100;backdrop-filter:blur(10px)}
header h1{font-size:20px;font-weight:600;letter-spacing:-.3px}
header .meta{color:var(--fg2);font-size:13px;margin-top:2px}
.stats{display:flex;gap:24px;margin-top:12px;flex-wrap:wrap}
.stat{background:var(--accent-bg);color:var(--accent);padding:6px 14px;
  border-radius:20px;font-size:13px;font-weight:600}
.search-wrap{margin-top:14px;position:relative}
.search-wrap input{width:100%;max-width:480px;padding:10px 14px 10px 36px;
  border:1px solid var(--border);border-radius:var(--radius);font-size:14px;
  background:var(--bg);color:var(--fg);outline:none;transition:border .2s}
.search-wrap input:focus{border-color:var(--accent)}
.search-wrap svg{position:absolute;left:10px;top:50%;transform:translateY(-50%);
  width:16px;height:16px;color:var(--fg2)}
nav.tabs{display:flex;gap:0;margin-top:14px;border-bottom:2px solid var(--border)}
nav.tabs button{background:none;border:none;padding:8px 18px;font-size:14px;
  color:var(--fg2);cursor:pointer;border-bottom:2px solid transparent;
  margin-bottom:-2px;font-weight:500;transition:all .15s}
nav.tabs button:hover{color:var(--fg)}
nav.tabs button.active{color:var(--accent);border-bottom-color:var(--accent)}
main{max-width:1200px;margin:0 auto;padding:24px 32px 60px}
.section{display:none}
.section.active{display:block}
.card{background:var(--bg2);border:1px solid var(--border);border-radius:var(--radius);
  margin-bottom:16px;box-shadow:var(--card-shadow);overflow:hidden}
.card-header{padding:16px 20px}
.card-header h2{font-size:16px;font-weight:600}
.card-header .desc{color:var(--fg2);font-size:13px;margin-top:4px}
.related{font-size:12px;color:var(--fg2);margin-top:8px}
.rel-chip{display:inline-block;background:var(--accent-bg);color:var(--accent);
  padding:2px 8px;border-radius:10px;font-size:11px;margin:2px 2px;cursor:pointer;
  text-decoration:none;transition:opacity .15s}
.rel-chip:hover{opacity:.75}
details{border-top:1px solid var(--border)}
summary{padding:10px 20px;font-size:13px;font-weight:600;cursor:pointer;
  color:var(--fg2);user-select:none}
summary:hover{color:var(--fg)}
table{width:100%;border-collapse:collapse;font-size:13px}
thead{background:var(--bg)}
th{text-align:left;padding:8px 12px;font-weight:600;color:var(--fg2);
  border-bottom:1px solid var(--border);white-space:nowrap}
td{padding:8px 12px;border-bottom:1px solid var(--border);vertical-align:top}
tr:last-child td{border-bottom:none}
.col-name{font-weight:600;white-space:nowrap}
.desc-cell{color:var(--fg2);max-width:300px}
.expr-cell{max-width:420px}
.expr-cell code{font-family:'SF Mono',Consolas,'Liberation Mono',monospace;
  font-size:12px;background:var(--code-bg);padding:2px 6px;border-radius:4px;
  display:inline-block;word-break:break-word;white-space:pre-wrap}
.tag{display:inline-block;padding:1px 7px;border-radius:8px;font-size:11px;
  font-weight:600;margin-left:6px;vertical-align:middle}
.tag-hidden{background:#fee2e2;color:#b91c1c}
.tag-key{background:#dcfce7;color:#15803d}
.tag-active{background:#dcfce7;color:#15803d}
.tag-inactive{background:#fee2e2;color:#b91c1c}
@media(prefers-color-scheme:dark){
  .tag-hidden{background:#451a1a;color:#fca5a5}
  .tag-key{background:#14532d;color:#86efac}
  .tag-active{background:#14532d;color:#86efac}
  .tag-inactive{background:#451a1a;color:#fca5a5}
}
.muted{color:var(--fg2)}
.empty{text-align:center;padding:40px;color:var(--fg2)}
#rel-section table td,#rel-section table th{white-space:nowrap}
.hidden{display:none!important}
</style>
</head>
<body>
<header>
  <h1>" + Esc(Model.Database.Name) + @"</h1>
  <div class='meta'>Compatibility Level " + Model.Database.CompatibilityLevel + @" &middot; Generated " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + @"</div>
  <div class='stats'>
    <span class='stat'>" + totalTables + @" Tables</span>
    <span class='stat'>" + totalColumns + @" Columns</span>
    <span class='stat'>" + totalMeasures + @" Measures</span>
    <span class='stat'>" + totalRels + @" Relationships</span>
  </div>
  <div class='search-wrap'>
    <svg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='currentColor' stroke-width='2'><circle cx='11' cy='11' r='8'/><line x1='21' y1='21' x2='16.65' y2='16.65'/></svg>
    <input id='search' type='text' placeholder='Search tables, columns, measures...' autocomplete='off'>
  </div>
  <nav class='tabs'>
    <button class='active' data-tab='tables'>Tables</button>
    <button data-tab='relationships'>Relationships</button>
  </nav>
</header>

<main>
  <div id='tables-section' class='section active'>
" + tableCards.ToString() + @"
    <div id='no-results' class='empty hidden'>No matching tables found.</div>
  </div>

  <div id='rel-section' class='section'>
    <div class='card'>
      <table>
        <thead><tr>
          <th>From Table</th><th>From Column</th><th>From</th>
          <th>To Table</th><th>To Column</th><th>To</th>
          <th>Cross Filter</th><th>Status</th>
        </tr></thead>
        <tbody>
" + relRows.ToString() + @"
        </tbody>
      </table>
    </div>
  </div>
</main>

<script>
// Tab switching
document.querySelectorAll('.tabs button').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.tabs button').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
    document.getElementById(btn.dataset.tab === 'tables' ? 'tables-section' : 'rel-section').classList.add('active');
  });
});

// Search — matches only on names (table, column, measure), not expressions/descriptions
const searchInput = document.getElementById('search');
searchInput.addEventListener('input', function() {
  const q = this.value.toLowerCase().trim();
  const cards = document.querySelectorAll('.card[data-table]');
  let visible = 0;

  cards.forEach(card => {
    if (!q) { card.classList.remove('hidden'); card.querySelectorAll('tbody tr').forEach(r => r.classList.remove('hidden')); visible++; return; }

    // Check table name
    const tableName = (card.dataset.table || '').toLowerCase();
    const tableMatch = tableName.includes(q);

    // Check column/measure names only (cells with class col-name)
    let childMatch = false;
    card.querySelectorAll('tbody tr').forEach(row => {
      const nameCell = row.querySelector('.col-name');
      const name = nameCell ? nameCell.textContent.toLowerCase() : '';
      const rowMatch = name.includes(q);
      row.classList.toggle('hidden', !rowMatch && !tableMatch);
      if (rowMatch) childMatch = true;
    });

    const show = tableMatch || childMatch;
    card.classList.toggle('hidden', !show);
    if (show) visible++;
  });

  document.getElementById('no-results').classList.toggle('hidden', visible > 0 || !q);

  // Filter relationship rows by table/column names only (not cardinality, cross-filter, etc.)
  document.querySelectorAll('#rel-section tbody tr').forEach(row => {
    if (!q) { row.classList.remove('hidden'); return; }
    const cells = row.querySelectorAll('td');
    const names = [0,1,3,4].map(i => cells[i] ? cells[i].textContent.toLowerCase() : '').join(' ');
    row.classList.toggle('hidden', !names.includes(q));
  });
});

// Keyboard shortcut
document.addEventListener('keydown', e => {
  if ((e.metaKey || e.ctrlKey) && e.key === 'k') { e.preventDefault(); searchInput.focus(); searchInput.select(); }
  if (e.key === 'Escape') { searchInput.blur(); }
});

// Scroll to table
function scrollToTable(name) {
  // Switch to tables tab
  document.querySelectorAll('.tabs button').forEach(b => b.classList.remove('active'));
  document.querySelector('.tabs button[data-tab=""tables""]').classList.add('active');
  document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
  document.getElementById('tables-section').classList.add('active');

  const el = document.getElementById('table-' + CSS.escape(name));
  if (el) { el.scrollIntoView({ behavior: 'smooth', block: 'start' }); el.style.outline = '2px solid var(--accent)'; setTimeout(() => el.style.outline = '', 1500); }
}
</script>
</body>
</html>");

// ── Write file ──────────────────────────────────────────────────────────────
System.IO.File.WriteAllText(
    docPath + "DOCS_" + Model.Database.Name + ".html",
    sb.ToString(),
    System.Text.Encoding.UTF8
);

// ── Helper functions ────────────────────────────────────────────────────────
string Esc(string s) {
    if (string.IsNullOrEmpty(s)) return "";
    return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
}
string EscAttr(string s) {
    if (string.IsNullOrEmpty(s)) return "";
    return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;").Replace("'","&#39;");
}
string EscJs(string s) {
    if (string.IsNullOrEmpty(s)) return "";
    return s.Replace("\\","\\\\").Replace("'","\\'");
}