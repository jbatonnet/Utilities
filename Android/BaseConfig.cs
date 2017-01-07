using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Android.Content;
using Android.Utilities;

namespace Android.Utilities
{
    public class BaseConfig
    {
        protected ISharedPreferences Preferences { get; }

        private Dictionary<string, object> preferenceCache = new Dictionary<string, object>();

        protected BaseConfig(Context context)
        {
            Preferences = context.GetSharedPreferences(BaseApplication.Instance.Name + ".conf", FileCreationMode.Private);
        }
        protected BaseConfig(Context context, FileCreationMode mode)
        {
            Preferences = context.GetSharedPreferences(BaseApplication.Instance.Name + ".conf", mode);
        }
        protected BaseConfig(Context context, string name)
        {
            Preferences = context.GetSharedPreferences(name + ".conf", FileCreationMode.Private);
        }
        protected BaseConfig(Context context, string name, FileCreationMode mode)
        {
            Preferences = context.GetSharedPreferences(name + ".conf", mode);
        }

        protected void SetValue<T>(T value, [CallerMemberName]string name = null)
        {
            ISharedPreferencesEditor editor = Preferences.Edit();

            preferenceCache.Remove(name);

            Type type = typeof(T);
            string key = "Config." + name;

            if (type == typeof(bool)) editor.PutBoolean(key, (bool)(object)value);
            else if (type == typeof(int)) editor.PutInt(key, (int)(object)value);
            else if (type == typeof(long)) editor.PutLong(key, (long)(object)value);
            else if (type == typeof(float)) editor.PutFloat(key, (float)(object)value);
            else if (type == typeof(string)) editor.PutString(key, (string)(object)value);
            else if (typeof(ICollection<string>).IsAssignableFrom(type)) editor.PutStringSet(key, (ICollection<string>)value);
            else if (type == typeof(Android.Net.Uri)) editor.PutString(key, (value as Android.Net.Uri)?.ToString());
            else if (type == typeof(byte[]))
            {
                byte[] bytes = (byte[])(object)value;
                string data = bytes == null ? null : Convert.ToBase64String(bytes);
                editor.PutString(key, data);
            }
            else
                throw new NotSupportedException("Could not write the specified preference type");

            editor.Apply();
        }
        protected T GetValue<T>(T defaultValue, [CallerMemberName]string name = null)
        {
            Type type = typeof(T);
            string key = "Config." + name;

            if (type == typeof(bool)) return (T)(object)Preferences.GetBoolean(key, (bool)(object)defaultValue);
            else if (type == typeof(int)) return (T)(object)Preferences.GetInt(key, (int)(object)defaultValue);
            else if (type == typeof(long)) return (T)(object)Preferences.GetLong(key, (long)(object)defaultValue);
            else if (type == typeof(float)) return (T)(object)Preferences.GetFloat(key, (float)(object)defaultValue);
            else if (type == typeof(string)) return (T)(object)Preferences.GetString(key, (string)(object)defaultValue);
            else if (type == typeof(List<string>)) return (T)(object)Preferences.GetStringSet(key, (ICollection<string>)defaultValue).ToList();
            else if (type == typeof(string[])) return (T)(object)Preferences.GetStringSet(key, (ICollection<string>)defaultValue).ToArray();
            else if (type.IsAssignableFrom(typeof(ICollection<string>))) return (T)(object)Preferences.GetStringSet(key, (ICollection<string>)defaultValue);
            else if (type == typeof(Android.Net.Uri))
            {
                string uri = Preferences.GetString(key, (defaultValue as Android.Net.Uri)?.ToString());
                return (T)(object)(uri != null ? Android.Net.Uri.Parse(uri) : null);
            }
            else if (type == typeof(byte[]))
            {
                string data = Preferences.GetString(key, null);
                return data != null ? (T)(object)Convert.FromBase64String(data) : defaultValue;
            }
            else
                throw new NotSupportedException("Could not read the specified preference type");
        }
        protected T CacheValue<T>(T defaultValue, [CallerMemberName]string name = null)
        {
            object result;

            if (!preferenceCache.TryGetValue(name, out result))
            {
                result = GetValue(defaultValue, name);
                preferenceCache.Add(name, result);
            }

            return (T)result;
        }
    }
}