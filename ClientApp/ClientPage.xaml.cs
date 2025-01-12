using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace ClientApp
{
    public partial class ClientPage : Page
    {
        //Local client variable
        private Client _client;
        //Singletons of classes and GUI element declarations
        private Networking _network;
        private ServiceHost _serviceHost;
        private JobService _jobService;
        private PythonRunner _pythonRunner;
        //Booleans for runnings loops
        private bool _isNetworkingActive;
        private bool _isPollingActive;
        //Observables for updating GUI dynamically
        public ObservableCollection<Job> AvailableJobs;
        public ObservableCollection<Job> CompletedJobs;
        public ObservableCollection<Client> AvailableClients { get; set; }

        //Initialize everything here
        public ClientPage(Client client)
        {
            InitializeComponent();
            DataContext = this;

            AvailableJobs = new ObservableCollection<Job>();
            CompletedJobs = new ObservableCollection<Job>();
            AvailableClients = new ObservableCollection<Client>();
            JobsDataGrid.ItemsSource = AvailableJobs;
            CompletedJobsDataGrid.ItemsSource = CompletedJobs;

            _client = client;
            _network = new Networking();
            _isNetworkingActive = true;
            _isPollingActive = true;
            _jobService = new JobService();
            _pythonRunner = new PythonRunner();

            //_network.CleanupInactiveClients(); // Trigger client cleanup to ensure only active clients/jobs

            StartWcfServer(); //Starts the server
            StartNetworkingLoop(); //Starts networking loop to handle connections/jobs
            PollServer(); //Polls server to remain active
            StartUiUpdateThread(); //Starts dynamic UI updates

            this.Unloaded += ClientPage_Unloaded; //Handles freeing of resources on close
        }

        // Handles freeing resources from page
        private void ClientPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
                _isNetworkingActive = false;
                _isPollingActive = false;

                Console.WriteLine("Closing app and releasing resources.");
            }
        }

        // Starts Server Thread and registers client to web server
        // Didn't need to handle IP as unit coord stated in email
        private async Task StartWcfServer()
        {
            try
            {
                string port = _client.Port.ToString();
                var clientId = await _network.RegisterClient(port);
                _client.Id = clientId;
                UpdateClientInfo();

                Thread serverThread = new Thread(() =>
                {
                    // If we wanted to handle dynamic IP change localhost to actual IP user inputs
                    _serviceHost = new ServiceHost(typeof(JobService), new Uri($"net.tcp://localhost:{port}/JobService"));
                    _serviceHost.AddServiceEndpoint(typeof(IJobService), new NetTcpBinding(), "");
                    _serviceHost.Open();
                    Console.WriteLine($"WCF Server started at port {port}.");
                });

                serverThread.IsBackground = true;
                serverThread.Start();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to start WCF Server: " +  ex.Message);
            }
        }

        // Networking thread loop that continuously looks for new clients and jobs
        private void StartNetworkingLoop()
        {
            Task.Run(async () =>
            {
                while (_isNetworkingActive)
                {
                    try
                    {
                        // Fetch available clients
                        var clients = await _network.GetAvailableClients();

                        // Process jobs for each client (excluding self)
                        foreach (var client in clients)
                        {
                            if (client.Id == _client.Id)
                            {
                                continue;
                            }

                            // On connect retrieve clients jobs if any
                            var jobs = await _network.GetJobsFromClient(client.IpAddress, client.Port);

                            if (jobs != null && jobs.Count > 0)
                            {
                                foreach (var job in jobs)
                                {
                                    if (job.ClientId == _client.Id) // Skip own jobs
                                    {
                                        continue;
                                    }

                                    // Retrieves the actual job data
                                    var downloadedJob = await _network.DownloadJobFromClient(client.IpAddress, client.Port, job.JobId);

                                    // Verifies content hasn't been mishandled and does job
                                    // Increments completed counter and returns result back to original client
                                    if (downloadedJob != null && downloadedJob.JobHash == SecureMethods.HashData(downloadedJob.JobData))
                                    {

                                        var doJob = SecureMethods.DecodeBase64(downloadedJob.JobData);
                                        var result = _pythonRunner.ExecutePythonJob(doJob);

                                        downloadedJob.Status = "Completed";
                                        downloadedJob.Result = result;

                                        _client.JobsCompleted++;
                                        await _network.UpdateClientInfo(_client.Id, _client.JobsCompleted);

                                        // Send the result back to the originating client
                                        var returnClient = await _network.GetClientInfo(downloadedJob.ClientId);
                                        await _network.SubmitJobResultToClient(returnClient.IpAddress, returnClient.Port, downloadedJob.JobId, result);
                                    }
                                }
                            }
                        }

                        // Delay to allow new jobs to be posted and then do the loop again
                        await Task.Delay(10000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in networking loop: {ex.Message}");
                    }
                }
            });
        }

        //Updates GUI periodically
        //Uses application invoke so it's done on GUI thread
        private void StartUiUpdateThread()
        {
            Task.Run(async () =>
            {
                while (_isNetworkingActive)
                {
                    try
                    {
                        // Update Client Info (Jobs Completed) UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            JobsCompletedTextBlock.Text = $"Jobs Completed: {_client.JobsCompleted}";  // Update JobsCompletedTextBlock with the current count
                        });

                        // Update Available Clients UI
                        var clients = await _network.GetAvailableClients();

                        // Update Available Clients UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AvailableClients.Clear();  // Clear existing clients
                            foreach (var client in clients)
                            {
                                AvailableClients.Add(client);  // Add new clients to ObservableCollection
                            }
                        });

                        // Update Available Jobs UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AvailableJobs.Clear();
                            foreach (var job in _jobService.GetAvailableJobs())
                            {
                                AvailableJobs.Add(job);
                            }
                        });

                        // Update Completed Jobs UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CompletedJobs.Clear();
                            foreach (var job in _jobService.GetCompletedJobs())
                            {
                                CompletedJobs.Add(job);
                            }
                        });

                        // Wait for 5 seconds before updating again
                        await Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in UI update loop: {ex.Message}");
                    }
                }
            });
        }

        //Polls server to stay active
        private async Task PollServer()
        {
            while (true)
            {
                try
                {
                    // Send a "keep-alive" ping to the server
                    await _network.SendTimePolling(_client.Id);

                    // Wait for the polling interval
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error polling server: {ex.Message}");
                }
            }
        }


        // Submit job from whats in the text box
        private async void SubmitJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pythonCode = PythonCodeTextBox.Text;

                if (!string.IsNullOrEmpty(pythonCode))
                {
                    string encodedCode = SecureMethods.EncodeBase64(pythonCode);
                    string codeHash = SecureMethods.HashData(encodedCode);

                    // Create a new job
                    var newJob = new Job
                    {
                        JobId = GetNextJobId(),
                        JobData = encodedCode,
                        JobHash = codeHash,
                        Status = "Pending",
                        ClientId = _client.Id
                    };

                    _jobService.AddJob(newJob);  // Add job to JobService
                    await _network.SubmitJobToServer(newJob); //Test call to check database ensuring jobs were done right

                    // Update available jobs in the UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableJobs.Add(newJob);  // Add to AvailableJobs ObservableCollection
                    });

                    StatusTextBlock.Text = "Job successfully submitted.";
                }
                else
                {
                    StatusTextBlock.Text = "Please enter Python code to submit.";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error submitting job: {ex.Message}";
            }
        }


        //Helper for SubmitJob to set "dynamic" id's
        private int GetNextJobId()
        {
            return _jobService.GetAvailableJobs().Count + 1;
        }

        //Allows uploading of files into text box
        //Open file dialog adapted from https://www.c-sharpcorner.com/UploadFile/mahesh/openfiledialog-in-C-Sharp/
        private async void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            // Create an OpenFileDialog to allow the user to select a Python file
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Python Files (*.py)|*.py|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Read the contents of the file and display it in the PythonCodeTextBox
                    string fileContent = await ReadFileAsync(openFileDialog.FileName);
                    PythonCodeTextBox.Text = fileContent;
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"Error loading file: {ex.Message}";
                }
            }
        }

        //Silly helper to keep async handling
        private async Task<string> ReadFileAsync(string fileName)
        {
            return await Task.Run(() => File.ReadAllText(fileName));
        }

        //Helper update method for setting client data
        private void UpdateClientInfo()
        {
            ClientIdTextBlock.Text = $"Client ID: {_client.Id}";
            ClientPortTextBlock.Text = $"Client Port: {_client.Port}";
            JobsCompletedTextBlock.Text = $"Jobs Completed: {_client.JobsCompleted}";
        }
    }
}

