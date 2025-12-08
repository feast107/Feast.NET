List<Func<Task<(int number,int delay)>>> handlers = [];
foreach (var number in Enumerable.Range(1,2))
{
    handlers.Add(Handler(number));
}

var tasks = handlers.Select(x => x()).ToArray();
while (true)
{
    var task  = await Task.WhenAny(tasks);
    var index = tasks.IndexOf(task);
    Console.WriteLine($"notify complete : {index + 1} - {task.Result.delay}");
    tasks[task.Result.number - 1] = handlers[task.Result.number - 1]();
}

Func<Task<(int number,int delay)>> Handler(int number)
{
    return async () =>
    {
        var delay = (int)(new Random().NextSingle() * 3000);
        await Task.Delay(delay);
        Console.WriteLine($"Self complete : {number} - {delay}");
        return (number, delay);
    };
}