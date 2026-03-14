namespace Blockbuster.API.DTOs;
/// <summary>
/// RatingDto es lo que se envia cuando el frontend quiere:
/// 1. Ver las calificaciones de una pelicula (GET)
/// 2. Crear una nueva calificacion (POST)
/// 3. Editar una calificacion existente (PUT)
/// </summary>

public class RatingDto
{
    /**
     * Id: identificador de la calificacion
     * Solo se incluye cuando se envia una calificacion existente (GET)
     * Es null cuando se crea una nueva (POST)
     */
    
    public string Id { get; set; } = string.Empty;
    
    /**
     * MovieId para mostrar rapidamente que pelicula es
     * El frontend no tiene que hacer otra llamada para saberlo
     */
    public string MovieId { get; set; } = string.Empty;
    
    /**
    * MovieTitle para mostrar rápidamente de qué película es
    * El frontend no tiene que hacer otra llamada para saberlo
    */
    public string MovieTitle { get; set; } = string.Empty;
    
    /**
     * Score la calificacion de 1-10
     * El frontend valida que este entre 1 y 10
     */
    public double Score { get; set; }
    
    /**
     * Review es el comentario que va dejar el usuario
     * Puede estar vacio (nullable) si solamente quiere calificar
     */
    public string Review { get; set; } = string.Empty;
    
    /**
     * Username es quien escribio la calificacion
     * Se muestra debajo del comentario
     */
    public string UserName  { get; set; } = string.Empty;
    
    /**
     * CreatedAt cuando se escribio la reseña
     */
    public DateTime CreatedAt { get; set; }
}
/// <summary>
/// CreateRatingDto es lo que se recibe desde el backend cuando el usuario crea un rating
/// Solo contiene lo necesario
/// </summary>
public class CreateRatingDto
{
    /**
     * MovieId para saber la pelicula exacta
     */
    public string MovieId { get; set; } = string.Empty;
    /**
     * La calificaion que le otorga
     */
    public double Score { get; set; }
    
    /**
     * El comentario de su opinion (si aplica)
     */
    public string Review { get; set; } = string.Empty;
}