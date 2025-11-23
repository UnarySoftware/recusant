#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/os.hpp>
#include <godot_cpp/classes/class_db_singleton.hpp>
#include <godot_cpp/classes/object.hpp>
#include <godot_cpp/classes/file_access.hpp>
#include <godot_cpp/classes/config_file.hpp>

#include <unary.core/system/bootstrap.hpp>
#include <unary.core/system/system.hpp>
#include <unary.core/system/logger.hpp>

#include <algorithm>

using namespace godot;

namespace core
{
    const std::string __logger_class_name__ = "Bootstrap";

    Bootstrap *Bootstrap::instance()
    {
        return _instance;
    }

    void Bootstrap::_bind_methods()
    {
        godot::ClassDB::bind_static_method(NAMEOF(Bootstrap), D_METHOD("instance"), &Bootstrap::instance);
    }

    Bootstrap::InitializationStage Bootstrap::get_initialization_stage()
    {
        return _stage;
    }

    const std::vector<godot::StringName> &Bootstrap::get_mod_load_order() const
    {
        return _mod_load_order;
    }

    void Bootstrap::_setup_load_order()
    {
        Ref<ConfigFile> config;
        config.instantiate();

        PackedStringArray default_array;
        default_array.push_back("spacerisk");

        if (!FileAccess::file_exists("mods.txt"))
        {
            config->set_value("mods", "order", default_array);
            config->save("mods.txt");
        }

        config->load("mods.txt");

        Variant order = config->get_value("mods", "order", default_array);

        if (order.get_type() != Variant::Type::PACKED_STRING_ARRAY)
        {
            order = default_array;
        }

        PackedStringArray array = order;
        array.insert(0, "core");

        for (const auto &entry : array)
        {
            _mod_load_order.push_back(entry);
        }
    }

    System *Bootstrap::_get_or_initialize_system(const godot::StringName &p_name)
    {
        if (_instance == nullptr)
        {
            return nullptr;
        }

        System *target = _system_lookup.at(p_name);

        if (_stage == InitializationStage::Done)
        {
            return target;
        }

        if (!target->_initialized)
        {
            target->_initialized = true;
            if (!target->initialize())
            {
                _stage = InitializationStage::Failed;
                return nullptr;
            }
            _system_order.push_back(target);
        }

        return target;
    }

    void Bootstrap::crash(godot::String text, godot::String class_crasher)
    {
        if (_stage != InitializationStage::Initialization)
        {
            return;
        }

        OS::get_singleton()->alert(text, class_crasher);
    }

    System *Bootstrap::get_system(const godot::StringName &name)
    {
        return _get_or_initialize_system(name);
    }

    void Bootstrap::_ready()
    {
        if (godot::Engine::get_singleton()->is_editor_hint())
        {
            return;
        }

        _instance = this;

        Global::_initialized = true;

        _stage = InitializationStage::LookupSetup;

        ClassDBSingleton *class_db = ClassDBSingleton::get_singleton();
        PackedStringArray class_list = class_db->get_class_list();

        std::vector<StringName> classesNames;

        for (const auto &entry : class_list)
        {
            classesNames.push_back(entry);
        }

        std::vector<System *> temp_order;

        const StringName systemName(NAMEOF(System));

        for (const auto &entry : classesNames)
        {
            if (!class_db->is_parent_class(entry, systemName))
            {
                continue;
            }

            if (entry == systemName)
            {
                continue;
            }

            Variant variant_class = class_db->instantiate(entry);

            if (variant_class.get_type() != Variant::OBJECT)
            {
                continue;
            }

            System *system = godot::Object::cast_to<System>(variant_class);

            if (system == nullptr)
            {
                continue;
            }

            const StringName &full_name = system->get_system_name_with_mod();

            system->set_name(full_name);
            _system_lookup.emplace(full_name, system);
            temp_order.push_back(system);
            _instance->add_child(system);
        }

        // We are sorting all systems here in order for them to get
        // unique and determenistic id ordering
        std::sort(temp_order.begin(), temp_order.end(), [](System *a, System *b)
                  { return a->get_system_name_with_mod().hash() < b->get_system_name_with_mod().hash(); });

        _stage = InitializationStage::Initialization;

        _setup_load_order();

        for (const auto &mod_id : _mod_load_order)
        {
            for (const auto &entry : temp_order)
            {
                if (entry->_initialized)
                {
                    continue;
                }

                if (entry->get_mod_name() != mod_id)
                {
                    continue;
                }

                System *system = _get_or_initialize_system(entry->get_system_name_with_mod());

                // If we recieve a nullptr here, this means this system errored-out while initializing
                if (system == nullptr)
                {
                    goto break_out;
                }
            }
        }

    break_out:

        if (_stage == InitializationStage::Failed)
        {
            get_tree()->quit(0);
            return;
        }

        _stage = InitializationStage::Done;

        PRINT << "Initialized" << _system_order.size() << "systems!" << END;
    }

    void Bootstrap::_exit_tree()
    {
        for (int i = _system_order.size() - 1; i >= 0; i--)
        {
            System *target = _system_order[i];
            target->deinitialize();
        }

        _instance = nullptr;

        Global::_initialized = false;
    }

    void Bootstrap::_process(double p_delta)
    {
        for (const auto &system : _system_order)
        {
            system->process(p_delta);
        }
    }

    void Bootstrap::_input(const godot::Ref<godot::InputEvent> &p_event)
    {
        for (const auto &system : _system_order)
        {
            system->input(p_event);
        }
    }
}
