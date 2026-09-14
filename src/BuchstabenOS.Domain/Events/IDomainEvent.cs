namespace BuchstabenOS.Domain.Events;

/// <summary>
/// Markierungs-Schnittstelle für alle fachlichen Domänen-Ereignisse.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
