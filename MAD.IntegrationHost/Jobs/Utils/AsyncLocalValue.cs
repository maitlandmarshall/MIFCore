using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs.Utils
{
    internal static class AsyncLocalValue<T>
    {
        private static readonly AsyncLocal<T> current = new AsyncLocal<T>();

        public static T Current
        {
            get => current.Value;
            set
            {
                current.Value = value;
                OnCurrentChanged?.Invoke(value);
            }
        }

        private static readonly AsyncLocal<Action<T>> onCurrentChanged = new AsyncLocal<Action<T>>();

        public static Action<T> OnCurrentChanged
        {
            get => onCurrentChanged.Value;
            set => onCurrentChanged.Value = value;
        }
    }
}
