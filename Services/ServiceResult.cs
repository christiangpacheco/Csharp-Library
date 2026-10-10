namespace Library.Services;

// Resultado de uma operação de service: sucesso, ou um erro ligado a um campo do formulário.
public class ServiceResult
{
    public bool Succeeded { get; private init; }

    public string? Field { get; private init; }

    public string? Error { get; private init; }

    public int? Id { get; private init; }

    public static ServiceResult Ok(int? id = null) => new() { Succeeded = true, Id = id };

    public static ServiceResult Fail(string error, string? field = null) => new() { Field = field, Error = error };
}
