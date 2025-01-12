using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ClientApp
{
    //Handles everything networking
    public class Networking
    {
        private PythonRunner _runner;

        public Networking()
        {
            _runner = new PythonRunner();
        }

        //Registers client to server
        public async Task<int> RegisterClient(string port)
        {
            var client = new RestClient("http://localhost:5000");
            var request = new RestRequest("/api/client/register", Method.Post);
            request.AddJsonBody(new { Port = port });

            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("Client registered successfully.");
                var registeredClient = JsonConvert.DeserializeObject<Client>(response.Content);
                return registeredClient.Id;
            }
            else
            {
                Console.WriteLine("Failed to register client.");
                return -1;
            }
        }

        //Gets available client list from server
        public async Task<List<Client>> GetAvailableClients()
        {
            var client = new RestClient("http://localhost:5000");
            var request = new RestRequest("/api/client/getAll", Method.Get);
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<List<Client>>(response.Content);
            }
            else
            {
                Console.WriteLine("Failed to fetch client list.");
                return null;
            }
        }

        //Returns a list of jobs from a specific client
        public async Task<List<Job>> GetJobsFromClient(string clientIp, int port)
        {
            try
            {
                var address = new Uri($"net.tcp://{clientIp}:{port}/JobService");
                var binding = new NetTcpBinding();
                var channelFactory = new ChannelFactory<IJobService>(binding, new EndpointAddress(address));

                IJobService jobService = channelFactory.CreateChannel();
                return await Task.Run(() => jobService.GetAvailableJobs());
            }
            catch (EndpointNotFoundException ex)
            {
                Console.WriteLine($"Client at {clientIp}:{port} is not available. Removing from client list.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching jobs from peer: {ex.Message}");
                return null;
            }
        }

        //Returns a specific job from client
        public async Task<Job> DownloadJobFromClient(string clientIp, int port, int jobId)
        {
            try
            {
                var address = new Uri($"net.tcp://{clientIp}:{port}/JobService");
                var binding = new NetTcpBinding();
                var channelFactory = new ChannelFactory<IJobService>(binding, new EndpointAddress(address));

                IJobService jobService = channelFactory.CreateChannel();

                // Call the DownloadJob method to lock the job for execution
                return await Task.Run(() => jobService.DownloadJob(jobId));  // Lock the job and return it
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading job from peer: {ex.Message}");
                return null;
            }
        }

        //Submits the job result to original client
        public async Task<bool> SubmitJobResultToClient(string clientIp, int port, int jobId, string result)
        {
            try
            {
                var address = new Uri($"net.tcp://{clientIp}:{port}/JobService");
                var binding = new NetTcpBinding();
                var channelFactory = new ChannelFactory<IJobService>(binding, new EndpointAddress(address));

                IJobService jobService = channelFactory.CreateChannel();
                await Task.Run(() => jobService.SubmitJobResult(jobId, result));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting job result to peer: {ex.Message}");
                return false;
            }
        }


        //Updates client info, specifically jobs completed
        public async Task UpdateClientInfo(int clientId, int jobsCompleted)
        {
            try
            {
                var clientApi = new RestClient("http://localhost:5000");
                var request = new RestRequest($"/api/client/update/{clientId}", Method.Post);
                request.AddJsonBody(new { JobsCompleted = jobsCompleted });

                var response = await clientApi.ExecuteAsync(request);
                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"Failed to update client {clientId}. Response: {response.Content}");
                }
                else
                {
                    Console.WriteLine($"Successfully updated client {clientId}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating client info: {ex.Message}");
            }
        }


        //Gets up to date client info for current client
        public async Task<Client> GetClientInfo(int clientId)
        {
            try
            {
                var clientApi = new RestClient("http://localhost:5000");
                var request = new RestRequest($"/api/client/get/{clientId}", Method.Get);

                var response = await clientApi.ExecuteAsync<Client>(request);
                if (response.IsSuccessful)
                {
                    return JsonConvert.DeserializeObject<Client>(response.Content);
                }
                else
                {
                    Console.WriteLine($"Error fetching client info: {response.Content}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetClientInfo: {ex.Message}");
                return null;
            }
        }

        //Updates client status to server via polling
        public async Task SendTimePolling(int clientId)
        {
            var client = new RestClient("http://localhost:5000");
            var request = new RestRequest($"/api/client/status/{clientId}", Method.Post);

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                Console.WriteLine("Failed to send signal to the server.");
            }
        }

        //Redundant method purely for debugging purposes
        //Sends job to server into job table, allowing view of data
        public async Task<bool> SubmitJobToServer(Job job)
        {
            try
            {
                var client = new RestClient("http://localhost:5000");
                var request = new RestRequest("/api/job/create", Method.Post);

                // Pass the job object, it will automatically be serialized into JSON
                request.AddJsonBody(job);

                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    Console.WriteLine("Job successfully posted to server.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to post job to server. Response: {response.Content}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error posting job to server: {ex.Message}");
                return false;
            }
        }

        //Method called to explicitly deal with inactive clients
        //Used on login page open incase of user quitting and rejoining under same port
        public async Task CleanupInactiveClients()
        {
            try
            {
                var client = new RestClient("http://localhost:5000");  // Adjust URL and port if necessary
                var request = new RestRequest("/api/client/remove", Method.Delete);

                // Asynchronously execute the request
                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    Console.WriteLine("Inactive clients cleaned up successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to clean up inactive clients. Status: {response.StatusCode}, Message: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling cleanup: {ex.Message}");
            }
        }
    }
}

