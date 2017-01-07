using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

using Java.Lang;
using Java.Lang.Reflect;

namespace Android.Utilities
{
    public class WrapLayoutManager : LinearLayoutManager
    {
        private const int DefaultChildSize = 100;
        private static readonly Rect TmpRect = new Rect();
        private int _childSize = DefaultChildSize;
        private static bool _canMakeInsetsDirty = true;
        private static readonly int[] ChildDimensions = new int[2];
        private const int ChildHeight = 1;
        private const int ChildWidth = 0;
        private static bool _hasChildSize;
        private static Field InsetsDirtyField = null;
        private static int _overScrollMode = ViewCompat.OverScrollAlways;
        private static RecyclerView _view;

        public WrapLayoutManager(Context context, int orientation, bool reverseLayout) : base(context, orientation, reverseLayout)
        {
            _view = null;
        }

        public WrapLayoutManager(Context context) : base(context)
        {
            _view = null;
        }

        public WrapLayoutManager(RecyclerView view) : base(view.Context)
        {
            _view = view;
            _overScrollMode = ViewCompat.GetOverScrollMode(view);
        }

        public WrapLayoutManager(RecyclerView view, int orientation, bool reverseLayout) : base(view.Context, orientation, reverseLayout)
        {
            _view = view;
            _overScrollMode = ViewCompat.GetOverScrollMode(view);
        }

        public void SetOverScrollMode(int overScrollMode)
        {
            if (overScrollMode < ViewCompat.OverScrollAlways || overScrollMode > ViewCompat.OverScrollNever)
                throw new ArgumentException("Unknown overscroll mode: " + overScrollMode);
            if (_view == null) throw new ArgumentNullException(nameof(_view));
            _overScrollMode = overScrollMode;
            ViewCompat.SetOverScrollMode(_view, overScrollMode);
        }

        public static int MakeUnspecifiedSpec()
        {
            return View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
        }

        public override void OnMeasure(RecyclerView.Recycler recycler, RecyclerView.State state, int widthSpec, int heightSpec)
        {
            var widthMode = View.MeasureSpec.GetMode(widthSpec);
            var heightMode = View.MeasureSpec.GetMode(heightSpec);

            var widthSize = View.MeasureSpec.GetSize(widthSpec);
            var heightSize = View.MeasureSpec.GetSize(heightSpec);

            bool hasWidthSize = widthMode != MeasureSpecMode.Unspecified;
            bool hasHeightSize = heightMode != MeasureSpecMode.Unspecified;
            bool hasFixedSize = _view?.HasFixedSize == true;

            var exactWidth = widthMode == MeasureSpecMode.Exactly;
            var exactHeight = heightMode == MeasureSpecMode.Exactly;

            var unspecified = MakeUnspecifiedSpec();

            if (exactWidth && exactHeight)
            {
                // in case of exact calculations for both dimensions let's use default "onMeasure" implementation
                base.OnMeasure(recycler, state, widthSpec, heightSpec);
                return;
            }

            var vertical = Orientation == Vertical;

            InitChildDimensions(widthSize, heightSize, vertical);

            var width = 0;
            var height = 0;

            // it's possible to get scrap views in recycler which are bound to old (invalid) adapter
            // entities. This happens because their invalidation happens after "onMeasure" method.
            // As a workaround let's clear the recycler now (it should not cause any performance
            // issues while scrolling as "onMeasure" is never called whiles scrolling)
            recycler.Clear();

            var stateItemCount = state.ItemCount;
            var adapterItemCount = ItemCount;
            // adapter always contains actual data while state might contain old data (f.e. data
            // before the animation is done). As we want to measure the view with actual data we
            // must use data from the adapter and not from the state
            for (var i = 0; i < adapterItemCount; i++)
            {
                if (vertical)
                {
                    if (!_hasChildSize)
                    {
                        if (i < stateItemCount)
                        {
                            // we should not exceed state count, otherwise we'll get
                            // IndexOutOfBoundsException. For such items we will use previously
                            // calculated dimensions
                            MeasureChild(recycler, i, widthSize, unspecified, ChildDimensions);
                        }
                        else
                        {
                            LogMeasureWarning(i);
                        }
                    }
                    height += ChildDimensions[ChildHeight];
                    if (i == 0)
                    {
                        width = ChildDimensions[ChildWidth];
                    }
                    if (hasHeightSize && height >= heightSize)
                    {
                        break;
                    }
                    if (hasFixedSize)
                    {
                        height *= adapterItemCount;
                        break;
                    }
                }
                else
                {
                    if (!_hasChildSize)
                    {
                        if (i < stateItemCount)
                        {
                            // we should not exceed state count, otherwise we'll get
                            // IndexOutOfBoundsException. For such items we will use previously
                            // calculated dimensions
                            MeasureChild(recycler, i, unspecified, heightSize, ChildDimensions);
                        }
                        else
                        {
                            LogMeasureWarning(i);
                        }
                    }
                    width += ChildDimensions[ChildWidth];
                    if (i == 0)
                    {
                        height = ChildDimensions[ChildHeight];
                    }
                    if (hasWidthSize && width >= widthSize)
                    {
                        break;
                    }
                    if (hasFixedSize)
                    {
                        width *= adapterItemCount;
                        break;
                    }
                }
            }

            if (exactWidth)
            {
                width = widthSize;
            }
            else
            {
                width += PaddingLeft + PaddingRight;
                if (hasWidthSize)
                {
                    width = System.Math.Min(width, widthSize);
                }
            }

            if (exactHeight)
            {
                height = heightSize;
            }
            else
            {
                height += PaddingTop + PaddingBottom;
                if (hasHeightSize)
                {
                    height = System.Math.Min(height, heightSize);
                }
            }

            SetMeasuredDimension(width, height);

            if (_view == null || _overScrollMode != ViewCompat.OverScrollIfContentScrolls) return;
            var fit = (vertical && (!hasHeightSize || height < heightSize))
                      || (!vertical && (!hasWidthSize || width < widthSize));

            ViewCompat.SetOverScrollMode(_view, fit ? ViewCompat.OverScrollNever : ViewCompat.OverScrollAlways);
        }

        private void LogMeasureWarning(int child)
        {
#if DEBUG
            Android.Util.Log.WriteLine(Android.Util.LogPriority.Warn, "LinearLayoutManager",
                "Can't measure child #" + child + ", previously used dimensions will be reused." +
                "To remove this message either use #SetChildSize() method or don't run RecyclerView animations");
#endif
        }

        private void InitChildDimensions(int width, int height, bool vertical)
        {
            if (ChildDimensions[ChildWidth] != 0 || ChildDimensions[ChildHeight] != 0)
            {
                // already initialized, skipping
                return;
            }
            if (vertical)
            {
                ChildDimensions[ChildWidth] = width;
                ChildDimensions[ChildHeight] = _childSize;
            }
            else
            {
                ChildDimensions[ChildWidth] = _childSize;
                ChildDimensions[ChildHeight] = height;
            }
        }

        public void ClearChildSize()
        {
            _hasChildSize = false;
            SetChildSize(DefaultChildSize);
        }

        public void SetChildSize(int size)
        {
            _hasChildSize = true;
            if (_childSize == size) return;
            _childSize = size;
            RequestLayout();
        }

        private void MeasureChild(RecyclerView.Recycler recycler, int position, int widthSize, int heightSize, int[] dimensions)
        {
            View child = null;
            try
            {
                child = recycler.GetViewForPosition(position);
            }
            catch (IndexOutOfRangeException e)
            {
                Android.Util.Log.WriteLine(Android.Util.LogPriority.Warn, "LinearLayoutManager",
                    "LinearLayoutManager doesn't work well with animations. Consider switching them off", e);
            }

            if (child != null)
            {
                var p = child.LayoutParameters.JavaCast<RecyclerView.LayoutParams>();

                var hPadding = PaddingLeft + PaddingRight;
                var vPadding = PaddingTop + PaddingBottom;

                var hMargin = p.LeftMargin + p.RightMargin;
                var vMargin = p.TopMargin + p.BottomMargin;

                // we must make insets dirty in order calculateItemDecorationsForChild to work
                MakeInsetsDirty(p);
                // this method should be called before any getXxxDecorationXxx() methods
                CalculateItemDecorationsForChild(child, TmpRect);

                var hDecoration = GetRightDecorationWidth(child) + GetLeftDecorationWidth(child);
                var vDecoration = GetTopDecorationHeight(child) + GetBottomDecorationHeight(child);

                var childWidthSpec = GetChildMeasureSpec(widthSize, hPadding + hMargin + hDecoration, p.Width,
                    CanScrollHorizontally());
                var childHeightSpec = GetChildMeasureSpec(heightSize, vPadding + vMargin + vDecoration, p.Height,
                    CanScrollVertically());

                child.Measure(childWidthSpec, childHeightSpec);

                dimensions[ChildWidth] = GetDecoratedMeasuredWidth(child) + p.LeftMargin + p.RightMargin;
                dimensions[ChildHeight] = GetDecoratedMeasuredHeight(child) + p.BottomMargin + p.TopMargin;

                // as view is recycled let's not keep old measured values
                MakeInsetsDirty(p);
            }
            recycler.RecycleView(child);
        }

        private static void MakeInsetsDirty(RecyclerView.LayoutParams p)
        {
            if (!_canMakeInsetsDirty)
            {
                return;
            }
            try
            {
                if (InsetsDirtyField == null)
                {
                    var klass = Java.Lang.Class.FromType(typeof(RecyclerView.LayoutParams));
                    InsetsDirtyField = klass.GetDeclaredField("mInsetsDirty");
                    InsetsDirtyField.Accessible = true;
                }
                InsetsDirtyField.Set(p, true);
            }
            catch (NoSuchFieldException e)
            {
                OnMakeInsertDirtyFailed();
            }
            catch (IllegalAccessException e)
            {
                OnMakeInsertDirtyFailed();
            }
        }

        private static void OnMakeInsertDirtyFailed()
        {
            _canMakeInsetsDirty = false;
#if DEBUG
            Android.Util.Log.Warn("LinearLayoutManager",
                "Can't make LayoutParams insets dirty, decorations measurements might be incorrect");
#endif
        }
    }
}