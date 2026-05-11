using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    public class GraphicsSetting<[MustBeVariant] T> where T : struct
    {
        public string Description;
        public Func<T, List<T>, T, T> Validator;
        public EventFunc<T> OnChanged = new();
        public List<T> Options;
        public List<string> OptionsLabels;

        public T DefaultValue;
        public Func<List<T>, T> DefaultCalculator;

        private T _value;
        private bool _gotValue = false;

        public T Value
        {
            get
            {
                if (!_gotValue)
                {
                    _gotValue = true;

                    if (DefaultCalculator != null)
                    {
                        _value = DefaultCalculator(Options);
                    }
                    else
                    {
                        _value = DefaultValue;
                    }
                }

                return _value;
            }
            set
            {
                T result = value;

                if (Validator != null)
                {
                    result = Validator(result, Options, DefaultValue);
                }

                _value = result;
                _gotValue = true;
                OnChanged.Publish(_value);
            }
        }

        public void Republish()
        {
            OnChanged.Publish(_value);
        }
    }
}
