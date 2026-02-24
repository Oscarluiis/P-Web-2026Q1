namespace Blockbuster.API.Models;

/// <summary>
/// Movie representa una pelicula o serie en la base de datos
/// Se almacena como un documento en la coleccion "movies" de Firestore
/// </summary>

public class Movie
{
    /** Id: Identificador unico de la pelicula
     *  En firestores, es el documento ID (autogenera)
     * Ejemplo: movie_001, etc...
    */

    public string Id { get; set; } = string.Empty;

    /**
     * Titulo
     * Titulo de la pelicula o serie
     * 007: goldeneye
     */


    public string Title { get; set; } = string.Empty;

    /**
     * Descripcion
     * De que trata...
     */

    public string Description { get; set; } = string.Empty;

    /**
     * Genero
     * Horror, accion, etc
     */
    public string Genre { get; set; } = string.Empty;

    /**
     * Año
     * 2020, 2021, etc
     */
    public int ReleaseYear { get; set; }

    /**
     * Poster
     * Almacena una url de la imagen de la pelicula
     * Se almacena en FS
     */
    public string PosterUrl { get; set; } = string.Empty;


    /*
     * Promedio
     * Califacion promedio de todos los usuarios
     * Se calcula automaticamente a partir de los ratings
     * 0.0 - 10.0
     */
    public double AverageRating { get; set; }

    /*
     * Rating Total
     * Se incrementa cada vez que un usuario califica la pelicula
     */
    public int TotalRating { get; set; }

    /*
     * Cuando se creo fecha y hora que se agrego la pelicula
     * Se asigna automaticamente al crear el documento
     */
    public DateTime CreatedAt { get; set; }

    /*
     * Creado por ID del usuario que creo la pelicula (admin)
     * Solo admins pueden agregar peliculas
     */
    public string CreatedBy { get; set; } = string.Empty;

}