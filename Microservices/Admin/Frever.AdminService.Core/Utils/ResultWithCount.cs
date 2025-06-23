namespace Frever.AdminService.Core.Utils;

public class ResultWithCount<T>
{
    public T[] Data { get; set; }

    public int Count { get; set; }
}