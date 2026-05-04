using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Clientes
{
    public class AtualizarClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<AtualizarClienteUseCase> _logger;

        public AtualizarClienteUseCase(IClienteRepository clienteRepository, ILogger<AtualizarClienteUseCase> logger)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<ClienteResponse> HandleAsync(AtualizarClienteRequest request, CancellationToken cancellationToken = default)
        {
            var cliente = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);
            if (cliente is null)
            {
                _logger.LogWarning("Cliente nao encontrado para atualizacao: {Id}", request.Id);
                throw new NotFoundException($"Cliente com Id {request.Id} não encontrado.");
            }

            // Atualiza documento se informado
            if (!string.IsNullOrWhiteSpace(request.Documento))
            {
                var comMesmoDoc = await _clienteRepository.GetByDocumentoAsync(request.Documento, cancellationToken);
                if (comMesmoDoc is not null && comMesmoDoc.Id != request.Id)
                    throw new ValidationException("Documento", "Já existe outro cliente com este documento.");

                try
                {
                    cliente.AtualizarDocumento(request.Documento);
                }
                catch (ArgumentException ex)
                {
                    throw new ValidationException("Documento", ex.Message);
                }
            }

            try
            {
                cliente.Atualizar(request.Nome, request.Email, request.Telefone);
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
                throw new ValidationException("Cliente", ex.Message);
            }

            await _clienteRepository.UpdateAsync(cliente, cancellationToken);
            _logger.LogInformation("Cliente atualizado: {Id}", request.Id);

            return CriarClienteUseCase.MapToResponse(cliente);
        }
    }
}
