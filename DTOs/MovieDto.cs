namespace Blockbuster.API.DTOs;
/// <summary>
/// MovieDto es el objeto que se envia al frontend cuando pide peliculas el user
/// NO expone toda la informacion interna, solo cuando es necesario mostrarla
/// </summary>
public class MovieDto
{
    /**
     * Id: Identificador de la pelicula
     * El frontend lo necesita para hacer solicitudes / request especificicos de las peliculas
     */
    
    public string Id { get; set; } = string.Empty;
    
    /**
     * Titulo es el que se muestra en la interfaz
     */
    public string Title { get; set; } = string.Empty;
    
    /**
     * Description sinopsis que aparece en la pagina de detalles
     */
    public string Description { get; set; } = string.Empty;
    
    /**
     * Genre para filtrar las peliculas por genero
     */
    public string Genre { get; set; } = string.Empty;
    
    /**
     * ReleaseYear para mostrar cuando se estreno
     */
    public int ReleaseYear { get; set; }
    
    /**
     * PosterUrl que es la imagen que se muestra en las tarjetas
     */
    public string PosterUrl { get; set; } = string.Empty;
    
    /**
     * AverageRating la calificacion promedio que ve el usuario
     */
    public double AverageRating { get; set; }
    
    /**
     * TotalRating cuantas personas la ha calificado (la pelicula)
     */
    public int TotalRating { get; set; }
}