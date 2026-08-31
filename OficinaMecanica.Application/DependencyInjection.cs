using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Application.UseCases.Autenticacao;
using OficinaMecanica.Application.UseCases.Clientes;
using OficinaMecanica.Application.UseCases.Pecas;
using OficinaMecanica.Application.UseCases.Servicos;
using OficinaMecanica.Application.UseCases.Veiculos;
using OficinaMecanica.Application.UseCases.OrdemServico;

namespace OficinaMecanica.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Autentica��o
            services.AddScoped<LoginUseCase>();
            services.AddScoped<RegistrarUsuarioUseCase>();

            // Ordens de Servi�o
            services.AddScoped<CriarOrdemServicoUseCase>();
            services.AddScoped<ObterOrdemServicoUseCase>();
            services.AddScoped<AdicionarServicoOrdemServicoUseCase>();
            services.AddScoped<AdicionarPecaOrdemServicoUseCase>();
            services.AddScoped<AcompanharOrdemServicoUseCase>();
            services.AddScoped<TempoMedioExecucaoUseCase>();
            services.AddScoped<RegistrarExecucaoServicoUseCase>();
            services.AddScoped<ConcluirDiagnosticoUseCase>();
            services.AddScoped<AprovarOrcamentoUseCase>();
            services.AddScoped<RegistrarEntregaUseCase>();
            services.AddScoped<CancelarOrdemServicoUseCase>();

            // Clientes
            services.AddScoped<CriarClienteUseCase>();
            services.AddScoped<ObterClienteUseCase>();
            services.AddScoped<AtualizarClienteUseCase>();
            services.AddScoped<AtualizarStatusClienteUseCase>();
            services.AddScoped<ExcluirClienteUseCase>();

            // Ve�culos
            services.AddScoped<CriarVeiculoUseCase>();
            services.AddScoped<ObterVeiculoUseCase>();
            services.AddScoped<AtualizarVeiculoUseCase>();
            services.AddScoped<ExcluirVeiculoUseCase>();

            // Servi�os
            services.AddScoped<CriarServicoUseCase>();
            services.AddScoped<ObterServicoUseCase>();
            services.AddScoped<AtualizarServicoUseCase>();
            services.AddScoped<ExcluirServicoUseCase>();

            // Pe�as
            services.AddScoped<CriarPecaUseCase>();
            services.AddScoped<ObterPecaUseCase>();
            services.AddScoped<AtualizarPecaUseCase>();
            services.AddScoped<ExcluirPecaUseCase>();
            services.AddScoped<MovimentarEstoqueUseCase>();

            return services;
        }
    }
}

