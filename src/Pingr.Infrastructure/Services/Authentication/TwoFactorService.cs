using System.Security.Cryptography;
using System.Text;
using Pingr.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using OtpNet;
using Pingr.Application.Abstractions.Services.Authentication;
using QRCoder;

namespace Pingr.Infrastructure.Services.Authentication;

/// <inheritdoc/>
public sealed class TwoFactorService(
    IConfiguration configuration) : ITwoFactorService
{
    public byte[] GenerateSecret()
    {
        return RandomNumberGenerator.GetBytes(32);
    }

    public string EncodeSecret(byte[] secret)
    {
        return Base32Encoding.ToString(secret);
    }


    /// <remarks>
    /// The code format is <c>XXXX-XXXX-XX</c>.
    /// </remarks>
    public IList<string> GenerateRecoveryCodes()
    {
        var recoveryCodes = new List<string>();
        string recoveryCodeAlphabet = configuration["TwoFactor:RecoveryCodeAlphabet"] 
                                      ?? throw new InvalidOperationException("Recovery code alphabet is missing.");
        
        while (recoveryCodes.Count < 10)
        {
            Span<char> chars = new char[10];

            for (int j = 0; j < 10; j++)
            {
                chars[j] = recoveryCodeAlphabet[RandomNumberGenerator.GetInt32(recoveryCodeAlphabet.Length)];
            }

            var code = $"{chars[..4]}-{chars[4..8]}-{chars[8..]}";
            
            recoveryCodes.Add(code);
        }

        return recoveryCodes;
    }

    /// <remarks>
    /// Uses SHA256 as the algorithm.
    /// </remarks>
    public IList<string> HashRecoveryCodes(IEnumerable<string> recoveryCodes)
    {
        return recoveryCodes
            .Select(code =>
                Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(code))))
            
            .ToList();
    }

    /// <remarks>QR code image is encoded in Base64.</remarks>
    public string GenerateQrCode(
        string userEmail,
        byte[] userTwoFactorSecret)
    {
        string escapedIssuer = Uri.EscapeDataString(configuration["TwoFactor:QrCode:Issuer"]
                                                    ?? throw new InvalidOperationException("QrCode Issuer is missing."));
        string escapedUser = Uri.EscapeDataString(userEmail);
        
        string otpUri = $"""
                         otpauth://totp/{escapedIssuer}:{escapedUser}?secret={Base32Encoding.ToString(userTwoFactorSecret)}&issuer={escapedIssuer}&digits=6&period=30
                         """;
        
        var qrCodeGenerator = new QRCodeGenerator();
        var qrCodeData = qrCodeGenerator.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new Base64QRCode(qrCodeData);
        
        return qrCode.GetGraphic(10);
    }

    public bool VerifyTotpCode(
        string totpCode,
        string userTwoFactorSecret)
    {
        var totp = new Totp(
            DecodeSecret(userTwoFactorSecret));

        return totp.VerifyTotp(
            totpCode,
            out _);
    }
    
    /// <remarks>Uses Base32.</remarks>
    private byte[] DecodeSecret(string secret)
    {
        return Base32Encoding.ToBytes(secret);
    }
}