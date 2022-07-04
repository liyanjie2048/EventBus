using System.Threading.Tasks;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandler<in TEvent>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    Task HandleAsync(TEvent eventData);
}
