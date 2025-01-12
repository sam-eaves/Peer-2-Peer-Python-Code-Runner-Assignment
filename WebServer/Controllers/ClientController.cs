using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebServer.Data;
using WebServer.Models; // Adjust to your actual namespace

namespace WebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly DBManager _dbManager;

        public ClientController(DBManager dbManager)
        {
            _dbManager = dbManager;
        }

        // POST: api/clients/register
        //Registers client into database
        [HttpPost("register")]
        public async Task<IActionResult> RegisterClient([FromBody] Client client)
        {
            // Input validation
            if (client == null || string.IsNullOrEmpty(client.IpAddress) || client.Port <= 0)
            {
                return BadRequest("Invalid client data.");
            }

            try
            {
                client.LastSend = DateTime.Now; //Updates their polling on register

                _dbManager.Clients.Add(client);
                await _dbManager.SaveChangesAsync();
                return Ok(client);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors: inner exception shows the actual problem
                return StatusCode(500, $"An error occurred while saving the client to the database: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return StatusCode(500, $"An unexpected error occurred during client register: {ex.Message}");
            }
        }

        // GET: client/clients
        // Retrieves all clients in database
        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients()
        {
            try
            {
                var clients = await _dbManager.Clients.ToListAsync();
                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving clients: {ex.Message}");
            }
        }

        //GET: api/client/{clientId}
        // Retrieves client by first matching id
        [HttpGet("get/{clientId}")]
        public IActionResult GetClientById(int clientId)
        {
            try
            {
                var client = _dbManager.Clients.FirstOrDefault(c => c.Id == clientId);
                if (client != null)
                {
                    return Ok(client);
                }
                return NotFound($"Client with ID {clientId} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        //POST: api/client/update/{clientId}
        //Updates client info based on id
        [HttpPost("update/{clientId}")]
        public IActionResult UpdateClientInfo(int clientId, [FromBody] Client updatedClient)
        {
            try
            {
                var client = _dbManager.Clients.FirstOrDefault(c => c.Id == clientId);
                if (client == null)
                {
                    return NotFound($"Client with ID {clientId} not found.");
                }

                // Update the jobs completed count
                client.JobsCompleted = updatedClient.JobsCompleted;
                _dbManager.SaveChanges();

                return Ok($"Client {clientId} successfully updated.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating client: {ex.Message}");
            }
        }

        //POST: client/status/{clientId}
        // Receives polls from clients storing in databse
        [HttpPost("status/{clientId}")]
        public IActionResult ReceiveTimePolling(int clientId)
        {
            try
            {
                var client = _dbManager.Clients.FirstOrDefault(c => c.Id == clientId);
                if (client != null)
                {
                    client.LastSend = DateTime.Now;
                    _dbManager.SaveChanges();
                    return Ok($"Client poll received for client: {clientId}");
                }
                return NotFound("Client not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating client status: {ex.Message}");
            }
        }

        //Removes clients based off of polling
        [HttpDelete("remove")]
        public IActionResult RemoveInactiveClients()
        {
            try
            {
                // Find clients that have been inactive for more than 1 minute via LastSend time
                // Changed to seconds to demonstrate
                var inactiveClients = _dbManager.Clients
                    .Where(c => c.LastSend < DateTime.Now.AddSeconds(-30))
                    .ToList();

                if (inactiveClients.Any())
                {
                    foreach (var client in inactiveClients)
                    {
                        // Find jobs associated with this client
                        var clientJobs = _dbManager.Jobs
                            .Where(j => j.ClientId == client.Id)
                            .ToList();

                        // Remove jobs associated with the client
                        if (clientJobs.Any())
                        {
                            _dbManager.Jobs.RemoveRange(clientJobs);
                        }

                        // Now remove the client
                        _dbManager.Clients.Remove(client);
                    }

                    // Save changes to remove jobs and clients
                    _dbManager.SaveChanges();
                }

                //return Ok($"{inactiveClients.Count} inactive clients removed.");
                return Ok(new { message = $"{inactiveClients.Count} inactive clients removed." });
            }
            catch (DbUpdateException dbEx)
            {
                var errorMessage = $"Error removing inactive clients: {dbEx.InnerException?.Message ?? dbEx.Message}";
                Console.WriteLine(errorMessage);  // Log the error to the console
                return StatusCode(500, errorMessage);  // Send the error to the client
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
