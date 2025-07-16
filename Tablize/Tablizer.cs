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
	public Dictionary<int, Column> Columns { get; } = [];
	public List<List<Cell>> Rows { get; } = [];
	internal List<List<object>> UserRows { get; } = [];
	internal List<List<string>> FormattedRows { get; } = [];
	internal List<string> Lines { get; } = [];

	public int TotalColumnWidth => Columns.Sum(x => x.Value.Width);
	public int Width => TotalColumnWidth + (HasBorders ? 1 + Columns.Count : 0);
}

public record Column
{
	public string Name { get; set; } = string.Empty;
	public Align Align { get; set; } = Align.Left;
	public int PaddingLeft { get; set; } = 1;
	public int PaddingRight { get; set; } = 1;
	public int? MaxWidth { get; set; } // includes padding, not borders

	public int ContentWidth { get; internal set; }
	public int Width => ContentWidth + Padding;
	public int Padding => PaddingLeft + PaddingRight;
}

public enum Align
{
	Left,
	Right,
	Center
}

public record Cell(string Value, Border Border);

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
					if (column.MaxWidth.HasValue && column.MaxWidth < column.ContentWidth)
						column.ContentWidth = column.MaxWidth.Value;
				}

				formattedRow.Add(value);
			}
			table.FormattedRows.Add(formattedRow);
		}

		// header rows
		if (table.ShowTableName)
		{
			var width = (table.HasBorders ? table.Width - 2 : table.Width) - 2;

			var value = GetCellValue(
				table.Name,
				width,
				width,
				1, 1, Align.Left);

			table.Rows.Add([new(value, Border.All)]);
		}

		if (table.ShowColumnNames)
		{
			var row = new List<Cell>();
			foreach (var (_, column) in table.Columns)
			{
				var value = GetCellValue(column.Name, column);
				row.Add(new(value, Border.All));
			}
			table.Rows.Add(row);
		}

		// data rows
		for (int i = 0; i < table.FormattedRows.Count; i++)
		{
			var row = table.FormattedRows[i];
			var rowValues = new List<Cell>();
			for (int k = 0; k < row.Count; k++)
			{
				var rowValue = row[k];
				var col = table.Columns[k];
				var value = GetCellValue(rowValue, col);
				var border = Border.Left | Border.Right;
				if (i == 0) border |= Border.Top;
				if (i == table.FormattedRows.Count - 1) border |= Border.Bottom;
				rowValues.Add(new(value, border));
			}
			table.Rows.Add(rowValues);
		}

		return table;
	}

	public static List<string> GetLines(this Table table)
	{
		var lines = new List<string>();

		if (table.HasBorders)
		{
			var firstPassLines = new List<(string Value, bool IsBorder)>();
			var chars = table.BorderCharSet!;
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

			// fix borders
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
		}
		else
		{
			lines = [.. table.Rows.Select(x => string.Join(string.Empty, x.Select(c => c.Value)))];
		}

		return lines;
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
		if (maxWidth.HasValue && value.Length > maxWidth)
		{
			contentWidth = maxWidth.Value;
			value = new string([.. value.Take(contentWidth)]);
		}

		Console.WriteLine($"max = {maxWidth}, contentWidth = {contentWidth} => value.length = {value.Length}");

		value = align switch
		{
			Align.Left => value.PadRight(contentWidth),
			Align.Center => value.PadLeft(contentWidth / 2 - value.Length / 2).PadRight(contentWidth),
			Align.Right => value.PadLeft(contentWidth),
			_ => throw new Exception($"unknown column.Align: {align}")
		};

		return GetPadding(paddingLeft, paddingChar) + value + GetPadding(paddingRight, paddingChar);
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
