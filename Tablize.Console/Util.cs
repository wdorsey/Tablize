using Newtonsoft.Json;

namespace Tablize.Console;

internal static class Util
{
	public static string? ToJson(
			this object? obj,
			bool format = false,
			JsonSerializerSettings? settings = null)
	{
		if (obj == null)
			return null;

		settings ??= SettingsDefault();

		if (obj is string)
			return obj.ToString();

		return JsonConvert.SerializeObject(
			obj,
			format ? Formatting.Indented : Formatting.None,
			settings);
	}

	public static T? As<T>(this string? json)
	{
		if (string.IsNullOrEmpty(json))
			return default;

		return JsonConvert.DeserializeObject<T>(json, SettingsDefault());
	}

	public static T? As<T>(this object? obj)
	{
		if (obj == null)
			return default;

		if (typeof(T) == typeof(string))
			return (T)obj;

		var json = obj.ToJson();

		if (json == null)
			return default;

		return JsonConvert.DeserializeObject<T>(json, SettingsDefault());
	}

	public static JsonSerializerSettings SettingsDefault()
	{
		return new JsonSerializerSettings
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			DateParseHandling = DateParseHandling.DateTimeOffset,
			DateTimeZoneHandling = DateTimeZoneHandling.Local
		};
	}

	public static JsonSerializerSettings IgnoreNulls()
	{
		return SettingsDefault().IgnoreNulls();
	}

	public static JsonSerializerSettings IgnoreNulls(this JsonSerializerSettings settings)
	{
		settings.NullValueHandling = NullValueHandling.Ignore;

		return settings;
	}

	public static bool JsonCompare(this object obj1, object obj2)
	{
		var settings = SettingsDefault();
		settings.NullValueHandling = NullValueHandling.Ignore;

		return obj1.ToJson(false, settings) == obj2.ToJson(false, settings);
	}
}
