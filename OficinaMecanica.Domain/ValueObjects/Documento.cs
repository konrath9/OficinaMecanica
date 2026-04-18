namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa um documento fiscal brasileiro (CPF ou CNPJ).
    /// Valida o dígito verificador e normaliza o valor (somente dígitos).
    /// </summary>
    public sealed class Documento : IEquatable<Documento>
    {
        public string Valor { get; }
        public TipoDocumento Tipo { get; }

        private Documento(string valor, TipoDocumento tipo)
        {
            Valor = valor;
            Tipo = tipo;
        }

        /// <summary>
        /// Cria um Documento validado a partir de uma string CPF ou CNPJ.
        /// Aceita formatos com ou sem máscara (000.000.000-00 ou 00000000000).
        /// </summary>
        public static Documento Criar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("CPF/CNPJ é obrigatório.", nameof(valor));

            var digitos = ExtrairDigitos(valor);

            return digitos.Length switch
            {
                11 => ValidarCpf(digitos),
                14 => ValidarCnpj(digitos),
                _ => throw new ArgumentException(
                    $"Documento inválido: deve ter 11 dígitos (CPF) ou 14 dígitos (CNPJ). Recebido: {valor}",
                    nameof(valor))
            };
        }

        // ??????????????????????????????????????????????
        // CPF
        // ??????????????????????????????????????????????

        private static Documento ValidarCpf(string digitos)
        {
            if (TodosDigitosIguais(digitos))
                throw new ArgumentException($"CPF inválido: todos os dígitos são iguais ({digitos}).");

            if (!VerificarDigitoCpf(digitos, 1) || !VerificarDigitoCpf(digitos, 2))
                throw new ArgumentException($"CPF inválido: dígito verificador incorreto ({FormatarCpf(digitos)}).");

            return new Documento(digitos, TipoDocumento.Cpf);
        }

        private static bool VerificarDigitoCpf(string digitos, int posicaoDigito)
        {
            int soma = 0;
            int pesoInicial = 9 + posicaoDigito;

            for (int i = 0; i < 8 + posicaoDigito; i++)
                soma += (digitos[i] - '0') * (pesoInicial - i);

            int resto = soma % 11;
            int digitoEsperado = resto < 2 ? 0 : 11 - resto;

            return (digitos[8 + posicaoDigito] - '0') == digitoEsperado;
        }

        private static string FormatarCpf(string digitos) =>
            $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..11]}";

        // ??????????????????????????????????????????????
        // CNPJ
        // ??????????????????????????????????????????????

        private static Documento ValidarCnpj(string digitos)
        {
            if (TodosDigitosIguais(digitos))
                throw new ArgumentException($"CNPJ inválido: todos os dígitos são iguais ({digitos}).");

            if (!VerificarDigitoCnpj(digitos, 1) || !VerificarDigitoCnpj(digitos, 2))
                throw new ArgumentException($"CNPJ inválido: dígito verificador incorreto ({FormatarCnpj(digitos)}).");

            return new Documento(digitos, TipoDocumento.Cnpj);
        }

        private static bool VerificarDigitoCnpj(string digitos, int posicaoDigito)
        {
            // Pesos do CNPJ: ciclo de 2 a 9, reiniciando
            int[] pesos = posicaoDigito == 1
                ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
                : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            int soma = 0;
            for (int i = 0; i < 12 + (posicaoDigito - 1); i++)
                soma += (digitos[i] - '0') * pesos[i];

            int resto = soma % 11;
            int digitoEsperado = resto < 2 ? 0 : 11 - resto;

            return (digitos[12 + (posicaoDigito - 1)] - '0') == digitoEsperado;
        }

        private static string FormatarCnpj(string digitos) =>
            $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..12]}-{digitos[12..14]}";

        // ??????????????????????????????????????????????
        // Helpers
        // ??????????????????????????????????????????????

        private static string ExtrairDigitos(string valor) =>
            new string(valor.Where(char.IsDigit).ToArray());

        private static bool TodosDigitosIguais(string digitos) =>
            digitos.Distinct().Count() == 1;

        // ??????????????????????????????????????????????
        // Formatação para exibição
        // ??????????????????????????????????????????????

        /// <summary>Retorna o documento formatado com máscara (000.000.000-00 ou 00.000.000/0000-00).</summary>
        public string Formatado => Tipo == TipoDocumento.Cpf
            ? FormatarCpf(Valor)
            : FormatarCnpj(Valor);

        // ??????????????????????????????????????????????
        // Igualdade (Value Object — comparação por valor)
        // ??????????????????????????????????????????????

        public bool Equals(Documento? other) =>
            other is not null && Valor == other.Valor;

        public override bool Equals(object? obj) => Equals(obj as Documento);
        public override int GetHashCode() => Valor.GetHashCode();
        public override string ToString() => Valor;

        public static bool operator ==(Documento? a, Documento? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(Documento? a, Documento? b) => !(a == b);
    }

    public enum TipoDocumento
    {
        Cpf = 1,
        Cnpj = 2
    }
}
