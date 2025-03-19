namespace E.Standard.DependencyInjection.Abstractions;
public interface IDependencyProvider
{
    object GetDependency(Type dependencyType);
}
