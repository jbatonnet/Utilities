using System;

using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

namespace Android.Utilities
{
    public static class ContextWrapperExtensions
    {
        public static void StartService(this ContextWrapper contextWrapper, Type type)
        {
            Intent intent = new Intent(contextWrapper, type);
            contextWrapper.StartService(intent);
        }
    }
}