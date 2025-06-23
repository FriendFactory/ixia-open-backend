using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Common.Infrastructure.ServiceInfo;

public class ServiceInfoControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var currentAssembly = Assembly.GetEntryAssembly();

        var type = currentAssembly.GetTypes().First(x => x.Name.Equals("Startup"));

        feature.Controllers.Add(typeof(ServiceInfoControllerBase<>).MakeGenericType(type).GetTypeInfo());
        ;
    }
}