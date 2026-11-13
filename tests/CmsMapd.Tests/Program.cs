using CmsMapd.Core;

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var fixture = File.ReadAllLines(Path.Combine(root,"samples/enrollment-tc61.mbit.txt"))[0];
var enrollment = new MbitEnrollment(fixture);
var clock = new DateTime(2026,10,9,15,30,45).AddTicks(1234500);
var generator = new DtrrGenerator();
ReplyContext Context(IReadOnlyDictionary<string,string>? fields=null) => new(clock,"SIM00000000000000001",fields);
int count=0;
void Check(string name,Action action) { action(); count++; Console.WriteLine("PASS " + name); }
void Equal<T>(T expected,T actual) { if (!Equals(expected,actual)) throw new Exception($"Expected '{expected}', found '{actual}'."); }
void Reject(Action action) { try { action(); } catch (FormatException) { return; } throw new Exception("Expected FormatException."); }
string Read(string r,int start,int width) => r.Substring(start-1,width);
var accepted = generator.Generate(enrollment,ReplyCatalog.Select("11"),Context());
Check("acceptance creates T and P",()=>Equal(2,accepted.Records.Count));
Check("every record is 800 printable ASCII characters",()=>
{
 foreach(var r in accepted.Records) { Equal(800,r.Length); Equal(true,r.All(c=>c is >= ' ' and <= '~')); }
});
var t=accepted.Records[0]; var p=accepted.Records[1];
Check("identity copied exactly",()=>Equal(fixture[..41],t[..41]));
Check("contract moved to 43",()=>Equal("H9999",Read(t,43,5)));
Check("reply and transaction code positions",()=> {Equal("011",Read(t,57,3));Equal("61",Read(t,60,2));});
Check("effective date moved one column left",()=>Equal("20270101",Read(t,63,8)));
Check("PBP moved to 72",()=>Equal("001",Read(t,72,3)));
Check("transaction date uses processing clock",()=>Equal("20261009",Read(t,76,8)));
Check("processing timestamp",()=>Equal("15.30.45.12345 ",Read(t,163,15)));
Check("primary 4Rx mapping",()=> {Equal("012345",Read(t,279,6));Equal("TEST-RX-ID-001".PadRight(20),Read(t,310,20));});
Check("new contact/OEC fields",()=> {Equal("N",Read(t,527,1));Equal("2025550123",Read(t,548,10));Equal("jane@example.com".PadRight(74),Read(t,558,74));});
Check("unknown CMS values blank",()=>{Equal(" ",Read(t,62,1));Equal("   ",Read(t,158,3));Equal(new string(' ',16),Read(t,138,16));});
Check("acceptance reports submitted uncovered months",()=>Equal("000",Read(t,186,3)));
Check("relationship omitted for non-394/395/396",()=>Equal(new string(' ',7),Read(t,510,7)));
Check("verbatim full 600 bytes preserved",()=>Equal(fixture,Read(p,48,600)));
Check("verbatim accepted status",()=>Equal("A",Read(p,648,1)));
Check("tracking correlation",()=>{Equal(Read(t,475,20),Read(p,649,20));Equal(Read(t,495,15),Read(p,669,15));Equal("TEST-TC61-00001".PadRight(15),Read(t,495,15));});
Check("fillers and TRC union blank",()=>{Equal(new string(' ',12),Read(t,85,12));Equal(new string(' ',169),Read(t,632,169));Equal(new string(' ',117),Read(p,684,117));});
Check("rejection status and conditional fields",()=>
{
 var rejected=generator.Generate(enrollment,ReplyCatalog.Select("102"),Context(new Dictionary<string,string>{{"CumulativeUncoveredMonths","005"},{"CreditableCoverageFlag","N"}}));
 Equal("102",Read(rejected.Records[0],57,3));Equal("R",Read(rejected.Records[1],648,1));Equal("   ",Read(rejected.Records[0],186,3));Equal("    ",Read(rejected.Records[0],158,4));
});
Check("informational reply accompanies acceptance with shared ID",()=>
{
 var multiple=generator.Generate(enrollment,ReplyCatalog.Select("134,011"),Context());
 Equal(3,multiple.Records.Count);Equal("011",Read(multiple.Records[0],57,3));Equal("134",Read(multiple.Records[1],57,3));Equal(Read(multiple.Records[0],475,20),Read(multiple.Records[1],475,20));
});
Check("explicit CMS context written",()=>Equal("Y",Read(generator.Generate(enrollment,ReplyCatalog.Select("011"),Context(new Dictionary<string,string>{{"EntitlementTypeCode","Y"}})).Records[0],62,1)));
Check("SEP included only for eligible selection",()=>
{
 var changed=fixture.ToCharArray();changed[45]='S';changed[100]='0';changed[101]='1';
 var sep=new MbitEnrollment(new string(changed));
 Equal("01",Read(generator.Generate(sep,ReplyCatalog.Select("011"),Context()).Records[0],108,2));
 Equal("  ",Read(generator.Generate(sep,ReplyCatalog.Select("102"),Context()).Records[0],108,2));
});
Check("header accepted and omitted",()=>Equal(1,MbitReader.Read(new[]{"AAAAAAHEADER".PadRight(600),fixture}).Count));
Check("batch has multiple details",()=>Equal(2,MbitReader.Read(new[]{fixture,fixture}).Count));
Check("header only rejected",()=>Reject(()=>MbitReader.Read(new[]{"AAAAAAHEADER".PadRight(600)})));
Check("header in wrong position rejected",()=>Reject(()=>MbitReader.Read(new[]{fixture,"AAAAAAHEADER".PadRight(600)})));
Check("short/long/non-ASCII input rejected",()=> {Reject(()=>new MbitEnrollment(fixture[..599]));Reject(()=>new MbitEnrollment(fixture+" "));Reject(()=>new MbitEnrollment("é"+fixture[1..]));});
Check("other TC rejected",()=>Reject(()=>new MbitEnrollment(fixture[..59]+"51"+fixture[61..])));
Check("business-invalid input remains available for rejection scenarios",()=>Equal(600,new MbitEnrollment(new string(' ',59)+"61"+new string(' ',539)).Raw.Length));
Check("unsupported failed/maintenance TRCs rejected",()=>{Reject(()=>ReplyCatalog.Select("003"));Reject(()=>ReplyCatalog.Select("121"));});
Check("invalid scenario combinations rejected",()=>{foreach(var choice in new[]{"011,102","134","102,134","011,011","","ABC","00011"}) Reject(()=>ReplyCatalog.Select(choice));});
Check("field overflow and Unicode rejected",()=>{Reject(()=>new RecordBuilder(10).Put(1,3,"abcd"));Reject(()=>new RecordBuilder(10).Put(1,3,"é"));});
Check("bad profile and tracking IDs rejected",()=> {Reject(()=>generator.Generate(enrollment,ReplyCatalog.Select("011"),Context(new Dictionary<string,string>{{"Typo","x"}})));Reject(()=>generator.Generate(enrollment,ReplyCatalog.Select("011"),new(clock,"short")));});
Console.WriteLine($"{count} checks passed.");
