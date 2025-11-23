#include <unary.core/system/bootstrap.hpp>
#include <unary.core/system/network/network.hpp>
#include <unary.core/system/logger.hpp>
// #include <unary.core/system/launcher.hpp>

// #include <core/system/network/network_transport.hpp>
// #include <core/system/network/enet_transport.hpp>

using namespace godot;

namespace core
{
    void Network::_bind_methods()
    {
        BIND_SYSTEM();
    }

    /*
    Ref<NetworkTransport> Network::get_transport()
    {
        return _transport;
    }
    */
    bool Network::is_host()
    {
        /*
        if (_transport.is_valid())
        {
            return _transport->is_host();
        }
            */
        return false;
    }

    bool Network::is_client()
    {
        /*
        if (_transport.is_valid())
        {
            return _transport->is_client();
        }
            */
        return false;
    }

    uint64_t Network::register_packet(std::function<void()> p_start_read, std::function<void()> p_finish_read)
    {
        PacketEntry new_entry;
        new_entry.start_read = p_start_read;
        new_entry.finish_read = p_finish_read;

        _packets.insert({_packet_id, new_entry});
        return _packet_id++;
    }

    void Network::read_packet(uint64_t p_packet_id)
    {
        _packets.at(p_packet_id).start_read();
        _packets.at(p_packet_id).finish_read();
    }

    uint8_t Network::get_packet_bits()
    {
        return Global::bits_needed_for_encoding(_packets.size());
    }

    void Network::set_limits(uint8_t p_max_clients, uint8_t p_reliable_tickrate, uint8_t p_unreliable_tickrate, uint32_t p_max_buffer_size)
    {
        _max_clients = p_max_clients;
        _reliable_tickrate = p_reliable_tickrate;
        _unreliable_tickrate = p_unreliable_tickrate;
        _max_buffer_size = p_max_buffer_size;
    }

    uint8_t Network::get_max_clients()
    {
        return _max_clients;
    }

    uint8_t Network::get_reliable_tickrate()
    {
        return _reliable_tickrate;
    }

    uint8_t Network::get_unreliable_tickrate()
    {
        return _unreliable_tickrate;
    }

    uint32_t Network::get_max_buffer_size()
    {
        return _max_buffer_size;
    }

    bool Network::initialize()
    {
        /*
        if (Launcher::instance()->is_steam())
        {
            // TODO Implement Steam transport
        }
        else
        {
            _transport = godot::Ref<NetworkTransport>(memnew(ENetTransport));
        }

        return _transport->initialize();
*/
        return true;
    }

    void Network::deinitialize()
    {
        /*
        if (_transport.is_valid())
        {
            _transport->deinitialize();
        }
            */
    }

    void Network::_transport_flush()
    {
        /*
        if (_transport.is_valid())
        {
            _transport->flush();
        }
            */
    }

    void Network::process(double p_delta)
    {
        /*
        if (_transport.is_valid())
        {
            _transport->process();
        }

        _timer += p_delta;

        if (_timer >= 1.0 / get_reliable_tickrate())
        {
            _timer = 0.0;
            _transport_flush();
        }
            */
    }
}
