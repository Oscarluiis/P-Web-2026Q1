using Blockbuster.API.Models;
using Blockbuster.API.Services;

namespace Blockbuster.API.Controllers;
using Microsoft.AspNetCore.Mvc;


/// <summary>
/// MovieController maneja todo lo relacionado con películas
/// Endpoints para obtener, crear, editar y eliminar películas
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;
        private readonly ILogger<MovieController> _logger;

        /// <summary>
        /// Constructor: Recibe IMovieService inyectado
        /// </summary>
        public MovieController(IMovieService movieService, ILogger<MovieController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/movie
        /// 
        /// Obtiene todas las películas (con filtro opcional por género)
        /// 
        /// Parámetros query (opcional):
        ///   genre: Filtrar por género específico
        ///   Ejemplo: GET /api/movie?genre=Action
        /// 
        /// Respuesta exitosa (200):
        /// [
        ///   {
        ///     "id": "movie_001",
        ///     "title": "Inception",
        ///     "description": "...",
        ///     "genre": "Science Fiction",
        ///     "releaseYear": 2010,
        ///     "averageRating": 8.5,
        ///     "totalRatings": 150
        ///   }
        /// ]
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllMovies([FromQuery] string? genre = null)
        {
            try
            {
                var movies = await _movieService.GetAllMovies(genre);
                return Ok(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener películas: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener películas" });
            }
        }

        /// <summary>
        /// GET /api/movie/{movieId}
        /// 
        /// Obtiene una película específica por su ID
        /// 
        /// Parámetro:
        ///   movieId: ID de la película (en la URL)
        /// 
        /// Respuesta exitosa (200):
        /// {
        ///   "id": "movie_001",
        ///   "title": "Inception",
        ///   "description": "...",
        ///   "genre": "Science Fiction",
        ///   "releaseYear": 2010,
        ///   "posterUrl": "https://...",
        ///   "averageRating": 8.5,
        ///   "totalRatings": 150
        /// }
        /// 
        /// Errores:
        /// 404: Película no encontrada
        /// </summary>
        [HttpGet("{movieId}")]
        public async Task<IActionResult> GetMovieById(string movieId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    return BadRequest(new { message = "El ID de película es requerido" });
                }

                var movie = await _movieService.GetMovieById(movieId);

                if (movie == null)
                {
                    return NotFound(new { message = "Película no encontrada" });
                }

                return Ok(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener película: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener película" });
            }
        }

        /// <summary>
        /// POST /api/movie
        /// 
        /// Crea una nueva película (SOLO ADMIN)
        /// 
        /// Header requerido:
        ///   Authorization: Bearer {token}
        /// 
        /// Cuerpo esperado (JSON):
        /// {
        ///   "title": "Inception",
        ///   "description": "A skilled thief...",
        ///   "genre": "Science Fiction",
        ///   "releaseYear": 2010,
        ///   "posterUrl": "https://..."
        /// }
        /// 
        /// Respuesta exitosa (201):
        /// {
        ///   "id": "movie_001",
        ///   "title": "Inception",
        ///   ...
        /// }
        /// 
        /// Errores:
        /// 400: Datos inválidos
        /// 401: No autenticado
        /// 403: No es administrador
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateMovie([FromBody] Movie movie)
        {
            try
            {
                if (movie == null)
                {
                    return BadRequest(new { message = "El cuerpo de la petición es requerido" });
                }

                if (string.IsNullOrWhiteSpace(movie.Title))
                {
                    return BadRequest(new { message = "El título es requerido" });
                }

                // TODO: Validar que el usuario es admin (requiere token parsing)
                var adminId = "admin_123"; // Por ahora, hardcodeado

                var createdMovie = await _movieService.CreateMovie(movie, adminId);

                _logger.LogInformation($"Película creada: {createdMovie.Title}");

                return Created($"/api/movie/{createdMovie.Id}", createdMovie);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear película: {ex.Message}");
                return StatusCode(500, new { message = "Error al crear película" });
            }
        }

        /// <summary>
        /// PUT /api/movie/{movieId}
        /// 
        /// Edita una película existente (SOLO ADMIN)
        /// 
        /// Header requerido:
        ///   Authorization: Bearer {token}
        /// 
        /// Parámetro:
        ///   movieId: ID de la película a editar
        /// 
        /// Cuerpo: Mismo formato que CreateMovie
        /// 
        /// Respuesta exitosa (200): La película actualizada
        /// 
        /// Errores:
        /// 404: Película no encontrada
        /// 401: No autenticado
        /// 403: No es administrador
        /// </summary>
        [HttpPut("{movieId}")]
        public async Task<IActionResult> UpdateMovie(string movieId, [FromBody] Movie movie)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    return BadRequest(new { message = "El ID de película es requerido" });
                }

                if (movie == null)
                {
                    return BadRequest(new { message = "El cuerpo de la petición es requerido" });
                }

                // TODO: Validar que el usuario es admin
                var adminId = "admin_123"; // Por ahora, hardcodeado

                var updatedMovie = await _movieService.UpdateMovie(movieId, movie, adminId);

                _logger.LogInformation($"Película actualizada: {movieId}");

                return Ok(updatedMovie);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar película: {ex.Message}");
                return StatusCode(500, new { message = "Error al actualizar película" });
            }
        }

        /// <summary>
        /// DELETE /api/movie/{movieId}
        /// 
        /// Elimina una película (SOLO ADMIN)
        /// 
        /// Header requerido:
        ///   Authorization: Bearer {token}
        /// 
        /// Parámetro:
        ///   movieId: ID de la película a eliminar
        /// 
        /// Respuesta exitosa (204): No content
        /// 
        /// Errores:
        /// 404: Película no encontrada
        /// 400: Película tiene calificaciones
        /// 401: No autenticado
        /// 403: No es administrador
        /// </summary>
        [HttpDelete("{movieId}")]
        public async Task<IActionResult> DeleteMovie(string movieId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(movieId))
                {
                    return BadRequest(new { message = "El ID de película es requerido" });
                }

                // TODO: Validar que el usuario es admin
                var adminId = "admin_123"; // Por ahora, hardcodeado

                await _movieService.DeleteMovie(movieId, adminId);

                _logger.LogInformation($"Película eliminada: {movieId}");

                return NoContent(); // 204
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar película: {ex.Message}");
                return StatusCode(500, new { message = "Error al eliminar película" });
            }
        }

        /// <summary>
        /// GET /api/movie/search/{searchTerm}
        /// 
        /// Busca películas por título
        /// 
        /// Parámetro:
        ///   searchTerm: Lo que el usuario busca
        ///   Ejemplo: GET /api/movie/search/inception
        /// 
        /// Respuesta exitosa (200):
        /// [
        ///   {
        ///     "id": "movie_001",
        ///     "title": "Inception",
        ///     ...
        ///   }
        /// ]
        /// </summary>
        [HttpGet("search/{searchTerm}")]
        public async Task<IActionResult> SearchMovies(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { message = "El término de búsqueda es requerido" });
                }

                var results = await _movieService.SearchMovies(searchTerm);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al buscar películas: {ex.Message}");
                return StatusCode(500, new { message = "Error al buscar películas" });
            }
        }
}