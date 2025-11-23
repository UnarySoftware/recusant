#include <unary.core/system/system.hpp>

namespace core
{
    const godot::StringName &System::get_mod_name() const
    {
        static const godot::StringName string_name = godot::StringName("core");
        return string_name;
    }

    const godot::StringName &System::get_system_name_with_mod() const
    {
        static const godot::StringName string_name = godot::StringName("core.System");
        return string_name;
    }

    bool System::initialize()
    {
        return true;
    }

    void System::deinitialize()
    {
    }

    void System::process(double p_delta)
    {
    }

    bool System::ignores_pause()
    {
        return false;
    }

    void System::input(const godot::Ref<godot::InputEvent> &p_event)
    {
    }
}
