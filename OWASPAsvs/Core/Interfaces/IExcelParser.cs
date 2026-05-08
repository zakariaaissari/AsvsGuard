using OWASPAsvs.Core.Entities;

namespace OWASPAsvs.Core.Interfaces;

public interface IExcelParser
{
    IEnumerable<Exigence> Parse(Stream stream);
}
