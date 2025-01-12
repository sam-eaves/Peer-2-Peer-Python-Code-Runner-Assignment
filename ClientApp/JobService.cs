using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    public class JobService : IJobService
    {
        // Store the list of jobs for the current client both to be done and completed
        private static List<Job> _localJobs = new List<Job>();
        private static List<Job> _completedJobs = new List<Job>();

        //Simple index we can use to increment a clients own jobid
        private static int _nextJobId = 1;

        public List<Job> GetAvailableJobs()
        {
            return _localJobs.Where(j => j.Status == "Pending").ToList();
        }

        public List<Job> GetCompletedJobs()
        {
            return _completedJobs.ToList();
        }

        //Retrieves the first job that matches the specified job ID and is pending to be done
        public Job DownloadJob(int jobId)
        {
            var job = _localJobs.FirstOrDefault(j => j.JobId == jobId && j.Status == "Pending");

            if (job != null)
            {
                job.Status = "InProgress";  // Lock the job for execution
                Console.WriteLine($"Job {jobId} marked as InProgress.");
                return job;
            }
            return null;
        }

        //Sets job as complete moving from available to completed
        public void SubmitJobResult(int jobId, string result)
        {
            var job = _localJobs.FirstOrDefault(j => j.JobId == jobId);
            if (job != null)
            {
                // Check if the result contains an error message
                if (result.StartsWith("Error executing job:"))
                {
                    job.Status = "Failed";  // Mark job as failed if result indicates an error
                }
                else
                {
                    job.Status = "Completed";  // Mark completed if fine
                }

                job.Result = result;
                _localJobs.Remove(job);  // Remove from pending jobs
                _completedJobs.Add(job);  // Add to completed jobs

                // Log the completion or failure
                Console.WriteLine($"Job {jobId} {job.Status} with result: {result}");
            }
        }

        //Adds jobs to list and increments jobid counter
        public void AddJob(Job job)
        {
            job.JobId = _nextJobId++;
            _localJobs.Add(job);
            Console.WriteLine($"Job {job.JobId} added for client.");
        }
    }
}
