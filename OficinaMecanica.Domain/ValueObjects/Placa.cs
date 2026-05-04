using System.Text.RegularExpressions;

namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa a placa de um veículo brasileiro.
    /// Aceita o formato antigo (ABC-1234) e o formato Mercosul (ABC1D23).
    /// Normaliza para maiúsculas sem hífen.
    /// </summary>
    public sealed class Placa : IEquatable<Placa>
    {
        // Formato antigo: 3 letras + 4 dígitos  (ex: ABC1234)
        private static readonly Regex RegexFormatoAntigo =
            new(@"^[A-Z]{3}\d{4}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

        // Formato Mercosul: 3 letras + 1 dígito + 1 letra + 2 dígitos  (ex: ABC1D23)
        private static readonly Regex RegexMercosul =
            new(@"^[A-Z]{3}\d[A-Z]\d{2}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

        public string Valor { get; }
        public FormatoPlaca Formato { get; }

        private Placa(string valor, FormatoPlaca formato)
        {
            Valor = valor;
            Formato = formato;
        }

        /// <summary>
        /// Cria uma Placa validada.
        /// Aceita com ou sem hífen (ABC-1234 ou ABC1234) em ambos os formatos.
        /// </summary>
        public static Placa Criar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Placa é obrigatória.", nameof(valor));

            var normalizada = Normalizar(valor);

            if (RegexFormatoAntigo.IsMatch(normalizada))
                return new Placa(normalizada, FormatoPlaca.Antigo);

            if (RegexMercosul.IsMatch(normalizada))
                return new Placa(normalizada, FormatoPlaca.Mercosul);

            throw new ArgumentException(
                $"Placa inválida: '{valor}'. " +
                "Formatos aceitos: padrão antigo (ABC-1234) ou Mercosul (ABC1D23).",
                nameof(valor));
        }

        /// <summary>Retorna a placa formatada com hífen (ex: ABC-1234 ou ABC1D23).</summary>
        public string Formatada => Formato == FormatoPlaca.Antigo
            ? $"{Valor[..3]}-{Valor[3..]}"
            : Valor;

        // ??????????????????????????????????????????????
        // Helpers
        // ??????????????????????????????????????????????

        private static string Normalizar(string valor) =>
            valor.Replace("-", "").Trim().ToUpperInvariant();

        // ??????????????????????????????????????????????
        // Igualdade (Value Object — comparação por valor)
        // ??????????????????????????????????????????????

        public bool Equals(Placa? other) =>
            other is not null && Valor == other.Valor;

        public override bool Equals(object? obj) => Equals(obj as Placa);
        public override int GetHashCode() => Valor.GetHashCode();
        public override string ToString() => Valor;

        public static bool operator ==(Placa? a, Placa? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(Placa? a, Placa? b) => !(a == b);
    }

    public enum FormatoPlaca
    {
        Antigo = 1,
        Mercosul = 2
    }
}
