#include <godot_cpp/variant/utility_functions.hpp>

#include <unary.core/system/bootstrap.hpp>
#include <unary.core/system/logger.hpp>
//#include <unary.core/events/log_output.hpp>

#include <sstream>

using namespace godot;

namespace core
{
    void Logger::_bind_methods()
    {
        BIND_SYSTEM();
    }

    bool Logger::initialize()
    {
        return true;
    }

    void Logger::deinitialize()
    {
    }

    Logger &Logger::operator<<(const godot::Variant &p_arg)
    {
        _buffer += ' ';
        _buffer += p_arg.stringify().utf8();
        return *this;
    }

    Logger &Logger::operator<<(const Logger::End &p_arg)
    {
        const char *result = _buffer.c_str();
        switch (_type)
        {
        default:
        case Print:
        {
            godot::UtilityFunctions::print(result);
            break;
        }
        case Warning:
        {
            godot::UtilityFunctions::push_warning(result);
            break;
        }
        case Error:
        {
            Bootstrap *bootstrap = Bootstrap::instance();
            if (bootstrap->get_initialization_stage() == Bootstrap::InitializationStage::Initialization)
            {
                bootstrap->crash(result, _file.c_str());
                return *this;
            }

            godot::UtilityFunctions::push_error(result);
            break;
        }
        case Rich:
        {
            godot::UtilityFunctions::print_rich(result);
            break;
        }
        }

        //LogOutput::publish(_type, result);

        _buffer.clear();

        return *this;
    }

    Logger &Logger::print(std::string p_class, int p_line)
    {
        Logger *target = Logger::instance();
        target->_buffer += '[' + p_class + ':' + std::to_string(p_line) + "]:";
        target->_class = p_class;
        target->_type = Print;
        return *target;
    }

    Logger &Logger::warning(std::string p_class, int p_line)
    {
        Logger *target = instance();
        target->_buffer += '[' + p_class + ':' + std::to_string(p_line) + "]:";
        target->_class = p_class;
        target->_type = Warning;
        return *target;
    }

    Logger &Logger::error(std::string p_file, std::string p_class, int p_line)
    {
        Logger *target = instance();
        target->_buffer += '[' + p_class + ':' + std::to_string(p_line) + "]:";
        target->_class = p_class;
        target->_file = p_file;
        target->_type = Error;
        return *target;
    }

    Logger &Logger::rich(std::string p_class, int p_line)
    {
        Logger *target = instance();
        target->_buffer += '[' + p_class + ':' + std::to_string(p_line) + "]:";
        target->_class = p_class;
        target->_type = Rich;
        return *target;
    }

    Logger::End Logger::end()
    {
        return Logger::End::Flush;
    }
}
