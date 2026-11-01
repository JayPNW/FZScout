namespace FZScout.Tests;

public class ProgramTests
{
    /// <summary>What the real app is wired to: a type for each object, and a method for each function.</summary>
    [Fact]
    public Task RealDependencies()
    {
        var dependencies = Program.CreateApp().Dependencies;

        var description = typeof(AppDependencies)
            .GetProperties()
            .ToDictionary(property => property.Name, property => Describe(property.GetValue(dependencies)));

        return Verify(description);
    }

    static string Describe(object? dependency) => dependency switch
    {
        null => "null",
        Delegate function => $"{function.Method.DeclaringType!.Name}.{function.Method.Name}",
        _ => dependency.GetType().Name,
    };
}
