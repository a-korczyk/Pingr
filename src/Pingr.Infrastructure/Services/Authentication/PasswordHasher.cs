using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Pingr.Application.Abstractions.Services;

namespace Pingr.Infrastructure.Services.Authentication;

/// <summary>
/// Implementation of <see cref="IPasswordHasher"/>
/// </summary>
public class PasswordHasher(IOptions<PasswordHasherOptions> options) : IPasswordHasher
{
    private readonly PasswordHasherOptions _options = options.Value;

    public string HashPassword(string password, CancellationToken cancellationToken)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltSizeInBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _options.IterationCount, HashAlgorithmName.SHA512, _options.HashSizeInBytes);
        return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}-{_options.IterationCount}-{HashAlgorithmName.SHA512}-{_options.HashSizeInBytes}";
    }

    public bool VerifyPassword(string inputPassword, string hashedPassword, CancellationToken cancellationToken)
    {
        string[] parts = hashedPassword.Split("-");
        
        byte[] hash = Convert.FromHexString(parts[0]);
        byte[] salt = Convert.FromHexString(parts[1]);
        int iterations = Convert.ToInt32(parts[2]);
        string hashAlgorithmName = parts[3];
        int hashSize = Convert.ToInt32(parts[4]);

        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt, iterations, new HashAlgorithmName(hashAlgorithmName), hashSize);
        
        return CryptographicOperations.FixedTimeEquals(inputHash, hash);
    }
}

/// <summary>
/// Configuration options for <see cref="IPasswordHasher"/>
/// </summary>
public sealed class PasswordHasherOptions
{
    public const string SectionName = "PasswordHasher";
    
    [Required]
    [Range(16, int.MaxValue)]
    public int SaltSizeInBytes { get; init; }
    
    [Required]
    [Range(32, int.MaxValue)]
    public int HashSizeInBytes { get; init; }
    
    [Required]
    [Range(100_000, int.MaxValue)]
    public int IterationCount { get; init; }
}
