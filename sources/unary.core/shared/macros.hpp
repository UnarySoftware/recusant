#pragma once

#include <godot_cpp/core/class_db.hpp>
#include <string>
#include <vector>

// Generic macros

#define __CONCAT(x, y) #x "" #y
#define __TOKEN_PASTE(x, y) x##y
#define __CAT(x, y) __TOKEN_PASTE(x, y)
#define NAMEOF(p_x) #p_x

#define GD_CLASS(p_class, p_inherits)                                        \
private:                                                                     \
    typedef p_inherits Super;                                                \
    typedef p_class CurrentClass;                                            \
    static const inline std::string __logger_class_name__ = NAMEOF(p_class); \
    GDCLASS(p_class, p_inherits);                                            \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

// System macros

#define DEFINE_SYSTEM(p_namespace, p_class, p_inherits)                                                                        \
    GD_CLASS(p_class, p_inherits);                                                                                             \
                                                                                                                               \
private:                                                                                                                       \
    static inline p_class *_instance = nullptr;                                                                                \
                                                                                                                               \
public:                                                                                                                        \
    static p_class *instance()                                                                                                 \
    {                                                                                                                          \
        if (_instance == nullptr)                                                                                              \
        {                                                                                                                      \
            _instance = godot::Object::cast_to<p_class>(core::Global::get_system(get_system_name_with_mod_static()));          \
        }                                                                                                                      \
        return _instance;                                                                                                      \
    }                                                                                                                          \
                                                                                                                               \
    const godot::StringName &                                                                                                  \
    get_mod_name() const override                                                                                              \
    {                                                                                                                          \
        static const godot::String name = #p_namespace;                                                                        \
        static const godot::StringName string_name = name;                                                                     \
        return string_name;                                                                                                    \
    }                                                                                                                          \
    const godot::StringName &get_system_name_with_mod() const override                                                         \
    {                                                                                                                          \
        static const godot::String concat_name = godot::String(#p_namespace) + "." + godot::String(#p_class).to_pascal_case(); \
        static const godot::StringName string_name = concat_name;                                                              \
        return string_name;                                                                                                    \
    }                                                                                                                          \
    static const godot::StringName &get_system_name_with_mod_static()                                                          \
    {                                                                                                                          \
        static const godot::String concat_name = godot::String(#p_namespace) + "." + godot::String(#p_class).to_pascal_case(); \
        static const godot::StringName string_name = concat_name;                                                              \
        return string_name;                                                                                                    \
    }                                                                                                                          \
                                                                                                                               \
private:                                                                                                                       \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define BIND_SYSTEM() \
    godot::ClassDB::bind_static_method(CurrentClass::get_class_static(), D_METHOD("instance"), &CurrentClass::instance)

// This should only be used by systems that need some kind of asset in place
#define ASSET_INJECT(p_type, p_name, p_path)                                              \
    godot::Ref<godot::p_type> __##p_name##_resource;                                      \
    godot::Ref<godot::p_type> get_##p_name##_resource()                                   \
    {                                                                                     \
        if (__##p_name##_resource.is_null())                                              \
        {                                                                                 \
            __##p_name##_resource = godot::ResourceLoader::get_singleton()->load(p_path); \
        }                                                                                 \
        return __##p_name##_resource;                                                     \
    }                                                                                     \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

// Properties (primarily used for Resources)

#define DEFINE_PROPERTY_NONCONST(p_type, p_name) \
private:                                         \
    p_type _##p_name;                            \
                                                 \
public:                                          \
    void set_##p_name(p_type p_##p_name)         \
    {                                            \
        _##p_name = p_##p_name;                  \
    }                                            \
    p_type get_##p_name()                        \
    {                                            \
        return _##p_name;                        \
    }                                            \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define DEFINE_PROPERTY_NONCONST_DEFAULT(p_type, p_name, p_default) \
private:                                                            \
    p_type _##p_name = p_default;                                   \
                                                                    \
public:                                                             \
    void set_##p_name(p_type p_##p_name)                            \
    {                                                               \
        _##p_name = p_##p_name;                                     \
    }                                                               \
    p_type get_##p_name()                                           \
    {                                                               \
        return _##p_name;                                           \
    }                                                               \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define DEFINE_PROPERTY(p_type, p_name)        \
private:                                       \
    p_type _##p_name;                          \
                                               \
public:                                        \
    void set_##p_name(const p_type p_##p_name) \
    {                                          \
        _##p_name = p_##p_name;                \
    }                                          \
    p_type get_##p_name() const                \
    {                                          \
        return _##p_name;                      \
    }                                          \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define DEFINE_PROPERTY_DEFAULT(p_type, p_name, p_default) \
private:                                                   \
    p_type _##p_name = p_default;                          \
                                                           \
public:                                                    \
    void set_##p_name(const p_type p_##p_name)             \
    {                                                      \
        _##p_name = p_##p_name;                            \
    }                                                      \
    p_type get_##p_name() const                            \
    {                                                      \
        return _##p_name;                                  \
    }                                                      \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define DEFINE_PROPERTY_TYPED_ARRAY(p_type, p_name)                \
private:                                                           \
    godot::TypedArray<p_type> p_name;                              \
                                                                   \
public:                                                            \
    void set_##p_name(const godot::TypedArray<p_type> &p_##p_name) \
    {                                                              \
        p_name = p_##p_name;                                       \
    }                                                              \
                                                                   \
    godot::TypedArray<p_type> get_##p_name() const                 \
    {                                                              \
        return p_name;                                             \
    }                                                              \
                                                                   \
private:                                                           \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define DEFINE_PROPERTY_TYPED_DICTIONARY(p_key_type, p_value_type, p_name)                \
private:                                                                                  \
    godot::TypedDictionary<p_key_type, p_value_type> p_name;                              \
                                                                                          \
public:                                                                                   \
    void set_##p_name(const godot::TypedDictionary<p_key_type, p_value_type> &p_##p_name) \
    {                                                                                     \
        p_name = p_##p_name;                                                              \
    }                                                                                     \
                                                                                          \
    godot::TypedDictionary<p_key_type, p_value_type> get_##p_name() const                 \
    {                                                                                     \
        return p_name;                                                                    \
    }                                                                                     \
                                                                                          \
private:                                                                                  \
    struct __CAT(semicolon_place, __LINE__) // Forces semicolon use

#define BIND_PROPERTY(p_type, p_name)                                                                  \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(p_type, #p_name), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

#define BIND_PROPERTY_HINTED(p_type, p_name, p_hint, p_hint_string)                                    \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(p_type, #p_name, p_hint, p_hint_string), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

#define BIND_PROPERTY_HINTED_USAGE(p_type, p_name, p_hint, p_hint_string, p_usage)                     \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(p_type, #p_name, p_hint, p_hint_string, p_usage), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

#define BIND_PROPERTY_PATH(p_type, p_name)                                                             \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(godot::Variant::OBJECT, #p_name, PROPERTY_HINT_NODE_TYPE, #p_type), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

#define BIND_PROPERTY_PATH_RESOURCE(p_type, p_name)                                                    \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(godot::Variant::OBJECT, #p_name, godot::PROPERTY_HINT_RESOURCE_TYPE, #p_type), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

#define BIND_PROPERTY_TYPED_ARRAY(p_name, p_format, ...)                                               \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(godot::Variant::ARRAY, #p_name, godot::PROPERTY_HINT_ARRAY_TYPE, godot::vformat(p_format, __VA_ARGS__)), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

#define BIND_PROPERTY_TYPED_DICTIONARY(p_name, p_format, ...)                                          \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(get_, p_name)), &CurrentClass::get_##p_name); \
    godot::ClassDB::bind_method(godot::D_METHOD(__CONCAT(set_, p_name)), &CurrentClass::set_##p_name); \
    ADD_PROPERTY(godot::PropertyInfo(godot::Variant::DICTIONARY, #p_name, godot::PROPERTY_HINT_DICTIONARY_TYPE, godot::vformat(p_format, __VA_ARGS__)), __CONCAT(set_, p_name), __CONCAT(get_, p_name))

// Debug only

#ifdef DEBUG_ENABLED
#define IS_EDITOR Util::is_editor()
#define IS_RUNTIME !(IS_EDITOR)
#define RUNTIME_ONLY(v) \
    if (IS_EDITOR)      \
    return v
#define EDITOR_ONLY(v) \
    if (IS_RUNTIME)    \
    return v
// TODO: Add GODOT_PROFILING_FUNCTION support https://github.com/godotengine/godot-cpp/issues/1032
#else
#define IS_EDITOR false
#define IS_RUNTIME true
#define RUNTIME_ONLY(v)
#define EDITOR_ONLY(v) return v;
#endif

// Global class
// It is used to implement useful methods that should be available from everywhere in code,
// while at the same time hiding additional required includes that you wouldnt want in a file
// that is included by almost every file

namespace godot
{
    class Node; // Forward-declare Node in order to prevent its accidental inclusion
}

namespace core
{
    class Bootstrap;
    class System;

    class Global
    {
    private:
        friend Bootstrap;
        static bool _initialized;
        static void _get_files_recursively(godot::String p_path, godot::String p_file_extension, godot::PackedStringArray &p_files);

    public:
        // TODO Move this to networking, its the only place where this is needed
        static constexpr uint8_t bits_needed_for_encoding(uint64_t n)
        {
            size_t size = sizeof(uint64_t) * CHAR_BIT;

            for (size_t i = size; i--;)
            {
                if ((n & (1ULL << i)) != 0)
                {
                    return i + 1;
                }
            }

            return 1;
        }

        static System *get_system(const godot::StringName &p_name);
        static bool is_editor();
        static bool is_host();
        static bool is_client();
        static bool is_instance_valid(const godot::Object *p_object);
        static void get_children_recursive(godot::Node *p_target, std::vector<godot::Node *> &p_children);
        static godot::PackedStringArray get_files_recursively(godot::String p_path, godot::String p_file_extension = "");
    };
}
