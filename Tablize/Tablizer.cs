using System.Text;

namespace Tablize;

public record Table
{
	public string Name { get; set; } = string.Empty;
	public bool ShowTableName { get; set; } = true;
	public bool ShowColumnNames { get; set; } = true;
	// Key is ColumnIndex
	internal Dictionary<int, Column> Columns { get; set; } = [];
	// <RowIndex, <ColumnIndex, Cell Value>
	//internal Dictionary<int, Dictionary<int, string>> Rows { get; set; } = [];
	internal List<List<string>> Rows { get; set; } = [];
	internal List<List<object>> UserData { get; set; } = [];
	public BorderCharSet? BorderCharSet { get; set; } = BorderCharSet.Single;

	internal int Width => Columns.Sum(x => x.Value.Width);
}

public record Column
{
	public string Name { get; set; } = string.Empty;
	public Align Align { get; set; } = Align.Left;
	public int PaddingLeft { get; set; } = 1;
	public int PaddingRight { get; set; } = 1;

	// @TODO: max width

	internal int Width { get; set; } = 0; // includes padding
	internal int Padding => PaddingLeft + PaddingRight;
}

public enum Align
{
	Left,
	Right,
	Center
}

public record Border(string value);

public record BorderCharSet(
	char HorizontalLine,
	char VerticalLine,
	char CornerUL,
	char CornerUR,
	char CornerLL,
	char CornerLR,
	char HorizontalIntersectionUp,
	char HorizontalIntersectionDown,
	char VerticalIntersectionRight,
	char VerticalIntersectionLeft,
	char Intersection)
{
	public static BorderCharSet Empty => new(
		' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ');

	public static BorderCharSet Single => new(
		'─', '│', '┌', '┐', '└', '┘', '┴', '┬', '├', '┤', '┼');

	public static BorderCharSet Double => new(
		'═', '║', '╔', '╗', '╚', '╝', '╩', '╦', '╠', '╣', '╬');
}

public static class Tablizer
{
	public static Table SetColumns(this Table table, List<Column> columns)
	{
		table.Columns.Clear();

		for (int i = 0; i < columns.Count; i++)
		{
			table.Columns.Add(i, columns[i]);
		}

		return table.SetData(table.UserData);
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
		table.UserData = rows;
		table.Rows.Clear();

		if (rows.Count == 0) return table;

		// pre-process
		foreach (var row in rows)
		{
			for (int k = 0; k < row.Count; k++)
			{
				// use default if columns not set
				if (!table.Columns.TryGetValue(k, out var column))
				{
					column = new();
					table.Columns.Add(k, column);
				}
			}
		}

		// set headers
		if (table.ShowTableName)
		{
		}

		for (int i = 0; i < rows.Count; i++)
		{
			var rowValues = new List<string>();

			var row = rows[i];
			for (int k = 0; k < row.Count; k++)
			{
				var column = table.Columns[k];
				var cell = row[k];

				// @TODO: formatters. for now .ToString() to get all values
				var value = cell.ToString() ?? string.Empty;

				// @TODO: Width needs to take into account column.Name and MaxWidth
				if (value.Length > column.Width - column.Padding)
				{
					column.Width = value.Length + column.Padding;
				}

				rowValues.Add(value);
			}

			table.Rows.Add(rowValues);
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

		foreach (var rowValues in table.Rows)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < rowValues.Count; i++)
			{
				var value = rowValues[i];
				var column = table.Columns[i];
				sb.Append(PadValue(value, column));
			}
			lines.Add(sb.ToString());
		}
		return lines;
	}

	private static string PadValue(string value, Column column) =>
		PadValue(value, column.Width, column.PaddingLeft, column.PaddingRight, column.Align);

	private static string PadValue(
		string value,
		int width,
		int paddingLeft,
		int paddingRight,
		Align align,
		char paddingChar = ' ')
	{
		var valueWidth = width - paddingLeft - paddingRight;

		value = align switch
		{
			Align.Left => value.PadRight(valueWidth),
			Align.Center => value.PadLeft(valueWidth / 2 - value.Length / 2).PadRight(valueWidth),
			Align.Right => value.PadLeft(valueWidth),
			_ => throw new Exception($"unknown column.Align: {align}")
		};

		return GetPadding(paddingLeft, paddingChar) + value + GetPadding(paddingRight, paddingChar);
	}

	private static string GetPadding(int padding, char paddingChar = ' ')
	{
		return new string([.. Enumerable.Repeat(paddingChar, padding)]);
	}
}
