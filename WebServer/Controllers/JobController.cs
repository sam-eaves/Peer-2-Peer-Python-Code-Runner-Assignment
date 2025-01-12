using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Security.Cryptography;
using WebServer.Data;
using WebServer.Models;

/*
 * Redundant controller purely for testing
 * Used DB Browser for SQLite to ensure job objects were being passed correctly
 * and handled correctly, kept for testing
 */

namespace WebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : Controller
    {
        private readonly DBManager _dbManager;


        public JobController(DBManager dbManager)
        {
            _dbManager = dbManager;
        }

        //POST: api/job/create
        [HttpPost("create")]
        public IActionResult PostJob([FromBody] JobRequest jobRequest)
        {
            try
            {
                if (jobRequest == null || string.IsNullOrEmpty(jobRequest.JobData) || string.IsNullOrEmpty(jobRequest.JobHash))
                {
                    return BadRequest("Invalid job data.");
                }

                // Find the client based on ClientId
                var client = _dbManager.Clients.FirstOrDefault(c => c.Id == jobRequest.ClientId);
                if (client == null)
                {
                    return NotFound("Client not found.");
                }

                // Hash the Base64 encoded data
                var computedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(jobRequest.JobData));

                // Convert to make comparing easier
                var computedHashString = BitConverter.ToString(computedHashBytes).Replace("-", "").ToLower();

                // Compare hash with received hash
                if (computedHashString != jobRequest.JobHash.ToLower())
                {
                    return BadRequest("Hash mismatch. Job data might have been tampered with.");
                }

                // Create Job object
                var job = new Job
                {
                    JobData = jobRequest.JobData,
                    JobHash = jobRequest.JobHash,
                    Status = jobRequest.Status,
                    Client = client,
                    Result = ""
                };

                _dbManager.Jobs.Add(job);
                _dbManager.SaveChanges();

                return Ok(new { message = "Job successfully posted.", job.JobId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                return StatusCode(500, "An error occurred while processing the PostJob request.");
            }
        }

        //GET: api/job/available
        [HttpGet("available")]
        public IActionResult GetAvailableJobs()
        {
            try
            {
                var availableJobs = _dbManager.Jobs
                    .Where(j => j.Status == "Pending")
                    .ToList();

                return Ok(availableJobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving available jobs: {ex.Message}");
            }
        }

        //GET: api/job/{jobId}
        [HttpGet("get/{jobId}")]
        public IActionResult GetJobById(int jobId)
        {
            try
            {
                var job = _dbManager.Jobs.FirstOrDefault(j => j.JobId == jobId);
                if (job == null)
                {
                    return NotFound("Job not found.");
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving job by id {jobId}: {ex.Message}");
            }
        }


        // POST: api/job/update/{jobId}
        [HttpPost("update/{jobId}")]
        public IActionResult UpdateJobStatus(int jobId, [FromBody] JobStatus statusUpdate)
        {
            try
            {
                var job = _dbManager.Jobs.FirstOrDefault(j => j.JobId == jobId);
                if (job == null)
                {
                    return NotFound("Job not found.");
                }

                // Update job status and result if provided
                job.Status = statusUpdate.Status;
                job.Result = statusUpdate.Result ?? ""; // Use empty string if result is null

                _dbManager.SaveChanges();
                return Ok(new { message = $"Job {jobId} status updated to {statusUpdate.Status}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating job {jobId}: {ex.Message}");
            }
        }



        //POST api/job/complete/{jobId}
        [HttpPost("complete/{jobId}")]
        public IActionResult CompleteJob(int jobId, [FromBody] Job jobUpdate)
        {
            try
            {
                var job = _dbManager.Jobs.FirstOrDefault(j => j.JobId == jobId);
                if (job == null)
                {
                    return NotFound("Job not found.");
                }

                job.Status = "Completed";
                job.Result = jobUpdate.Result;

                // Increment the client's JobsCompleted count
                var client = _dbManager.Clients.FirstOrDefault(c => c.Id == job.ClientId);
                if (client != null)
                {
                    client.JobsCompleted += 1;
                }

                _dbManager.SaveChanges();
                return Ok(new { message = "Job successfully completed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while completing the job: {ex.Message}");
            }
        }
    }
}
