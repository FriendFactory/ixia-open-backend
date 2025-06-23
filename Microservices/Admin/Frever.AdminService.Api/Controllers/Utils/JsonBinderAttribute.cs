using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers.Utils;

public class JsonBinderAttribute() : ModelBinderAttribute(typeof(JsonModelBinder));