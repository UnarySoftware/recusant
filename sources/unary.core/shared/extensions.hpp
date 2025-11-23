#pragma once

#include <godot_cpp/variant/vector3.hpp>
#include <godot_cpp/variant/vector2.hpp>
#include <godot_cpp/variant/quaternion.hpp>
#include <godot_cpp/variant/aabb.hpp>
#include <godot_cpp/variant/basis.hpp>
#include <godot_cpp/variant/string.hpp>
#include <godot_cpp/templates/hashfuncs.hpp>

namespace godot
{
    class Node;

    namespace zero
    {
        static const godot::Vector3 Vector3(0.0f, 0.0f, 0.0f);
        static const godot::Vector2 Vector2(0.0f, 0.0f);
        static const godot::Quaternion Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        static const godot::AABB AABB(Vector3, Vector3);
    }
    namespace one
    {
        static const godot::Vector3 Vector3(1.0f, 1.0f, 1.0f);
        static const godot::Vector2 Vector2(1.0f, 1.0f);
    }
    namespace up
    {
        static const godot::Vector3 Vector3(0.0f, 1.0f, 0.0f);
    }
    namespace base
    {
        static const godot::Basis Basis(godot::Vector3(1.0f, 0.0f, 0.0f), godot::Vector3(0.0f, 1.0f, 0.0f), godot::Vector3(0.0f, 0.0f, 1.0f));
    }
    class hash
    {

    public:
        struct String
        {
            std::size_t operator()(const godot::String &h) const { return static_cast<std::size_t>(h.hash()); }
        };

        struct StringName
        {
            std::size_t operator()(const godot::StringName &h) const { return h.hash(); }
        };

        struct AABB
        {
            std::size_t operator()(const godot::AABB &a) const
            {
                return static_cast<std::size_t>(HashMapHasherDefault::hash(a));
            }
        };

        struct Vector2
        {
            std::size_t operator()(const godot::Vector2 &a) const
            {
                return static_cast<std::size_t>(HashMapHasherDefault::hash(a));
            }
        };
    };
}
