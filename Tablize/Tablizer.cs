using System.Text;

namespace Tablize;

public record Table
{
	public string Name { get; set; } = string.Empty;
	public bool ShowTableName { get; set; } = true;
	public bool ShowColumnNames { get; set; } = true;
	public BorderCharSet? BorderCharSet { get; set; } = BorderCharSet.Single;
	public bool HasBorders => BorderCharSet != null;

	// Key is ColumnIndex
	internal Dictionary<int, Column> Columns { get; } = [];
	internal List<List<string>> Rows { get; } = [];
	internal List<List<object>> UserRows { get; } = [];
	internal List<List<string>> FormattedRows { get; } = [];
	internal List<string> Lines { get; } = [];
	// Key is the index within the row
	internal List<Dictionary<int, BorderIntersection>> Intersections { get; } = [];

	internal int TotalColumnWidth => Columns.Sum(x => x.Value.Width);
	internal int Width => TotalColumnWidth + (HasBorders ? 1 + Columns.Count : 0);
}

public record Column
{
	public string Name { get; set; } = string.Empty;
	public Align Align { get; set; } = Align.Left;
	public int PaddingLeft { get; set; } = 1;
	public int PaddingRight { get; set; } = 1;
	public int? MaxWidth { get; set; } // includes padding, not borders

	internal int ContentWidth { get; set; }
	internal int Width => ContentWidth + Padding;
	internal int Padding => PaddingLeft + PaddingRight;
}

public enum Align
{
	Left,
	Right,
	Center
}

internal record BorderIntersection
{
	public bool Up;
	public bool Down;
	public bool Left;
	public bool Right;
}

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
			var col = columns[i];
			col.ContentWidth = col.Name.Length;
			table.Columns.Add(i, col);
		}

		return table.SetData(table.UserRows);
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
		table.UserRows.Clear();
		table.UserRows.AddRange(rows);
		table.Rows.Clear();
		table.FormattedRows.Clear();

		// pre-process
		foreach (var row in rows)
		{
			var formattedRow = new List<string>();
			for (int k = 0; k < row.Count; k++)
			{
				// use default if columns not set
				if (!table.Columns.TryGetValue(k, out var column))
				{
					column = new();
					table.Columns.Add(k, column);
				}

				// @TODO: formatters. for now .ToString() to get all values
				var value = row[k].ToString() ?? string.Empty;

				if (value.Length > column.ContentWidth)
				{
					column.ContentWidth = value.Length;
					if (column.MaxWidth.HasValue && column.MaxWidth < column.Width)
						column.ContentWidth = column.MaxWidth.Value - column.Padding;
				}

				formattedRow.Add(value);
			}
			table.FormattedRows.Add(formattedRow);
		}

		// header rows
		if (table.ShowTableName)
		{
			var width = table.HasBorders ? table.Width - 2 : table.Width;
			var value = GetCellValue(
				table.Name,
				width,
				width,
				1, 1, Align.Left);
			table.Rows.Add([value]);

			// borders
			var top = new Dictionary<int, BorderIntersection>
			{
				{ 0, new BorderIntersection {Down = true, Right = true} },
				{ table.Width - 1, new BorderIntersection {Down = true, Left = true} },
			};
			var bottom = new Dictionary<int, BorderIntersection>
			{
				{ 0, new BorderIntersection {Up = true, Right = true} },
				{ table.Width - 1, new BorderIntersection {Up = true, Left = true} },
			};
			table.Intersections.Add(top);
			table.Intersections.Add(bottom);
		}

		if (table.ShowColumnNames)
		{
			var row = new List<string>();
			var idx = 0;
			foreach (var (_, column) in table.Columns)
			{
				var value = GetCellValue(column.Name, column);
				row.Add(value);
			}
			table.Rows.Add(row);
		}

		// data rows
		foreach (var row in table.FormattedRows)
		{
			var rowValues = new List<string>();
			for (int k = 0; k < row.Count; k++)
			{
				var value = row[k];
				var col = table.Columns[k];
				rowValues.Add(GetCellValue(value, col));
			}
			table.Rows.Add(rowValues);
		}

		// borders
		if (table.HasBorders && table.Rows.Count > 0)
		{
			// result will be the new table.Rows
			var horizontalBorders = new List<string>();
			var charset = table.BorderCharSet!;

			foreach (var row in table.Intersections)
			{
				var sb = new StringBuilder();
				var prev = 0;
				foreach (var (i, intersection) in row)
				{
					sb.Append(new string([.. Enumerable.Repeat(charset.HorizontalLine, i - prev)]));
					sb.Append(GetChar(intersection, charset));
					prev = i;
				}
				horizontalBorders.Add(sb.ToString());
			}

			var borderIndex = 1;
			table.Lines.Add(horizontalBorders.First());
			foreach (var row in table.Rows)
			{
				var sb = new StringBuilder();
				foreach (var cell in row)
				{
					sb.Append(charset.VerticalLine);
					sb.Append(cell);
				}
				sb.Append(charset.VerticalLine);
				table.Lines.Add(sb.ToString());
				if (horizontalBorders.Count > borderIndex)
					table.Lines.Add(horizontalBorders[borderIndex++]);
			}
		}

		return table;
	}

	public static List<string> GetLines(this Table table)
	{
		return [.. table.Lines];
	}

	private static string GetCellValue(string value, Column column) =>
		GetCellValue(value, column.ContentWidth, column.MaxWidth, column.PaddingLeft, column.PaddingRight, column.Align);

	private static string GetCellValue(
		string value,
		int contentWidth,
		int? maxWidth,
		int paddingLeft,
		int paddingRight,
		Align align,
		char paddingChar = ' ')
	{
		var padding = paddingLeft + paddingRight;
		if (maxWidth.HasValue && contentWidth > maxWidth - padding)
		{
			value = new string([.. value.Take(maxWidth.Value - padding)]);
		}

		value = align switch
		{
			Align.Left => value.PadRight(contentWidth),
			Align.Center => value.PadLeft(contentWidth / 2 - value.Length / 2).PadRight(contentWidth),
			Align.Right => value.PadLeft(contentWidth),
			_ => throw new Exception($"unknown column.Align: {align}")
		};

		return GetPadding(paddingLeft, paddingChar) + value + GetPadding(paddingRight, paddingChar);
	}

	private static string GetPadding(int padding, char paddingChar = ' ')
	{
		return new string([.. Enumerable.Repeat(paddingChar, padding)]);
	}

	private static char GetChar(BorderIntersection i, BorderCharSet charset)
	{
		if (i.Up && i.Down && i.Left && i.Right)
			return charset.Intersection;
		if (i.Up && i.Down && i.Left)
			return charset.VerticalIntersectionLeft;
		if (i.Up && i.Down && i.Right)
			return charset.VerticalIntersectionRight;
		if (i.Up && i.Left && i.Right)
			return charset.HorizontalIntersectionUp;
		if (i.Down && i.Left && i.Right)
			return charset.HorizontalIntersectionDown;
		if (i.Up && i.Left)
			return charset.CornerLR;
		if (i.Up && i.Right)
			return charset.CornerLL;
		if (i.Down && i.Left)
			return charset.CornerUR;
		if (i.Down && i.Right)
			return charset.CornerUL;

		throw new Exception($"unhandled intersection: Up:{i.Up} Down:{i.Down} Left:{i.Left} Right:{i.Right}");
	}
}
