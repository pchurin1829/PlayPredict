using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

// Concepto central de PlayPredict (docs/arquitectura/MODELO_CONCEPTUAL_EXPERIENCIA_v1.0.md):
// el producto digital que una organización ofrece a sus usuarios. En este Sprint (MVP)
// solo contiene datos generales y la configuración de puntuación por defecto; branding
// avanzado, sponsors y participantes quedan para sprints posteriores.
public class Experience
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExperienceStatus Status { get; set; } = ExperienceStatus.Draft;
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsPublic { get; set; }

    // Configuración por defecto: los valores pertenecen directamente a la Experience
    // (sin "Motor" ni plantillas). Las Ediciones pueden heredarlos completamente.
    public int DefaultExactScorePoints { get; set; }
    public int DefaultCorrectOutcomePoints { get; set; }
    public int DefaultIncorrectPoints { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Competition> Competitions { get; set; } = new List<Competition>();
}
