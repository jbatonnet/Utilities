using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

using Java.Lang;

namespace Android.Utilities
{
    public class SimpleRunnable : Java.Lang.Object, IRunnable
    {
        private Action action;

        public SimpleRunnable(Action action)
        {
            this.action = action;
        }

        public void Run()
        {
            action();
        }
    }
}