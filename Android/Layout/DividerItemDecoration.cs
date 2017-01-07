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
    public class DividerItemDecoration : RecyclerView.ItemDecoration
    {
        private static int[] ATTRS = new int[]{ global::Android.Resource.Attribute.ListDivider };
        private Drawable mDivider;
        private int mOrientation;
        private bool showFirstDivider;

        public DividerItemDecoration(Context context, int orientation, bool showFirstDivider = false)
        {
            this.showFirstDivider = showFirstDivider;

            TypedArray a = context.ObtainStyledAttributes(ATTRS);
            mDivider = a.GetDrawable(0);
            a.Recycle();
            SetOrientation(orientation);
        }

        public void SetOrientation(int orientation)
        {
            if (orientation != LinearLayoutManager.Horizontal && orientation != LinearLayoutManager.Vertical)
                throw new IllegalArgumentException("invalid orientation");

            mOrientation = orientation;
        }

        public override void OnDraw(Canvas c, RecyclerView parent)
        {
            if (mOrientation == LinearLayoutManager.Vertical)
                DrawVertical(c, parent);
            else
                DrawHorizontal(c, parent);
        }

        public void DrawVertical(Canvas c, RecyclerView parent)
        {
            int left = parent.PaddingLeft;
            int right = parent.Width - parent.PaddingRight;

            int childCount = parent.ChildCount;

            if (childCount > 0 && showFirstDivider)
            {
                View child = parent.GetChildAt(0);
                RecyclerView.LayoutParams parameters = (RecyclerView.LayoutParams)child.LayoutParameters;

                int top = child.Top;
                int bottom = top + mDivider.IntrinsicHeight;

                mDivider.SetBounds(left, top, right, bottom);
                mDivider.Draw(c);
            }

            for (int i = 0; i < childCount; i++)
            {
                View child = parent.GetChildAt(i);
                RecyclerView.LayoutParams parameters = (RecyclerView.LayoutParams)child.LayoutParameters;

                int top = child.Bottom + parameters.BottomMargin;
                int bottom = top + mDivider.IntrinsicHeight;

                mDivider.SetBounds(left, top, right, bottom);
                mDivider.Draw(c);
            }
        }

        public void DrawHorizontal(Canvas c, RecyclerView parent)
        {
            int top = parent.PaddingTop;
            int bottom = parent.Height - parent.PaddingBottom;

            int childCount = parent.ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                View child = parent.GetChildAt(i);
                RecyclerView.LayoutParams parameters = (RecyclerView.LayoutParams)child.LayoutParameters;

                int left = child.Right + parameters.RightMargin;
                int right = left + mDivider.IntrinsicHeight;

                mDivider.SetBounds(left, top, right, bottom);
                mDivider.Draw(c);
            }
        }

        public override void GetItemOffsets(Rect outRect, int itemPosition, RecyclerView parent)
        {
            if (mOrientation == LinearLayoutManager.Vertical)
                if (showFirstDivider)
                    outRect.Set(0, mDivider.IntrinsicHeight, 0, 0);
                else
                    outRect.Set(0, 0, 0, mDivider.IntrinsicHeight);
            else
                outRect.Set(0, 0, mDivider.IntrinsicWidth, 0);
        }
    }
}