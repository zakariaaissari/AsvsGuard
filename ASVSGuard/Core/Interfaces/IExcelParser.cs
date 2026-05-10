using ASVSGuard.Core.Entities;

namespace ASVSGuard.Core.Interfaces;

public interface IExcelParser
{
    IEnumerable<Exigence> Parse(Stream stream);
}
