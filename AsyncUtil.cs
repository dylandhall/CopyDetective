using System.Collections.Concurrent;
using Dasync.Collections;

namespace CopyDetective;

public static class AsyncUtil
{

    public static async IAsyncEnumerable<T2> ParallelSelectEnumAsync<T, T2>(
        this IEnumerable<T> items,
        Func<T, Task<T2>> processor,
        int maxDegreeOfParallelism = 8,
        bool includeNulls = false)
    {
        if (items == null) yield break;
        var tasks = new HashSet<Task<T2>>();

        foreach (var item in items)
        {
            var task = processor(item);
            tasks.Add(task);

            if (tasks.Count < maxDegreeOfParallelism) continue;

            var completedTask = await Task.WhenAny(tasks);
            if (includeNulls || await completedTask != null)
                yield return await completedTask;
            tasks.Remove(completedTask);
        }

        // Process remaining tasks
        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            if (includeNulls || await completedTask != null)
                yield return await completedTask;
            tasks.Remove(completedTask);
        }
    }
}