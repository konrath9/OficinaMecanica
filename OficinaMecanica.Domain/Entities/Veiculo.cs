using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa um veiculo da oficina
    /// </summary>
    public class Veiculo : Entity
    {
        public string Placa { get; private set; }
        public string Marca { get; private set; }
        public string Modelo { get; private set; }
        public int Ano { get; private set; }
        public Guid ClienteId { get; private set; }

        // Propriedade de navegacao
        public Cliente? Cliente { get; private set; }

        private Veiculo() { }

        public Veiculo(string placa, string marca, string modelo, int ano, Guid clienteId)
            : base()
        {
            if (string.IsNullOrWhiteSpace(marca))
                throw new ArgumentException("Marca e obrigatoria.", nameof(marca));

            if (string.IsNullOrWhiteSpace(modelo))
                throw new ArgumentException("Modelo e obrigatorio.", nameof(modelo));

            if (ano < 1900 || ano > DateTime.UtcNow.Year + 1)
                throw new ArgumentException("Ano invalido.", nameof(ano));

            if (clienteId == Guid.Empty)
                throw new ArgumentException("Cliente e obrigatorio.", nameof(clienteId));

            // Valida formato da placa via Value Object — lança ArgumentException se inválido
            var placaValidada = ValueObjects.Placa.Criar(placa);

            Placa = placaValidada.Valor;
            Marca = marca;
            Modelo = modelo;
            Ano = ano;
            ClienteId = clienteId;
        }

        public void Atualizar(string marca, string modelo, int ano)
        {
            if (string.IsNullOrWhiteSpace(marca))
                throw new ArgumentException("Marca e obrigatoria.", nameof(marca));

            if (string.IsNullOrWhiteSpace(modelo))
                throw new ArgumentException("Modelo e obrigatorio.", nameof(modelo));

            if (ano < 1900 || ano > DateTime.UtcNow.Year + 1)
                throw new ArgumentException("Ano invalido.", nameof(ano));

            Marca = marca;
            Modelo = modelo;
            Ano = ano;
            UpdateModificationDate();
        }
    }
}
