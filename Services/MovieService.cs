using Blockbuster.API.DTOs;
using Blockbuster.API.Models;
using Google.Cloud.Firestore;

namespace Blockbuster.API.Services
{
    /// <summary>
    /// MovieService implementa la gestión de películas
    /// Permite obtener, crear, editar y eliminar películas
    /// Solo administradores pueden crear, editar y eliminar
    /// </summary>
    public class MovieService : IMovieService
    {
        private readonly FirebaseService _firebaseService;

        /// <summary>
        /// Constructor: Recibe FirebaseService inyectado
        /// Se usa para acceder a Firestore
        /// </summary>
        public MovieService(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        /// <summary>
        /// GetAllMovies: Obtiene todas las películas (con filtro opcional por género)
        /// </summary>
        public async Task<List<MovieDto>> GetAllMovies(string? genre = null)
        {
            try
            {
                var moviesCollection = _firebaseService.GetCollection("movies");
                
                Query query = moviesCollection;

                // Si se especifica género, filtrar por él
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    query = query.WhereEqualTo("Genre", genre);
                }

                // Obtener snapshot (lectura de datos)
                var snapshot = await query.GetSnapshotAsync();

                // Convertir cada documento a MovieDto
                var movies = new List<MovieDto>();
                foreach (var doc in snapshot.Documents)
                {
                    var movieDict = doc.ToDictionary();
                    var movie = ConvertDictToMovie(movieDict);
                    movies.Add(ConvertToDto(movie));
                }

                return movies;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener películas: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// GetMovieById: Obtiene una película específica por su ID
        /// </summary>
        public async Task<MovieDto?> GetMovieById(string movieId)
        {
            try
            {
                var moviesCollection = _firebaseService.GetCollection("movies");
                var doc = await moviesCollection.Document(movieId).GetSnapshotAsync();

                // Si el documento no existe
                if (!doc.Exists)
                {
                    return null;
                }

                // Convertir a Movie y luego a MovieDto
                var movieDict = doc.ToDictionary();
                var movie = ConvertDictToMovie(movieDict);
                return ConvertToDto(movie);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener película: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// CreateMovie: Crea una nueva película (solo admin)
        /// </summary>
        public async Task<Movie> CreateMovie(Movie movie, string adminId)
        {
            try
            {
                // Validar que los datos requeridos existen
                if (string.IsNullOrWhiteSpace(movie.Title))
                {
                    throw new ArgumentException("El título es requerido");
                }

                if (string.IsNullOrWhiteSpace(movie.Description))
                {
                    throw new ArgumentException("La descripción es requerida");
                }

                // Generar ID si no lo tiene
                if (string.IsNullOrWhiteSpace(movie.Id))
                {
                    movie.Id = Guid.NewGuid().ToString();
                }

                // Establecer información de auditoría
                movie.CreatedAt = DateTime.UtcNow;
                movie.CreatedBy = adminId;

                // Inicializar contadores
                movie.AverageRating = 0;
                movie.TotalRatings = 0;

                // Guardar en Firestore usando Dictionary
                var movieData = new Dictionary<string, object>
                {
                    { "Id", movie.Id },
                    { "Title", movie.Title },
                    { "Description", movie.Description },
                    { "Genre", movie.Genre },
                    { "ReleaseYear", movie.ReleaseYear },
                    { "PosterUrl", movie.PosterUrl },
                    { "AverageRating", movie.AverageRating },
                    { "TotalRatings", movie.TotalRatings },
                    { "CreatedAt", movie.CreatedAt },
                    { "CreatedBy", movie.CreatedBy }
                };

                var moviesCollection = _firebaseService.GetCollection("movies");
                await moviesCollection.Document(movie.Id).SetAsync(movieData);

                Console.WriteLine($"Película creada: {movie.Title} ({movie.Id})");
                return movie;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear película: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// UpdateMovie: Edita una película existente (solo admin)
        /// </summary>
        public async Task<Movie> UpdateMovie(string movieId, Movie movie, string adminId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    throw new ArgumentException("El ID de película es requerido");
                }

                var moviesCollection = _firebaseService.GetCollection("movies");

                // Verificar que la película existe
                var existingDoc = await moviesCollection.Document(movieId).GetSnapshotAsync();
                if (!existingDoc.Exists)
                {
                    throw new InvalidOperationException($"Película con ID {movieId} no existe");
                }

                // Obtener la película existente para preservar campos de auditoría
                var existingDict = existingDoc.ToDictionary();
                var existingMovie = ConvertDictToMovie(existingDict);

                // Actualizar solo los campos permitidos
                existingMovie.Title = movie.Title ?? existingMovie.Title;
                existingMovie.Description = movie.Description ?? existingMovie.Description;
                existingMovie.Genre = movie.Genre ?? existingMovie.Genre;
                existingMovie.ReleaseYear = movie.ReleaseYear > 0 ? movie.ReleaseYear : existingMovie.ReleaseYear;
                existingMovie.PosterUrl = movie.PosterUrl ?? existingMovie.PosterUrl;

                // Guardar cambios usando Dictionary
                var movieData = new Dictionary<string, object>
                {
                    { "Id", existingMovie.Id },
                    { "Title", existingMovie.Title },
                    { "Description", existingMovie.Description },
                    { "Genre", existingMovie.Genre },
                    { "ReleaseYear", existingMovie.ReleaseYear },
                    { "PosterUrl", existingMovie.PosterUrl },
                    { "AverageRating", existingMovie.AverageRating },
                    { "TotalRatings", existingMovie.TotalRatings },
                    { "CreatedAt", existingMovie.CreatedAt },
                    { "CreatedBy", existingMovie.CreatedBy }
                };

                await moviesCollection.Document(movieId).SetAsync(movieData);

                Console.WriteLine($"Película actualizada: {movieId}");
                return existingMovie;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar película: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// DeleteMovie: Elimina una película (solo admin)
        /// </summary>
        public async Task DeleteMovie(string movieId, string adminId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    throw new ArgumentException("El ID de película es requerido");
                }

                var moviesCollection = _firebaseService.GetCollection("movies");
                var ratingsCollection = _firebaseService.GetCollection("ratings");

                // Verificar que la película existe
                var movieDoc = await moviesCollection.Document(movieId).GetSnapshotAsync();
                if (!movieDoc.Exists)
                {
                    throw new InvalidOperationException($"Película con ID {movieId} no existe");
                }

                // Verificar que no tiene calificaciones
                var ratingsQuery = await ratingsCollection
                    .WhereEqualTo("MovieId", movieId)
                    .GetSnapshotAsync();

                if (ratingsQuery.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"No se puede eliminar. La película tiene {ratingsQuery.Count} calificaciones. " +
                        "Debe eliminar las calificaciones primero."
                    );
                }

                // Eliminar la película
                await moviesCollection.Document(movieId).DeleteAsync();

                Console.WriteLine($"Película eliminada: {movieId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar película: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// SearchMovies: Busca películas por título (búsqueda simple)
        /// </summary>
        public async Task<List<MovieDto>> SearchMovies(string searchTerm)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return new List<MovieDto>();
                }

                // Obtener todas las películas
                var allMovies = await GetAllMovies();

                // Filtrar por título que contiene el término de búsqueda
                var searchLower = searchTerm.ToLower();
                var results = allMovies
                    .Where(m => m.Title.ToLower().Contains(searchLower))
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al buscar películas: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Método privado auxiliar: ConvertToDto
        /// Convierte un Movie (modelo interno) a MovieDto (lo que se envía al frontend)
        /// </summary>
        private MovieDto ConvertToDto(Movie movie)
        {
            return new MovieDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Genre = movie.Genre,
                ReleaseYear = movie.ReleaseYear,
                PosterUrl = movie.PosterUrl,
                AverageRating = movie.AverageRating,
                TotalRatings = movie.TotalRatings
            };
        }

        /// <summary>
        /// Método privado auxiliar: ConvertDictToMovie
        /// Convierte un diccionario de Firestore a objeto Movie
        /// </summary>
        private Movie ConvertDictToMovie(Dictionary<string, object> dict)
        {
            return new Movie
            {
                Id = dict["Id"].ToString(),
                Title = dict["Title"].ToString(),
                Description = dict["Description"].ToString(),
                Genre = dict["Genre"].ToString(),
                ReleaseYear = (int)(long)dict["ReleaseYear"],
                PosterUrl = dict["PosterUrl"].ToString(),
                AverageRating = Convert.ToDouble(dict["AverageRating"]),
                TotalRatings = (int)(long)dict["TotalRatings"],
                CreatedAt = ((Timestamp)dict["CreatedAt"]).ToDateTime(),
                CreatedBy = dict["CreatedBy"].ToString()
            };
        }
    }
}