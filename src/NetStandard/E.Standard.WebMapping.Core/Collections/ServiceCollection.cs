using E.Standard.ThreadSafe;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Collections;

public class ServiceCollection : ThreadSafeList<IMapService>
{
    public IMapService FindById(string id)
    {
        id = id.ToLower();
        foreach (IMapService service in this)
        {
            if (service.ID.ToLower() == id)
            {
                return service;
            }
        }
        return null;
    }

    public IMapService FindByName(string name)
    {
        foreach (IMapService service in this)
        {
            if (service.Name == name)
            {
                return service;
            }
        }
        return null;
    }

    public IMapService FindByLayer(ILayer layer)
    {
        if (layer == null)
        {
            return null;
        }

        foreach (IMapService service in this)
        {
            if (service == null || service.Layers == null)
            {
                continue;
            }

            if (service.Layers.FindById(layer.GlobalID) != null)
            {
                return service;
            }
        }
        return null;
    }

    public IMapService FindByUrl(string url)
    {
        foreach (IMapService service in this)
        {
            if (service.Url == url)
            {
                return service;
            }
        }
        return null;
    }

    public ServiceCollection Inverse()
    {
        ServiceCollection inverse = new ServiceCollection();

        foreach (IMapService service in this)
        {
            inverse.Insert(0, service);
        }
        return inverse;
    }

    private void Reorder(IEnumerable<string> serviceIds, bool inv)
    {
        if (serviceIds == null)
        {
            return;
        }

        //if (serviceIds.Length != this.Count) return;

        serviceIds = serviceIds?.Where(i => this.FindById(i) != null).MakeUnique<string>();

        ServiceCollection services = new ServiceCollection();
        foreach (string serviceId in serviceIds)
        {
            IMapService service = this.FindById(serviceId);
            if (service == null)
            {
                return;
            }

            services.Add(service);
        }

        // Alle übrigen Services, die nicht in der Liste waren
        // auch hinzufügen (Graphics, ...)
        foreach (IMapService service in this)
        {
            if (!services.Contains(service))
            {
                services.Insert(0, service);
            }
        }

        if (inv)
        {
            services = services.Inverse();
        }

        this.Clear();
        foreach (IMapService service in services)
        {
            this.Add(service);
        }
    }

    public void Reorder(string[] serviceIds)
    {
        Reorder(serviceIds, false);
    }
    public void ReorderInv(string[] serviceIds)
    {
        Reorder(serviceIds, true);
    }

    public List<IMapService> ByCollectionId(string collectionid)
    {
        List<IMapService> coll = new List<IMapService>();

        foreach (IMapService service in this)
        {
            if (service.CollectionId == collectionid)
            {
                coll.Add(service);
            }
        }

        return coll;
    }
}
