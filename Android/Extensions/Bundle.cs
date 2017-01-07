using System;

using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

namespace Android.Utilities
{
    public static class BundleExtensions
    {
        public static void Save(this Bundle bundle, string key, int value)
        {
            bundle?.PutInt(key, value);
        }
        public static void Save(this Bundle bundle, string key, Color value)
        {
            bundle?.PutInt(key, value.ToArgb());
        }

        public static void Restore(this Bundle bundle, string key, ref int value)
        {
            if (bundle?.ContainsKey(key) != true)
                return;

            value = bundle.GetInt(key);
        }
        public static void Restore(this Bundle bundle, string key, ref Color value)
        {
            if (bundle?.ContainsKey(key) != true)
                return;

            int argb = bundle.GetInt(key);
            value = new Color(argb);
        }
    }
}