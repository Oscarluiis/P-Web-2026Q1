namespace Blockbuster.API.Models;

/// <summary>
/// Rating representa una calificación que un usuario da a una pelicula
/// Se almacena como documento en la coleccion "ratings" de FS
///
///Relacion: Un usario puede tener multiples ratings
///          Una pelicula puede tener multiples ratings
/// 
/// </summary>

public class Rating
{
    
    /**
     * Identificador unico para el rating
     * String porque FB lo va gestionar
     */
    public string Id { get; set; } = string.Empty;
    
    /**
     * MovieId de la pelicula
     * Necesario para saber a que pelicula pertenece la calificación
     */
    public string MovieId { get; set; } = string.Empty;
    
    /**
     * MovieTitle como se llama la pelicula
     */
    public string MovieTitle { get; set; } = string.Empty;
    
    /**
     * UserId para saber que usuario esta calificando
     * x2546x6464-sadas
     */
    public string UserId { get; set; } = string.Empty;
    
    /**
     * Nombre del usuario que la califica
     * Juan Bustillo
     */
    public string UserName { get; set; } = string.Empty;
    
    /**
     * Calificacion que le otorga el usuario
     * 0.0 - 10.0
     */
    public double Score { get; set; }
    
    /**
     * Review, que le parecio la pelicula
     */
    public string Review { get; set; } = string.Empty;
    
    /**
     * Cuando se creo
     */
    public DateTime CreatedAt { get; set; }
    
    /**
     * Cuando se actualizo por ultima vez
     */
    public DateTime UpdatedAt { get; set; }
}