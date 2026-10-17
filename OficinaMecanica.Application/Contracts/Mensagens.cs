// Contratos de mensageria entre os microsservicos - fonte da verdade em docs/fase4/contratos-mensageria.md.
// O namespace e os nomes precisam ser identicos nos tres repositorios (o MassTransit roteia por namespace + nome do tipo).
namespace OficinaMecanica.Contracts;

// ---------- OS Service -> Billing Service (comandos) ----------
public record ItemOrcamento(string Descricao, string Tipo, int Quantidade, decimal PrecoUnitario); // Tipo: "Servico" | "Peca"

public record GerarOrcamento(Guid OrdemServicoId, string NumeroOs, Guid ClienteId, string? EmailCliente,
    IReadOnlyList<ItemOrcamento> Itens, decimal Valor);
public record AprovarOrcamento(Guid OrdemServicoId);
public record CancelarOrcamento(Guid OrdemServicoId, string Motivo);
public record EstornarPagamento(Guid OrdemServicoId, string Motivo);

// ---------- Billing Service -> OS Service (eventos) ----------
public record OrcamentoGerado(Guid OrdemServicoId, Guid OrcamentoId, decimal Valor);
public record OrcamentoFalhou(Guid OrdemServicoId, string Motivo);
public record PagamentoCriado(Guid OrdemServicoId, Guid PagamentoId, string? LinkPagamento);
public record PagamentoAprovado(Guid OrdemServicoId, Guid PagamentoId, decimal Valor);
public record PagamentoRecusado(Guid OrdemServicoId, Guid PagamentoId, string Motivo);
public record PagamentoEstornado(Guid OrdemServicoId, Guid PagamentoId);

// ---------- OS Service -> Execution Service (comandos) ----------
public record ServicoExecucao(Guid ServicoId, string Descricao, int Quantidade);

public record IniciarExecucao(Guid OrdemServicoId, string NumeroOs, IReadOnlyList<ServicoExecucao> Servicos);
public record CancelarExecucao(Guid OrdemServicoId, string Motivo);

// ---------- Execution Service -> OS Service (eventos) ----------
public record ExecucaoIniciada(Guid OrdemServicoId);
public record ExecucaoFinalizada(Guid OrdemServicoId, DateTime FinalizadaEm);
public record ExecucaoFalhou(Guid OrdemServicoId, string Motivo);

// ---------- Eventos internos do OS Service (disparam/alimentam a Saga; nao saem do servico) ----------
public record DiagnosticoConcluido(Guid OrdemServicoId);
public record OrcamentoAprovadoPeloCliente(Guid OrdemServicoId);
public record OrdemServicoCancelada(Guid OrdemServicoId, string Motivo);
