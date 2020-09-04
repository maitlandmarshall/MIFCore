using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs.Utils
{
    internal static class ThreadStaticValue<T>
    {
        [ThreadStatic]
        private static T current;

        public static T Current
        {
            get => current;
            set
            {
                current = value;
                onCurrentChanged?.Invoke(value);
            }
        }

        [ThreadStatic]
        private static Action<T> onCurrentChanged;
        public static Action<T> OnCurrentChanged
        {
            get => onCurrentChanged;
            set => onCurrentChanged = value;
        }
    }
}
