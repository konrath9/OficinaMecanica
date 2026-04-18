namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Item de peca dentro de uma Ordem de Servico (snapshot do catalogo no momento da OS)
    /// </summary>
    public class ItemPeca
    {
        public Guid PecaId { get; private set; }
        public string Codigo { get; private set; }
        public string Descricao { get; private set; }
        public decimal PrecoUnitario { get; private set; }
        public int Quantidade { get; private set; }
        public decimal TotalPrice => PrecoUnitario * Quantidade;

        private ItemPeca() { }

        public ItemPeca(Guid pecaId, string codigo, string descricao, decimal precoUnitario, int quantidade)
        {
            if (pecaId == Guid.Empty)
                throw new ArgumentException("Id da peca e obrigatorio.", nameof(pecaId));

            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("Codigo da peca e obrigatorio.", nameof(codigo));

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descricao da peca e obrigatoria.", nameof(descricao));

            if (precoUnitario <= 0)
                throw new ArgumentException("Preco unitario deve ser maior que zero.", nameof(precoUnitario));

            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

            PecaId = pecaId;
            Codigo = codigo;
            Descricao = descricao;
            PrecoUnitario = precoUnitario;
            Quantidade = quantidade;
        }

        public void AtualizarQuantidade(int novaQuantidade)
        {
            if (novaQuantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(novaQuantidade));

            Quantidade = novaQuantidade;
        }
    }
}
