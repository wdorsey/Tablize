using System.Text;

namespace Tablize;

public record Table
{
	public string Name { get; set; } = string.Empty;
	public bool ShowTableName { get; set; }
	public bool ShowColumnNames { get; set; }
	// Key is ColumnIndex
	public Dictionary<int, Column> Columns { get; set; } = [];
	// <RowIndex, <ColumnIndex, Cell Value>
	public Dictionary<int, Dictionary<int, string>> Rows { get; set; } = [];
}

public record Column
{
	public string Name { get; set; } = string.Empty;
	public int Width { get; set; } // includes padding
	public int PaddingLeft { get; set; }
	public int PaddingRight { get; set; }
	public int Padding => PaddingLeft + PaddingRight;
	public Align Align { get; set; }
}

public enum Align
{
	Left,
	Right,
	Center
}

public static class Tablizer
{
	public static Table CreateTable(
		string name,
		bool showName,
		bool showColumnNames)
	{
		return new()
		{
			Name = name,
			ShowTableName = showName,
			ShowColumnNames = showColumnNames
		};
	}

	public static Table SetData<T>(this Table table, Dictionary<string, T> data)
	{
		var rows = new List<List<object>>();
		foreach (var (k, v) in data)
		{
			rows.Add([k, v]);
		}

		return table.SetData(rows);
	}

	public static Table SetData(this Table table, List<List<object>> rows)
	{
		table.Columns.Clear();
		table.Rows.Clear();
		for (int i = 0; i < rows.Count; i++)
		{
			if (!table.Rows.TryGetValue(i, out var cellDict))
			{
				cellDict = [];
				table.Rows.Add(i, cellDict);
			}

			var row = rows[i];
			for (int k = 0; k < row.Count; k++)
			{
				if (!table.Columns.TryGetValue(k, out var column))
				{
					column = new()
					{
						// @TODO: column settings: Name, Padding, Align
						Name = k.ToString(),
						Width = 0,
						PaddingLeft = 1,
						PaddingRight = 1,
						Align = Align.Left,
					};
					table.Columns.Add(k, column);
				}

				var cell = row[k];
				// @TODO: for now just doing .ToString() to get all values
				var value = cell.ToString() ?? string.Empty;

				// @TODO: Width needs to take into account column.Name
				if (value.Length > column.Width - column.Padding)
				{
					column.Width = value.Length + column.Padding;
				}

				cellDict.Add(k, value);
			}
		}

		return table;
	}

	public static List<string> GetLines(this Table table)
	{
		var lines = new List<string>();

		// @TODO: HeaderValue type (string Name, Align Align)
		// for both table name and column names
		if (table.ShowTableName)
		{
			lines.Add($" {table.Name}");
		}

		if (table.ShowColumnNames)
		{
			var sb = new StringBuilder();
			foreach (var (_, column) in table.Columns)
			{
				sb.Append(PadValue(column.Name, column));
			}
			lines.Add(sb.ToString());
		}

		foreach (var (i, row) in table.Rows)
		{
			var sb = new StringBuilder();
			foreach (var (k, value) in row)
			{
				var column = table.Columns[k];
				sb.Append(PadValue(value, column));
			}
			lines.Add(sb.ToString());
		}
		return lines;
	}

	private static string PadValue(string value, Column column)
	{
		var valueWidth = column.Width - column.Padding;

		value = column.Align switch
		{
			Align.Left => value.PadRight(valueWidth),
			Align.Center => value.PadRight(valueWidth), // @TODO
			Align.Right => value.PadLeft(valueWidth),
			_ => throw new Exception($"unknown column.Align: {column.Align}")
		};

		return column.GetPaddingLeft() + value + column.GetPaddingRight();
	}

	private static string GetPaddingLeft(this Column column) => GetPadding(column.PaddingLeft);
	private static string GetPaddingRight(this Column column) => GetPadding(column.PaddingRight);
	private static string GetPadding(int padding)
	{
		return new string([.. Enumerable.Repeat(' ', padding)]);
	}
}
