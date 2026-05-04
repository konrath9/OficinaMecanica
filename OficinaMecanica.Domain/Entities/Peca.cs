using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa uma peca ou insumo da oficina com controle de estoque
    /// </summary>
    public class Peca : Entity
    {
        public string Codigo { get; private set; }
        public string Nome { get; private set; }
        public decimal PrecoUnitario { get; private set; }
        public int QuantidadeEstoque { get; private set; }

        private Peca() { }

        public Peca(string codigo, string nome, decimal precoUnitario, int quantidadeEstoque = 0)
            : base()
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("Codigo da peca e obrigatorio.", nameof(codigo));

            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome da peca e obrigatorio.", nameof(nome));

            if (precoUnitario < 0)
                throw new ArgumentException("Preco unitario nao pode ser negativo.", nameof(precoUnitario));

            if (quantidadeEstoque < 0)
                throw new ArgumentException("Quantidade em estoque nao pode ser negativa.", nameof(quantidadeEstoque));

            Codigo = codigo.ToUpperInvariant();
            Nome = nome;
            PrecoUnitario = precoUnitario;
            QuantidadeEstoque = quantidadeEstoque;
        }

        public void Atualizar(string nome, decimal precoUnitario)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome da peca e obrigatorio.", nameof(nome));

            if (precoUnitario < 0)
                throw new ArgumentException("Preco unitario nao pode ser negativo.", nameof(precoUnitario));

            Nome = nome;
            PrecoUnitario = precoUnitario;
            UpdateModificationDate();
        }

        public void AtualizarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("Codigo da peca e obrigatorio.", nameof(codigo));

            Codigo = codigo.ToUpperInvariant();
            UpdateModificationDate();
        }

        public void EntradaEstoque(int quantidade)
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

            QuantidadeEstoque += quantidade;
            UpdateModificationDate();
        }

        public void SaidaEstoque(int quantidade)
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

            if (quantidade > QuantidadeEstoque)
                throw new InvalidOperationException($"Estoque insuficiente. Disponivel: {QuantidadeEstoque}");

            QuantidadeEstoque -= quantidade;
            UpdateModificationDate();
        }

        public bool TemEstoque(int quantidade) => QuantidadeEstoque >= quantidade;
    }
}
