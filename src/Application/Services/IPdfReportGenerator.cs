using WcagAnalyzer.Application.Features.Analysis.Queries;

namespace WcagAnalyzer.Application.Services;

public interface IPdfReportGenerator
{
    byte[] Generate(GetAnalysisByIdResult analysis);
    int CalculateScore(GetAnalysisByIdResult analysis);
}
