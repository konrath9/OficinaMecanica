using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Domain.Entities
{
    public class OrdemServico : Entity
    {
        private readonly List<ItemServico> _servicos;
        private readonly List<ItemPeca> _pecas;
        private readonly List<HistoricoStatusOrdemServico> _historicoStatus;

        public string Numero { get; private set; }
        public Guid ClienteId { get; private set; }
        public Guid VeiculoId { get; private set; }
        public StatusOrdemServico Status { get; private set; }
        public string? Observacoes { get; private set; }
        public DateTime? IniciadaEm { get; private set; }
        public DateTime? FinalizadaEm { get; private set; }
        public DateTime? EntregueEm { get; private set; }
        /// <summary>Link de checkout do Mercado Pago, recebido do Billing Service via Saga.</summary>
        public string? LinkPagamento { get; private set; }
        public IReadOnlyCollection<ItemServico> Servicos => _servicos.AsReadOnly();
        public IReadOnlyCollection<ItemPeca> Pecas => _pecas.AsReadOnly();
        public IReadOnlyCollection<HistoricoStatusOrdemServico> HistoricoStatus => _historicoStatus.AsReadOnly();

        public decimal TotalServicos => _servicos.Sum(s => s.TotalPrice);
        public decimal TotalPecas => _pecas.Sum(p => p.TotalPrice);
        public decimal TotalOrcamento => TotalServicos + TotalPecas;

        private OrdemServico()
        {
            _servicos = new List<ItemServico>();
            _pecas = new List<ItemPeca>();
            _historicoStatus = new List<HistoricoStatusOrdemServico>();
        }

        public OrdemServico(string numero, Guid clienteId, Guid veiculoId, string? observacoes = null)
            : base()
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("Numero da OS e obrigatorio.", nameof(numero));

            if (clienteId == Guid.Empty)
                throw new ArgumentException("Cliente e obrigatorio.", nameof(clienteId));

            if (veiculoId == Guid.Empty)
                throw new ArgumentException("Veiculo e obrigatorio.", nameof(veiculoId));

            Numero = numero;
            ClienteId = clienteId;
            VeiculoId = veiculoId;
            Status = StatusOrdemServico.Recebida;
            Observacoes = observacoes;
            _servicos = new List<ItemServico>();
            _pecas = new List<ItemPeca>();
            _historicoStatus = new List<HistoricoStatusOrdemServico>
            {
                new HistoricoStatusOrdemServico(StatusOrdemServico.Recebida, "OS criada e recebida")
            };
        }

        public void AdicionarServico(ItemServico servico)
        {
            if (servico == null)
                throw new ArgumentNullException(nameof(servico));

            ValidarEdicao();
            _servicos.Add(servico);
            UpdateModificationDate();
        }

        public void RemoverServico(Guid servicoId)
        {
            ValidarEdicao();
            var item = _servicos.FirstOrDefault(s => s.ServicoId == servicoId);
            if (item != null)
            {
                _servicos.Remove(item);
                UpdateModificationDate();
            }
        }

        public void AdicionarPeca(ItemPeca peca)
        {
            if (peca == null)
                throw new ArgumentNullException(nameof(peca));

            ValidarEdicao();
            _pecas.Add(peca);
            UpdateModificationDate();
        }

        public void RemoverPeca(Guid pecaId)
        {
            ValidarEdicao();
            var item = _pecas.FirstOrDefault(p => p.PecaId == pecaId);
            if (item != null)
            {
                _pecas.Remove(item);
                UpdateModificationDate();
            }
        }

        public void IniciarDiagnostico()
        {
            if (Status != StatusOrdemServico.Recebida)
                throw new InvalidOperationException("Apenas ordens recebidas podem iniciar diagnostico.");

            Status = StatusOrdemServico.EmDiagnostico;
            IniciadaEm = DateTime.UtcNow;
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.EmDiagnostico));
            UpdateModificationDate();
        }

        /// <summary>
        /// Técnico concluiu o diagnóstico: valida que há itens para orçar e registra a solicitação.
        /// O status só muda para AguardandoAprovacao quando o Billing Service confirma o orçamento
        /// (evento OrcamentoGerado da Saga) - ver <see cref="EnviarParaAprovacao"/>.
        /// </summary>
        public void SolicitarOrcamento()
        {
            ValidarEnvioParaAprovacao();
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.EmDiagnostico, "Diagnóstico concluído - orçamento solicitado ao Billing"));
            UpdateModificationDate();
        }

        public void EnviarParaAprovacao()
        {
            ValidarEnvioParaAprovacao();
            Status = StatusOrdemServico.AguardandoAprovacao;
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.AguardandoAprovacao, "Orçamento enviado para aprovação do cliente"));
            UpdateModificationDate();
        }

        /// <summary>Compensação da Saga: o Billing não conseguiu gerar o orçamento - a OS segue em diagnóstico.</summary>
        public void RegistrarFalhaOrcamento(string motivo)
        {
            _historicoStatus.Add(new HistoricoStatusOrdemServico(Status, $"Falha ao gerar orçamento: {motivo}"));
            UpdateModificationDate();
        }

        /// <summary>Cliente aprovou o orçamento - a execução só começa após a confirmação do pagamento.</summary>
        public void Aprovar()
        {
            if (Status != StatusOrdemServico.AguardandoAprovacao)
                throw new InvalidOperationException("Apenas ordens aguardando aprovacao podem ser aprovadas.");

            Status = StatusOrdemServico.AguardandoPagamento;
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.AguardandoPagamento, "Orçamento aprovado pelo cliente - aguardando pagamento"));
            UpdateModificationDate();
        }

        public void RegistrarLinkPagamento(string? link)
        {
            LinkPagamento = link;
            UpdateModificationDate();
        }

        public void ConfirmarPagamento()
        {
            if (Status != StatusOrdemServico.AguardandoPagamento)
                throw new InvalidOperationException("Apenas ordens aguardando pagamento podem ter o pagamento confirmado.");

            Status = StatusOrdemServico.EmExecucao;
            IniciadaEm ??= DateTime.UtcNow;
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.EmExecucao, "Pagamento confirmado - OS enviada para a fila de execução"));
            UpdateModificationDate();
        }

        public void Finalizar()
        {
            if (Status != StatusOrdemServico.EmExecucao)
                throw new InvalidOperationException("Apenas ordens em execucao podem ser finalizadas.");

            Status = StatusOrdemServico.Finalizada;
            FinalizadaEm = DateTime.UtcNow;
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.Finalizada, "Execução concluída"));
            UpdateModificationDate();
        }

        public void Entregar()
        {
            if (Status != StatusOrdemServico.Finalizada)
                throw new InvalidOperationException("Apenas ordens finalizadas podem ser entregues.");

            Status = StatusOrdemServico.Entregue;
            EntregueEm = DateTime.UtcNow;
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.Entregue, "Veículo entregue ao cliente"));
            UpdateModificationDate();
        }

        public void Cancelar(string? motivo = null)
        {
            if (Status == StatusOrdemServico.Finalizada || Status == StatusOrdemServico.Entregue)
                throw new InvalidOperationException("Ordens finalizadas ou entregues nao podem ser canceladas.");

            Status = StatusOrdemServico.Cancelada;
            if (!string.IsNullOrWhiteSpace(motivo))
                Observacoes = $"{Observacoes}\nMotivo do cancelamento: {motivo}";
            _historicoStatus.Add(new HistoricoStatusOrdemServico(StatusOrdemServico.Cancelada, motivo ?? "Cancelada"));
            UpdateModificationDate();
        }

        public void AtualizarObservacoes(string observacoes)
        {
            Observacoes = observacoes;
            UpdateModificationDate();
        }

        private void ValidarEnvioParaAprovacao()
        {
            if (Status != StatusOrdemServico.EmDiagnostico)
                throw new InvalidOperationException("Apenas ordens em diagnostico podem ser enviadas para aprovacao.");

            if (!_servicos.Any() && !_pecas.Any())
                throw new InvalidOperationException("A OS deve ter pelo menos um servico ou peca para enviar o orcamento.");
        }

        private void ValidarEdicao()
        {
            if (Status == StatusOrdemServico.Finalizada ||
                Status == StatusOrdemServico.Entregue ||
                Status == StatusOrdemServico.Cancelada)
            {
                throw new InvalidOperationException("Nao e possivel editar uma OS finalizada, entregue ou cancelada.");
            }
        }
    }
}
