using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Android.Utilities
{
    public abstract class GenericResourceBinder<T>
    {
        public abstract void Bind(View view, T item);
        public abstract void Unbind(View view, T item);
    }
    public class GenericResourceAutoBinder<T> : GenericResourceBinder<T>
    {
        private Dictionary<int, Action<View, T>> resourceBinders = new Dictionary<int, Action<View, T>>();
        private Dictionary<View, Dictionary<int, View>> resourceViews = new Dictionary<View, Dictionary<int, View>>();

        public GenericResourceAutoBinder(IDictionary<int, Action<View, T>> resourceBinders)
        {
            this.resourceBinders = resourceBinders.ToDictionary();
        }

        public override void Bind(View view, T item)
        {
            Dictionary<int, View> views;

            if (!resourceViews.TryGetValue(view, out views))
                resourceViews.Add(view, views = new Dictionary<int, View>());

            foreach (var pair in resourceBinders)
            {
                View binderView;

                if (!views.TryGetValue(pair.Key, out binderView))
                    views.Add(pair.Key, binderView = view.FindViewById(pair.Key));

                pair.Value(binderView, item);
            }
        }
        public override void Unbind(View view, T item)
        {
        }
    }
    public class GenericResourceManualBinder<T> : GenericResourceBinder<T>
    {
        private Action<View, T> bind, unbind;

        public GenericResourceManualBinder(Action<View, T> bind)
        {
            this.bind = bind;
        }
        public GenericResourceManualBinder(Action<View, T> bind, Action<View, T> unbind)
        {
            this.bind = bind;
            this.unbind = unbind;
        }

        public override void Bind(View view, T item)
        {
            bind?.Invoke(view, item);
        }
        public override void Unbind(View view, T item)
        {
            unbind?.Invoke(view, item);
        }
    }

    public delegate void GenericAdapterCallback<T>(GenericAdapter<T> adapter, View view, T item) where T : class;
    public delegate void GenericAdapterItemCallback<T>(View view, T item) where T : class;
    public delegate bool GenericAdapterFilter<T>(T item, string filter) where T : class;

    public class GenericAdapter<T> where T : class
    {
        public IEnumerable<T> Items
        {
            get
            {
                return rawItems;
            }
            set
            {
                rawItems = value?.ToArray();

                ApplyFilter(false);
                ApplySorting(false);

                Refresh();
            }
        }
        public GenericAdapterFilter<T> Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;

                ApplyFilter(false);
                ApplySorting(false);

                Refresh();
            }
        }
        public string FilterText
        {
            get
            {
                return filterText;
            }
            set
            {
                filterText = value;

                ApplyFilter(false);
                ApplySorting(false);

                Refresh();
            }
        }
        public Comparer<T> Sort
        {
            get
            {
                return sort;
            }
            set
            {
                sort = value;

                ApplySorting(false);

                Refresh();
            }
        }

        public event GenericAdapterCallback<T> Click;

        private int layoutId;
        private GenericResourceBinder<T> resourceBinder;

        private T[] rawItems;
        private T[] filteredItems;

        private GenericAdapterFilter<T> filter;
        private string filterText;
        private Comparer<T> sort;

        protected GenericAdapter(int layoutId) : this(Enumerable.Empty<T>(), layoutId) { }
        protected GenericAdapter(IEnumerable<T> items, int layoutId)
        {
            Items = items;

            this.layoutId = layoutId;
            this.resourceBinder = new GenericResourceManualBinder<T>(OnBind, OnUnbind);
        }

        public GenericAdapter(int layoutId, Action<View, T> resourceBinder) : this(Enumerable.Empty<T>(), layoutId, new GenericResourceManualBinder<T>(resourceBinder)) { }
        public GenericAdapter(int layoutId, GenericResourceBinder<T> resourceBinder) : this(Enumerable.Empty<T>(), layoutId, resourceBinder) { }
        public GenericAdapter(IEnumerable<T> items, int layoutId, Action<View, T> resourceBinder) : this(items, layoutId, new GenericResourceManualBinder<T>(resourceBinder)) { }
        public GenericAdapter(IEnumerable<T> items, int layoutId, GenericResourceBinder<T> resourceBinder)
        {
            Items = items;

            this.layoutId = layoutId;
            this.resourceBinder = resourceBinder;
        }

        protected virtual void OnBind(View view, T item) { }
        protected virtual void OnUnbind(View view, T item) { }
        protected virtual void OnClick(View view, T item)
        {
            Click?.Invoke(this, view, item);
        }

        protected virtual T GetItemFromView(View view)
        {
            foreach (GenericRecyclerViewAdapter<T> adapter in recyclerViewAdapters)
            {
                T item = adapter.GetItemFromView(view);
                if (item != null)
                    return item;
            }

            return null;
        }
        protected virtual T GetItemFromSubView(View view)
        {
            while (view != null)
            {
                T item = GetItemFromView(view);
                if (item != null)
                    return item;

                view = view.Parent as View;
            }

            return null;
        }

        private void ApplyFilter(bool refresh = true)
        {
            if (filter == null)
                filteredItems = rawItems;
            else if (string.IsNullOrWhiteSpace(filterText))
                filteredItems = rawItems.ToArray();
            else
                filteredItems = rawItems.Where(i => filter(i, filterText)).ToArray();

            if (refresh)
                Refresh();
        }
        private void ApplySorting(bool refresh = true)
        {
            if (sort != null)
                Array.Sort(filteredItems, sort);

            if (refresh)
                Refresh();
        }
        private void Refresh()
        {
            foreach (GenericRecyclerViewAdapter<T> adapter in recyclerViewAdapters)
                adapter.Refresh(filteredItems);
        }

        private List<GenericRecyclerViewAdapter<T>> recyclerViewAdapters = new List<GenericRecyclerViewAdapter<T>>();
        public static implicit operator RecyclerView.Adapter(GenericAdapter<T> me)
        {
            GenericRecyclerViewAdapter<T> adapter = new GenericRecyclerViewAdapter<T>(me.filteredItems, me.layoutId, me.resourceBinder);
            adapter.Click += me.OnClick;

            me.recyclerViewAdapters.Add(adapter);
            return adapter;
        }
    }

    public class GenericRecyclerViewAdapter<T> : RecyclerView.Adapter where T : class
    {
        private class GenericRecyclerViewHolder<T> : RecyclerView.ViewHolder
        {
            public GenericRecyclerViewHolder(View view) : base(view) { }
        }

        public override int ItemCount
        {
            get
            {
                return items.Length;
            }
        }

        public event GenericAdapterItemCallback<T> Click;

        private T[] items;
        private int layoutId;
        private GenericResourceBinder<T> resourceBinder;

        private Association<View, GenericRecyclerViewHolder<T>> viewHolders = new Association<View, GenericRecyclerViewHolder<T>>();
        private Association<GenericRecyclerViewHolder<T>, T> viewHolderItems = new Association<GenericRecyclerViewHolder<T>, T>();

        public GenericRecyclerViewAdapter(T[] items, int layoutId, GenericResourceBinder<T> resourceBinder)
        {
            this.items = items;
            this.layoutId = layoutId;
            this.resourceBinder = resourceBinder;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context).Inflate(layoutId, parent, false);
            GenericRecyclerViewHolder<T> viewHolder = new GenericRecyclerViewHolder<T>(view);

            viewHolders.Add(view, viewHolder);

            return viewHolder;
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            GenericRecyclerViewHolder<T> viewHolder = holder as GenericRecyclerViewHolder<T>;
            T item = items[position];

            viewHolderItems[viewHolder] = item;

            resourceBinder.Bind(viewHolder.ItemView, item);
        }
        public override void OnViewAttachedToWindow(Java.Lang.Object holder)
        {
            GenericRecyclerViewHolder<T> viewHolder = holder as GenericRecyclerViewHolder<T>;
            viewHolder.ItemView.Click += View_Click;
        }
        public override void OnViewDetachedFromWindow(Java.Lang.Object holder)
        {
            GenericRecyclerViewHolder<T> viewHolder = holder as GenericRecyclerViewHolder<T>;
            viewHolder.ItemView.Click -= View_Click;

            T item;
            if (viewHolderItems.TryGetRight(viewHolder, out item))
                resourceBinder.Unbind(viewHolder.ItemView, item);
        }

        private void View_Click(object sender, EventArgs e)
        {
            View view = sender as View;
            GenericRecyclerViewHolder<T> viewHolder = viewHolders[view];
            T item = viewHolderItems[viewHolder];

            Click?.Invoke(view, item);
        }

        public void Refresh(T[] items)
        {
            this.items = items;
            NotifyDataSetChanged();
        }
        public T GetItemFromView(View view)
        {
            GenericRecyclerViewHolder<T> viewHolder;
            if (!viewHolders.TryGetRight(view, out viewHolder))
                return null;

            T item;
            if (!viewHolderItems.TryGetRight(viewHolder, out item))
                return null;

            return item;
        }
    }
    public class GenericBaseAdapter<T> : BaseAdapter<T>, IFilterable, View.IOnClickListener where T : class
    {
        public class GenericBaseAdapterFilter : Filter
        {
            private GenericBaseAdapter<T> adapter;

            public GenericBaseAdapterFilter(GenericBaseAdapter<T> adapter)
            {
                this.adapter = adapter;
            }

            protected override FilterResults PerformFiltering(ICharSequence constraint)
            {
                FilterResults filterResults = new FilterResults();

                if (constraint == null)
                    return filterResults;

                string filterText = new string(constraint.Select(c => c).ToArray());
                GenericAdapterFilter<T> filter = adapter.adapter.Filter;

                T[] filteredItems = adapter.items.Where(i => filter(i, filterText)).ToArray();

                filterResults.Values = FromArray(filteredItems.Select(i => new Java.Lang.String(i.ToString())).ToArray());
                filterResults.Count = filteredItems.Length;

                constraint.Dispose();

                return filterResults;
            }
            protected override void PublishResults(ICharSequence constraint, FilterResults results)
            {
                string filterText = constraint == null ? null : new string(constraint.Select(c => c).ToArray());

                adapter.adapter.FilterText = filterText;

                constraint?.Dispose();
                results.Dispose();
            }
        }

        public override int Count
        {
            get { return items.Length; }
        }
        public Filter Filter { get; private set; }

        public override T this[int position]
        {
            get
            {
                return items[position];
            }
        }

        public event GenericAdapterItemCallback<T> Click;

        private GenericAdapter<T> adapter;
        private T[] items;
        private int layoutId;
        private GenericResourceBinder<T> resourceBinder;
        private Dictionary<int, View> itemViews = new Dictionary<int, View>();

        public GenericBaseAdapter(GenericAdapter<T> adapter, T[] items, int layoutId, GenericResourceBinder<T> resourceBinder)
        {
            this.adapter = adapter;
            this.items = items;
            this.layoutId = layoutId;
            this.resourceBinder = resourceBinder;

            Filter = new GenericBaseAdapterFilter(this);
        }

        public override long GetItemId(int position)
        {
            return position;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            T item = items[position];

            View view = LayoutInflater.From(parent.Context).Inflate(layoutId, parent, false);
            view.SetOnClickListener(this);

            resourceBinder.Bind(view, item);
            
            return view;
        }
        public override void NotifyDataSetChanged()
        {
            // If you are using cool stuff like sections
            // remember to update the indices here!
            base.NotifyDataSetChanged();
        }

        public void Refresh(T[] items)
        {
            this.items = items;

            NotifyDataSetChanged();
        }

        void View.IOnClickListener.OnClick(View view)
        {
            int position = itemViews.Values.IndexOf(view);
            T item = items[position];

            Click?.Invoke(view, item);
        }
    }
}