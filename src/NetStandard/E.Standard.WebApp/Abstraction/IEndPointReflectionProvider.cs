namespace E.Standard.WebApp.Abstraction;

public interface IEndPointReflectionProvider
{
    T GetCustomAttribute<T>() where T : Attribute;

    T GetControllerCustomAttribute<T>() where T : Attribute;

    T GetActionMethodCustomAttribute<T>() where T : Attribute;
}
