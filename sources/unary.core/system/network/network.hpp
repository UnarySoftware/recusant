#pragma once

#include <unary.core/system/system.hpp>

#include <godot_cpp/classes/ref.hpp>
// #include <core/system/network/network_transport.hpp>

#include <functional>

namespace core
{
    class Network : public System
    {
        DEFINE_SYSTEM(core, Network, System);

    private:
        // godot::Ref<NetworkTransport> _transport;

        struct PacketEntry
        {
            std::function<void()> start_read;
            std::function<void()> finish_read;
        };

        uint64_t _packet_id = 1;
        std::unordered_map<uint64_t, PacketEntry> _packets;

        double _timer = 0.0;
        void _transport_flush();

        uint8_t _max_clients = 4;
        uint8_t _reliable_tickrate = 10;
        uint8_t _unreliable_tickrate = 20;
        uint32_t _max_buffer_size = 256 * 1024;

    protected:
        static void _bind_methods();

    public:
        // godot::Ref<NetworkTransport> get_transport();

        bool is_host();
        bool is_client();

        uint64_t register_packet(std::function<void()> p_start_read, std::function<void()> p_finish_read);
        void read_packet(uint64_t p_packet_id);
        uint8_t get_packet_bits();

        void set_limits(uint8_t p_max_clients, uint8_t p_reliable_tickrate, uint8_t p_unreliable_tickrate, uint32_t p_max_buffer_size);
        uint8_t get_max_clients();
        uint8_t get_reliable_tickrate();
        uint8_t get_unreliable_tickrate();
        uint32_t get_max_buffer_size();

        bool initialize() override;
        void deinitialize() override;
        void process(double p_delta) override;
    };
}
