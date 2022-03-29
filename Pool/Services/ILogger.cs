using Pool.Models;

namespace Pool.Services;
public interface ILogger
{
    void Log(Severity severity, string msg);
}