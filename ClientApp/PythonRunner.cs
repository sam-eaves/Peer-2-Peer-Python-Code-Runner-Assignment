using System;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace ClientApp
{
    public class PythonRunner
    {
        //Handles python jobs
        public string ExecutePythonJob(string pythonCode)
        {
            ScriptEngine engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();

            try
            {
                // Capture the standard output of the Python job
                using (var outputStream = new System.IO.MemoryStream())
                {
                    engine.Runtime.IO.SetOutput(outputStream, System.Text.Encoding.UTF8);

                    // Execute the Python code
                    engine.Execute(pythonCode, scope);

                    // Get the output as a string
                    outputStream.Seek(0, System.IO.SeekOrigin.Begin);
                    using (var reader = new System.IO.StreamReader(outputStream))
                    {
                        var result = reader.ReadToEnd();
                        return !string.IsNullOrEmpty(result) ? result : "Job executed successfully, but no output was produced.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error executing job: {ex.Message}\nStack Trace: {ex.StackTrace}";
            }
        }
    }
}
