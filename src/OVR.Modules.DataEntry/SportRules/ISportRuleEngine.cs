namespace OVR.Modules.DataEntry.SportRules;

public interface ISportRuleEngine
{
    string Discipline { get; }
    bool ValidateResult(object resultData);
    int CalculateRank(object resultData);
}
