using System;
using System.Collections.Generic;
using System.IO;
using Nez.Persistence;

namespace Nez.ImGuiTools.Persistence
{
	public static class ImGuiSettingsLoader
	{
		private static readonly string SettingsFilePath = GetSettingsFilePath();
		private static Dictionary<string, object> _settings;

		static ImGuiSettingsLoader()
		{
			LoadSettingsFromFile();
		}

		private static void LoadSettingsFromFile()
		{
			if (File.Exists(SettingsFilePath))
			{
				try
				{
					var json = File.ReadAllText(SettingsFilePath);
					_settings = Json.FromJson<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
				}
				catch
				{
					_settings = new Dictionary<string, object>();
				}
			}
			else
			{
				_settings = new Dictionary<string, object>();
			}
		}

		private static void SaveSettingsToFile()
		{
			try
			{
				var json = Json.ToJson(_settings);
				File.WriteAllText(SettingsFilePath, json);
			}
			catch
			{
				Debug.Log(Debug.LogType.Error, "Couldn't Save the Editor's Settings.");
			}
		}

		/// <summary>
		/// Save a setting value (e.g. ImGuiSettingsSaver.SaveSetting(_groupLogs, "GroupLogs"))
		/// </summary>
		public static void SaveSetting<T>(T value, string key)
		{
			_settings[key] = value;
			SaveSettingsToFile();
		}

		/// <summary>
		/// Load a setting value (e.g. _groupLogs = ImGuiSettingsSaver.LoadSetting(_groupLogs, "GroupLogs"))
		/// </summary>
		public static T LoadSetting<T>(T defaultValue, string key)
		{
			if (_settings != null && _settings.TryGetValue(key, out var val))
			{
				try
				{
					// Handle type conversion for bool, int, float, etc.
					if (val is T typedVal)
						return typedVal;
					if (typeof(T) == typeof(bool) && val is bool b) return (T)(object)b;
					if (typeof(T) == typeof(bool) && val is string s) return (T)(object)bool.Parse(s);
					if (typeof(T) == typeof(int) && val is int i) return (T)(object)i;
					if (typeof(T) == typeof(float) && val is float f) return (T)(object)f;
					if (typeof(T) == typeof(float) && val is double d) return (T)(object)(float)d;
					if (typeof(T) == typeof(string)) return (T)Convert.ChangeType(val, typeof(T));
				}
				catch
				{
					return defaultValue;
				}
			}
			return defaultValue;
		}

		private static string GetSettingsFilePath()
		{
			string appName = "JoltEngine";
			string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string settingsDir = Path.Combine(baseDir, appName);
			Directory.CreateDirectory(settingsDir);
			return Path.Combine(settingsDir, "ImGuiEditorSettings.json");
		}
	}
}