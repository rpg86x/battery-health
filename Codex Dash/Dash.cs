using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

class Entry {
 public string Category, Name, Status, Source, Detail;
 public string Identity;
 public string Key { get { return Identity ?? Category+"|"+Name+"|"+Source+"|"+Status+"|"+Detail; } }
 public Entry(string c,string n,string s,string src,string d) { Category=c;Name=n;Status=s;Source=src;Detail=d; }
}
class Priorities {
 public Dictionary<string,int> Scores=new Dictionary<string,int>();
 public string FilePath, Error="";
 public Priorities(string path) { FilePath=path;try {if(File.Exists(path)) Scores=new JavaScriptSerializer().Deserialize<Dictionary<string,int>>(File.ReadAllText(path)) ?? new Dictionary<string,int>(); Scores=Scores.Where(p=>p.Value>=1 && p.Value<=10).ToDictionary(p=>p.Key,p=>p.Value);}catch {Error="Saved scores could not be loaded. File preserved; saving disabled.";} }
 public static int Suggested(Entry e) {
  if(e.Status=="danger-full-access" || (e.Name=="approval_policy" && e.Status=="never")) return 10;
  if(e.Status=="Unknown" || e.Status=="Unparsed") return 9;
  if(e.Status=="Unverified" || (e.Name=="network_access" && e.Status=="true")) return 8;
  if(e.Status=="allow") return 7;
  if(e.Status=="Configured writable root" || e.Status=="Configured allow") return 6;
  if(e.Status=="Enabled" || e.Status=="Configured" || e.Status=="Saved chat write root") return 5;
  if(e.Status=="trusted") return 4;
  if(e.Status=="Disabled" || e.Status=="forbidden" || e.Status=="Configured deny") return 2;
  return 3;
 }
 public int? User(Entry e) {int value;return Scores.TryGetValue(e.Key,out value)?(int?)value:null;}
 public double? Average(Entry e) {var v=User(e);return v.HasValue?(Suggested(e)+v.Value)/2.0:(double?)null;}
 public double Rank(Entry e) {return Average(e) ?? Suggested(e);}
 public void Set(Entry e,int? score) {
  if(Error!="") throw new IOException(Error);
  if(score.HasValue && (score.Value<1 || score.Value>10)) throw new ArgumentOutOfRangeException("score");
  var next=new Dictionary<string,int>(Scores);if(score.HasValue) next[e.Key]=score.Value;else next.Remove(e.Key);
  string temp=FilePath+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(next));
  if(File.Exists(FilePath)) File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);Scores=next;
 }
}
class Reader {
 public List<Entry> Items = new List<Entry>();
 public string Home;
 public Reader(string home) { Home=home; }
 void Add(string c,string n,string s,string src,string d) { Items.Add(new Entry(c,n,s,src,d)); }
 public void Load() {
  Items.Clear(); ReadConfig(); ReadState(); ReadRules();
  Add("Websites","Browser and connected-account access","Unverified","Codex settings","Browser permissions, signed-in accounts and remote connectors have separate controls. Local configuration cannot establish their current access. Review them in Codex Settings > Plugins / Connections and browser settings.");
 }
 void ReadConfig() {
  string file=Path.Combine(Home,"config.toml");
  try {
   string section=""; string[] lines=File.ReadAllLines(file);
   foreach(string raw in lines) {
    string line=raw.Trim();
    if(line.StartsWith("[")) { section=line; continue; }
    Match pair=Regex.Match(line,@"^([A-Za-z_]+)\s*=\s*(.*)"); if(!pair.Success) continue;
    string key=pair.Groups[1].Value, value=pair.Groups[2].Value;
    if(section.StartsWith("[projects.") && key=="trust_level") {
     string name=section.Substring(10).TrimEnd(']').Trim('\'', '"');
     Add("Folders",name,value.Trim('"'),file,"Project trust setting. Trust is not a permanent read/write grant; each chat's sandbox and approvals still apply.");
    } else if((section.StartsWith("[plugins.") && key=="enabled") || (section.StartsWith("[mcp_servers.") && !section.EndsWith(".env]"))) {
     if(key=="enabled" || key=="command" || key=="url") {
      string name=section.Substring(section.IndexOf('.')+1).TrimEnd(']').Trim('"');
      Add("Apps",name,key=="enabled" ? (value.StartsWith("true")?"Enabled":"Disabled") : "Configured",file,"Plugin / MCP configuration. Enabled or configured does not verify account connection or tool approval. Credentials and server URLs are not displayed.");
     }
    } else if(key=="sandbox_mode" || key=="approval_policy" || key=="approvals_reviewer" || key=="network_access" || (section=="[windows]" && key=="sandbox")) {
     Add("Overview",key,value.Split('#')[0].Trim().Trim('"'),file,"Saved configuration in " + (section==""?"global scope":section) + ". Profile, project, chat and organization overrides may change the effective setting.");
    } else if(key=="writable_roots") {
     foreach(Match m in Regex.Matches(value,"\"([^\"]+)\"|'([^']+)'")) Add("Folders",m.Groups[1].Success?m.Groups[1].Value.Replace("\\\\","\\"):m.Groups[2].Value,"Configured writable root",file,"Saved writable root in " + section + ". Current chat access may differ.");
    } else if(key=="allowed_domains" || key=="denied_domains") {
     foreach(Match m in Regex.Matches(value,"\"([^\"]+)\"|'([^']+)'")) Add("Websites",m.Groups[1].Success?m.Groups[1].Value:m.Groups[2].Value,key=="allowed_domains"?"Configured allow":"Configured deny",file,"Saved network domain rule in " + section + ". Applies to that network profile, not every browser or connector.");
    }
   }
   Add("Overview","Permission scope","Per chat",file,"This dashboard reads saved local records. It does not evaluate effective sandbox permissions or organization policy. Missing values are unknown, not denied. No permission settings are changed.");
  } catch(Exception ex) { Add("Overview","Configuration unavailable","Unknown",file,ex.GetType().Name+": could not read configuration."); }
 }
 void ReadState() {
  string file=Path.Combine(Home,".codex-global-state.json");
  try {
   var j=new JavaScriptSerializer(); j.MaxJsonLength=64*1024*1024;
   var state=j.DeserializeObject(File.ReadAllText(file)) as Dictionary<string,object>;
   if(state==null) throw new FormatException();
   object roots;
   if(state.TryGetValue("thread-writable-roots",out roots)) {
    var map=roots as Dictionary<string,object>;
    if(map!=null) foreach(var p in map) {
     var values=p.Value as IEnumerable; if(values==null || p.Value is string) continue;
     foreach(var root in values) if(root is string) Add("Folders",(string)root,"Saved chat write root",file,"Recorded for chat " + p.Key + ". May belong to a past chat; not proof of current access.");
    }
   }
  } catch(Exception ex) { Add("Overview","Saved chat folders unavailable","Unknown",file,ex.GetType().Name+": could not read desktop state."); }
 }
 void ReadRules() {
  string dir=Path.Combine(Home,"rules");
  if(!Directory.Exists(dir)) { Add("Commands","No local rule folder","Unknown",dir,"No persistent local command rules found. One-time and managed approvals are not included."); return; }
  foreach(string file in Directory.GetFiles(dir,"*.rules")) try {
   int lineNo=0;
   foreach(string line in File.ReadLines(file)) {
    lineNo++; if(line.TrimStart().StartsWith("#") || String.IsNullOrWhiteSpace(line)) continue;
    Match m=Regex.Match(line,@"prefix_rule\s*\(\s*pattern\s*=\s*(\[.*?\])\s*,\s*decision\s*=\s*""(allow|prompt|forbidden)""");
    if(!m.Success) { Add("Commands","Rule at line "+lineNo,"Unparsed",file,"Unsupported rule format. Review this rule in Codex; it has not been classified."); continue; }
    try {
     string[] args=new JavaScriptSerializer().Deserialize<string[]>(m.Groups[1].Value);
     if(args.Length==0) throw new FormatException();
     // Only display the executable, never script bodies or embedded credentials.
     Add("Commands",args[0],m.Groups[2].Value,file,"Persistent command prefix rule at line " + lineNo + ". " + args.Length + " prefix parts. Only the executable is shown; arguments are hidden for privacy. This applies to the recorded prefix, not arbitrary use of this app. Other rules and sandbox restrictions still apply.");
     using(var hash=System.Security.Cryptography.SHA256.Create()) Items[Items.Count-1].Identity="command|"+file+"|"+m.Groups[2].Value+"|"+Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(args))));
    } catch { Add("Commands","Rule at line "+lineNo,"Unparsed",file,"Could not parse this command prefix safely."); }
   }
  } catch(Exception ex) { Add("Commands",Path.GetFileName(file),"Unknown",file,ex.GetType().Name+": could not read rules."); }
 }
}
class Dash : Form {
 static Color Bg=Color.FromArgb(16,23,25), Card=Color.FromArgb(25,35,37), Green=Color.FromArgb(152,228,180), Muted=Color.FromArgb(155,175,166);
 Reader reader; Priorities priorities; List<Entry> shown=new List<Entry>(), ranked=new List<Entry>(); bool filling;
 ComboBox statusFilter, sortOrder, pageSize; Label pageInfo; Button previous, next; int page;
 TableLayoutPanel top; ToolTip tips=new ToolTip();
 ComboBox category; TextBox search; DataGridView grid; TextBox detail; Label summary, refreshed;
 public Dash() {
  Text="Codex Dash"; BackColor=Bg; ForeColor=Color.WhiteSmoke; Font=new Font("Segoe UI",10); ClientSize=new Size(1180,860); MinimumSize=new Size(1040,760); StartPosition=FormStartPosition.CenterScreen;
  Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  string home=Environment.GetEnvironmentVariable("CODEX_HOME"); if(String.IsNullOrEmpty(home)) home=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex"); reader=new Reader(home);
  priorities=new Priorities(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"priority-scores.json"));
  Text="Codex Dash 1.1";
  var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=10};
  foreach(int h in new[]{48,58,118,32,40,40}) layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
  layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,80));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); Controls.Add(layout);
  var header=new Label {Text="Codex Dash",Dock=DockStyle.Fill,Font=new Font("Segoe UI",25,FontStyle.Bold),ForeColor=Green};layout.Controls.Add(header);
  summary=new Label {Dock=DockStyle.Fill,BackColor=Color.FromArgb(35,60,51),Padding=new Padding(10),ForeColor=Green,Font=new Font("Segoe UI",11)};layout.Controls.Add(summary);
  top=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=3,RowCount=1};for(int i=0;i<3;i++) top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));layout.Controls.Add(top);
  layout.Controls.Add(new Label {Text="Prioriteit 1–10: hoger = meer aandacht. Vul Jouw score in; leeg = voorlopig mijn score. Gemiddelde = (mijn + jouw) / 2.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Muted});
  var tools=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};layout.Controls.Add(tools);
  category=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,Width=150,BackColor=Card,ForeColor=Color.WhiteSmoke}; category.Items.AddRange(new object[]{"All","Overview","Folders","Apps","Websites","Commands"});category.SelectedIndex=0;
  search=new TextBox {Width=260,BackColor=Card,ForeColor=Color.WhiteSmoke}; tools.Controls.Add(category);tools.Controls.Add(new Label {Text="Search",Width=58,Padding=new Padding(0,5,0,0)});tools.Controls.Add(search);
  var refresh=new Button {Text="Refresh",Width=90,Height=30,FlatStyle=FlatStyle.Flat,BackColor=Card};tools.Controls.Add(refresh);
  var clear=new Button {Text="Wis filters",Width=100,Height=30,FlatStyle=FlatStyle.Flat,BackColor=Card};tools.Controls.Add(clear);
  var filters=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};layout.Controls.Add(filters);
  filters.Controls.Add(new Label {Text="Status",Width=55,Padding=new Padding(0,5,0,0)});
  statusFilter=new ComboBox {Width=185,DropDownStyle=ComboBoxStyle.DropDownList};statusFilter.Items.AddRange(new object[]{"Alle statussen","Aandacht nodig","Nog niet beoordeeld","Door mij beoordeeld"});statusFilter.SelectedIndex=0;filters.Controls.Add(statusFilter);
  filters.Controls.Add(new Label {Text="Sorteren",Width=75,Padding=new Padding(10,5,0,0)});
  sortOrder=new ComboBox {Width=220,DropDownStyle=ComboBoxStyle.DropDownList};sortOrder.Items.AddRange(new object[]{"Prioriteit hoog → laag","Prioriteit laag → hoog","Naam A → Z","Naam Z → A","Categorie","Mijn score hoog → laag","Jouw score hoog → laag","Gemiddelde hoog → laag"});sortOrder.SelectedIndex=0;filters.Controls.Add(sortOrder);
  filters.Controls.Add(new Label {Text="Per pagina",Width=95,Padding=new Padding(10,5,0,0)});
  pageSize=new ComboBox {Width=65,DropDownStyle=ComboBoxStyle.DropDownList};pageSize.Items.AddRange(new object[]{10,20,50});pageSize.SelectedIndex=0;filters.Controls.Add(pageSize);
  grid=new DataGridView {Dock=DockStyle.Fill,BackgroundColor=Card,BorderStyle=BorderStyle.None,ReadOnly=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,EnableHeadersVisualStyles=false,AutoGenerateColumns=false};
  grid.DefaultCellStyle.BackColor=Card;grid.DefaultCellStyle.ForeColor=Color.WhiteSmoke;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(43,70,57);grid.DefaultCellStyle.SelectionForeColor=Green;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
  grid.ColumnHeadersDefaultCellStyle.BackColor=Bg;grid.ColumnHeadersDefaultCellStyle.ForeColor=Muted;grid.ColumnHeadersHeight=36;grid.RowTemplate.Height=48;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;grid.GridColor=Color.FromArgb(43,56,58);
  grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Category",FillWeight=20});grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Folder / app / website",FillWeight=85});grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Recorded status",FillWeight=37});layout.Controls.Add(grid);
  grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Mijn score",FillWeight=20,MinimumWidth=85});grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Jouw score",FillWeight=20,MinimumWidth=85});grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText="Gemiddelde",FillWeight=23,MinimumWidth=95});
  foreach(DataGridViewColumn col in grid.Columns) {col.ReadOnly=col.Index!=4;col.SortMode=DataGridViewColumnSortMode.NotSortable;}
  grid.Columns[4].DefaultCellStyle.BackColor=Color.FromArgb(42,57,52);grid.Columns[5].DefaultCellStyle.ForeColor=Green;
  var pager=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};layout.Controls.Add(pager);
  previous=new Button {Text="<",Width=48,Height=30,FlatStyle=FlatStyle.Flat,BackColor=Card};next=new Button {Text=">",Width=48,Height=30,FlatStyle=FlatStyle.Flat,BackColor=Card};pageInfo=new Label {Width=480,Height=30,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Muted};pager.Controls.Add(previous);pager.Controls.Add(pageInfo);pager.Controls.Add(next);
  detail=new TextBox {Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Bg,ForeColor=Muted,BorderStyle=BorderStyle.None};layout.Controls.Add(detail);
  refreshed=new Label {Dock=DockStyle.Fill,ForeColor=Muted,Font=new Font("Segoe UI",8)};layout.Controls.Add(refreshed);
  category.SelectedIndexChanged+=delegate {page=0;Filter();};search.TextChanged+=delegate {page=0;Filter();};refresh.Click+=delegate {if(grid.EndEdit()) Reload();};grid.SelectionChanged+=delegate {ShowDetail();};
  statusFilter.SelectedIndexChanged+=delegate {page=0;Filter();};sortOrder.SelectedIndexChanged+=delegate {page=0;Filter();};pageSize.SelectedIndexChanged+=delegate {page=0;Filter();};
  previous.Click+=delegate {if(grid.EndEdit()) {page--;Filter();}};next.Click+=delegate {if(grid.EndEdit()) {page++;Filter();}};
  clear.Click+=delegate {search.Clear();category.SelectedIndex=0;statusFilter.SelectedIndex=0;sortOrder.SelectedIndex=0;page=0;Filter();};
  foreach(var combo in new[]{category,statusFilter,sortOrder,pageSize}) StyleCombo(combo); grid.CellValidating+=ValidateScore;grid.CellEndEdit+=SaveScore;Reload();
 }
 void Reload() { reader.Load(); summary.Text=reader.Items.Count(e=>e.Category=="Folders")+" folder records     " + reader.Items.Count(e=>e.Category=="Apps")+" app settings     "+reader.Items.Count(e=>e.Category=="Commands")+" command rules\r\nLocal view only  •  Current permissions can differ by chat";refreshed.Text="Updated "+DateTime.Now.ToString("HH:mm:ss")+"  |  Read-only  |  No account keys or chat messages accessed"; Filter(); }
 void StyleCombo(ComboBox combo) {
  combo.DrawMode=DrawMode.OwnerDrawFixed;combo.BackColor=Card;combo.ForeColor=Color.WhiteSmoke;combo.ItemHeight=22;
  combo.DrawItem+=delegate(object sender,DrawItemEventArgs a) {a.DrawBackground();int index=a.Index>=0?a.Index:combo.SelectedIndex;if(index>=0) TextRenderer.DrawText(a.Graphics,Convert.ToString(combo.Items[index]),combo.Font,a.Bounds,Color.WhiteSmoke,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);a.DrawFocusRectangle();};
 }
 void Filter() {
  if(grid==null) return;string c=Convert.ToString(category.SelectedItem), q=search.Text.Trim();
  string selected=grid.CurrentRow!=null && grid.CurrentRow.Index<shown.Count?shown[grid.CurrentRow.Index].Key:null;
  var matches=reader.Items.Where(e=>(c=="All"||e.Category==c) && (e.Name+" "+e.Status+" "+e.Category+" "+e.Source+" "+e.Detail).IndexOf(q,StringComparison.OrdinalIgnoreCase)>=0 && (statusFilter.SelectedIndex==0 || (statusFilter.SelectedIndex==1 && priorities.Rank(e)>=7) || (statusFilter.SelectedIndex==2 && !priorities.User(e).HasValue) || (statusFilter.SelectedIndex==3 && priorities.User(e).HasValue))).ToList();
  ranked=matches.OrderByDescending(e=>priorities.Rank(e)).ThenBy(e=>e.Name,StringComparer.OrdinalIgnoreCase).ThenBy(e=>e.Key,StringComparer.Ordinal).ToList();
  IOrderedEnumerable<Entry> sorted;
  switch(sortOrder.SelectedIndex) {
   case 1:sorted=matches.OrderBy(e=>priorities.Rank(e));break;
   case 2:sorted=matches.OrderBy(e=>e.Name,StringComparer.OrdinalIgnoreCase);break;
   case 3:sorted=matches.OrderByDescending(e=>e.Name,StringComparer.OrdinalIgnoreCase);break;
   case 4:sorted=matches.OrderBy(e=>e.Category);break;
   case 5:sorted=matches.OrderByDescending(e=>Priorities.Suggested(e));break;
   case 6:sorted=matches.OrderByDescending(e=>priorities.User(e)??-1);break;
   case 7:sorted=matches.OrderByDescending(e=>priorities.Average(e)??-1);break;
   default:sorted=matches.OrderByDescending(e=>priorities.Rank(e));break;
  }
  var ordered=sorted.ThenBy(e=>e.Name,StringComparer.OrdinalIgnoreCase).ThenBy(e=>e.Key,StringComparer.Ordinal).ToList();
  int size=Convert.ToInt32(pageSize.SelectedItem),pages=Math.Max(1,(ordered.Count+size-1)/size);page=Math.Max(0,Math.Min(page,pages-1));shown=ordered.Skip(page*size).Take(size).ToList();
  filling=true;grid.Rows.Clear();foreach(var e in shown) {int index=grid.Rows.Add(e.Category,e.Name,e.Status,Priorities.Suggested(e),priorities.User(e),priorities.Average(e).HasValue?priorities.Average(e).Value.ToString("0.0"):"—");int rank=ranked.IndexOf(e);if(rank<3) {grid.Rows[index].DefaultCellStyle.BackColor=Color.FromArgb(34,55,46);grid.Rows[index].Cells[1].Value="#"+(rank+1)+"  "+e.Name;} }
  previous.Enabled=page>0;next.Enabled=page<pages-1;pageInfo.Text="Toegangslog • Pagina "+(page+1)+" / "+pages+"   |   "+(ordered.Count==0?"Geen resultaten":(page*size+1)+"–"+(page*size+shown.Count)+" van "+ordered.Count);
  int keep=shown.FindIndex(e=>e.Key==selected);if(keep>=0) grid.CurrentCell=grid.Rows[keep].Cells[4];filling=false;BuildTop();ShowDetail();
 }
 void BuildTop() {
  while(top.Controls.Count>0) {var control=top.Controls[0];top.Controls.Remove(control);control.Dispose();}
  for(int i=0;i<Math.Min(3,ranked.Count);i++) {var e=ranked[i];var card=new Button {Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(35,60,51),ForeColor=Green,TextAlign=ContentAlignment.TopLeft,Padding=new Padding(8),Margin=new Padding(3,6,8,6),Font=new Font("Segoe UI",10,FontStyle.Bold),AutoEllipsis=true};
   string title=e.Name.Length>34?e.Name.Substring(0,31)+"…":e.Name;var avg=priorities.Average(e);card.Text="TOP "+(i+1)+"   •   "+(avg.HasValue?avg.Value.ToString("0.0")+" gemiddeld":Priorities.Suggested(e)+" voorlopig")+"\r\n"+title+"\r\nMijn: "+Priorities.Suggested(e)+"  |  Jouw: "+(priorities.User(e).HasValue?priorities.User(e).Value.ToString():"nog invullen");
   tips.SetToolTip(card,e.Name);card.Click+=delegate {if(!grid.EndEdit()) return;search.Text=e.Name;page=0;Filter();int index=shown.FindIndex(item=>item.Key==e.Key);if(index>=0) {grid.CurrentCell=grid.Rows[index].Cells[4];grid.Focus();}};top.Controls.Add(card,i,0);
  }
 }
 void ValidateScore(object sender,DataGridViewCellValidatingEventArgs a) {
  if(filling || a.ColumnIndex!=4) return;int value;string text=Convert.ToString(a.FormattedValue).Trim();if(text!="" && (!Int32.TryParse(text,out value)||value<1||value>10)) {a.Cancel=true;grid.Rows[a.RowIndex].ErrorText="Vul een heel getal van 1 tot 10 in, of laat leeg.";}else grid.Rows[a.RowIndex].ErrorText="";
 }
 void SaveScore(object sender,DataGridViewCellEventArgs a) {
  if(filling || a.ColumnIndex!=4 || a.RowIndex<0 || a.RowIndex>=shown.Count) return;var e=shown[a.RowIndex];string value=Convert.ToString(grid.Rows[a.RowIndex].Cells[4].Value).Trim();int parsed;
  if(value!="" && (!Int32.TryParse(value,out parsed)||parsed<1||parsed>10)) return;
  try {priorities.Set(e,value==""?(int?)null:Int32.Parse(value));refreshed.Text="Jouw score opgeslagen • Top 3 geldt voor de huidige selectie • Codex-rechten worden niet gewijzigd";}catch(Exception ex) {MessageBox.Show("Opslaan mislukt. Je vorige score blijft bewaard.\r\n"+ex.Message,"Codex Dash");}
  BeginInvoke(new Action(Filter));
 }
 void ShowDetail() { if(grid.CurrentRow==null || grid.CurrentRow.Index>=shown.Count) {detail.Text="No matching records.";return;} var e=shown[grid.CurrentRow.Index]; detail.Text=e.Name+"\r\nPrioriteit: "+Priorities.Suggested(e)+"/10. Vaste inschatting: onbekende toegang eerst, daarna ruimere toegang; geen live AI- of veiligheidsanalyse.\r\n"+e.Detail+"\r\nSource: "+e.Source; }
 void TestNavigation() {
  reader.Items=Enumerable.Range(1,25).Select(n=>new Entry("Apps","app-"+n.ToString("000"),"Enabled","fixture","Test record")).ToList();page=0;sortOrder.SelectedIndex=2;Filter();
  if(shown.Count!=10 || shown[0].Name!="app-001" || previous.Enabled || !next.Enabled) throw new Exception("First page failed");
  page=2;Filter();if(shown.Count!=5 || shown[0].Name!="app-021" || next.Enabled || !previous.Enabled) throw new Exception("Last page failed");
  search.Text="app-003";if(page!=0 || shown.Count!=1 || next.Enabled) throw new Exception("Search reset failed");
  search.Text="no-matches";if(shown.Count!=0 || previous.Enabled || next.Enabled || top.Controls.Count!=0) throw new Exception("Empty results failed");
  search.Clear();sortOrder.SelectedIndex=3;if(shown[0].Name!="app-025") throw new Exception("Descending sort failed");
  statusFilter.SelectedIndex=1;if(shown.Count!=0) throw new Exception("Attention filter failed");statusFilter.SelectedIndex=0;
  pageSize.SelectedIndex=1;if(shown.Count!=20) throw new Exception("Page size failed");
  priorities.Set(reader.Items[0],8);statusFilter.SelectedIndex=3;if(shown.Count!=1 || shown[0].Name!="app-001") throw new Exception("Rated filter failed");priorities.Set(reader.Items[0],null);
 }
 [STAThread] static void Main(string[] args) {
  if(args.Length>0 && args[0]=="--self-test") {
   string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-fixture");Directory.CreateDirectory(dir);Directory.CreateDirectory(Path.Combine(dir,"rules"));
   File.WriteAllText(Path.Combine(dir,"config.toml"),"sandbox_mode = \"workspace-write\"\n[projects.'C:\\demo']\ntrust_level = \"trusted\"\n[plugins.\"demo@test\"]\nenabled = false\n[network]\nallowed_domains = [\"example.com\"]\n");
   File.WriteAllText(Path.Combine(dir,".codex-global-state.json"),"{\"thread-writable-roots\":{\"chat-1\":[\"C:\\\\demo\"]}}");
   File.WriteAllText(Path.Combine(dir,"rules","test.rules"),"prefix_rule(pattern=[\"node\", \"private-argument\"], decision=\"allow\")\nunsupported_rule()\n");
   var r=new Reader(dir);r.Load();
   if(!r.Items.Any(e=>e.Name=="C:\\demo" && e.Status=="trusted") || !r.Items.Any(e=>e.Status=="Saved chat write root") || !r.Items.Any(e=>e.Status=="Disabled") || !r.Items.Any(e=>e.Name=="example.com" && e.Status=="Configured allow") || !r.Items.Any(e=>e.Name=="node" && e.Status=="allow") || !r.Items.Any(e=>e.Status=="Unparsed") || r.Items.Any(e=>(e.Name+e.Detail).Contains("private-argument"))) throw new Exception("Parser check failed");
   var missing=new Reader(Path.Combine(dir,"missing"));missing.Load();if(!missing.Items.Any(e=>e.Status=="Unknown")) throw new Exception("Missing-source check failed");
   var p=new Priorities(Path.Combine(dir,"scores.json"));var command=r.Items.First(e=>e.Name=="node");p.Set(command,null);
   if(p.Average(command).HasValue || p.Rank(command)!=7) throw new Exception("Unrated priority check failed");
   p.Set(command,10);if(p.Average(command)!=8.5 || new Priorities(p.FilePath).User(command)!=10) throw new Exception("Average or persistence failed");
   File.WriteAllText(Path.Combine(dir,"rules","test.rules"),"# added comment\nprefix_rule(pattern=[\"node\", \"private-argument\"], decision=\"allow\")\n");r.Load();if(p.User(r.Items.First(e=>e.Name=="node"))!=10) throw new Exception("Stable rule identity failed");
   bool rejected=false;try {p.Set(command,11);}catch(ArgumentOutOfRangeException) {rejected=true;}if(!rejected || p.User(command)!=10) throw new Exception("Validation failed");
   p.Set(command,null);if(new Priorities(p.FilePath).User(command).HasValue) throw new Exception("Clear score failed");
   File.WriteAllText(Path.Combine(dir,"broken.json"),"broken");var broken=new Priorities(Path.Combine(dir,"broken.json"));if(broken.Error=="") throw new Exception("Corrupt scores not reported");
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var f=new Dash()) { f.priorities=p;f.Filter();f.Show();Application.DoEvents();var target=f.shown.First();f.grid.CurrentCell=f.grid.Rows[0].Cells[4];f.grid.Rows[0].Cells[4].Value=10;f.SaveScore(f.grid,new DataGridViewCellEventArgs(4,0));Application.DoEvents();if(f.priorities.User(target)!=10) throw new Exception("UI score edit failed");int idx=f.shown.FindIndex(e=>e.Key==target.Key);f.grid.Rows[idx].Cells[4].Value=null;f.SaveScore(f.grid,new DataGridViewCellEventArgs(4,idx));Application.DoEvents();if(f.priorities.User(target).HasValue || f.top.Controls.Count!=Math.Min(3,f.shown.Count)) throw new Exception("UI rerank or clear failed");f.TestNavigation();f.Close(); }
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),"PASS: parser, privacy, score calculation, score persistence, stable rule identity, range validation, clear score, corrupt-file handling, UI edit / rerank / top-three cards.");return;
  }
  if(args.Length>0 && args[0]=="--preview") {Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var f=new Dash()) { f.Show();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(0,0,f.Width,f.Height));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png")); } f.Close(); }return;}
  if(args.Length>0 && args[0]=="--check") {var r=new Reader(args.Length>1?args[1]:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex"));r.Load();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"check.txt"),String.Join(Environment.NewLine,r.Items.GroupBy(e=>e.Category).Select(g=>g.Key+": "+g.Count())));return;}
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Dash());
 }
}
