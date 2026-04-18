using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa um serviço oferecido pela oficina
    /// </summary>
    public class Servico : Entity
    {
        public string Nome { get; private set; }
        public string Descricao { get; private set; }
        public decimal Preco { get; private set; }

        private Servico() { }

        public Servico(string nome, string descricao, decimal preco)
            : base()
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do servico e obrigatorio.", nameof(nome));

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descricao do servico e obrigatoria.", nameof(descricao));

            if (preco < 0)
                throw new ArgumentException("Preco nao pode ser negativo.", nameof(preco));

            Nome = nome;
            Descricao = descricao;
            Preco = preco;
        }

        public void Atualizar(string nome, string descricao, decimal preco)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do servico e obrigatorio.", nameof(nome));

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descricao do servico e obrigatoria.", nameof(descricao));

            if (preco < 0)
                throw new ArgumentException("Preco nao pode ser negativo.", nameof(preco));

            Nome = nome;
            Descricao = descricao;
            Preco = preco;
            UpdateModificationDate();
        }
    }
}
