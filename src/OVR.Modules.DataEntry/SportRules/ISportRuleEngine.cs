namespace OVR.Modules.DataEntry.SportRules;

public interface ISportRuleEngine
{
    string DisciplineCode { get; }
    bool ValidateResult(object resultData);
    int CalculateRank(object resultData);
}
