#include <unary.core/shared/macros.hpp>

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/dir_access.hpp>
#include <godot_cpp/classes/engine.hpp>

#include <unary.core/system/bootstrap.hpp>
#include <unary.core/system/network/network.hpp>

#include <bitset>

using namespace godot;

namespace core
{
    bool Global::_initialized = false;
    
    System *Global::get_system(const godot::StringName &p_name)
    {
        if (Bootstrap::instance() != nullptr)
        {
            return Bootstrap::instance()->get_system(p_name);
        }
        return nullptr;
    }

    bool Global::is_editor()
    {
        return (godot::Engine::get_singleton()->is_editor_hint() || !Global::_initialized);
    }

    bool Global::is_host()
    {
        return Network::instance()->is_host();
    }

    bool Global::is_client()
    {
        return Network::instance()->is_client();
    }

    bool Global::is_instance_valid(const godot::Object *p_object)
    {
        if (p_object == nullptr)
        {
            return false;
        }

        const uint64_t instance_id = p_object->get_instance_id();

        godot::Object *obj = godot::ObjectDB::get_instance(instance_id);

        return instance_id > 0 && obj != nullptr;
    }

    void Global::get_children_recursive(godot::Node *p_target, std::vector<godot::Node *> &p_children)
    {
        if (p_target == nullptr || !is_instance_valid(p_target))
        {
            return;
        }

        p_children.push_back(p_target);

        int32_t children = p_target->get_child_count();

        for (int32_t i = 0; i < children; ++i)
        {
            godot::Node *p_node = p_target->get_child(i);

            if (p_node == nullptr || !Global::is_instance_valid(p_node))
            {
                continue;
            }

            get_children_recursive(p_node, p_children);
        }
    }

    void Global::_get_files_recursively(godot::String p_path, godot::String p_file_extension, godot::PackedStringArray &p_files)
    {
        // Remove trailing slash if present
        if (p_path.ends_with("/") || p_path.ends_with("\\"))
        {
            p_path = p_path.left(p_path.length() - 1);
        }

        Ref<DirAccess> dir = DirAccess::open(p_path);

        if (dir == nullptr)
        {
            return;
        }

        dir->list_dir_begin();

        String file_name = dir->get_next();

        while (file_name != "")
        {
            if (dir->current_is_dir())
            {
                _get_files_recursively(dir->get_current_dir() + "/" + file_name, p_file_extension, p_files);
            }
            else
            {
                if (!p_file_extension.is_empty() && file_name.get_extension() != p_file_extension)
                {
                    file_name = dir->get_next();
                    continue;
                }
                p_files.append(dir->get_current_dir() + "/" + file_name);
            }

            file_name = dir->get_next();
        }
    }

    PackedStringArray Global::get_files_recursively(String p_path, String p_file_extension)
    {
        PackedStringArray result;
        _get_files_recursively(p_path, p_file_extension, result);
        return result;
    }
}
