using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Android.Widget
{
    [Register("android.widget.TopAlignedImageView")]
    public class TopAlignedImageView : ImageView
    {
        public TopAlignedImageView(Context context) : base(context)
        {
            Setup();
        }
        public TopAlignedImageView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Setup();
        }
        public TopAlignedImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Setup();
        }

        private void Setup()
        {
            SetScaleType(ScaleType.Matrix);
        }

        protected override bool SetFrame(int l, int t, int r, int b)
        {
            float frameWidth = r - l;
            float frameHeight = b - t;

            float fitHorizontallyScaleFactor = frameWidth / Drawable.IntrinsicWidth;
            float fitVerticallyScaleFactor = frameHeight / Drawable.IntrinsicHeight;

            float usedScaleFactor = Math.Max(fitHorizontallyScaleFactor, fitVerticallyScaleFactor);

            float newImageWidth = Drawable.IntrinsicWidth * usedScaleFactor;
            float newImageHeight = Drawable.IntrinsicHeight * usedScaleFactor;

            Matrix matrix = ImageMatrix;
            matrix.SetScale(usedScaleFactor, usedScaleFactor, 0, 0);
            matrix.PostTranslate((frameWidth - newImageWidth) / 2, 0);
            ImageMatrix = matrix;

            return base.SetFrame(l, t, r, b);
        }
    }
}