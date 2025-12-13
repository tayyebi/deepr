using Deepr.Domain.Common;

namespace Deepr.Domain.Entities;

public class Issue : BaseEntity
{
    public string Title { get; private set; }
    public string ContextVector { get; private set; }
    public Guid OwnerId { get; private set; }
    public bool IsArchived { get; private set; }

    private Issue()
    {
        Title = string.Empty;
        ContextVector = string.Empty;
    }

    public Issue(string title, string contextVector, Guid ownerId)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        ContextVector = contextVector ?? throw new ArgumentNullException(nameof(contextVector));
        OwnerId = ownerId;
        IsArchived = false;
    }

    public void UpdateContext(string newContext)
    {
        if (string.IsNullOrWhiteSpace(newContext))
            throw new ArgumentException("Context cannot be empty", nameof(newContext));

        ContextVector = newContext;
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));

        Title = newTitle;
    }

    public void Archive()
    {
        IsArchived = true;
    }

    public void Restore()
    {
        IsArchived = false;
    }
}
