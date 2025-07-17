using Tablize;
using Tablize.Console;

ConsoleUtil.InitializeConsole(1900, 1000);

Console.ForegroundColor = ConsoleColor.Green;
static void PrintTable(Table t) => t.GetLines().ForEach(Console.WriteLine);

/* examples for readme */

// create table
var table = new Table("Customers");

// set columns (optional),
// There are several different ways to add/set columns
table.SetColumns("Id", "Name");

// set data
// A dictionary with any key/value types works.
// Dictionaries always create a 2-column table.
var dataDict = new Dictionary<int, string> { { 1234, "John Smith" }, { 2, "Jane Doe" } };
table.SetData(dataDict);

// Or use a list. Desired properties must be explicitly passed.
// Lists allow you to create as many columns are you want,
// so let's add another column and throw in custom formatters.
table.SetColumns([
	new Column("Id") { Formatter = Tablizer.Formatter<int>(x => x.ToString(), Align.Right) },
	new Column("Name"),
	new Column("SignupDate") { Formatter = Tablizer.Formatter<DateTime>(x => x.ToString("MM/dd/yyyy")) }]);

// order of objects is the column order
table.SetData(dataDict.Select(x => new List<object?>
{
	x.Key,       // Id 
	x.Value,     // Name 
	DateTime.Now // SignupDate
}));

// that's it, GetLines() and print it
List<string> lines = table.GetLines();
lines.ForEach(Console.WriteLine);

/*  dictionary as data, 2-column table */
table = new Table
{
	Name = "Table",
	// default Table properties
	ShowTableName = true,
	NameHeaderAlignment = Align.Left,
	ShowColumnNames = true,
	ShowBorders = true,
	CellBorders = Border.Left | Border.Right, // all cells share the same border configuration, otherwise it just looks dumb, trust me.
	BorderCharSet = BorderCharSet.Single
}.SetColumns(
[
	new Column
	{
		// default Column properties
		Name = "Key",
		NameHeaderAlignment = Align.Center,
		Alignment = null,
		Formatter = null,
		PaddingLeft = 1,
		PaddingRight = 1,
		PaddingLeftChar = ' ',
		PaddingRightChar = ' ',
		ValuePadChar = ' ',
		MinWidth = null,
		MaxWidth = null
	},
	new Column
	{
		// customized properties
		Name = "Value",
		NameHeaderAlignment = Align.Left,
		Alignment = Align.Center,
		Formatter = Formatters.Default,
		PaddingLeft = 5,
		PaddingRight = 5,
		PaddingLeftChar = '>',
		PaddingRightChar = '<',
		ValuePadChar = '.',
		MinWidth = 10, // Min/MaxWidth do not include padding
		MaxWidth = 15  // If a cell value is longer than Max, it gets chopped off.
	},
]);

table.SetData(new Dictionary<string, object>
{
	{ "one", "Hello" },
	{ "42", 69 },
	{ "Hello", "World!" },
	{ "420", 69.69 },
	{ "DateTimeOffset", DateTimeOffset.Now },
	{ "TimeSpan", TimeSpan.FromHours(1) }
});

PrintTable(table);

/* list of objects as data, multi-column table */
table = new Table("Customers");

// override formatter for type int
table.AddFormatter<int>(x => x.ToString(), Align.Right);

// custom money fomatter, add to column three
var money = Tablizer.Formatter<double>(x => $"$ {x:N2}", Align.Right);

// use AddColumn
table.AddColumn("Id")
	.AddColumn("Name")
	.AddColumn("Count")
	.AddColumn(new Column("Balance") { Formatter = money })
	.AddColumn("Attempts")
	.AddColumn("LastUpdate");

// simulate getting customers from database
var objs = new List<(int Id, string Name, int Count, double Balance, long? Attempts, DateTimeOffset LastUpdate)>
{
	new (1, "John Smith", 1, 5.42, null, DateTimeOffset.Now),
	new (2, "Jane Doe", 10, 42.0, 1, DateTimeOffset.Now.AddSeconds(-23894234)),
	new (3, "John Smith", 42, 23, 32984789324, DateTimeOffset.Now.AddSeconds(-232344)),
	new (4, "John Smith", 14889, 10.10, null, DateTimeOffset.Now.AddSeconds(-23445)),
	new (5, "Jane Doe", 238974, 32.23, 69, DateTimeOffset.Now.AddSeconds(-333)),
};

table.SetData(objs.Select(x => new List<object?>
{
	x.Id,
	x.Name,
	x.Count,
	x.Balance,
	x.Attempts,
	x.LastUpdate
}));

PrintTable(table);
