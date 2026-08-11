using ElectionApp.Models.Sistema;
using Microsoft.AspNetCore.Identity;

namespace ElectionApp.Services;

// Envuelve Microsoft.AspNetCore.Identity.PasswordHasher<T> (PBKDF2 con salt
// integrado y aleatorio por contraseña). PasswordHasher trabaja con string
// en Base64; la columna sistema.Usuarios.password_hash es VARBINARY(256),
// asi que aqui convertimos entre bytes y Base64.
public class PasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public byte[] HashPassword(Usuario usuario, string plainPassword)
    {
        string base64Hash = _hasher.HashPassword(usuario, plainPassword);
        return Convert.FromBase64String(base64Hash);
    }

    public bool VerifyPassword(Usuario usuario, string plainPassword)
    {
        if (usuario.PasswordHash.Length == 0)
        {
            return false;
        }

        string base64Hash = Convert.ToBase64String(usuario.PasswordHash);
        PasswordVerificationResult result = _hasher.VerifyHashedPassword(usuario, base64Hash, plainPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
