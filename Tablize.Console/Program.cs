using Tablize;
using Tablize.Console;

ConsoleUtil.InitializeConsole(1900, 1000);

var stringDict = new Dictionary<string, string>
{
	{ "one", "Hello" },
	{ "2", "Hello World!" },
	{ "on", "Hello W" },
	{ "Hello", "World!" },
	{ "unefois", "Hello" },
	{ "42", "69" },
	{ "Demon", "Slayer" }
};

var table = new Table
{
	Name = "SUperduperlongnamingway Table"
}.SetColumns(
[
	new Column { Name = "Key" },
	new Column { Name = "Value", MaxWidth = 10, Align = Align.Right }
]).SetData(stringDict);

Console.WriteLine(table.ToJson(true));

Console.WriteLine("== TABLE ==");

foreach (var line in table.GetLines())
{
	Console.WriteLine(line);
}
