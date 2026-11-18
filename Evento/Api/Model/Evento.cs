
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Api.Error;

namespace Api.Model;

// Esse modelo é válido o suficiente para casos de eventos presenciais e simples.
// Em casos remotos, com termino não determinado, acontecimentos especificos dentro do evento
// ou com mais organizadores. Diante desse cenário, esse modelo é incompleto.
public class Evento
{
    public Guid Id { get; init; }
    public Guid OrganizadorId { get; init; }

    public string Nome
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(
                    "O nome do evento é obrigatório."
                );
            }

            var nomeNormalizado = value.Trim();
            var slugGerado = GerarSlug(nomeNormalizado);

            if (string.IsNullOrEmpty(slugGerado))
            {
                throw new DomainException(
                    "O nome do evento não permite gerar um slug válido."
                );
            }

            field = nomeNormalizado;
            Slug = slugGerado;
        }
    } = string.Empty;

    public string Descricao { get; set => field = value.Trim(); } = string.Empty;
    public string Local { get; set => field = value.Trim(); } = string.Empty;

    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public int SlugNumero { get; private set; } = 1;



    public DateTime? PublicadoEm { get; set; }
    public DateTime? PublicacaoEncerradaEm { get; set; }

    public DateTimeOffset CriadoEm { get; init; }

    public EnumStatusPublicacaoEvento GetStatusPublicacao()
    {
        return PublicadoEm.HasValue
            ? EnumStatusPublicacaoEvento.PUBLICADO
            : EnumStatusPublicacaoEvento.RASCUNHO;
    }

    public EnumStatusEvento GetStatus()
    {
        var agora = DateTime.UtcNow;

        if (agora < Inicio)
            return EnumStatusEvento.AGENDADO;

        if (agora < Fim)
            return EnumStatusEvento.EM_ANDAMENTO;

        return EnumStatusEvento.ENCERRADO;
    }

    private Evento() {}

    public Evento(Guid organizadorId, string nome, DateTime inicio, DateTime fim) {

        if (organizadorId == Guid.Empty)
            throw new DomainException(
                "O organizador do evento é obrigatório."
            );

        Id = Guid.CreateVersion7();
        OrganizadorId = organizadorId;
        Nome = nome;

        Agendar(inicio, fim);
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public void Agendar(DateTime inicio, DateTime fim)
    {
        if (inicio == DateTime.MinValue ||
            fim == DateTime.MinValue)
        {
            throw new DomainException(
                "O evento deve ter um período de início e término definido."
            );
        }

        if (fim <= inicio)
        {
            throw new DomainException(
                "A data de término deve ser posterior à data de início."
            );
        }

        Inicio = inicio;
        Fim = fim;
    }

    private static string GerarSlug(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var textoNormalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in textoNormalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string resultado = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();


        resultado = Regex.Replace(resultado, @"[^a-z0-9]+", "-");


        return resultado.Trim('-');
    }

    [NotMapped]
    public string SlugPublico =>
        SlugNumero == 1
            ? Slug
            : $"{Slug}-{SlugNumero}";

    // Atualiza somente os campos informados no PATCH.
    // O período é validado com os valores finais (novos + existentes).
    public void AtualizarInformacoes(
        string? nome,
        string? descricao,
        string? local,
        DateTime? inicio,
        DateTime? fim)
    {
        // Validar antes de modificar as propriedades da entidade.
        if (nome is not null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new DomainException("O nome do evento é obrigatório.");

            if (string.IsNullOrEmpty(GerarSlug(nome.Trim())))
                throw new DomainException(
                    "O nome do evento não permite gerar um slug válido."
                );
        }

        Agendar(inicio ?? Inicio, fim ?? Fim);

        if (nome is not null)
            Nome = nome;
        if (descricao is not null)
            Descricao = descricao;
        if (local is not null)
            Local = local;
    }

    public void DefinirNumeroSlug(int numero)
    {
        if (numero < 1)
        {
            throw new DomainException(
                "O número do slug deve ser positivo."
            );
        }

        SlugNumero = numero;
    }
}

public enum EnumStatusPublicacaoEvento
{
    RASCUNHO,
    PUBLICADO
}

public enum EnumStatusEvento
{
    AGENDADO,
    EM_ANDAMENTO,
    ENCERRADO
}
