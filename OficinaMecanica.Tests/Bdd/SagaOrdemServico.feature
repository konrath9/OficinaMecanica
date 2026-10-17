# language: pt
Funcionalidade: Saga da Ordem de Serviço entre os microsserviços
  Como oficina com várias filiais
  Quero que a OS percorra orçamento, pagamento e execução em serviços independentes
  Para que uma falha em qualquer etapa seja compensada sem deixar dados inconsistentes

  Contexto:
    Dado uma OS com um serviço e uma peça em diagnóstico

  Cenário: Fluxo completo da OS até a entrega
    Quando o técnico conclui o diagnóstico
    E o Billing Service gera o orçamento
    Então a OS deve estar "AguardandoAprovacao"
    Quando o cliente aprova o orçamento
    Então a OS deve estar "AguardandoPagamento"
    Quando o Mercado Pago confirma o pagamento
    Então a OS deve estar "EmExecucao"
    Quando o Execution Service finaliza a execução
    Então a OS deve estar "Finalizada"
    Quando a recepção registra a entrega
    Então a OS deve estar "Entregue"

  Cenário: Pagamento recusado cancela a OS (compensação)
    Quando o técnico conclui o diagnóstico
    E o Billing Service gera o orçamento
    E o cliente aprova o orçamento
    E o Mercado Pago recusa o pagamento
    Então a OS deve estar "Cancelada"

  Cenário: Falha na execução estorna o pagamento e cancela a OS (compensação)
    Quando o técnico conclui o diagnóstico
    E o Billing Service gera o orçamento
    E o cliente aprova o orçamento
    E o Mercado Pago confirma o pagamento
    Então a OS deve estar "EmExecucao"
    Quando o Execution Service reporta falha na execução
    E o Billing Service confirma o estorno
    Então a OS deve estar "Cancelada"

  Cenário: Falha ao gerar orçamento mantém a OS em diagnóstico (compensação)
    Quando o técnico conclui o diagnóstico
    E o Billing Service falha ao gerar o orçamento
    Então o histórico da OS deve registrar a falha do orçamento
    E a OS deve estar "EmDiagnostico"
