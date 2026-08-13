using ElectionApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ElectionApp.Services;

// Validación + guardado en disco de evidencia (Opr_Documentos). Compartido
// por DocumentosController (subida directa desde /Documentos) y cualquier
// otro controller que quiera adjuntar evidencia al vuelo (p.ej.
// GastosApoyoController al registrar un apoyo). El archivo se guarda fuera
// de wwwroot -- ver CLAUDE.md, sección "Documentos y evidencia".
public class DocumentoService
{
    public static readonly string[] ExtensionesPermitidas =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

    public const long TamanoMaximoBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IWebHostEnvironment _env;

    public DocumentoService(IWebHostEnvironment env)
    {
        _env = env;
    }

    // Devuelve null si el archivo es válido, o el mensaje de error a mostrar.
    public string? Validar(IFormFile archivo)
    {
        if (archivo.Length == 0)
        {
            return "El archivo está vacío.";
        }

        if (archivo.Length > TamanoMaximoBytes)
        {
            return "El archivo supera el tamaño máximo permitido (10 MB).";
        }

        var extension = Path.GetExtension(archivo.FileName);
        if (string.IsNullOrEmpty(extension) || !ExtensionesPermitidas.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return "Tipo de archivo no permitido. Solo imágenes (jpg, png, gif, webp) o documentos (pdf, doc, docx, xls, xlsx).";
        }

        return null;
    }

    // Guarda el archivo en disco y regresa un OprDocumento listo para
    // agregar al DbContext (el caller decide cuándo hacer SaveChangesAsync).
    public async Task<OprDocumento> GuardarAsync(
        IFormFile archivo,
        int idCampana,
        int idTipoDocumento,
        int idUsuarioRegistro,
        int? idEvento = null,
        int? idGasto = null,
        int? idGastoApoyo = null,
        string? descripcion = null)
    {
        var extension = Path.GetExtension(archivo.FileName);
        var carpeta = Path.Combine(_env.ContentRootPath, "Uploads", "Documentos", idCampana.ToString());
        Directory.CreateDirectory(carpeta);

        var nombreEnDisco = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = Path.Combine(carpeta, nombreEnDisco);

        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        return new OprDocumento
        {
            IdCampana = idCampana,
            IdEvento = idEvento,
            IdGasto = idGasto,
            IdGastoApoyo = idGastoApoyo,
            IdTipoDocumento = idTipoDocumento,
            NombreArchivo = archivo.FileName,
            RutaArchivo = Path.Combine(idCampana.ToString(), nombreEnDisco),
            ContentType = string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType,
            TamanoBytes = archivo.Length,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            IdUsuarioRegistro = idUsuarioRegistro,
        };
    }

    public string RutaFisica(string rutaArchivo) => Path.Combine(_env.ContentRootPath, "Uploads", "Documentos", rutaArchivo);

    public void Eliminar(string rutaArchivo)
    {
        try
        {
            var rutaCompleta = RutaFisica(rutaArchivo);
            if (File.Exists(rutaCompleta))
            {
                File.Delete(rutaCompleta);
            }
        }
        catch
        {
            // Best-effort: no interrumpe el flujo si no se pudo borrar el archivo.
        }
    }
}
