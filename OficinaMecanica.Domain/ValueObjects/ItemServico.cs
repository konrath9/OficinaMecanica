namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Item de servico dentro de uma Ordem de Servico (snapshot do catalogo no momento da OS)
    /// </summary>
    public class ItemServico
    {
        public Guid ServicoId { get; private set; }
        public string Descricao { get; private set; }
        public decimal PrecoUnitario { get; private set; }
        public int Quantidade { get; private set; }
        public decimal TotalPrice => PrecoUnitario * Quantidade;

        private ItemServico() { }

        public ItemServico(Guid servicoId, string descricao, decimal precoUnitario, int quantidade)
        {
            if (servicoId == Guid.Empty)
                throw new ArgumentException("Id do servico e obrigatorio.", nameof(servicoId));

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descricao do servico e obrigatoria.", nameof(descricao));

            if (precoUnitario <= 0)
                throw new ArgumentException("Preco unitario deve ser maior que zero.", nameof(precoUnitario));

            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

            ServicoId = servicoId;
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
