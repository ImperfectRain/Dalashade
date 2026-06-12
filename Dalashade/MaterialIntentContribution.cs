namespace Dalashade;

public sealed record MaterialIntentContribution(
    string Channel,
    string Source,
    float Amount,
    string Reason);
