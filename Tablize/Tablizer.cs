using System.Text;

namespace Tablize;

public record Table
{
	public string Name { get; set; } = string.Empty;
	public bool ShowTableName { get; set; } = true;
	public Align NameHeaderAlignment { get; set; } = Align.Left;
	public bool ShowColumnNames { get; set; } = true;
	/// <summary>
	/// Toggles all borders on or off
	/// </summary>
	public bool ShowBorders { get; set; } = true;
	public Border CellBorders { get; set; } = Border.Left | Border.Right;
	public BorderCharSet BorderCharSet { get; set; } = BorderCharSet.Single;

	internal Dictionary<Type, Formatter> Formatters { get; } = new()
	{
		{ typeof(object), Tablize.Formatters.Default },
		{ typeof(short), Tablize.Formatters.Short },
		{ typeof(int), Tablize.Formatters.Int },
		{ typeof(long), Tablize.Formatters.Long },
		{ typeof(double), Tablize.Formatters.Double },
		{ typeof(decimal), Tablize.Formatters.Decimal },
	};

	public int Width { get; internal set; }

	// Key is ColumnNumber
	internal Dictionary<int, Column> ColumnLookup { get; } = [];
	internal List<Row> Rows { get; } = [];
	internal List<List<object?>> UserRows { get; } = [];
	internal List<List<string>> FormattedRows { get; } = [];

	public List<Column> Columns => [.. ColumnLookup.Values];
}

public record Column
{
	public int ColumnNumber { get; internal set; }
	public string Name { get; set; } = string.Empty;
	public Align NameHeaderAlignment { get; set; } = Align.Center;
	public Align? Alignment { get; set; }
	internal Align? AlignmentByValue { get; set; } // internal Alignment tracked by the Type of value
	public Formatter? Formatter { get; set; }
	public int PaddingLeft { get; set; } = 1;
	public int PaddingRight { get; set; } = 1;

	/// <summary>
	/// Char to use for <see cref="PaddingLeft"/>
	/// </summary>
	public char PaddingLeftChar { get; set; } = ' ';
	/// <summary>
	/// Char to use for <see cref="PaddingRight"/>
	/// </summary>
	public char PaddingRightChar { get; set; } = ' ';
	/// <summary>
	/// Char to use to for padding the Cell values
	/// </summary>
	public char ValuePadChar { get; set; } = ' ';

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

public record Row(int RowNumber, List<Cell> Cells);

public record Cell(string Value, Border Border)
{
	public string Value { get; set; } = Value;
	public Border Border { get; set; } = Border;
}

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

public record Formatter(Func<object, string> FormatFn, Align Alignment = Align.Left);

public static class Formatters
{
	// pre-defined Formatters only exist for types that require something other than .ToString() and Align.Left
	public static Formatter Default { get; } = new(x => x?.ToString() ?? string.Empty, Align.Left);
	public static Formatter Short { get; } = new(x => Convert.ToInt16(x).ToString("N0"), Align.Right);
	public static Formatter Int { get; } = new(x => Convert.ToInt32(x).ToString("N0"), Align.Right);
	public static Formatter Long { get; } = new(x => Convert.ToInt64(x).ToString("N0"), Align.Right);
	public static Formatter Double { get; } = new(x => Convert.ToDouble(x).ToString("N2"), Align.Right);
	public static Formatter Decimal { get; } = new(x => Convert.ToDecimal(x).ToString("N2"), Align.Right);
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
	public static Table AddFormatter(this Table table, Formatter formatter, Type type)
	{
		table.Formatters[type] = formatter;
		return table;
	}

	public static Table AddFormatters(this Table table, List<(Formatter Formatter, Type Type)> formatters)
	{
		foreach (var (f, t) in formatters)
		{
			table.AddFormatter(f, t);
		}
		return table;
	}

	public static Table SetColumns(this Table table, List<Column> columns)
	{
		table.ColumnLookup.Clear();

		for (int i = 0; i < columns.Count; i++)
		{
			var col = columns[i];
			col.ColumnNumber = i + 1;
			col.InnerWidth = col.Name.Length;
			table.ColumnLookup.Add(col.ColumnNumber, col);
		}

		return table.SetData(table.UserRows);
	}

	public static Table SetData<TKey, TValue>(this Table table, Dictionary<TKey, TValue> data) where TKey : notnull
	{
		var rows = new List<List<object?>>();

		foreach (var (k, v) in data)
		{
			rows.Add([k, v]);
		}

		return table.SetData(rows);
	}

	public static Table SetData(this Table table, List<List<object?>> rows)
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
				// use default if column not set
				if (!table.ColumnLookup.TryGetValue(k + 1, out var col))
				{
					col = new()
					{
						ColumnNumber = k + 1
					};
					table.ColumnLookup.Add(col.ColumnNumber, col);
				}

				var value = row[k];

				// get formatter
				var val = string.Empty;
				if (value != null)
				{
					var formatter = col.Formatter;
					if (formatter == null)
					{
						formatter = table.Formatters.TryGetValue(value.GetType(), out var f)
							? formatter = f
							: Formatters.Default;
					}

					val = formatter.FormatFn(value);

					if (col.AlignmentByValue == null)
						col.AlignmentByValue = formatter.Alignment;
					// mixed value Types, use default alignment of Left
					else if (col.AlignmentByValue != formatter.Alignment)
						col.AlignmentByValue = Align.Left;
				}

				if (val.Length > col.InnerWidth)
				{
					col.InnerWidth = val.Length;

					if (col.MaxWidth.HasValue && col.MaxWidth < col.InnerWidth)
						col.InnerWidth = col.MaxWidth.Value;
				}

				if (col.MinWidth.HasValue && col.InnerWidth < col.MinWidth)
					col.InnerWidth = col.MinWidth.Value;

				formattedRow.Add(val);
			}
			table.FormattedRows.Add(formattedRow);
		}

		table.Width = table.ColumnLookup.Sum(x => x.Value.Width) +
			(table.ShowBorders ? BorderWidth([.. table.ColumnLookup.Values], table.CellBorders) : 0);

		// header rows
		if (table.ShowTableName)
		{
			int paddingLeft = 1, paddingRight = 1;

			var borderCount = table.ShowBorders ? 2 : 0;

			var headerWidth = table.Width - borderCount - paddingLeft - paddingRight;

			var value = GetCellValue(
				table.Name, headerWidth, headerWidth,
				paddingLeft, paddingRight, table.NameHeaderAlignment);

			table.Rows.Add(new(1, [new(value, Border.All)]));
		}

		if (table.ShowColumnNames)
		{
			var cells = new List<Cell>();
			foreach (var (_, col) in table.ColumnLookup)
			{
				var value = GetCellValue(
					col.Name, col.InnerWidth, col.MaxWidth,
					col.PaddingLeft, col.PaddingRight, col.NameHeaderAlignment);

				cells.Add(new(value, Border.All));
			}
			table.Rows.Add(new(table.Rows.Count + 1, cells));
		}

		// data rows
		for (int i = 0; i < table.FormattedRows.Count; i++)
		{
			var row = table.FormattedRows[i];
			var cells = new List<Cell>();
			for (int k = 0; k < row.Count; k++)
			{
				var rowValue = row[k];
				var col = table.ColumnLookup[k + 1];

				var value = GetCellValue(
					rowValue, col.InnerWidth, col.MaxWidth,
					col.PaddingLeft, col.PaddingRight,
					col.Alignment ?? col.Formatter?.Alignment ?? col.AlignmentByValue ?? Align.Left,
					col.PaddingLeftChar, col.PaddingRightChar, col.ValuePadChar);

				cells.Add(new(value, table.CellBorders));
			}
			table.Rows.Add(new(table.Rows.Count + 1, cells));
		}

		// ensure outside of table has borders
		if (table.Rows.Count > 0 && table.ShowBorders)
		{
			var first = table.Rows.First();
			foreach (var cell in first.Cells)
			{
				cell.Border |= Border.Top;
			}

			var last = table.Rows.Last();
			foreach (var cell in last.Cells)
			{
				cell.Border |= Border.Bottom;
			}

			foreach (var row in table.Rows)
			{
				if (row.Cells.Count > 0)
				{
					row.Cells.First().Border |= Border.Left;
					row.Cells.Last().Border |= Border.Right;
				}
			}
		}

		return table;
	}

	public static List<string> GetLines(this Table table)
	{
		var lines = new List<string>();

		// all the code in this function is for displaying borders.
		if (!table.ShowBorders)
			return [.. table.Rows.Select(x => string.Join(string.Empty, x.Cells.Select(c => c.Value)))];

		// the first-pass loop adds the "easy" borders in a dumb fashion.
		// it blindly adds all the vertical and horizontal borders which 
		// duplicates some of them and does not include intersections.
		// the second-pass will fix all of the dupes and add intersections.
		var firstPassLines = new List<(string Value, bool IsBorder)>();
		var chars = table.BorderCharSet;
		foreach (var row in table.Rows)
		{
			var sbBorderTop = new StringBuilder();
			var sb = new StringBuilder();
			var sbBorderBottom = new StringBuilder();
			var idx = 0;
			foreach (var cell in row.Cells)
			{
				if (cell.Border.IsEqual(Border.Left))
				{
					if (sb.Length == 0 || sb[^1] != chars.VerticalLine)
					{
						sbBorderTop.Append(' ');
						sbBorderBottom.Append(' ');
						sb.Append(chars.VerticalLine);
						idx++;
					}
				}

				if (cell.Border.IsEqual(Border.Top))
				{
					var diff = idx - sbBorderTop.Length;
					if (diff > 0)
						sbBorderTop.Append(Enumerable.Repeat(' ', diff).GetString());
					sbBorderTop.Append(Enumerable.Repeat(chars.HorizontalLine, cell.Value.Length).GetString());
				}

				if (cell.Border.IsEqual(Border.Bottom))
				{
					var diff = idx - sbBorderBottom.Length;
					if (diff > 0)
						sbBorderBottom.Append(Enumerable.Repeat(' ', diff).GetString());
					sbBorderBottom.Append(Enumerable.Repeat(chars.HorizontalLine, cell.Value.Length).GetString());
				}

				sb.Append(cell.Value);
				idx += cell.Value.Length;

				if (cell.Border.IsEqual(Border.Right))
				{
					sbBorderTop.Append(' ');
					sbBorderBottom.Append(' ');
					sb.Append(chars.VerticalLine);
					idx++;
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
				/* THAR BE DRAGONS */
				// (it just works, move along)
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
					else if (prev == chars.HorizontalLine && prevLineChar == chars.VerticalLine)
					{ c = chars.CornerLR; }
					// else just stay as ' '

					// debug
					// Console.WriteLine($"{i}\t{value}: {next}\t{prev}\t{prevLineChar}\t{nextLineChar}\t => {c}\n");
				}

				sb.Append(c);
				prev = c;
			}

			prevLine = [.. value];

			lines.Add(sb.ToString());
		}

		return lines;
	}

	private static string GetCellValue(
		string value,
		int contentWidth,
		int? maxWidth,
		int paddingLeft,
		int paddingRight,
		Align align,
		char paddingLeftChar = ' ',
		char paddingRightChar = ' ',
		char valuePadChar = ' ')
	{
		// this function creates the exact final cell value trimming and padding
		// based on widths and alignment.
		if (maxWidth.HasValue && value.Length > maxWidth)
		{
			contentWidth = maxWidth.Value;
			value = new string([.. value.Take(contentWidth)]);
		}

		value = align switch
		{
			Align.Left => value.PadRight(contentWidth, valuePadChar),
			Align.Center => value.PadLeft(value.Length + contentWidth / 2 - value.Length / 2, valuePadChar).PadRight(contentWidth, valuePadChar),
			Align.Right => value.PadLeft(contentWidth, valuePadChar),
			_ => throw new Exception($"unknown column.Align: {align}")
		};

		return GetPadding(paddingLeft, paddingLeftChar) + value + GetPadding(paddingRight, paddingRightChar);
	}

	private static int BorderWidth(List<Column> cols, Border cellBorders)
	{
		// assumes Table.HasBorder
		// start width at 2 for outside table border
		int width = 2;
		var prevHadRightBorder = true;
		foreach (var col in cols)
		{
			width += !prevHadRightBorder && cellBorders.IsEqual(Border.Left) ? 1 : 0;
			prevHadRightBorder = cellBorders.IsEqual(Border.Right);
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

	private static string GetPadding(int padding, char paddingChar)
	{
		return Enumerable.Repeat(paddingChar, padding).GetString();
	}

	private static string GetString(this IEnumerable<char> chars) => new([.. chars]);
}
