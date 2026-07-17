using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Namespaces are reserved by the name of ModIds instead of following folder structure", Scope = "module")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Static use might be perfomant, but most of the time you have to enforce use of an accessor like Singleton/Instance", Scope = "module")]
