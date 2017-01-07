using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public class Association<TLeft, TRight>
    {
        public IEnumerable<TLeft> Left => leftValues;
        public IEnumerable<TRight> Right => rightValues;
        public int Count => leftValues.Count;

        public TRight this[TLeft left]
        {
            get
            {
                lock (mutex)
                {
                    int index = leftValues.IndexOf(left);
                    if (index == -1)
                        throw new ArgumentException();

                    return rightValues[index];
                }
            }
            set
            {
                lock (mutex)
                {
                    int index = leftValues.IndexOf(left);

                    if (index == -1)
                    {
                        leftValues.Add(left);
                        rightValues.Add(value);
                    }
                    else
                        rightValues[index] = value;
                }
            }
        }
        public TLeft this[TRight right]
        {
            get
            {
                lock (mutex)
                {
                    int index = rightValues.IndexOf(right);
                    if (index == -1)
                        throw new ArgumentException();

                    return leftValues[index];
                }
            }
            set
            {
                lock (mutex)
                {
                    int index = rightValues.IndexOf(right);

                    if (index == -1)
                    {
                        rightValues.Add(right);
                        leftValues.Add(value);
                    }
                    else
                        leftValues[index] = value;
                }
            }
        }

        private List<TLeft> leftValues = new List<TLeft>();
        private List<TRight> rightValues = new List<TRight>();
        private object mutex = new object();
        
        public void Add(TLeft left, TRight right)
        {
            lock (mutex)
            {
                if (leftValues.Contains(left))
                    throw new NotSupportedException();

                leftValues.Add(left);
                rightValues.Add(right);
            }
        }

        public bool TryGetLeft(TRight right, out TLeft left)
        {
            lock (mutex)
            {
                int index = rightValues.IndexOf(right);

                if (index == -1)
                {
                    left = default(TLeft);
                    return false;
                }
                else
                {
                    left = leftValues[index];
                    return true;
                }
            }
        }
        public bool TryGetRight(TLeft left, out TRight right)
        {
            lock (mutex)
            {
                int index = leftValues.IndexOf(left);

                if (index == -1)
                {
                    right = default(TRight);
                    return false;
                }
                else
                {
                    right = rightValues[index];
                    return true;
                }
            }
        }
    }
}