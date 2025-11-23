#include <register_types.hpp>

#include <godot_cpp/core/class_db.hpp>

// We dont use anything but the runtime classes here
// Registering a class as a runtime one is of higher priority than
// having some of them be abstract and then being forced into a default registration
// that makes you wrap all methods into is_runtime checks
#define BIND_CLASS(m_class) \
    ::godot::ClassDB::register_runtime_class<m_class>()

#define BIND_RESOURCE(m_class) \
    ::godot::ClassDB::register_class<m_class>()

extern "C"
{
    GDExtensionBool GDE_EXPORT game_init(
        GDExtensionInterfaceGetProcAddress p_get_proc_address,
        GDExtensionClassLibraryPtr p_library,
        GDExtensionInitialization *r_initialization)
    {
        godot::GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);

        init_obj.register_initializer(initialize_game);
        init_obj.register_terminator(uninitialize_game);
        init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);

        return init_obj.init();
    }
}

void uninitialize_game(godot::ModuleInitializationLevel p_level)
{
    if (p_level != godot::MODULE_INITIALIZATION_LEVEL_SCENE)
    {
        return;
    }
}

// Core Resources
/*
#include <core/resources/mod_manifest.hpp>

// Core

#include <core/system/bootstrap.hpp>
#include <core/system/system.hpp>
#include <core/system/logger.hpp>
#include <core/system/launcher.hpp>
#include <core/system/steam.hpp>
#include <core/system/network/packet_writer.hpp>
#include <core/system/network/packet_reader.hpp>
#include <core/system/network/network.hpp>
#include <core/system/network/network_transport.hpp>
#include <core/system/network/enet_transport.hpp>

// Spacerisk Resources

#include <spacerisk/prototype/shared/prototype.hpp>
#include <spacerisk/prototype/planet/planet_biome_prototype.hpp>
#include <spacerisk/prototype/planet/planet_obstruction_prototype.hpp>
#include <spacerisk/prototype/planet/planet_place_prototype.hpp>
#include <spacerisk/prototype/game/tile_prototype.hpp>

// Spacerisk

#include <spacerisk/system/dynamic_string_database.hpp>
#include <spacerisk/system/static_string_database.hpp>
#include <spacerisk/system/prototype_database.hpp>
#include <spacerisk/planet/planet_tilemap.hpp>
#include <spacerisk/planet/planet_camera.hpp>
#include <spacerisk/planet/planet_selector.hpp>
#include <spacerisk/system/tilemap_manager.hpp>
*/
void initialize_game(godot::ModuleInitializationLevel p_level)
{
    if (p_level != godot::MODULE_INITIALIZATION_LEVEL_SCENE)
    {
        return;
    }
/*
    // Core Resources
    BIND_RESOURCE(core::ModManifest);

    // Core
    BIND_CLASS(core::System);
    BIND_CLASS(core::Bootstrap);
    BIND_CLASS(core::Logger);
    BIND_CLASS(core::Launcher);
    BIND_CLASS(core::Steam);
    BIND_CLASS(core::PacketWriter);
    BIND_CLASS(core::PacketReader);
    BIND_CLASS(core::Network);
    BIND_CLASS(core::NetworkTransport);
    BIND_CLASS(core::ENetTransport);

    // Spacerisk Resources
    BIND_CLASS(spacerisk::Prototype);
    BIND_CLASS(spacerisk::PlanetBiomePrototype);
    BIND_CLASS(spacerisk::PlanetObstructionPrototype);
    BIND_CLASS(spacerisk::PlanetPlacePrototype);
    BIND_CLASS(spacerisk::TilePrototype);

    // Spacerisk
    BIND_CLASS(spacerisk::DynamicStringDatabase);
    BIND_CLASS(spacerisk::StaticStringDatabase);
    BIND_CLASS(spacerisk::PrototypeDatabase);
    BIND_CLASS(spacerisk::PlanetTileMap);
    BIND_CLASS(spacerisk::PlanetCamera);
    BIND_CLASS(spacerisk::PlanetSelector);
    BIND_CLASS(spacerisk::TileMapManager);
	*/
}
