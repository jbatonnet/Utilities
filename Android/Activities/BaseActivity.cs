using Android.OS;
using Android.Views;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;

namespace Android.Utilities
{
    public abstract class BaseActivity : AppCompatActivity
    {
        public float Density
        {
            get
            {
                return Resources.DisplayMetrics.Density;
            }
        }

        protected AppBarLayout appbarLayout;
        protected Toolbar toolbar;
        protected View toolbarShadow;
        protected View defaultFocus;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected virtual void OnPostCreate()
        {
            // AppBar
            int appbarResourceId = Resources.GetIdentifier("appbar", "id", PackageName);
            if (appbarResourceId != 0)
                appbarLayout = FindViewById<AppBarLayout>(appbarResourceId);

            // Toolbar
            int toolbarResourceId = Resources.GetIdentifier("toolbar", "id", PackageName);
            if (toolbarResourceId != 0)
                toolbar = FindViewById<Toolbar>(toolbarResourceId);
            if (toolbar != null)
                SetSupportActionBar(toolbar);

            // Shadow for pre lollipop devices
            int shadowResourceId = Resources.GetIdentifier("shadow", "id", PackageName);
            if (shadowResourceId != 0)
            {
                toolbarShadow = FindViewById(shadowResourceId);
                if (toolbarShadow != null && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    toolbarShadow.Visibility = ViewStates.Gone;

                    float elevation = 4 * Resources.DisplayMetrics.Density;

                    if (appbarLayout != null)
                        appbarLayout.Elevation = elevation;
                    else if (toolbar != null)
                        toolbar.Elevation = elevation;
                }
            }

            // Default focus
            int defaultFocusResourceId = Resources.GetIdentifier("defaultFocus", "id", PackageName);
            if (defaultFocusResourceId != 0)
                defaultFocus = FindViewById(defaultFocusResourceId);
            if (defaultFocus != null)
                defaultFocus.RequestFocus();
        }

        protected void OnCreate(Bundle savedInstanceState, int layoutId)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(layoutId);

            OnPostCreate();
        }
    }
}