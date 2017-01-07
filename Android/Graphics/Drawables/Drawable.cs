using System;
using System.Collections.Generic;
using System.Text;

namespace Android.Graphics.Drawables
{
    public static class DrawableExtensions
    {
        public static Bitmap ToBitmap(this Drawable drawable)
        {
            Bitmap bitmap = null;

            if (drawable is BitmapDrawable)
            {
                BitmapDrawable bitmapDrawable = (BitmapDrawable)drawable;
                if (bitmapDrawable.Bitmap != null)
                    return bitmapDrawable.Bitmap;
            }

            if (drawable.IntrinsicWidth <= 0 || drawable.IntrinsicHeight <= 0)
                bitmap = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888); // Single color bitmap will be created of 1x1 pixel
            else
                bitmap = Bitmap.CreateBitmap(drawable.IntrinsicWidth, drawable.IntrinsicHeight, Bitmap.Config.Argb8888);

            Canvas canvas = new Canvas(bitmap);

            drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
            drawable.Draw(canvas);

            return bitmap;
        }
    }
}
