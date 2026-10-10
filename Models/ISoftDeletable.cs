namespace Library.Models;

// Entidades que nunca são apagadas do banco: "excluir" preenche DeletedAt.
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
