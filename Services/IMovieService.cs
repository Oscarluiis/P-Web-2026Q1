using Blockbuster.API.DTOs;
using Blockbuster.API.Models;

namespace Blockbuster.API.Services;

public interface IMovieService
{

    Task<List<MovieDto>> GetAllMovies(string? genre = null);
    
    Task<MovieDto?> GetMovieById(string movieId);
    
    Task<Movie> CreateMovie(Movie movie, string adminId);
    
    Task<Movie> UpdateMovie(string movieId, Movie movie, string adminId);

    Task DeleteMovie(string movieId, string adminId);
    
    Task<List<MovieDto>> SearchMovies(string searchTerm);

}