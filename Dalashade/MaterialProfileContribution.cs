namespace Dalashade;

public sealed record MaterialProfileContribution(
    string Channel,
    string Source,
    float Amount,
    string Reason);
