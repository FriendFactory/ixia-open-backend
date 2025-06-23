using System;

namespace Frever.Client.Core.Utils.Models;

internal interface IPublication
{
    DateTime? PublicationDate { get; set; }
    DateTime? DepublicationDate { get; set; }
}