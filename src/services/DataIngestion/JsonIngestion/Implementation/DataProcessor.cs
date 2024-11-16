using JsonIngestion.DataModels;

namespace JsonIngestion.Implementation;

// TODO Data Processing
public class DataProcessor(ILogger<DataProcessor> logger)
{
    public void EnqueueData(InputDataDto data)
    {
        logger.LogInformation($"Data is enqueued {data}");
    }
}
