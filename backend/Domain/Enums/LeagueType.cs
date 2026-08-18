namespace PlayPredict.Api.Domain.Enums;

/// <summary>
/// Tipo de Liga: quién la creó y gestiona.
/// </summary>
public enum LeagueType
{
    /// <summary>
    /// Liga Oficial creada por PlayPredict/ADMIN.
    /// Visible con badge distintivo.
    /// </summary>
    Official = 0,
    
    /// <summary>
    /// Liga Privada creada por un PLAYER.
    /// Puede ser propia (el usuario es creador) o de amigos (el usuario participa).
    /// </summary>
    Private = 1
}
