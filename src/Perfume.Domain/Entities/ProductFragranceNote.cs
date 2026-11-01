using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class ProductFragranceNote
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int FragranceNoteId { get; set; }
    public FragranceNote? FragranceNote { get; set; }

    public NoteLevel NoteLevel { get; set; } = NoteLevel.Top; // Top / Heart / Base
    public int DisplayOrder { get; set; } = 0;
}
