using Microsoft.AspNetCore.Mvc;

namespace AssetServer.ModelBinders;

internal class ResolutionBinderAttribute() : ModelBinderAttribute(typeof(ResolutionModelBinder));