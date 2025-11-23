#pragma once

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/variant/string_name.hpp>

#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/classes/input_event.hpp>

#include <unary.core/shared/macros.hpp>

namespace core
{
    class Network;

    class System : public godot::Node
    {
        GDCLASS(System, Node);
        friend Bootstrap;
        friend Network;

        bool _initialized = false;
        uint16_t _id = UCHAR_MAX;

    protected:
        static void _bind_methods()
        {
        }

    public:
        virtual const godot::StringName &get_mod_name() const;
        virtual const godot::StringName &get_system_name_with_mod() const;
        virtual bool initialize();
        virtual void deinitialize();
        virtual void process(double p_delta);
        virtual bool ignores_pause();
        virtual void input(const godot::Ref<godot::InputEvent> &p_event);

        bool is_initialized()
        {
            return _initialized;
        }

        uint16_t get_id()
        {
            return _id;
        }
    };
}
