namespace ElectionApp.Models;

// Catalogo de estados de una peticion (Pendiente, En proceso, Concluida,
// Cancelada). Toda peticion nueva arranca en "Pendiente".
public class CatEstatusPeticion
{
    public int IdEstatusPeticion { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}
