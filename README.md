# Tablize
Turn any C# collection into a pretty ASCII table. Ideal for console applications that need to display tabular data.

<img width="787" height="210" alt="image" src="https://github.com/user-attachments/assets/9a75bead-102a-443c-985e-f02807bbbe81" />

The whole library is just a single file. Grab [Tablizer.cs](https://github.com/wdorsey/Tablize/blob/master/Tablize/Tablizer.cs) to start tablizing all the things.

For example usage, check out the [Console project](https://github.com/wdorsey/Tablize/blob/master/Tablize.Console/Program.cs).

## Quick Start
```C#
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
```
<img width="320" height="162" alt="image" src="https://github.com/user-attachments/assets/ab96a13b-adac-441d-a3a8-0bc2ceb30726" />
