using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Android.OS;
using Android.Preferences;
using Android.Views;

namespace Android.Utilities
{
    public class SettingsFragment : PreferenceFragment
    {
        private static Dictionary<int, BaseConfig> savedConfig = new Dictionary<int, BaseConfig>();

        public BaseConfig Configuration { get; private set; }

        public SettingsFragment() { }
        public SettingsFragment(BaseConfig configuration)
        {
            Configuration = configuration;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (savedInstanceState?.ContainsKey(nameof(Configuration)) == true)
            {
                int hash = savedInstanceState.GetInt(nameof(Configuration));

                BaseConfig configuration;
                if (savedConfig.TryGetValue(hash, out configuration))
                    Configuration = configuration;
            }

            PreferenceScreen = PreferenceManager.CreatePreferenceScreen(inflater.Context);

            OnAddPreferences(PreferenceScreen);

            return base.OnCreateView(inflater, container, savedInstanceState);
        }
        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            int hash = Configuration.GetHashCode();
            savedConfig[hash] = Configuration;
            outState.PutInt(nameof(Configuration), hash);
        }
        public override void OnResume()
        {
            base.OnResume();

            for (int i = 0; i < PreferenceScreen.PreferenceCount; i++)
            {
                Preference preference = PreferenceScreen.GetPreference(i);
                if (preference.Key == null)
                    continue;

                try
                {
                    PropertyInfo property = Configuration.GetType().GetProperty(preference.Key);
                    GetPreference(preference, property);
                }
                catch (Exception e)
                {
                }
            }
        }
        public override bool OnPreferenceTreeClick(PreferenceScreen preferenceScreen, Preference preference)
        {
            PropertyInfo property = Configuration.GetType().GetProperty(preference.Key);

            SetPreference(preference, property);

            return base.OnPreferenceTreeClick(preferenceScreen, preference);
        }
        protected virtual void OnAddPreferences(PreferenceScreen preferenceScreen)
        {
            foreach (PropertyInfo property in Configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<BrowsableAttribute>()?.Browsable == false)
                    continue;

                Preference preference = CreatePreference(property);
                if (preference == null)
                    continue;

                PreferenceScreen.AddPreference(preference);
            }
        }

        protected virtual Preference CreatePreference(PropertyInfo property)
        {
            Preference preference = null;

            if (property.PropertyType == typeof(bool))
            {
                preference = new CheckBoxPreference(base.PreferenceScreen.Context)
                {
                    Checked = (bool)property.GetValue(Configuration),
                };
            }
            else if (property.PropertyType == typeof(int))
            {
                preference = new EditTextPreference(base.PreferenceScreen.Context)
                {
                    Text = ((int)property.GetValue(Configuration)).ToString(),
                };
            }
            else
                return null;

            preference.Key = property.Name;
            preference.Title = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            preference.Summary = property.GetCustomAttribute<DescriptionAttribute>()?.Description;

            return preference;
        }
        protected virtual void GetPreference(Preference preference, PropertyInfo property)
        {
            if (preference is CheckBoxPreference)
                (preference as CheckBoxPreference).Checked = (bool)property.GetValue(Configuration);
            else if (preference is EditTextPreference)
            {
                string text = property.GetValue(Configuration).ToString();

                (preference as EditTextPreference).Text = text;
                preference.Summary = text;
            }
        }
        protected virtual void SetPreference(Preference preference, PropertyInfo property)
        {
            if (preference is CheckBoxPreference)
                property.SetValue(Configuration, (preference as CheckBoxPreference).Checked);
            else if (preference is EditTextPreference)
            {
                string text = (preference as EditTextPreference).Text;

                if (property.PropertyType == typeof(int))
                {
                    int value;
                    if (int.TryParse(text, out value))
                        property.SetValue(Configuration, value);
                }
            }
        }
    }
}