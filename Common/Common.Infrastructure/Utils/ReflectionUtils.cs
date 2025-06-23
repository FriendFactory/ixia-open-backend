using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Common.Infrastructure.Utils;

public static class ReflectionUtils
{
    public static async Task<T> InvokeGenericAsync<T>(
        this MethodInfo methodInfo,
        Type genericArgument,
        object thisVal,
        params object[] arguments
    )
    {
        var task = (Task) methodInfo.MakeGenericMethod(genericArgument).Invoke(thisVal, arguments);

        await task;

        return (T) task.GetType().GetProperty("Result").GetValue(task);
    }


    public static async Task<T> InvokeAsync<T>(this MethodInfo methodInfo, object thisVal, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        var task = (Task) methodInfo.Invoke(thisVal, arguments);

        await task;

        return (T) task.GetType().GetProperty("Result").GetValue(task);
    }
}