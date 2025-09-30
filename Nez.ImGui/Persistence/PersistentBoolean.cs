using Nez.ImGuiTools.Persistence;
using Nez.ImGuiTools.Utils;

namespace Nez.ImGuiTools.Utils
{
	/// <summary>
	/// A boolean setting that persists its value across sessions using ImGuiSettingsLoader.
	/// </summary>
	public class PersistentBool
	{
		public string Key { get; }
		private bool _value;

		public PersistentBool(string key, bool defaultValue = false)
		{
			Key = key;
			_value = ImGuiSettingsLoader.LoadSetting(defaultValue, key);
		}

		public bool GetValue()
		{
			return _value;
		}

		public bool Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					_value = value;
					ImGuiSettingsLoader.SaveSetting(_value, Key);
				}
			}
		}

		// Implicit conversion for convenience
		public static implicit operator bool(PersistentBool setting) => setting._value;
	}
}