using System.Text;

namespace Tablize;

public record Table
{
	public string Name { get; set; } = string.Empty;
	public bool ShowTableName { get; set; } = true;
	public Align NameHeaderAlignment { get; set; } = Align.Left;
	public bool ShowColumnNames { get; set; } = true;
	public bool NoBorders { get; set; } = false;
	public BorderCharSet BorderCharSet { get; set; } = BorderCharSet.Single;

	public int Width { get; internal set; }

	// Key is ColumnIndex
	internal Dictionary<int, Column> ColumnLookup { get; } = [];
	internal List<List<Cell>> Rows { get; } = [];
	internal List<List<TableValue>> UserRows { get; } = [];
	internal List<List<string>> FormattedRows { get; } = [];

	public List<Column> Columns => [.. ColumnLookup.Values];
}

public record Column
{
	public int ColumnIndex { get; internal set; }
	public string Name { get; set; } = string.Empty;
	public Align NameHeaderAlignment { get; set; } = Align.Left;
	public Align? Alignment { get; set; }
	public int PaddingLeft { get; set; } = 1;
	public int PaddingRight { get; set; } = 1;
	public char PaddingChar { get; set; } = ' ';

	public Border Border { get; set; } = Border.Left | Border.Right;

	/// <summary>
	/// Min Inner/ContentWidth, does not include padding or borders
	/// </summary>
	public int? MinWidth { get; set; } = null;

	/// <summary>
	/// Max Inner/ContentWidth, does not include padding or borders
	/// </summary>
	public int? MaxWidth { get; set; } = null;

	/// <summary>
	/// Width of the cell content, no padding.
	/// </summary>
	public int InnerWidth { get; internal set; }
	public int Width => InnerWidth + Padding;
	public int Padding => PaddingLeft + PaddingRight;
}

public record Cell(string Value, Border Border)
{
	public string Value { get; set; } = Value;
	public Border Border { get; set; } = Border;
}

public record TableValue(string Value, Type Type);

public enum Align
{
	Left,
	Right,
	Center
}

[Flags]
public enum Border
{
	None = 0,
	Top = 1 << 0,
	Bottom = 1 << 1,
	Left = 1 << 2,
	Right = 1 << 3,
	All = 1 << 4
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
	public static TableValue Value<T>(this Table table, T? value)
	{
		// @TODO: formatters
		var type = value?.GetType() ?? typeof(string);
		return new(value?.ToString() ?? string.Empty, type);
	}

	public static Table SetColumns(this Table table, List<Column> columns)
	{
		table.ColumnLookup.Clear();

		for (int i = 0; i < columns.Count; i++)
		{
			var col = columns[i];
			col.InnerWidth = col.Name.Length;
			table.ColumnLookup.Add(i, col);
		}

		return table.SetData(table.UserRows);
	}

	public static Table SetData<T>(this Table table, Dictionary<string, T> data)
	{
		var rows = new List<List<TableValue>>();

		foreach (var (k, v) in data)
		{
			rows.Add([table.Value(k), table.Value(v)]);
		}

		return table.SetData(rows);
	}

	public static Table SetData(this Table table, List<List<object>> rows)
	{
		return table.SetData([.. rows.Select(x => x.Select(obj => table.Value(obj)).ToList())]);
	}

	public static Table SetData(this Table table, List<List<TableValue>> rows)
	{
		table.UserRows.Clear();
		table.FormattedRows.Clear();
		table.Rows.Clear();

		table.UserRows.AddRange(rows);

		// pre-processing loop:
		//	set default columns
		//  figure out width of every column
		//  get formatted values of each cell
		foreach (var row in rows)
		{
			var formattedRow = new List<string>();
			for (int k = 0; k < row.Count; k++)
			{
				// use default if columns not set
				if (!table.ColumnLookup.TryGetValue(k, out var column))
				{
					column = new();
					table.ColumnLookup.Add(k, column);
				}

				column.ColumnIndex = k;

				var value = row[k];

				if (value.Value.Length > column.InnerWidth)
				{
					column.InnerWidth = value.Value.Length;

					if (column.MaxWidth.HasValue && column.MaxWidth < column.InnerWidth)
						column.InnerWidth = column.MaxWidth.Value;
				}

				if (column.MinWidth.HasValue && column.InnerWidth < column.MinWidth)
					column.InnerWidth = column.MinWidth.Value;

				// @TODO: auto-detect via value.Type
				// Think value.Type should switch to Formatter
				// and the formatter would have to default Align
				column.Alignment ??= Align.Left;

				formattedRow.Add(value.Value);
			}
			table.FormattedRows.Add(formattedRow);
		}

		table.Width = table.ColumnLookup.Sum(x => x.Value.Width) +
			(table.NoBorders ? 0 : BorderWidth([.. table.ColumnLookup.Values]));

		// header rows
		if (table.ShowTableName)
		{
			int paddingLeft = 1, paddingRight = 1;

			var borderCount = table.NoBorders ? 0 : 2;

			var headerWidth = table.Width - borderCount - paddingLeft - paddingRight;

			var value = GetCellValue(
				table.Name, headerWidth, headerWidth,
				paddingLeft, paddingRight, table.NameHeaderAlignment);

			table.Rows.Add([new(value, Border.All)]);

			if (table.NoBorders)
				table.Rows.Add([]);
		}

		if (table.ShowColumnNames)
		{
			var row = new List<Cell>();
			foreach (var (_, col) in table.ColumnLookup)
			{
				var value = GetCellValue(
					col.Name, col.InnerWidth, col.MaxWidth,
					col.PaddingLeft, col.PaddingRight, col.NameHeaderAlignment);

				var border = table.NoBorders
					? Border.None
					: col.Border | Border.Top | Border.Bottom;

				row.Add(new(value, border));
			}
			table.Rows.Add(row);

			if (table.NoBorders)
				table.Rows.Add([]);
		}

		// data rows
		for (int i = 0; i < table.FormattedRows.Count; i++)
		{
			var row = table.FormattedRows[i];
			var rowValues = new List<Cell>();
			for (int k = 0; k < row.Count; k++)
			{
				var rowValue = row[k];
				var col = table.ColumnLookup[k];

				var value = GetCellValue(rowValue, col);

				rowValues.Add(new(value, col.Border));
			}
			table.Rows.Add(rowValues);
		}

		// ensure outside of table has borders
		if (table.Rows.Count > 0 && !table.NoBorders)
		{
			var first = table.Rows.First();
			foreach (var cell in first)
			{
				cell.Border |= Border.Top;
			}

			var last = table.Rows.Last();
			foreach (var cell in last)
			{
				cell.Border |= Border.Bottom;
			}

			foreach (var row in table.Rows)
			{
				if (row.Count > 0)
				{
					row.First().Border |= Border.Left;
					row.Last().Border |= Border.Right;
				}
			}
		}

		return table;
	}

	public static List<string> GetLines(this Table table)
	{
		var lines = new List<string>();

		if (table.NoBorders)
			return [.. table.Rows.Select(x => string.Join(string.Empty, x.Select(c => c.Value)))];

		// all the code in this function is for displaying borders.

		// the first-pass loop adds the "easy" borders in a dumb fashion.
		// it blindly adds all the vertical and horizontal borders but 
		// duplicates them and does not do intersections.
		// the second-pass will fix all of the dupes and add intersections.
		var firstPassLines = new List<(string Value, bool IsBorder)>();
		var chars = table.BorderCharSet;
		foreach (var row in table.Rows)
		{
			var sbBorderTop = new StringBuilder();
			var sb = new StringBuilder();
			var sbBorderBottom = new StringBuilder();
			foreach (var cell in row)
			{
				if (cell.Border.IsEqual(Border.Left))
				{
					if (sb.Length == 0 || sb[^1] != chars.VerticalLine)
					{
						sbBorderTop.Append(' ');
						sbBorderBottom.Append(' ');
						sb.Append(chars.VerticalLine);
					}
				}

				if (cell.Border.IsEqual(Border.Top))
				{
					sbBorderTop.Append(Enumerable.Repeat(chars.HorizontalLine, cell.Value.Length).GetString());
				}

				if (cell.Border.IsEqual(Border.Bottom))
				{
					sbBorderBottom.Append(Enumerable.Repeat(chars.HorizontalLine, cell.Value.Length).GetString());
				}

				sb.Append(cell.Value);

				if (cell.Border.IsEqual(Border.Right))
				{
					sbBorderTop.Append(' ');
					sbBorderBottom.Append(' ');
					sb.Append(chars.VerticalLine);
				}
			}

			var top = sbBorderTop.ToString();
			var cells = sb.ToString();
			var bottom = sbBorderBottom.ToString();

			if (!string.IsNullOrWhiteSpace(top))
				firstPassLines.Add((top, true));

			firstPassLines.Add((cells, false));

			if (!string.IsNullOrWhiteSpace(bottom))
				firstPassLines.Add((bottom, true));
		}

		// final border pass to fix dupes and add intersections
		// this code is really gnarly
		var prevLine = new List<char>();
		for (int i = 0; i < firstPassLines.Count; i++)
		{
			var (value, isBorder) = firstPassLines[i];

			if (!isBorder)
			{
				prevLine = [.. value];
				lines.Add(value);
				continue;
			}

			// skip the duped horizontal border lines
			var nextLineIsBorder = i < firstPassLines.Count - 1 && firstPassLines[i + 1].IsBorder;
			if (nextLineIsBorder) continue;

			var nextLine = i < firstPassLines.Count - 1 ? firstPassLines[i + 1].Value.ToList() : [];
			var sb = new StringBuilder();
			var prev = '0';
			for (int k = 0; k < value.Length; k++)
			{
				var c = value[k];
				char? next = k < value.Length - 1 ? value[k + 1] : null;
				char? prevLineChar = k < prevLine.Count ? prevLine[k] : null;
				char? nextLineChar = k < nextLine.Count ? nextLine[k] : null;
				if (c == ' ')
				{
					if (prev == chars.HorizontalLine && next == chars.HorizontalLine &&
						prevLineChar == chars.VerticalLine && nextLineChar == chars.VerticalLine)
					{ c = chars.Intersection; }
					else if (prev == chars.HorizontalLine && next == chars.HorizontalLine && prevLineChar == chars.VerticalLine)
					{ c = chars.HorizontalIntersectionUp; }
					else if (prev == chars.HorizontalLine && next == chars.HorizontalLine && nextLineChar == chars.VerticalLine)
					{ c = chars.HorizontalIntersectionDown; }
					else if (next == chars.HorizontalLine && prevLineChar == chars.VerticalLine && nextLineChar == chars.VerticalLine)
					{ c = chars.VerticalIntersectionRight; }
					else if (prev == chars.HorizontalLine && prevLineChar == chars.VerticalLine && nextLineChar == chars.VerticalLine)
					{ c = chars.VerticalIntersectionLeft; }
					else if (next == chars.HorizontalLine && nextLineChar == chars.VerticalLine)
					{ c = chars.CornerUL; }
					else if (next == chars.HorizontalLine && prevLineChar == chars.VerticalLine)
					{ c = chars.CornerLL; }
					else if (prev == chars.HorizontalLine && nextLineChar == chars.VerticalLine)
					{ c = chars.CornerUR; }
					else { c = chars.CornerLR; }
				}

				sb.Append(c);
				prev = c;
			}

			prevLine = [.. value];

			lines.Add(sb.ToString());
		}

		return lines;
	}

	private static string GetCellValue(string value, Column col) => GetCellValue(
		value, col.InnerWidth, col.MaxWidth,
		col.PaddingLeft, col.PaddingRight, col.Alignment ?? Align.Left, col.PaddingChar);

	private static string GetCellValue(
		string value,
		int contentWidth,
		int? maxWidth,
		int paddingLeft,
		int paddingRight,
		Align align,
		char paddingChar = ' ')
	{
		if (maxWidth.HasValue && value.Length > maxWidth)
		{
			contentWidth = maxWidth.Value;
			value = new string([.. value.Take(contentWidth)]);
		}

		value = align switch
		{
			Align.Left => value.PadRight(contentWidth),
			Align.Center => value.PadLeft(value.Length + contentWidth / 2 - value.Length / 2).PadRight(contentWidth),
			Align.Right => value.PadLeft(contentWidth),
			_ => throw new Exception($"unknown column.Align: {align}")
		};

		return GetPadding(paddingLeft, paddingChar) + value + GetPadding(paddingRight, paddingChar);
	}

	private static int BorderWidth(List<Column> cols)
	{
		// start width at 2 for outside table border
		int width = 2;
		var prevHadRightBorder = true;
		foreach (var col in cols)
		{
			width += !prevHadRightBorder && col.Border.IsEqual(Border.Left) ? 1 : 0;
			prevHadRightBorder = col.Border.IsEqual(Border.Right);
			width += prevHadRightBorder ? 1 : 0;
		}
		if (prevHadRightBorder) width--;
		return width;
	}

	private static bool IsEqual(this Border value, Border check)
	{
		return (value & check) == check ||
			(value & Border.All) == Border.All;
	}

	private static string GetPadding(int padding, char paddingChar = ' ')
	{
		return Enumerable.Repeat(paddingChar, padding).GetString();
	}

	private static string GetString(this IEnumerable<char> chars) => new([.. chars]);
}
