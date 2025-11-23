#pragma once

#include <unordered_map>
#include <unary.core/shared/macros.hpp>
#include <unary.core/shared/extensions.hpp>

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/classes/input_event.hpp>

namespace core
{
    class System;

    class Bootstrap : public godot::Node
    {
        GDCLASS(Bootstrap, Node);

    public:
        enum InitializationStage
        {
            None,
            LookupSetup,
            Initialization,
            Done,
            Failed
        };

        static InitializationStage get_initialization_stage();

    private:
        static inline Bootstrap *_instance = nullptr;

        static inline InitializationStage _stage = InitializationStage::None;

        static inline uint16_t _id_counter = 0;
        static inline std::vector<System *> _system_order;
        static inline std::unordered_map<godot::StringName, System *, godot::hash::StringName> _system_lookup;

        std::vector<godot::StringName> _mod_load_order;

        void _setup_load_order();

        static System *_get_or_initialize_system(const godot::StringName &p_name);

    protected:
        static void _bind_methods();

    public:
        const std::vector<godot::StringName> &get_mod_load_order() const;

        static void crash(godot::String text, godot::String class_crasher);

        static System *get_system(const godot::StringName &name);

        template <typename T>
        T *get_system()
        {
            static_assert(std::is_base_of<System, T>::value, "T must derive from System");

            const godot::StringName &name = T::get_class_name_with_mod_static();

            return godot::Object::cast_to<T>(get_system(name));
        }

        static Bootstrap *instance();

        void _ready() override;
        void _exit_tree() override;
        void _process(double p_delta) override;
        void _input(const godot::Ref<godot::InputEvent> &p_event) override;
    };
}
