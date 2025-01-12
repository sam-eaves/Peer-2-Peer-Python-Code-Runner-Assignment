using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ClientApp
{
    //Interface for Job Service
    [ServiceContract]
    public interface IJobService
    {
        [OperationContract]
        List<Job> GetAvailableJobs();  // Allows other clients to see available jobs

        [OperationContract]
        Job DownloadJob(int jobId);  // Allows clients to download a specific job

        [OperationContract]
        void SubmitJobResult(int jobId, string result);  // Allows clients to submit job results
    }
}
