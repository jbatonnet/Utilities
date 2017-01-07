using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class ExceptionExtensions
    {
        public static IEnumerable<Exception> GetAllExceptions(this IEnumerable<Exception> me)
        {
            return me.SelectMany(e => (e as AggregateException)?.InnerExceptions.GetAllExceptions() ?? new Exception[] { e });
        }
    }
}