#pragma once

#include <unary.core/system/system.hpp>

#define PRINT core::Logger::instance()->print(__logger_class_name__, __LINE__)
#define PRINT_CLASS(p_class) core::Logger::instance()->print(#p_class, __LINE__)

#define WARNING core::Logger::instance()->warning(__logger_class_name__, __LINE__)
#define WARNING_CLASS(p_class) core::Logger::instance()->warning(#p_class, __LINE__)

#define ERROR core::Logger::instance()->error(__FILE__, __logger_class_name__, __LINE__)
#define ERROR_CLASS(p_class) core::Logger::instance()->error(__FILE__, #p_class, __LINE__)

#define RICH core::Logger::instance()->rich(__logger_class_name__, __LINE__)
#define RICH_CLASS(p_class) core::Logger::instance()->rich(#p_class, __LINE__)

#define END core::Logger::end()

namespace core
{
    class Logger : public System
    {
        DEFINE_SYSTEM(core, Logger, System);

    public:
        enum Type
        {
            Print,
            Warning,
            Error,
            Rich
        };

        enum End
        {
            Flush
        };

    private:
        Type _type;

        static inline std::string _class;
        static inline std::string _file;
        static inline std::string _buffer;

    protected:
        static void _bind_methods();

    public:
        bool initialize() override;
        void deinitialize() override;

        Logger &operator<<(const godot::Variant &p_arg);
        Logger &operator<<(const Logger::End &p_arg);

        static Logger &print(std::string p_class, int p_line);
        static Logger &warning(std::string p_class, int p_line);
        static Logger &error(std::string p_file, std::string p_class, int p_line);
        static Logger &rich(std::string p_class, int p_line);
        static Logger::End end();
    };
}
