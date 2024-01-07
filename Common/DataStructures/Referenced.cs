using System;
using System.Diagnostics;

namespace NoxusBoss.Common.DataStructures
{
    [DebuggerDisplay("{Value}")]
    public class Referenced<T>
    {
        private T value;

        public Func<object> getter;

        public Action<object> setter;

        public T Value
        {
            get => (T)getter();
            set => setter(value);
        }

        protected internal Referenced(Func<object> getter)
        {
            this.getter = getter;
            setter = _ => { };
        }

        protected internal Referenced(Func<object> getter, Action<object> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public Referenced(T value)
        {
            this.value = value;
            getter = () => this.value;
            setter = v => this.value = (T)v;
        }

        // Useful shorthand for accessing a reference's underlying value implicitly.
        // Be careful to not rely on this in contexts where explicit typing is required, such as save/load tags, as doing so will provide such things with a Referenced wrapper type
        // and likely cause issues.
        public static implicit operator T(Referenced<T> reference)
        {
            return reference.Value;
        }

        // This is used for contexts where a Referenced<object> is used for a central storage but needs to be interpreted with a more explicit generic type, such as
        // converting a Referenced<object> to a Referenced<int>.
        // Importantly, this does not waste resources creating entirely new delegates or anything. The created Reference<int> in the example will have the exact same
        // getters and setters as the Referenced<object>, it's just that their contents will be interpreted different via the Value property.
        public static implicit operator Referenced<T>(Referenced<object> boxedRef)
        {
            return new(boxedRef.getter, boxedRef.setter);
        }
    }
}
