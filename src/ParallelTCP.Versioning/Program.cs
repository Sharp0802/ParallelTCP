using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

await using var stream = new FileStream("../ParallelTCP/Properties/AssemblyInfo.xml", FileMode.Open);

var docs = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, CancellationToken.None);
var all = docs.Descendants().ToArray();

var version = all.First(element => element.Name == "Version");
var asmVersion = all.First(element => element.Name == "AssemblyVersion");
var fileVersion = all.First(element => element.Name == "FileVersion");

var versions = Regex.Match(
    version.Value,
    "(?<major>\\d+)\\.(?<minor>\\d+)\\.(?<revision>\\d+)\\.(?<build>\\d+)",
    RegexOptions.Compiled).Groups;
var newVersion = $"{versions["major"]}.{versions["minor"]}.{versions["revision"]}.{int.Parse(versions["build"].Value) + 1}";

version.Value = newVersion;
asmVersion.Value = newVersion;
fileVersion.Value = newVersion;

stream.SetLength(0);
stream.Flush(true);

await docs.SaveAsync(stream, SaveOptions.OmitDuplicateNamespaces, CancellationToken.None);