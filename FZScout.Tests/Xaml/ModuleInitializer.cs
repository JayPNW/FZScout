using System.Runtime.CompilerServices;
using WpfBindingErrors;

namespace FZScout.Tests.Xaml;

static class ModuleInitializer
{
    [ModuleInitializer]
    public static void InitializeVerifyXaml() => VerifyXaml.Initialize();

    /// <summary>WPF doesn't throw for a binding it can't resolve, like a path to a property the view model doesn't have; it shows a default value instead.</summary>
    [ModuleInitializer]
    public static void ThrowOnBindingErrors() => BindingExceptionThrower.Attach();
}
