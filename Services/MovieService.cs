using Blockbuster.API.DTOs;
using Blockbuster.API.Models;

namespace Blockbuster.API.Services;


using Google.Cloud.Firestore;

public class MovieService : IMovieService
{
    /// <summary>
    /// MovieService implementa la gestión de películas
    /// Permite obtener, crear, editar y eliminar películas
    /// Solo administradores pueden crear, editar y eliminar
    /// </summary>
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
        /// 
        /// Proceso:
        /// 1. Obtener la colección "movies" de Firestore
        /// 2. Si se especifica género, filtrar
        /// 3. Convertir documentos a MovieDto
        /// 4. Devolver lista
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
                    var movie = doc.ConvertTo<Movie>();
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
        /// 
        /// Proceso:
        /// 1. Acceder al documento por ID
        /// 2. Verificar que existe
        /// 3. Convertir a MovieDto
        /// 4. Devolver
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
                var movie = doc.ConvertTo<Movie>();
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
        /// 
        /// Proceso:
        /// 1. Validar que los datos requeridos existen
        /// 2. Generar ID si no lo tiene
        /// 3. Establecer CreatedAt y CreatedBy
        /// 4. Guardar en Firestore
        /// 5. Devolver la película creada
        /// </summary>
        public async Task<Movie> CreateMovie(Movie movie, string adminId)
        {
            try
            {
                // Validar que los campos requeridos no estén vacíos
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

                // Guardar en Firestore
                var moviesCollection = _firebaseService.GetCollection("movies");
                await moviesCollection.Document(movie.Id).SetAsync(movie);

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
        /// 
        /// Proceso:
        /// 1. Verificar que la película existe
        /// 2. Actualizar los campos permitidos
        /// 3. Guardar en Firestore
        /// 4. Devolver película actualizada
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
                var existingMovie = existingDoc.ConvertTo<Movie>();

                // Actualizar solo los campos permitidos
                existingMovie.Title = movie.Title ?? existingMovie.Title;
                existingMovie.Description = movie.Description ?? existingMovie.Description;
                existingMovie.Genre = movie.Genre ?? existingMovie.Genre;
                existingMovie.ReleaseYear = movie.ReleaseYear > 0 ? movie.ReleaseYear : existingMovie.ReleaseYear;
                existingMovie.PosterUrl = movie.PosterUrl ?? existingMovie.PosterUrl;

                // No actualizar CreatedAt, CreatedBy, AverageRating, TotalRatings
                // Esos se controlan automáticamente

                // Guardar cambios
                await moviesCollection.Document(movieId).SetAsync(existingMovie);

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
        /// 
        /// Consideración importante:
        /// Antes de eliminar, verificar que NO tiene calificaciones
        /// Si la elimina con calificaciones, quedan "huérfanas"
        /// 
        /// Proceso:
        /// 1. Verificar que la película existe
        /// 2. Verificar que no tiene calificaciones
        /// 3. Eliminar de Firestore
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
        /// 
        /// Nota: Firestore no tiene búsqueda de texto completo nativa
        /// Esta es una búsqueda simple que obtiene todas las películas
        /// y filtra en memoria (no es escalable para muchos datos)
        /// 
        /// Proceso:
        /// 1. Obtener todas las películas
        /// 2. Filtrar por título (case-insensitive)
        /// 3. Devolver resultados
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
        /// 
        /// Convierte un Movie (modelo interno) a MovieDto (lo que se envía al frontend)
        /// Es privado porque solo lo usa internamente MovieService
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
}