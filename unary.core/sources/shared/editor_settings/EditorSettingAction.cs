using System.Reflection;

namespace Unary.Core
{
    public class EditorSettingAction : EditorSettingBase
    {
        public MethodInfo MethodInfo;

        public EditorSettingAction()
        {
            Type = EditorSettingType.Action;
        }
    }
}
