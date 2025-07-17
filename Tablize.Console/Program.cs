using Tablize;
using Tablize.Console;

ConsoleUtil.InitializeConsole(1900, 1000);

Console.ForegroundColor = ConsoleColor.Green;
static void PrintTable(Table t) => t.GetLines().ForEach(Console.WriteLine);

/*  dictionary as data, 2-column table */
var table = new Table
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
table.Name = "Customers";

// override formatter for type int
table.AddFormatter(new(x => Convert.ToInt32(x).ToString(), Align.Right), typeof(int));

// custom money fomatter, add to column three
var money = new Formatter(x => $"$ {Convert.ToDouble(x):N2}", Align.Right);

table.SetColumns(
[
	new() { Name = "Id" },
	new() { Name = "Name" },
	new() { Name = "Count" },
	new() { Name = "Balance", Formatter = money },
	new() { Name = "Attempts" },
	new() { Name = "LastUpdate" },
]);

var objs = new List<(int Id, string Name, int Count, double Balance, long? Attempts, DateTimeOffset LastUpdate)>
{
	new (1, "John Smith", 1, 5.42, null, DateTimeOffset.Now),
	new (2, "Jane Doe", 10, 42.0, 1, DateTimeOffset.Now.AddSeconds(-23894234)),
	new (3, "John Smith", 42, 23, 32984789324, DateTimeOffset.Now.AddSeconds(-232344)),
	new (4, "John Smith", 14889, 10.10, null, DateTimeOffset.Now.AddSeconds(-23445)),
	new (5, "Jane Doe", 238974, 32.23, 69, DateTimeOffset.Now.AddSeconds(-333)),
};

table.SetData([.. objs.Select(x => new List<object?>
{
	x.Id,
	x.Name,
	x.Count,
	x.Balance,
	x.Attempts,
	x.LastUpdate
})]);

PrintTable(table);