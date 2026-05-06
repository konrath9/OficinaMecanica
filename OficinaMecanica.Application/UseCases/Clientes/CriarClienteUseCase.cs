using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.Clientes
{
    public class CriarClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<CriarClienteUseCase> _logger;

        public CriarClienteUseCase(IClienteRepository clienteRepository, ILogger<CriarClienteUseCase> logger)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<ClienteResponse> HandleAsync(CriarClienteRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Criando cliente: {Nome}", request.Nome);

            // Valida o documento antes de consultar o repositório
            Cliente cliente;
            try
            {
                cliente = new Cliente(request.Nome, request.Documento, request.Email, request.Telefone);
            }
            catch (ArgumentException ex) when (ex.ParamName == "email")
            {
                throw new ValidationException("Email", ex.Message);
            }
            catch (ArgumentException ex) when (ex.ParamName == "telefone")
            {
                throw new ValidationException("Telefone", ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Documento", ex.Message);
            }

            var existente = await _clienteRepository.GetByDocumentoAsync(request.Documento, cancellationToken);
            if (existente is not null)
                throw new ValidationException("Documento", "Já existe um cliente com este documento.");

            var criado = await _clienteRepository.AddAsync(cliente, cancellationToken);
            _logger.LogInformation("Cliente criado com Id: {Id}", criado.Id);

            return MapToResponse(criado);
        }

        internal static ClienteResponse MapToResponse(Cliente c) => new()
        {
            Id = c.Id,
            Nome = c.Nome,
            Documento = c.Documento,
            DocumentoFormatado = c.DocumentoFormatado,
            TipoDocumento = c.TipoDocumento.ToString(),
            Email = c.Email,
            Telefone = c.Telefone,
            CriadoEm = c.CreatedAt,
            AtualizadoEm = c.UpdatedAt
        };
    }
}
