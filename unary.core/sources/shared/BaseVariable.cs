using Godot;

namespace Unary.Core
{
    public class BaseVariable
    {
        public string ModId;
        public string Group;
        public string Name;
        public string Description;

        protected Variant _field;

        public virtual Variant GetField()
        {
            return _field;
        }

        public virtual void SetField(Variant value)
        {
            _field = value;
        }
    }
}
