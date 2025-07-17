# Tablize
Turn any C# collection into a pretty ASCII table. Ideal for console applications that need to display tabular data.

<img width="787" height="210" alt="image" src="https://github.com/user-attachments/assets/9a75bead-102a-443c-985e-f02807bbbe81" />

The whole library is just a single file. Grab [Tablizer.cs](https://github.com/wdorsey/Tablize/blob/master/Tablize/Tablizer.cs) to start tablizing all the things.

For example usage, check out the [Console project](https://github.com/wdorsey/Tablize/blob/master/Tablize.Console/Program.cs).

## Quick Start
```C#
// create table
var table = new Table { Name = "Customers" };

// set columns (optional)
table.SetColumns([
	new Column { Name = "Id" },
	new Column { Name = "Name" }]);

// set data
// A dictionary with any key/value types works.
// Dictionaries always create a 2-column table.
table.SetData(new Dictionary<int, string> { { 1234, "John Smith" } });

// Or use a list. Desired properties must be explicitly passed.
// Lists allow you to create as many columns are you want,
// so let's add another column and throw in custom formatters.
table.SetColumns([
	new Column { Name = "Id", Formatter = Tablizer.Formatter<int>(x => x.ToString(), Align.Right) },
	new Column { Name = "Name" },
	new Column { Name = "SignupDate", Formatter = Tablizer.Formatter<DateTime>(x => x.ToString("MM/dd/yyyy")) }]);

var list = new List<(int Id, string Name, DateTime SignupDate)>
{
	new(1234, "John Smith", DateTime.Now)
};

table.SetData([.. list.Select(x =>
	new List<object?>
	{
		x.Id, x.Name, x.SignupDate
	})]);

// that's it, GetLines() and print it
List<string> lines = table.GetLines();
lines.ForEach(Console.WriteLine);
```
<img width="315" height="135" alt="image" src="https://github.com/user-attachments/assets/1bd776a7-732b-4388-89b0-194f1c3371b2" />
