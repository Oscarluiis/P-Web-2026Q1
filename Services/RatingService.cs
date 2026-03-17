using Blockbuster.API.DTOs;
using Blockbuster.API.Models;
using Google.Cloud.Firestore;

namespace Blockbuster.API.Services
{
    /// <summary>
    /// RatingService implementa la gestión de calificaciones
    /// Permite crear, obtener, editar y eliminar calificaciones de usuarios
    /// </summary>
    public class RatingService : IRatingService
    {
        private readonly FirebaseService _firebaseService;
        private readonly IMovieService _movieService;
        private readonly IAuthService _authService;

        /// <summary>
        /// Constructor: Recibe dependencias inyectadas
        /// </summary>
        public RatingService(
            FirebaseService firebaseService,
            IMovieService movieService,
            IAuthService authService)
        {
            _firebaseService = firebaseService;
            _movieService = movieService;
            _authService = authService;
        }

        /// <summary>
        /// GetRatingsByMovieId: Obtiene todas las calificaciones de una película
        /// </summary>
        public async Task<List<RatingDto>> GetRatingsByMovieId(string movieId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    throw new ArgumentException("El ID de película es requerido");
                }

                var ratingsCollection = _firebaseService.GetCollection("ratings");

                // Query: Obtener ratings donde MovieId == movieId
                // OrderByDescending: Ordenar por más reciente primero
                var query = ratingsCollection
                    .WhereEqualTo("MovieId", movieId)
                    .OrderByDescending("CreatedAt");

                var snapshot = await query.GetSnapshotAsync();

                // Convertir documentos a RatingDto
                var ratings = new List<RatingDto>();
                foreach (var doc in snapshot.Documents)
                {
                    var ratingDict = doc.ToDictionary();
                    var rating = ConvertDictToRating(ratingDict);
                    ratings.Add(ConvertToDto(rating));
                }

                return ratings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener ratings de película: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// GetRatingsByUserId: Obtiene todas las calificaciones hechas por un usuario
        /// </summary>
        public async Task<List<RatingDto>> GetRatingsByUserId(string userId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("El ID de usuario es requerido");
                }

                var ratingsCollection = _firebaseService.GetCollection("ratings");

                // Query: Obtener ratings donde UserId == userId
                var query = ratingsCollection
                    .WhereEqualTo("UserId", userId)
                    .OrderByDescending("CreatedAt");

                var snapshot = await query.GetSnapshotAsync();

                // Convertir documentos a RatingDto
                var ratings = new List<RatingDto>();
                foreach (var doc in snapshot.Documents)
                {
                    var ratingDict = doc.ToDictionary();
                    var rating = ConvertDictToRating(ratingDict);
                    ratings.Add(ConvertToDto(rating));
                }

                return ratings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener ratings del usuario: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// CreateRating: Crea una nueva calificación
        /// </summary>
        public async Task<Rating> CreateRating(CreateRatingDto createRatingDto, string userId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(createRatingDto.MovieId))
                {
                    throw new ArgumentException("El ID de película es requerido");
                }

                if (createRatingDto.Score < 1 || createRatingDto.Score > 10)
                {
                    throw new ArgumentException("La calificación debe estar entre 1 y 10");
                }

                // Obtener la película para verificar que existe
                var movieDto = await _movieService.GetMovieById(createRatingDto.MovieId);
                if (movieDto == null)
                {
                    throw new InvalidOperationException("La película no existe");
                }

                // Obtener el usuario para obtener su nombre
                var user = await _authService.GetUserById(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("El usuario no existe");
                }

                // Verificar que el usuario no ha calificado esta película antes
                var hasRated = await HasUserRatedMovie(userId, createRatingDto.MovieId);
                if (hasRated)
                {
                    throw new InvalidOperationException("Ya has calificado esta película");
                }

                // Crear el nuevo rating
                var newRating = new Rating
                {
                    Id = Guid.NewGuid().ToString(),
                    MovieId = createRatingDto.MovieId,
                    MovieTitle = movieDto.Title,
                    UserId = userId,
                    UserName = user.Fullname,
                    Score = createRatingDto.Score,
                    Review = createRatingDto.Review ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Obtener todas las películas para recalcular promedio
                var ratingsCollection = _firebaseService.GetCollection("ratings");
                var moviesCollection = _firebaseService.GetCollection("movies");

                // Guardar el nuevo rating usando Dictionary
                var ratingData = new Dictionary<string, object>
                {
                    { "Id", newRating.Id },
                    { "MovieId", newRating.MovieId },
                    { "MovieTitle", newRating.MovieTitle },
                    { "UserId", newRating.UserId },
                    { "UserName", newRating.UserName },
                    { "Score", newRating.Score },
                    { "Review", newRating.Review },
                    { "CreatedAt", newRating.CreatedAt },
                    { "UpdatedAt", newRating.UpdatedAt }
                };

                await ratingsCollection.Document(newRating.Id).SetAsync(ratingData);

                // Obtener todas las calificaciones de esta película (incluyendo la nueva)
                var allRatingsForMovie = await ratingsCollection
                    .WhereEqualTo("MovieId", createRatingDto.MovieId)
                    .GetSnapshotAsync();

                // Calcular el nuevo promedio
                double totalScore = 0;
                foreach (var doc in allRatingsForMovie.Documents)
                {
                    var rating = doc.ToDictionary();
                    totalScore += Convert.ToDouble(rating["Score"]);
                }

                double averageRating = totalScore / allRatingsForMovie.Count;

                // Actualizar la película con el nuevo promedio y contador
                await moviesCollection.Document(createRatingDto.MovieId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "AverageRating", averageRating },
                        { "TotalRatings", allRatingsForMovie.Count }
                    }
                );

                // Actualizar TotalRatings del usuario
                var usersCollection = _firebaseService.GetCollection("users");
                await usersCollection.Document(userId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "TotalRatings", user.TotalRatings + 1 }
                    }
                );

                Console.WriteLine($"Rating creado: Usuario {userId} calificó película {createRatingDto.MovieId}");
                return newRating;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear rating: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// UpdateRating: Edita una calificación existente
        /// </summary>
        public async Task<Rating> UpdateRating(string ratingId, CreateRatingDto createRatingDto, string userId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(ratingId))
                {
                    throw new ArgumentException("El ID de rating es requerido");
                }

                if (createRatingDto.Score < 1 || createRatingDto.Score > 10)
                {
                    throw new ArgumentException("La calificación debe estar entre 1 y 10");
                }

                var ratingsCollection = _firebaseService.GetCollection("ratings");
                var moviesCollection = _firebaseService.GetCollection("movies");

                // Obtener el rating existente
                var ratingDoc = await ratingsCollection.Document(ratingId).GetSnapshotAsync();
                if (!ratingDoc.Exists)
                {
                    throw new InvalidOperationException("El rating no existe");
                }

                var existingDict = ratingDoc.ToDictionary();
                var existingRating = ConvertDictToRating(existingDict);

                // Verificar que el usuario es propietario
                if (existingRating.UserId != userId)
                {
                    throw new UnauthorizedAccessException(
                        "No tienes permiso para editar este rating"
                    );
                }

                // Actualizar score y review
                existingRating.Score = createRatingDto.Score;
                existingRating.Review = createRatingDto.Review ?? string.Empty;
                existingRating.UpdatedAt = DateTime.UtcNow;

                // Guardar cambios usando Dictionary
                var ratingData = new Dictionary<string, object>
                {
                    { "Id", existingRating.Id },
                    { "MovieId", existingRating.MovieId },
                    { "MovieTitle", existingRating.MovieTitle },
                    { "UserId", existingRating.UserId },
                    { "UserName", existingRating.UserName },
                    { "Score", existingRating.Score },
                    { "Review", existingRating.Review },
                    { "CreatedAt", existingRating.CreatedAt },
                    { "UpdatedAt", existingRating.UpdatedAt }
                };

                await ratingsCollection.Document(ratingId).SetAsync(ratingData);

                // Recalcular promedio de la película
                var allRatingsForMovie = await ratingsCollection
                    .WhereEqualTo("MovieId", existingRating.MovieId)
                    .GetSnapshotAsync();

                double totalScore = 0;
                foreach (var doc in allRatingsForMovie.Documents)
                {
                    var rating = doc.ToDictionary();
                    totalScore += Convert.ToDouble(rating["Score"]);
                }

                double averageRating = totalScore / allRatingsForMovie.Count;

                // Actualizar la película
                await moviesCollection.Document(existingRating.MovieId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "AverageRating", averageRating }
                    }
                );

                Console.WriteLine($"Rating actualizado: {ratingId}");
                return existingRating;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar rating: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// DeleteRating: Elimina una calificación
        /// </summary>
        public async Task DeleteRating(string ratingId, string userId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(ratingId))
                {
                    throw new ArgumentException("El ID de rating es requerido");
                }

                var ratingsCollection = _firebaseService.GetCollection("ratings");
                var moviesCollection = _firebaseService.GetCollection("movies");
                var usersCollection = _firebaseService.GetCollection("users");

                // Obtener el rating
                var ratingDoc = await ratingsCollection.Document(ratingId).GetSnapshotAsync();
                if (!ratingDoc.Exists)
                {
                    throw new InvalidOperationException("El rating no existe");
                }

                var ratingDict = ratingDoc.ToDictionary();
                var rating = ConvertDictToRating(ratingDict);

                // Verificar que el usuario es propietario
                if (rating.UserId != userId)
                {
                    throw new UnauthorizedAccessException(
                        "No tienes permiso para eliminar este rating"
                    );
                }

                // Eliminar el rating
                await ratingsCollection.Document(ratingId).DeleteAsync();

                // Recalcular promedio de la película
                var allRatingsForMovie = await ratingsCollection
                    .WhereEqualTo("MovieId", rating.MovieId)
                    .GetSnapshotAsync();

                // Si no hay más ratings, el promedio es 0
                double averageRating = 0;
                int totalRatings = allRatingsForMovie.Count;

                if (totalRatings > 0)
                {
                    double totalScore = 0;
                    foreach (var doc in allRatingsForMovie.Documents)
                    {
                        var r = doc.ToDictionary();
                        totalScore += Convert.ToDouble(r["Score"]);
                    }
                    averageRating = totalScore / totalRatings;
                }

                // Actualizar la película
                await moviesCollection.Document(rating.MovieId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "AverageRating", averageRating },
                        { "TotalRatings", totalRatings }
                    }
                );

                // Decrementar TotalRatings del usuario
                var userDoc = await usersCollection.Document(userId).GetSnapshotAsync();
                var userDict = userDoc.ToDictionary();
                var userTotalRatings = (int)(long)userDict["TotalRatings"];

                await usersCollection.Document(userId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                        { "TotalRatings", Math.Max(0, userTotalRatings - 1) }
                    }
                );

                Console.WriteLine($"Rating eliminado: {ratingId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar rating: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HasUserRatedMovie: Verifica si un usuario ya calificó una película
        /// </summary>
        public async Task<bool> HasUserRatedMovie(string userId, string movieId)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(movieId))
                {
                    return false;
                }

                var ratingsCollection = _firebaseService.GetCollection("ratings");

                // Query: Buscar rating donde UserId == userId Y MovieId == movieId
                var query = ratingsCollection
                    .WhereEqualTo("UserId", userId)
                    .WhereEqualTo("MovieId", movieId);

                var snapshot = await query.GetSnapshotAsync();

                // Si hay al menos un resultado, el usuario ya calificó
                return snapshot.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar si usuario calificó película: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Método privado auxiliar: ConvertToDto
        /// Convierte un Rating (modelo interno) a RatingDto (lo que se envía al frontend)
        /// </summary>
        private RatingDto ConvertToDto(Rating rating)
        {
            return new RatingDto
            {
                Id = rating.Id,
                MovieId = rating.MovieId,
                MovieTitle = rating.MovieTitle,
                Score = rating.Score,
                Review = rating.Review,
                UserName = rating.UserName,
                CreatedAt = rating.CreatedAt
            };
        }

        /// <summary>
        /// Método privado auxiliar: ConvertDictToRating
        /// Convierte un diccionario de Firestore a objeto Rating
        /// </summary>
        private Rating ConvertDictToRating(Dictionary<string, object> dict)
        {
            return new Rating
            {
                Id = dict["Id"].ToString(),
                MovieId = dict["MovieId"].ToString(),
                MovieTitle = dict["MovieTitle"].ToString(),
                UserId = dict["UserId"].ToString(),
                UserName = dict["UserName"].ToString(),
                Score = Convert.ToDouble(dict["Score"]),
                Review = dict["Review"].ToString(),
                CreatedAt = ((Timestamp)dict["CreatedAt"]).ToDateTime(),
                UpdatedAt = ((Timestamp)dict["UpdatedAt"]).ToDateTime()
            };
        }
    }
}