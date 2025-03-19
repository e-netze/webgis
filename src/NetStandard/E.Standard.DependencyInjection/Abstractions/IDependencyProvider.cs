using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.DependencyInjection.Abstractions;
public interface IDependencyProvider
{
    object GetDependency(Type dependencyType);
}
