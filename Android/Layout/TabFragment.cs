using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace Android.Utilities
{
    public abstract class TabFragment : Fragment
    {
        public abstract string Title { get; }
        public override bool UserVisibleHint
        {
            get
            {
                return base.UserVisibleHint;
            }
            set
            {
                base.UserVisibleHint = value;

                if (View != null)
                {
                    if (value)
                        OnGotFocus();
                    else
                        OnLostFocus();
                }
            }
        }

        private bool initialized = false;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            OnGotFocus();
            OnLostFocus();
        }
        protected virtual void OnGotFocus() { }
        protected virtual void OnLostFocus() { }

        public virtual void Refresh() { }
    }
}