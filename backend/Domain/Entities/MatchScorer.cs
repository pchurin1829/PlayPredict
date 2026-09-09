namespace PlayPredict.Api.Domain.Entities;

public class MatchScorer
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    // Null cuando es autogol: un autogol cuenta para el resultado pero no se atribuye
    // a ningún jugador (nunca se crean jugadores ficticios para representarlo).
    public int? TeamPlayerId { get; set; }
    // Equipo beneficiado por el gol. En goles normales coincide con el equipo del
    // goleador; en autogoles es el equipo favorecido (el rival de quien lo convirtió).
    public int TeamId { get; set; }
    public bool IsOwnGoal { get; set; }
    public int Goals { get; set; }
    public Match Match { get; set; } = null!;
    public TeamPlayer? TeamPlayer { get; set; }
    public Team Team { get; set; } = null!;
}
