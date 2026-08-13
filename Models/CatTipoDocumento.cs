namespace ElectionApp.Models;

// Catalogo simple para distinguir el tipo de evidencia subida
// (Foto, Factura, Recibo, Comprobante de pago, Identificación, Otro...).
public class CatTipoDocumento
{
    public int IdTipoDocumento { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}
