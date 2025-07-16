using Tablize;
using Tablize.Console;

ConsoleUtil.InitializeConsole(1900, 1000);
Console.ForegroundColor = ConsoleColor.Green;
static void PrintTable(Table t) => t.GetLines().ForEach(Console.WriteLine);

var table = new Table
{
	Name = "Table",
	ShowTableName = true,
	ShowColumnNames = true,
	NameHeaderAlignment = Align.Left,
}.SetColumns(
[
	new Column { Name = "Key", NameHeaderAlignment = Align.Center },
	new Column { Name = "Value", NameHeaderAlignment = Align.Center, Alignment = Align.Right },
]);

table.SetData(new Dictionary<string, object>
{
	{ "one", "Hello" },
	{ "2", "Hello World!" },
	{ "on", "Hello W" },
	{ "Hello", "World!" },
	{ "unefois", "Hello" },
	{ "42", "69" },
	{ "Demon", "Slayer" }
});

PrintTable(table);

table.SetColumns(
[
	new() { Name = "One" },
	new() { Name = "Two" },
	new() { Name = "Three" },
	new() { Name = "Four" },
]);

var objs = new List<(string Name, int Count, double Avg, DateTimeOffset DateTime)>
{
	new ("One", 1, 5.42, DateTimeOffset.Now),
	new ("Two", 10, 42.0, DateTimeOffset.Now),
	new ("Three", 42, 23, DateTimeOffset.Now),
	new ("Four", 14889, 10.10, DateTimeOffset.Now),
	new ("Five", 238974, 32.23, DateTimeOffset.Now),
};

table.SetData([.. objs.Select(x => new List<object>
{
	x.Name,
	x.Count,
	x.Avg,
	x.DateTime
})]);

PrintTable(table);