using System.Threading.Tasks;

namespace Frever.Video.Core.Features.Views.ViewsExport;

public interface IVideoViewsExportService
{
    Task DoExport();
}